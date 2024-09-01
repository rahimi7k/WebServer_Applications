using Followergir.Controllers;
using Followergir.IO;
using Followergir.IONet;
using Microsoft.AspNetCore.SignalR;
using MySql.Data.MySqlClient;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Library;

namespace Followergir.Methods {

	public class User : Store {

		public static readonly BindingFlags BINDING_FLAGS = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;

		public static readonly int NAME_LENGTH_MAX = 64;
		public static readonly int NAME_LENGTH_MIN = 1;

		public static readonly int PASSWORD_LENGTH_MAX = 64;
		public static readonly int PASSWORD_LENGTH_MIN = 6;




		/*public static async Task<IOObject> GetMe(UserConfig.User user, IOApi.GetMe ioFunction, IOApi.Update[] updates) {
			IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.ip, IOError.USER_NOT_FOUND);
			if (error != null) {
				return error;
			}

			gRPC_Member.GetUserReq reqGetUser = new gRPC_Member.GetUserReq();
			reqGetUser.UserId = ioFunction.userId;
			gRPC_Member.GetUserRes resGetUser = await GRPC.GetMember().GetUserAsync(reqGetUser);

			if (resGetUser.Error == "USER_NOT_FOUND") {
				return new IOApi.Error("UNAUTHORIZED");
			}
			if (reqGetUser.Session != ioFunction.session) {
				return new IOApi.Error("UNAUTHORIZED");
			}

			if (resGetUser.Block) {
				return new IOApi.Error("USER_BLOCK");
			}

			IOApi.UserFull res = new IOApi.UserFull();
			res.name = resGetUser.Name;
			res.phoneNumber = resGetUser.PhoneNumber;
			res.coin = resGetUser.Coin;
			return res;
		}*/



		public static async Task<IOObject> ChangeName(UserConfig.User user, IOApi.ChangeName ioFunction, IOApi.Update[] updates) {
			if (String.IsNullOrEmpty(ioFunction.name) || ioFunction.name.Length > NAME_LENGTH_MAX) {
				return new IOApi.Error("INVALID_PARAMETERS");
			}

			gRPC_Member.SetUserFieldReq req = new gRPC_Member.SetUserFieldReq();
			req.UserId = user.id;
			req.Key = Redis.NAME;
			req.Value = ioFunction.name;
			gRPC_Member.SetUserFieldRes res = await GRPC.GetMember().SetUserFieldAsync(req);

			IOApi.UpdateName updateName = new IOApi.UpdateName();
			updateName.name = ioFunction.name;

			updates[0] = updateName;
			UserConfig.SendUpdateToGroup(user.id, user.connectionId, updateName);

			return new IOApi.Ok();
		}



		public static async Task<IOObject> ChangePhoneNumber(UserConfig.User user, IOApi.ChangePhoneNumber ioFunction, IOApi.Update[] updates) {
			Log.SendEmail("Change Phone Number Warning!", "UserId: " + user.id + "\r\nIP: " + user.ip);
			return null;
		}
		public static async Task<IOObject> ChangeEmailAddress(UserConfig.User user, IOApi.ChangeEmailAddress ioFunction, IOApi.Update[] updates) {
			Log.SendEmail("Change Email Address Warning!", "UserId: " + user.id + "\r\nIP: " + user.ip);
			return null;
		}


