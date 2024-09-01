using Followergir.Controllers;
using Followergir.IO;
using Followergir.IONet;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Library.Json;
using Library.SQL;
using Library;
using System.Reflection.Metadata;

namespace Followergir {

	public class UserConfig {

		public static readonly long MY_USER_ID = 0L;

		private static readonly byte[] AES_KEY_ORDER_HASH = new byte[16] {
			0x03, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01
		};

		public static readonly IOApi.Config config = new IOApi.Config();

		public static List<IOApi.Message> messageFa, messageEn;
		public static Dictionary<string, string> systemMessage = new Dictionary<string, string>();

		private static readonly ConcurrentDictionary<Int64, UserInfo> listConnection = new ConcurrentDictionary<Int64, UserInfo>();
		private static readonly ThreadLock<long> threadLockConnection = new ThreadLock<long>();
		private static readonly int MAX_CONNECTION = 20;



		private static readonly Dictionary<short, short> listTooManyRequest = new Dictionary<short, short>() {
			{ Error.CAN_NOT_DESERIALIZE,      5 },
			{ Error.FUNCTION_NOT_FOUND,       5 },
			{ Error.INVALID_PARAMETERS,       5 },

			{ Error.UNAUTHORIZED,             5 },
			//{ IOError.AUTHORIZE_HASH_EMPTY,     5 },
			//{ IOError.AUTHORIZE_HASH_INVALID,   5 },
			{ Error.PHONE_NUMBER_INVALID,     10 },
			{ Error.EMAIL_ADDRESS_INVALID,    10 },
			{ Error.CODE_INVALID,             5 },
			{ Error.CODE_EMPTY,               8 },
			{ Error.PASSWORD_NOT_MATCH,       8 },

			{ Error.USER_ID_INVALID,          10 },
			{ Error.USER_NOT_FOUND,           5 },

			{ Error.CAN_NOT_START_ORDER,      5 },
			{ Error.CAN_NOT_CANCEL_ORDER,     8 },
			{ Error.ORDER_ID_INVALID,         5 },
			{ Error.ORDER_NOT_FOUND,          20 },
			{ Error.ORDER_HAS_VIEWED,         10 },
			{ Error.CREDIT_LIMIT,             10 },

			{ Error.PURCHASE_FAILED,          4 },
			{ Error.PURCHASE_HAS_USED,         4 },

			// { IOError.MESSAGE_TYPE_INVALID,     2 },
			{ Error.TYPE_INVALID,        2 },
			{ Error.ACTION_NO_RESPONSE,       5 },

			{ Error.TRANSFER_USER_NOT_FOUND,  8 },


		};

		static UserConfig() {

			config.rate = new IOApi.RateConfig();
			config.rate.user = 1.00F;
			config.rate.like = 0.8F;
			config.rate.comment = 0.5F;

			config.add = new IOApi.AddConfig();
			config.add.lengthList = 3;

			config.order = new IOApi.OrderConfig();
			config.order.minUser = 25;
			config.order.maxUser = 10000;
			config.order.minLike = 25;
			config.order.maxLike = 10000;
			config.order.minComment = 10;
			config.order.maxComment = 1000;

			config.orderInfo = new IOApi.OrderInfoConfig();
			config.orderInfo.cancelRate = 0.15;
		}

