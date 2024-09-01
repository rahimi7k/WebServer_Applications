using Followergir.IO;
using Followergir.IONet;
using Library;
using Library.Json;
using Library.SQL;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Followergir.Methods {

	public class Add {

		private static readonly int LIMIT = 120;



		public static async Task<IOObject> AddUser(UserConfig.User user, IOApi.AddUser ioFunction, IOApi.Update[] updates) {
			IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.id, Error.ORDER_HAS_VIEWED);
			if (error != null) {
				return error;
			}

			if (ioFunction.iId <= 0L || ioFunction.order == null || String.IsNullOrEmpty(ioFunction.order.username) ||
				ioFunction.order.userId <= 0 ||
				String.IsNullOrEmpty(ioFunction.hash)) {
				return new IOApi.Error("INVALID_PARAMETERS");
			}

			error = Utils.FormatOrder(ioFunction.order);
			if (error != null) {
				return error;
			}


			if (await CheckAddLimit(user.id)) {
				return new IOApi.Error("TOO_MANY_REQUESTS");
			}


			if (await UserConfig.NeedData(user.id, ioFunction.iId)) {
				if (String.IsNullOrEmpty(ioFunction.data)) {
					return new IOApi.Error("DATA_INVALID");
				}

				bool isDataSaved = await UserConfig.SetData(user.id, ioFunction.iId, ioFunction.data);
				if (!isDataSaved) {
					return new IOApi.Error("DATA_INVALID");
				}
			}


			long userIdOrderer;
			try {
				JSONObject json = UserConfig.DecryptOrderHash(ioFunction.hash);
				if (json.GetLong("UserId") != ioFunction.iId) {
					return new IOApi.Error("HASH_INVALID");
				}
				userIdOrderer = json.GetLong("OrdererId");

			} catch (Exception) {
				await UserConfig.AddTooManyRequestAsync(user.id, Error.HASH_INVALID);
				return new IOApi.Error("HASH_INVALID");
			}

			gRPC_Followergir.AddUserReq reqAdd = new gRPC_Followergir.AddUserReq();
			reqAdd.UserId = user.id;
			reqAdd.IId = ioFunction.iId;
			reqAdd.Order = new gRPC_Followergir.Order();
			reqAdd.Order.Username = ioFunction.order.username;
			reqAdd.Order.UserId = ioFunction.order.userId;
			reqAdd.UserIdOrderer = userIdOrderer;
			gRPC_Followergir.AddUserRes resAdd = await GRPC.GetFollowergir().AddUserAsync(reqAdd);

			if (!String.IsNullOrEmpty(resAdd.Error)) {
				if (resAdd.Error == "ORDER_HAS_VIEWED") {
					await UserConfig.AddTooManyRequestAsync(user.id, Error.ORDER_HAS_VIEWED);
					return new IOApi.Error(resAdd.Error);

				} else if (resAdd.Error == "ORDER_NOT_FOUND") {
					await UserConfig.AddTooManyRequestAsync(user.id, Error.ORDER_NOT_FOUND);
					return new IOApi.Error(resAdd.Error);
				}
				return null;
			}

			//if (resAdd.Mande % 5 == 0) {
			IOApi.UpdateOrderInfo updateOrderInfo = new IOApi.UpdateOrderInfo();
			updateOrderInfo.orderInfo = new IOApi.OrderInfoItem();
			updateOrderInfo.orderInfo.order = new IOApi.Order();
			updateOrderInfo.orderInfo.order.username = ioFunction.order.username;
			updateOrderInfo.orderInfo.order.userId = ioFunction.order.userId;
			updateOrderInfo.orderInfo.type = Order.TYPE_USER;
			updateOrderInfo.orderInfo.remaining = resAdd.Mande;
			updateOrderInfo.orderInfo.status = Order.ORDER_STATUS_UPDATE;
			UserConfig.SendUpdateToGroup(userIdOrderer, null, updateOrderInfo);
			//}

			gRPC_Member.IncrementCoinReq reqCredit = new gRPC_Member.IncrementCoinReq();
			reqCredit.UserId = user.id;
			reqCredit.Count = UserConfig.config.rate.user;
			gRPC_Member.IncrementCoinRes resCredit = await GRPC.GetMember().IncrementCoinAsync(reqCredit);
			if (!String.IsNullOrEmpty(resCredit.Error)) {
				return new IOApi.Error("INTERNAL_ERROR");
			}



			/*
			if (!String.IsNullOrEmpty(resAdd.Type)) {
				if (resAdd.Type == Order.TELEGRAM_TYPE_CHANNEL + "" && StringUtils.Random(1, 1) == 1 || resAdd.Type == Order.TELEGRAM_TYPE_GROUP + "" && StringUtils.Random(1, 1) == 1) {
					IDatabase redisUnFollow = Redis.GetDatabaseUnFollow();
					int hour = DateTime.Now.Hour;//TODO DateTime.Now
					RedisValue redisValue2 = await redisUnFollow.KeyExistsAsync(ioFunction.tId + ":" + hour);
					if (redisValue2.IsNullOrEmpty) {
						int oldHour = hour - 1 < 0 ? 23 : hour - 1;
						redisValue2 = await redisUnFollow.KeyExistsAsync(ioFunction.tId + ":" + oldHour);
						if (redisValue2.IsNullOrEmpty) {
							await redisUnFollow.KeyRenameAsync(ioFunction.tId + ":" + oldHour, ioFunction.tId + ":" + hour);
						}
					}
					await redisUnFollow.ListRightPushAsync(ioFunction.tId + ":" + hour, resAdd.Type + ":" + ioFunction.orderId);
				}
			}
			*/

			await AddUnfollow(Order.TYPE_USER, ioFunction.order, user, ioFunction.iId);

			IOApi.UpdateCoin updateCoin = new IOApi.UpdateCoin();
			updateCoin.coin = resCredit.Coin;
			updateCoin.unixTime = resCredit.UnixTime;

			updates[0] = updateCoin;
			UserConfig.SendUpdateToGroup(user.id, user.connectionId, updateCoin);

			return new IOApi.Ok();
		}




		public static async Task<IOObject> AddLike(UserConfig.User user, IOApi.AddLike ioFunction, IOApi.Update[] updates) {
			IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.id, Error.ORDER_HAS_VIEWED);
			if (error != null) {
				return error;
			}
			
	

			if (ioFunction.iId <= 0L || ioFunction.order == null || String.IsNullOrEmpty(ioFunction.order.postId) ||
				ioFunction.order.userId <= 0 ||
				(ioFunction.type != Order.INSTAGRAM_MESSAGE_TYPE_IMAGE &&
				ioFunction.type != Order.INSTAGRAM_MESSAGE_TYPE_VIDEO &&
				ioFunction.type != Order.INSTAGRAM_MESSAGE_TYPE_MULTI_POST)) {
				return new IOApi.Error("INVALID_PARAMETERS");
			}
	
			if (await CheckAddLimit(user.id)) {
				return new IOApi.Error("TOO_MANY_REQUESTS");
			}
	
			if (await UserConfig.NeedData(user.id, ioFunction.iId)) {
				if (String.IsNullOrEmpty(ioFunction.data)) {
					return new IOApi.Error("DATA_INVALID");
				}
			
				bool isDataSaved = await UserConfig.SetData(user.id, ioFunction.iId, ioFunction.data);
				if (!isDataSaved) {
					return new IOApi.Error("DATA_INVALID");
				}
			}

	
			error = Utils.FormatOrder(ioFunction.order);
			if (error != null) {
				return error;
			}
			
			long userIdOrderer;
			try {
				JSONObject json = UserConfig.DecryptOrderHash(ioFunction.hash);
				if (json.GetLong("UserId") != ioFunction.iId) {
					return new IOApi.Error("HASH_INVALID");
				}
				userIdOrderer = json.GetLong("OrdererId");

			} catch (Exception) {
				await UserConfig.AddTooManyRequestAsync(user.id, Error.HASH_INVALID);
				return new IOApi.Error("HASH_INVALID");
			}
			
			gRPC_Followergir.AddLikeReq reqAdd = new gRPC_Followergir.AddLikeReq();
			reqAdd.UserId = user.id;
			reqAdd.IId = ioFunction.iId;
			reqAdd.Order = new gRPC_Followergir.Order();
			reqAdd.Order.Username = ioFunction.order.username;
			reqAdd.Order.PostId = ioFunction.order.postId;
			reqAdd.Order.UserId = ioFunction.order.userId;
			reqAdd.UserIdOrderer = userIdOrderer;
			reqAdd.Type = ioFunction.type;
			gRPC_Followergir.AddLikeRes resAdd = await GRPC.GetFollowergir().AddLikeAsync(reqAdd);
		
			if (!String.IsNullOrEmpty(resAdd.Error)) {
				if (resAdd.Error == "ORDER_HAS_VIEWED") {
					await UserConfig.AddTooManyRequestAsync(user.id, Error.ORDER_HAS_VIEWED);
					return new IOApi.Error(resAdd.Error);

				} else if (resAdd.Error == "ORDER_NOT_FOUND") {
					await UserConfig.AddTooManyRequestAsync(user.id, Error.ORDER_NOT_FOUND);
					return new IOApi.Error(resAdd.Error);

				} else if (resAdd.Error == "TYPE_INVALID") {
					await UserConfig.AddTooManyRequestAsync(user.id, Error.TYPE_INVALID);
					return new IOApi.Error(resAdd.Error);
				}
			
				return null;
			}

			//if (resAdd.Mande % 5 == 0) {
			IOApi.UpdateOrderInfo updateOrderInfo = new IOApi.UpdateOrderInfo();
			updateOrderInfo.orderInfo = new IOApi.OrderInfoItem();
			updateOrderInfo.orderInfo.order = new IOApi.Order();
			updateOrderInfo.orderInfo.order.username = ioFunction.order.username;
			updateOrderInfo.orderInfo.order.userId = ioFunction.order.userId;
			updateOrderInfo.orderInfo.order.postId = ioFunction.order.postId;
			updateOrderInfo.orderInfo.type = Order.TYPE_LIKE;
			updateOrderInfo.orderInfo.remaining = resAdd.Mande;
			updateOrderInfo.orderInfo.status = Order.ORDER_STATUS_UPDATE;
			updateOrderInfo.unixTime = resAdd.UnixTime;
			UserConfig.SendUpdateToGroup(userIdOrderer, null, updateOrderInfo);
			//}
		
			gRPC_Member.IncrementCoinReq reqCredit = new gRPC_Member.IncrementCoinReq();
			reqCredit.UserId = user.id;
			reqCredit.Count = UserConfig.config.rate.like;
			gRPC_Member.IncrementCoinRes resCredit = await GRPC.GetMember().IncrementCoinAsync(reqCredit);
			if (!String.IsNullOrEmpty(resCredit.Error)) {
				return new IOApi.Error("INTERNAL_ERROR");
			}

			
			await AddUnfollow(Order.TYPE_LIKE, ioFunction.order, user, ioFunction.iId);

			IOApi.UpdateCoin updateCoin = new IOApi.UpdateCoin();
			updateCoin.coin = resCredit.Coin;
			updateCoin.unixTime = resCredit.UnixTime;

			updates[0] = updateCoin;
			UserConfig.SendUpdateToGroup(user.id, user.connectionId, updateCoin);
		
			return new IOApi.Ok();
		}



		public static async Task<IOObject> AddComment(UserConfig.User user, IOApi.AddComment ioFunction, IOApi.Update[] updates) {
			IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.id, Error.ORDER_HAS_VIEWED);
			if (error != null) {
				return error;
			}

			if (ioFunction.iId <= 0L || ioFunction.order == null || String.IsNullOrEmpty(ioFunction.order.postId) || (
				ioFunction.type != Order.INSTAGRAM_MESSAGE_TYPE_IMAGE &&
				ioFunction.type != Order.INSTAGRAM_MESSAGE_TYPE_VIDEO &&
				ioFunction.type != Order.INSTAGRAM_MESSAGE_TYPE_MULTI_POST)) {
				return new IOApi.Error("INVALID_PARAMETERS");
			}

			if (await CheckAddLimit(user.id)) {
				return new IOApi.Error("TOO_MANY_REQUESTS");
			}

			if (await UserConfig.NeedData(user.id, ioFunction.iId)) {
				if (String.IsNullOrEmpty(ioFunction.data)) {
					return new IOApi.Error("DATA_INVALID");
				}

				bool isDataSaved = await UserConfig.SetData(user.id, ioFunction.iId, ioFunction.data);
				if (!isDataSaved) {
					return new IOApi.Error("DATA_INVALID");
				}
			}


			error = Utils.FormatOrder(ioFunction.order);
			if (error != null) {
				return error;
			}

			long userIdOrderer;
			try {
				JSONObject json = UserConfig.DecryptOrderHash(ioFunction.hash);
				if (json.GetLong("UserId") != ioFunction.iId) {
					return new IOApi.Error("HASH_INVALID");
				}
				userIdOrderer = json.GetLong("OrdererId");

			} catch (Exception) {
				await UserConfig.AddTooManyRequestAsync(user.id, Error.HASH_INVALID);
				return new IOApi.Error("HASH_INVALID");
			}


			gRPC_Followergir.AddCommentReq reqAdd = new gRPC_Followergir.AddCommentReq();
			reqAdd.UserId = user.id;
			reqAdd.IId = ioFunction.iId;
			reqAdd.Order = new gRPC_Followergir.Order();
			reqAdd.Order.Username = ioFunction.order.username;
			reqAdd.Order.PostId = ioFunction.order.postId;
			reqAdd.Order.UserId = ioFunction.order.userId;
			reqAdd.UserIdOrderer = userIdOrderer;
			reqAdd.Type = ioFunction.type;
			gRPC_Followergir.AddCommentRes resAdd = await GRPC.GetFollowergir().AddCommentAsync(reqAdd);

			if (!String.IsNullOrEmpty(resAdd.Error)) {
				if (resAdd.Error == "ORDER_HAS_VIEWED") {
					await UserConfig.AddTooManyRequestAsync(user.id, Error.ORDER_HAS_VIEWED);
					return new IOApi.Error(resAdd.Error);

				} else if (resAdd.Error == "ORDER_NOT_FOUND") {
					await UserConfig.AddTooManyRequestAsync(user.id, Error.ORDER_NOT_FOUND);
					return new IOApi.Error(resAdd.Error);

				} else if (resAdd.Error == "TYPE_INVALID") {
					await UserConfig.AddTooManyRequestAsync(user.id, Error.TYPE_INVALID);
					return new IOApi.Error(resAdd.Error);
				}
				return null;
			}

			//if (resAdd.Mande % 5 == 0) {
			IOApi.UpdateOrderInfo updateOrderInfo = new IOApi.UpdateOrderInfo();
			updateOrderInfo.orderInfo = new IOApi.OrderInfoItem();
			updateOrderInfo.orderInfo.order = new IOApi.Order();
			updateOrderInfo.orderInfo.order.username = ioFunction.order.username;
			updateOrderInfo.orderInfo.order.userId = ioFunction.order.userId;
			updateOrderInfo.orderInfo.order.postId = ioFunction.order.postId;
			updateOrderInfo.orderInfo.type = Order.TYPE_COMMENT;
			updateOrderInfo.orderInfo.remaining = resAdd.Mande;
			updateOrderInfo.orderInfo.status = Order.ORDER_STATUS_UPDATE;
			updateOrderInfo.unixTime = resAdd.UnixTime;
			UserConfig.SendUpdateToGroup(userIdOrderer, null, updateOrderInfo);
			//}

			gRPC_Member.IncrementCoinReq reqCredit = new gRPC_Member.IncrementCoinReq();
			reqCredit.UserId = user.id;
			reqCredit.Count = UserConfig.config.rate.comment;
			gRPC_Member.IncrementCoinRes resCredit = await GRPC.GetMember().IncrementCoinAsync(reqCredit);
			if (!String.IsNullOrEmpty(resCredit.Error)) {
				return new IOApi.Error("INTERNAL_ERROR");
			}

			IOApi.UpdateCoin updateCoin = new IOApi.UpdateCoin();
			updateCoin.coin = resCredit.Coin;
			updateCoin.unixTime = resCredit.UnixTime;

			updates[0] = updateCoin;
			UserConfig.SendUpdateToGroup(user.id, user.connectionId, updateCoin);

			return new IOApi.Ok();
		}




		private static async Task AddUnfollow(byte type, IOApi.Order order, UserConfig.User user, long iId) {

			if (false && StringUtils.Random(1, 7) < 6) {
				return;
			}

			Database database = new Database(ServerConfig.DATABASE);
			if (type == Order.TYPE_USER) {
				database.Prepare("INSERT INTO [UnFollow] (UserId, IId, Type, Username, OrderUserId) VALUES (@UserId, @IId, @Type, @Username, @OrderUserId)");
				database.BindValue("@UserId", user.id, SqlDbType.BigInt);
				database.BindValue("@IId", iId, SqlDbType.BigInt);
				database.BindValue("@Type", type, SqlDbType.TinyInt); ;
				database.BindValue("@Username", order.username, SqlDbType.VarChar);
				database.BindValue("@OrderUserId", order.userId, SqlDbType.VarChar);
			} else {
				database.Prepare("INSERT INTO [UnFollow] (UserId, IId, Type, Username, OrderUserId, PostId) VALUES (@UserId, @IId, @Type, @Username, @OrderUserId, @PostId)");
				database.BindValue("@UserId", user.id, SqlDbType.BigInt);
				database.BindValue("@IId", iId, SqlDbType.BigInt);
				database.BindValue("@Type", type, SqlDbType.TinyInt);
				database.BindValue("@Username", order.username, SqlDbType.VarChar);
				database.BindValue("@OrderUserId", order.userId, SqlDbType.VarChar);
				database.BindValue("@PostId", order.postId, SqlDbType.VarChar);
			}
			await database.ExecuteInsertAsync();

			/*
			JSONArray jsonArray;
			JSONObject jsonObject = new JSONObject();
			jsonObject.Add("U", order.username);
			jsonObject.Add("I", order.userId + "");
			if (!String.IsNullOrEmpty(order.postId)) {
				jsonObject.Add("P", order.postId);
			}

			Database database = new Database(ServerConfig.DATABASE);
			database.DisableClose();
			database.Prepare("SELECT TOP(1) " + type + " FROM [UnFollow] WHERE UserId = @UserId AND Id = @Id;");
			database.BindValue("@UserId", user.id, SqlDbType.BigInt);
			database.BindValue("@Id", iId, SqlDbType.BigInt);
			List<Row> rows = await database.ExecuteSelectAsync();
			if (rows.Count > 0) {
				try {
					jsonArray = new JSONArray(rows[0].GetString(type));
					jsonArray.Add(jsonObject);
					database.Prepare("UPDATE [UnFollow] SET " + type + " = @" + type + ", Date = @Date WHERE UserId = @UserId AND Id = @Id;");
					database.BindValue("@UserId", user.id, SqlDbType.BigInt);
					database.BindValue("@Id", iId, SqlDbType.BigInt);
					database.BindValue("@" + type, jsonArray.ToString(), SqlDbType.VarChar);
					database.BindValue("@Date", DateTime.UtcNow, SqlDbType.DateTime);
					await database.ExecuteUpdateAsync();
				} catch (Exception e) {
					Log.SendEmailKorosh("Error Unfollow Json", "Type: " + type + "\r\nUserId: " + user.id + "\r\nOrder: " + jsonObject.ToString() + "\r\nrows[0].GetString(\" type \"): " + rows[0].GetString(type) + "\r\nError: " + e);
				}

			} else {
				jsonArray = new JSONArray();
				jsonArray.Add(jsonObject);
				try {
					database.Prepare("INSERT INTO [UnFollow] (UserId, Id, " + type + ", Date) VALUES (@UserId, @Id, @" + type + ", @Date)");
					database.BindValue("@UserId", user.id, SqlDbType.BigInt);
					database.BindValue("@Id", iId, SqlDbType.BigInt);
					database.BindValue("@" + type, jsonArray.ToString(), SqlDbType.VarChar); ;
					database.BindValue("@Date", DateTime.UtcNow, SqlDbType.DateTime);
					await database.ExecuteInsertAsync();
				} catch (Exception) { }
			}

			await database.CloseAsync();
			*/
		}


		private static async Task<bool> CheckAddLimit(long id) {
			try {
				RedisValue redisValue = await Redis.GetDatabaseLimit().StringGetAsync(id + ":Add");
				if ((int) redisValue > LIMIT) {
					return true;
				}
				await Redis.GetDatabaseLimit().StringSetAsync(id + ":Add", ((int) redisValue) + 1, TimeSpan.FromSeconds(60));
				return false;
			} catch (Exception e){
				return false;
			}
		}

	}
}
