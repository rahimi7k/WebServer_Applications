using Followergir.Controllers;
using Followergir.IO;
using Followergir.IONet;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Followergir.Methods {

	public class Action {

		public static readonly BindingFlags BINDING_FLAGS = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;


		public static async Task AuthenticationAll(UserConfig.User user, IOApi.AuthenticationAll ioAction) {

		}

		public static async Task ActionUnFollowResponse(UserConfig.User user, IOApi.ActionUnFollowResponse ioAction) {
			if (true) {
				return;
			}
			
			/*IDatabase database = Redis.GetDatabaseActionUnFollowResponse();

			if ((await database.HashGetAsync(ioAction.hash, "U:" + user.id)).IsNullOrEmpty) {
				return;
			}

			await database.HashSetAsync(ioAction.hash, "U:" + user.id, ioAction.res);

			int count = 0;
			long userId = 0L;

			HashEntry[] hashEntries = await database.HashGetAllAsync(ioAction.hash);
			byte trues = 0;
			byte falses = 0;
			for (int i = 0; i < hashEntries.Length; i++) {
				if (hashEntries[i].Name.StartsWith("U:")) {
					if (hashEntries[i].Value != "") {
						if (ioAction.res) {
							trues++;
						} else {
							falses++;
						}

					}
				} else if (hashEntries[i].Name == "Count") {
					count = StringUtils.ParseInt(hashEntries[i].Value);

				} else if (hashEntries[i].Name == "UserId") {
					userId = StringUtils.ParseLong(hashEntries[i].Value);
				}
			}

			if (count == 0) {
				Log.SendEmail("Service ActionUnFollowResponse", "Count is 0\r\nUserId: " + user.id + "\r\nHash: " + ioAction.hash + "\r\ntrues: " + trues + "\r\nfalses: " + falses);
				return;
			}

			if (trues >= 2) {
				await database.KeyDeleteAsync(ioAction.hash);

			} else if (falses >= 2) {
				/gRPC_Followergir.DecrementGemReq reqCredit = new gRPC_Followergir.DecrementGemReq();
				reqCredit.UserId = userId;
				reqCredit.Count = (int) (count * 1.5);
				reqCredit.AllowNegative = true;
				gRPC_Followergir.DecrementGemRes resCredit = await GRPC.GetDatabaseMethods().DecrementGemAsync(reqCredit);
				/
				/
				List<WebSoketConnection> list = UserConfig.listConnection.GetValueOrDefault(userId);
				if (list == null || list.Count == 0) {
					Database databaseSql = new Database(ServerConfig.DATABASE_FOLLOWERGIR);
					databaseSql.DisableClose();
					databaseSql.Prepare("UPDATE [UnFollow] SET DecresedGem = DecresedGem + @DecresedGem, Date = GETDATE() WHERE UserId = @UserId;");
					databaseSql.BindValue("@DecresedGem", count * 1.5, SqlDbType.Int);
					databaseSql.BindValue("@UserId", userId, SqlDbType.BigInt);
					int rowChanged = await databaseSql.ExecuteNonQueryAsync();
					if (rowChanged == 0) {
						databaseSql.Prepare("INSERT INTO [UnFollow] (UserId, DecresedGem) VALUES (@UserId, @DecresedGem)");
						databaseSql.BindValue("@UserId", userId, SqlDbType.BigInt);
						databaseSql.BindValue("@DecresedGem", count * 1.5, SqlDbType.Int);
						await databaseSql.ExecuteInsertAsync();
					}
					databaseSql.Close();
					return;
				}
			


				WebSoketConnection[] copy = null;
				int c = 0;
				while (true) {
					try {
						copy = new WebSoketConnection[list.Count];
					} catch (Exception) {
						return;
					}

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
				

				new Thread(delegate () {
					IOApi.UpdateGem updateGem = new IOApi.UpdateGem();
					updateGem.gem = resCredit.Gem;


					for (int i = 0; i < copy.Length; i++) {

						Console.WriteLine("Action i: " + i);

						try {
							WebSoketConnection webSocket = copy[i];

							IOApi.UpdateMessage update = new IOApi.UpdateMessage();
							update.message = string.Format(App.Configuration().GetSection("Strings:" + webSocket.user.language + ":UnFollowMessage").Value, count * 2);

							//byte[] bytes = SerializedData.SerializeObject(update, webSocket.aesKey, WebSoketConnection.aesIv);

							Console.WriteLine("Action - user: " + webSocket.user.connectionId);

							//webSocket.webSocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Binary, true, CancellationToken.None);

						} catch (Exception e) {
							Console.WriteLine("Action Exception: " + e);
						}
					}
				}).Start();

				/

			}*/


		}


		public static async Task FollowClientResult(UserConfig.User user, IOApi.FollowClientResult ioAction) {

		}
	}
}