		public static async Task CheckUnFollowAndUnGem(long userId, string language) {

			try {
				gRPC_Followergir.UnCreditReq reqUnCredit = new gRPC_Followergir.UnCreditReq();
				reqUnCredit.UserId = userId;
				gRPC_Followergir.UnCreditRes resUnCredit = await IONet.GRPC.GetFollowergir().UnCreditAsync(reqUnCredit);
				if (resUnCredit.CoinBack == 0 && resUnCredit.CoinUnFollow == 0) {
					return;
				}

				double effectedCoin = resUnCredit.CoinBack - resUnCredit.CoinUnFollow;
				IOApi.UpdateCoin updateCoin = new IOApi.UpdateCoin();
				if (effectedCoin > 0) {
					gRPC_Member.IncrementCoinReq reqIncrementCredit = new gRPC_Member.IncrementCoinReq();
					reqIncrementCredit.UserId = userId;
					reqIncrementCredit.Count = effectedCoin;
					gRPC_Member.IncrementCoinRes resIncrementCredit = await IONet.GRPC.GetMember().IncrementCoinAsync(reqIncrementCredit);
					updateCoin.coin = resIncrementCredit.Coin;
					updateCoin.unixTime = resIncrementCredit.UnixTime;

				} else if (effectedCoin < 0) {
					gRPC_Member.DecrementCoinReq reqDecreaseCredit = new gRPC_Member.DecrementCoinReq();
					reqDecreaseCredit.UserId = userId;
					reqDecreaseCredit.Count = Math.Abs(effectedCoin);
					reqDecreaseCredit.AllowNegative = true;
					gRPC_Member.DecrementCoinRes resDecrementCredit = await IONet.GRPC.GetMember().DecrementCoinAsync(reqDecreaseCredit);
					updateCoin.coin = resDecrementCredit.Coin;
					updateCoin.unixTime = resDecrementCredit.UnixTime;
				}


				IOApi.UpdateNewMessage updateNewMessageUnFollow = null;
				if (resUnCredit.CoinUnFollow > 0) {
					updateNewMessageUnFollow = new IOApi.UpdateNewMessage();
					updateNewMessageUnFollow.message = new IOApi.Message();
					updateNewMessageUnFollow.message.id = "UnFollow";
					if (language == "fa") {
						updateNewMessageUnFollow.message.text = String.Format(systemMessage["UnFollow_Fa"], resUnCredit.CoinUnFollow);
					} else {
						updateNewMessageUnFollow.message.text = String.Format(systemMessage["UnFollow_En"], resUnCredit.CoinUnFollow);
					}
					updateNewMessageUnFollow.unixTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				}

				IOApi.UpdateNewMessage updateNewMessageBack = null;
				if (resUnCredit.CoinBack > 0) {
					updateNewMessageBack = new IOApi.UpdateNewMessage();
					updateNewMessageBack.message = new IOApi.Message();
					updateNewMessageBack.message.id = "UnCredit";
					if (language == "fa") {
						updateNewMessageBack.message.text = String.Format(systemMessage["UnCredit_Fa"], resUnCredit.CoinBack);
					} else {
						updateNewMessageBack.message.text = String.Format(systemMessage["UnCredit_En"], resUnCredit.CoinBack);
					}
					updateNewMessageBack.unixTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				}

				SendUpdateToGroup(userId, null, updateCoin, updateNewMessageUnFollow, updateNewMessageBack);

			} catch (Exception e) {
				WebSocket_Log.Send("CheckUnFollowAndUnGem", e.ToString());
			}
		}




		public static async Task AddTooManyRequestAsync(long userId, short error) {
			await AddTooManyRequestAsync(userId, error, 1800D);
		}



		public static async Task AddTooManyRequestAsync(long userId, short error, double time) {
			/*IDatabase redisBlock = Redis.GetDatabaseWebServerTooManyRequest();
			RedisValue redisValue = await redisBlock.StringGetAsync("Id:" + userId + "_" + error);
			redisValue = redisValue.IsNullOrEmpty ? 1 : ((short) redisValue) + 1;
			await redisBlock.StringSetAsync("Id:" + userId + "_" + error, redisValue, TimeSpan.FromSeconds(time));
			Log.SendEmail("AddTooManyRequest", "Id: " + userId + "\r\n" + "Error: " + error);*/
		}

		public static async Task AddTooManyRequestAsync(string ip, short error) {
			await AddTooManyRequestAsync(ip, error, 1800D);
		}

		public static async Task AddTooManyRequestAsync(string ip, short error, double time) {
			/*IDatabase redisBlock = Redis.GetDatabaseWebServerTooManyRequest();
			RedisValue redisValue = await redisBlock.StringGetAsync("Ip:" + ip + "_" + error);
			redisValue = redisValue.IsNullOrEmpty ? 1 : ((short) redisValue) + 1;
			await redisBlock.StringSetAsync("Ip:" + ip + "_" + error, redisValue, TimeSpan.FromSeconds(time));
			Log.SendEmail("AddTooManyRequest", "Ip:" + ip + "\r\n" + "Error: " + error);*/
		}