		public static async Task<IOObject> ChangePassword(UserConfig.User user, IOApi.ChangePassword ioFunction, IOApi.Update[] updates) {
			if (String.IsNullOrEmpty(ioFunction.oldPassword) || String.IsNullOrEmpty(ioFunction.newPassword)) {
				return new IOApi.Error("INVALID_PARAMETERS");
			}

			if (ioFunction.newPassword.Length > PASSWORD_LENGTH_MAX) {
				return new IOApi.Error("PASSWORD_LENGTH_MAX");

			} else if (ioFunction.newPassword.Length < PASSWORD_LENGTH_MIN) {
				return new IOApi.Error("PASSWORD_LENGTH_MIM");
			}

			gRPC_Member.CheckPasswordReq reqCheckPassword = new gRPC_Member.CheckPasswordReq();
			reqCheckPassword.UserId = user.id;
			reqCheckPassword.Password = ioFunction.oldPassword;
			gRPC_Member.CheckPasswordRes resCheckPassword = await GRPC.GetMember().CheckPasswordAsync(reqCheckPassword);
			if (!String.IsNullOrEmpty(resCheckPassword.Error)) {
				if (resCheckPassword.Error == "PASSWORD_NOT_MATCH") {
					return new IOApi.Error("PASSWORD_NOT_MATCH");

				} else if (resCheckPassword.Error == "USER_NOT_FOUND") {
				}

				return null;
			}

			gRPC_Member.ChangePasswordReq reqPassword = new gRPC_Member.ChangePasswordReq();
			reqPassword.UserId = user.id;
			reqPassword.Password = ioFunction.newPassword;
			gRPC_Member.ChangePasswordRes resChange = await GRPC.GetMember().ChangePasswordAsync(reqPassword);
			if (!String.IsNullOrEmpty(resChange.Error)) {
				throw new Exception("ChangePassword returnd error! \r\nuser.id:" + user.id + "\r\nioFunction.newPassword: " + ioFunction.newPassword);
			}

			if (String.IsNullOrEmpty(resChange.Session)) {
				throw new Exception("UpdateSession returnd error! \r\nuser.id:" + user.id);
			}

			IOApi.UpdateSession updateSession = new IOApi.UpdateSession();
			updateSession.session = resChange.Session;
			updates[0] = updateSession;

			Nats.connectionUpdate.Publish(user.id + "", user.connectionId, new byte[] { 0x01 }); //LOGOUT

			return new IOApi.Ok();
		}


		public static async Task<IOObject> CheckUnFollow(UserConfig.User user, IOApi.CheckUnFollow ioFunction, IOApi.Update[] updates) {
			/*
					Database database = new Database(ServerConfig.DATABASE_FOLLOWERGIR);
					database.DisableClose();
					database.Prepare("SELECT TOP(1) Channels FROM [UnFollow] WHERE TId = @TId;");
					database.BindValue("@TId", ioFunction.iId, SqlDbType.BigInt);
					List<Row> rows = await database.ExecuteSelectAsync();
					if (rows.Count == 0) {
						return null;
					}
					string channels = rows[0].GetString("Channels");

					if (ioFunction.count == -1) {
						await database.CloseAsync();

						int count = StringUtils.CharacterCount(channels, ",");
						int countAdd = 0;
						int index = ioFunction.index;

						while ((index = channels.IndexOf(',', index)) != -1) {
							countAdd += 1;
							index += 1;
							if (countAdd == 10 && count >= 20) {
								break;
							}
						}

						IOApi.UnFollow res = new IOApi.UnFollow();
						try {
							res.list = channels.Substring(ioFunction.index, index - ioFunction.index);
							res.hasMore = count > 30;
							return res;
						} catch (Exception ex) {
							Log.SendEmail("UnFollow Warning!", "UserId: " + user.id + "\r\nTId: " + ioFunction.iId + "\r\nChannels: " + channels + "\r\nCount: " + count +
								"\r\nioObject index: " + ioFunction.index + "\r\nindex: " + index + "\r\nException: " + ex);
							return null;
						}

					} else {
						channels = channels.Substring(ioFunction.index);
						if (channels == "") {
							database.Prepare("DELETE FROM [UnFollow] WHERE TId = @TId");
							database.BindValue("@TId", ioFunction.iId, SqlDbType.BigInt);
							await database.ExecuteDeleteAsync();
						} else {
							database.Prepare("UPDATE [UnFollow] SET Channels = @Channels WHERE TId = @TId;");
							database.BindValue("@Channels", channels, SqlDbType.VarChar);//TODo VarChar
							database.BindValue("@TId", ioFunction.iId, SqlDbType.BigInt);
							await database.ExecuteUpdateAsync();
						}
						await database.CloseAsync();

						if (ioFunction.count > 1) {
							ioFunction.count *= 2;//(ioFunction.count > 10 ? 3 : 2);

						}
					}*/
			return null;
		}