		public static async Task<IOApi.Error> CheckTooManyRequestAsync(long userId, params short[] errors) {
			/*IDatabase redisBlock = Redis.GetDatabaseWebServerTooManyRequest();
			foreach (short error in errors) {
				RedisValue redisValue = await redisBlock.StringGetAsync("Id:" + userId + "_" + error);
				if (redisValue.IsNullOrEmpty) {
					return null;
				}
				TimeSpan? timeSpan = await redisBlock.KeyTimeToLiveAsync("Id:" + userId + "_" + error);
				if (timeSpan.HasValue) {
					return null;
				}

				try {
					if ((short) redisValue >= listTooManyRequest[error]) {
						if (timeSpan.Value.TotalSeconds == -2 || timeSpan.Value.TotalSeconds == 0) {
							return null;
						} else if (timeSpan.Value.TotalSeconds > 0) {
							return new IOApi.Error("TOO_MANY_REQUESTS_" + ((int) timeSpan.Value.TotalSeconds));
						}
					}
				} catch (KeyNotFoundException) {
					Log.SendEmail("Check Too Many Request KeyNotFoundException", "UserId: " + userId + "\r\n" + "Error: " + error + "\r\n" + "TotalSeconds: " + timeSpan.Value.TotalSeconds);
				}
			}*/
			return null;
		}

		public static async Task<IOApi.Error> CheckTooManyRequestAsync(string ip, params short[] errors) {
			/*IDatabase redisBlock = Redis.GetDatabaseWebServerTooManyRequest();
			foreach (short error in errors) {
				RedisValue redisValue = await redisBlock.StringGetAsync("Ip:" + ip + "_" + error);
				if (redisValue.IsNullOrEmpty) {
					return null;
				}

				TimeSpan? timeSpan = await redisBlock.KeyTimeToLiveAsync("Ip:" + ip + "_" + error);
				if (timeSpan.HasValue) {
					return null;
				}
				try {
					if ((short) redisValue >= listTooManyRequest[error]) {
						if (timeSpan.Value.TotalSeconds == -2 || timeSpan.Value.TotalSeconds == 0) {
							return null;
						} else if (timeSpan.Value.TotalSeconds > 0) {
							return new IOApi.Error("TOO_MANY_REQUESTS_" + ((int) timeSpan.Value.TotalSeconds));
						}
					}
				} catch (KeyNotFoundException) {
					Log.SendEmail("Check Too Many Request KeyNotFoundException", "IP: " + ip + "\r\n" + "Error: " + error + "\r\n" + "TotalSeconds: " + timeSpan.Value.TotalSeconds);
				}
			}*/
			return null;
		}