		public static async Task<IOObject> LogOut(UserConfig.User user, IOApi.LogOut ioFunction, IOApi.Update[] updates) {

			return null;
		}


		public static async Task<IOObject> DeleteAccount(UserConfig.User user, IOApi.DeleteAccount ioFunction, IOApi.Update[] updates) {
			Log.SendEmail("Delete Account Warning!", "UserId: " + user.id + "\r\nIP: " + user.ip);
			return null;
		}









		public static async Task<IOObject> SetData(UserConfig.User user, IOApi.SetData ioFunction, IOApi.Update[] updates) {

			if (ioFunction.id <= 0 || String.IsNullOrEmpty(ioFunction.data)) {
				return null;
			}
			bool isDataSet = await UserConfig.SetData(user.id, ioFunction.id, ioFunction.data);
			if (!isDataSet) {
				return null;
			}

			MySqlConnection mysqlConnection = null;
			try {
				mysqlConnection = new MySqlConnection(ServerConfig.DATABASE_FOLLOWERGIR_OLD);
				mysqlConnection.Open();

				MySqlCommand cmd = new MySqlCommand("SELECT Gem, Pol, Warning FROM member WHERE UserId = @UserId LIMIT 1;", mysqlConnection);
				cmd.Parameters.AddWithValue("@UserId", ioFunction.id);
				MySqlDataReader reader = cmd.ExecuteReader();
				cmd.Dispose();

				bool isRowExist = reader.Read();
				if (!isRowExist) {
					reader.Close();
					reader.Dispose();
					mysqlConnection.Close();
					return null;
				}

				int gem = (int) reader.GetValue(reader.GetOrdinal("Gem"));
				int coin = (int) reader.GetValue(reader.GetOrdinal("Pol"));
				string warning = (string) reader.GetValue(reader.GetOrdinal("Warning"));

				reader.Close();
				reader.Dispose();


				if (warning.StartsWith("Transferred")) {
					mysqlConnection.Close();
					return null;
				}

				double newCoin = gem + (0.5 * coin);
				if (newCoin <= 0) {
					mysqlConnection.Close();
					return null;
				}

				cmd = new MySqlCommand("UPDATE member SET Gem = 0, Pol = 0, Warning = @Warning WHERE UserId = @UserId LIMIT 1;", mysqlConnection);
				cmd.Parameters.AddWithValue("@UserId", ioFunction.id);
				cmd.Parameters.AddWithValue("@Warning", "Transferred To Id:" + user.id + " - Gem: " + gem + " - Pol: " + coin);
				int count = cmd.ExecuteNonQuery();
				cmd.Dispose();
				if (count == 0) {
					Log.SendEmailKorosh("SetData Error", "Mysql update user gem and coin not work, user.id: " + user.id);
					mysqlConnection.Close();
					return null;
				}

				gRPC_Member.IncrementCoinReq reqIncrement = new gRPC_Member.IncrementCoinReq();
				reqIncrement.UserId = user.id;
				reqIncrement.Count = newCoin;
				gRPC_Member.IncrementCoinRes resIncrement = GRPC.GetMember().IncrementCoin(reqIncrement);


				IOApi.UpdateCoin updateCoin = new IOApi.UpdateCoin();
				updateCoin.coin = resIncrement.Coin;
				updateCoin.unixTime = resIncrement.UnixTime;
				updates[0] = updateCoin;
				UserConfig.SendUpdateToGroup(user.id, user.connectionId, updateCoin);

			} catch (Exception ex) {
				//Console.WriteLine("Error Connecting To Mysql" + ex.ToString());
			}
			if (mysqlConnection != null) {
				await mysqlConnection.CloseAsync();
			}
			
			return null;
		}


	}
}