		public static string EncryptOrderHash(long ordererId, long userId) {
			JSONObject json = new JSONObject();
			json.Add("UserId", userId);
			json.Add("OrdererId", ordererId);
			json.Add("UnixTime", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
			return StringUtils.RandomString(8) + AESCrypt.Encrypt(json.ToString(), AES_KEY_ORDER_HASH);
		}

		public static JSONObject DecryptOrderHash(string hash) {
			hash = hash.Substring(8);
			hash = AESCrypt.Decrypt(hash, AES_KEY_ORDER_HASH);
			return new JSONObject(hash);
		}


		/*
		public static async Task AddToGroupAsync(WebSoketConnection websocketClass) {
			using (await threadLockGroup.LockAsync(websocketClass.user.id)) {
				List<WebSoketConnection> list = listConnection.GetOrAdd(websocketClass.user.id, new Func<long, List<WebSoketConnection>>(delegate (long userId) {
					return new List<WebSoketConnection>();
				}));
				list.Add(websocketClass);
				Console.WriteLine("Add to List: " + websocketClass.user.connectionId + "\r\n");
			}
		}


		public static async Task RemoveFromGroupAsync(WebSoketConnection websocketClass) {
			using (await threadLockGroup.LockAsync(websocketClass.user.id)) {
				List<WebSoketConnection> list = listConnection.GetOrAdd(websocketClass.user.id, new Func<long, List<WebSoketConnection>>(delegate (long userId) {
					return new List<WebSoketConnection>();
				}));
				list.Remove(websocketClass);
				if (list.Count == 0) {
					UserConfig.listConnection.TryRemove(websocketClass.user.id, out _);
				}
				Console.WriteLine("Remove from List: " + websocketClass.user.connectionId + "\r\n");
			}
		}
		*/

		public static void SendUpdateToGroup(long userId, string exceptConnectionId, params IOApi.Update[] updates) {

			Nats.connectionUpdate.Publish(userId + "", exceptConnectionId, SerializedData.SerializeUpdate(updates));

			/*
			List<WebSoketConnection> list = listConnection.GetValueOrDefault(userId);
			Console.WriteLine("list.Count " + list.Count);

			if (list == null) {
				return;
			}
			WebSoketConnection[] copy = null;
			int c = 0;
			while (true) {
				copy = new WebSoketConnection[list.Count];

				try {
					list.CopyTo(copy, 0);
					break;
				} catch (Exception) {
					c += 1;
					//Email
					if (c > 3) {
						return;
					}
				}
			}
			
			for (int i = 0; i < copy.Length; i++) {
				// list.ForEach((WebSoketConnection webSocket) => {
				// await Task.Delay(5000);
				// Thread.Sleep(5000);
				Console.WriteLine("update i: " + i);

				try {
					WebSoketConnection webSocket = copy[i];
					Console.WriteLine("UPDATE - user: " + webSocket.user.connectionId);


					if (webSocket.user.connectionId != exceptConnectionId) {
						webSocket.SendUpdate(updates);
						Console.WriteLine("UPDATE Send: " + updates);
					}
				} catch (Exception e) {
					Console.WriteLine("UPDATE Exception: " + e);
				}
			}*/
		}

		public static void SendUpdateAll(IOApi.Update update) {
		}

		public static void SendAction(long userId, IOObject action) {
		}


		public static async Task<bool> SetData(long id, long userId, string data) {

			string auth = "";
			string sessionId = "";
			string mid = "";
			string csrftoken = "";
			string username = "";
			string userAgent = "";
			//Console.WriteLine("data : " + data);
			try {
				JSONObject json = new JSONObject(data);
				if (!json.IsNull("A")) {
					auth = json.GetString("A");

				} else if (!json.IsNull("S")) {
					sessionId = json.GetString("S");
					csrftoken = json.GetString("C");
				} else {
					throw new Exception();
				}
				username = json.GetString("U");
				mid = json.GetString("M");
				userAgent = json.GetString("UAG");

			} catch (Exception) {
				return false;
			}

			Database database = new Database(ServerConfig.DATABASE);
			database.DisableClose();

			database.Prepare("UPDATE Data SET Username = @Username, Auth = @Auth, Session = @Session, Csrftoken = @Csrftoken, Mid = @Mid, UserAgent = @UserAgent, Date = GETUTCDATE() WHERE UserId = @UserId AND Id = @Id;");
			database.BindValue("@Username", username, SqlDbType.VarChar);
			database.BindValue("@Auth", auth, SqlDbType.VarChar);
			database.BindValue("@Session", sessionId, SqlDbType.VarChar);
			database.BindValue("@Csrftoken", csrftoken, SqlDbType.VarChar);
			database.BindValue("@Mid", mid, SqlDbType.VarChar);
			database.BindValue("@UserAgent", userAgent, SqlDbType.VarChar);
			database.BindValue("@UserId", userId, SqlDbType.BigInt);
			database.BindValue("@Id", id, SqlDbType.BigInt);
			int changedRow = await database.ExecuteNonQueryAsync();

			if (changedRow == 0) {
				database.Prepare("INSERT INTO Data (Id, UserId, Username, Auth, Session, Csrftoken, Mid, UserAgent) VALUES (@Id, @UserId, @Username, @Auth, @Session, @Csrftoken, @Mid, @UserAgent);");
				database.BindValue("@Username", username, SqlDbType.VarChar);
				database.BindValue("@Auth", auth, SqlDbType.VarChar);
				database.BindValue("@Session", sessionId, SqlDbType.VarChar);
				database.BindValue("@Csrftoken", csrftoken, SqlDbType.VarChar);
				database.BindValue("@Mid", mid, SqlDbType.VarChar);
				database.BindValue("@UserAgent", userAgent, SqlDbType.VarChar);
				database.BindValue("@UserId", userId, SqlDbType.BigInt);
				database.BindValue("@Id", id, SqlDbType.BigInt);
				await database.ExecuteInsertAsync();
			}
			database.Close();
			await UserConfig.NeedData(id, userId);
			return true;
		}

		public static async Task<bool> NeedData(long id, long userId) {

			try {
				if (await Redis.GetDatabaseData().KeyExistsAsync(id + ":" + userId)) {
					bool hasSetExpire = await Redis.GetDatabaseData().KeyExpireAsync(id + ":" + userId, TimeSpan.FromMinutes(5));
					if (hasSetExpire) { //Maybe it was last second and before timeout key deleted, so we check first
						return false;
					}
				}
			} catch (Exception e) {
				return false;
			}


			Database database = new Database(ServerConfig.DATABASE);
			database.Prepare("SELECT TOP(1) * FROM Data WHERE UserId = @UserId AND Id = @Id;");
			database.BindValue("@UserId", userId, SqlDbType.BigInt);
			database.BindValue("@Id", id, SqlDbType.BigInt);
			List<Row> row = await database.ExecuteSelectAsync();
			if (row.Count == 1) {
				HashEntry[] hashEntries;
				if (!String.IsNullOrEmpty(row[0].GetString("Auth"))) {
					hashEntries = new HashEntry[] {
						new HashEntry("A", row[0].GetString("Auth")),
						new HashEntry("M", row[0].GetString("Mid")),
						new HashEntry("U", row[0].GetString("Username")),
						new HashEntry("UAG", row[0].GetString("UserAgent"))
					};
				} else {
					hashEntries = new HashEntry[] {
						new HashEntry("S", row[0].GetString("Session")),
						new HashEntry("C", row[0].GetString("Csrftoken")),
						new HashEntry("M", row[0].GetString("Mid")),
						new HashEntry("U", row[0].GetString("Username")),
						new HashEntry("UAG", row[0].GetString("UserAgent"))
					};
				}


				try {
					await Redis.GetDatabaseData().HashSetAsync(id + ":" + userId, hashEntries);
					await Redis.GetDatabaseData().KeyExpireAsync(id + ":" + userId, TimeSpan.FromMinutes(5));
				} catch (Exception e) {}

				return false;

			}


			return true;
		}




		public static async Task<bool> addConnection(User user) {
			using (await threadLockConnection.LockAsync(user.id)) {
				UserInfo userInfo = UserConfig.listConnection.GetOrAdd(user.id, new Func<Int64, UserInfo>(delegate (Int64 userId) {
					return new UserInfo();
				}));

				if (userInfo.count >= MAX_CONNECTION) {
					return false;
				}
				userInfo.count += 1;


				//for test
				if (user.operatingSystem == "android") {
					WebSocketConnection.androidCount += 1;
				} else {
					WebSocketConnection.webCount += 1;
				}

				//WebSocket_Log.Send("ConnectionMember", "addConnection: " + userInfo.count +"\r\nuserId: " + userId);
			}

			return true;
		}

		public static async Task removeConnection(User user) {
			using (await threadLockConnection.LockAsync(user.id)) {
				UserInfo userInfo;
				UserConfig.listConnection.TryGetValue(user.id, out userInfo);
				if (userInfo != null) {
					userInfo.count -= 1;

					if (userInfo.count == 0) {
						UserConfig.listConnection.TryRemove(user.id, out _);
					}

					//for test
					if (user.operatingSystem == "android") {
						WebSocketConnection.androidCount -= 1;
					} else {
						WebSocketConnection.webCount -= 1;
					}
				}


				//WebSocket_Log.Send("ConnectionMember", "removeConnection: " + (userInfo == null ? "null" : userInfo.count) + "\r\nuserId: " + userId);
			}

		}

		public class UserInfo {
			public byte count = 0x00;

			public override string ToString() {
				return count + "";
			}
		}

		public sealed class User {
			public long id;
			public string operatingSystem;
			public string language;
			public int version;

			public string ip;
			public string connectionId;
		}
	}
}
