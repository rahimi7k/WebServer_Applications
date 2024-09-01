using Followergir.IO;
using Followergir.IONet;
using Library.Json;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Followergir.Methods {

	public class Order : List {

		public static readonly byte TYPE_USER = 0x01;
		public static readonly byte TYPE_LIKE = 0x02;
		public static readonly byte TYPE_COMMENT = 0x03;
		//public static readonly byte TYPE_VIEW = 0x04;


		public static readonly int INSTAGRAM_MESSAGE_TYPE_IMAGE = 1;
		public static readonly int INSTAGRAM_MESSAGE_TYPE_VIDEO = 2;
		public static readonly int INSTAGRAM_MESSAGE_TYPE_MULTI_POST = 8;

		public static readonly byte ORDER_STATUS_NEW = 1;
		public static readonly byte ORDER_STATUS_START = 2;
		public static readonly byte ORDER_STATUS_CANCEL = 3;
		public static readonly byte ORDER_STATUS_UPDATE = 4;

		public static readonly TimeSpan TTL_TEMP_REMOVED_ORDER = TimeSpan.FromSeconds(20);
		public static readonly TimeSpan TTL_VIEW_REMOVED = TTL_TEMP_REMOVED_ORDER + TimeSpan.FromSeconds(10);


		public static async Task<IOObject> GetOrderInfo(UserConfig.User user, IOApi.GetOrderInfo ioFunction, IOApi.Update[] updates) {
			IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.id, Error.CREDIT_LIMIT);
			if (error != null) {
				return error;
			}

			IOApi.OrderInfo res = new IOApi.OrderInfo();
			res.list = new List<IOApi.OrderInfoItem>();

			gRPC_Followergir.GetOrderInfoReq reqInfo = new gRPC_Followergir.GetOrderInfoReq();
			reqInfo.UserId = user.id;
			reqInfo.Index = ioFunction.index;
			gRPC_Followergir.GetOrderInfoRes resInfo = await IONet.GRPC.GetFollowergir().GetOrderInfoAsync(reqInfo);

			foreach (gRPC_Followergir.OrderInfoItem item in resInfo.List) {
				IOApi.OrderInfoItem content = new IOApi.OrderInfoItem();
				content.order = new IOApi.Order();
				content.order.username = item.Order.Username;
				content.order.postId = item.Order.PostId;
				content.order.userId = item.Order.UserId;

				if (item.Type == "U") {
					content.type = TYPE_USER;

				} else if (item.Type == "L") {
					content.type = TYPE_LIKE;

				} else if (item.Type == "C") {
					content.type = TYPE_COMMENT;
				}
				content.count = item.Count;
				content.remaining = item.Remaining;
				content.error = item.Error;
				//content.orderInfo.status = 0;

				res.list.Add(content);
			}
			res.hasMore = resInfo.HasMore;
			return res;
		}





		public static async Task<IOObject> GetOrderHistory(UserConfig.User user, IOApi.GetOrderHistory ioFunction, IOApi.Update[] updates) {
			IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.id, Error.CREDIT_LIMIT);
			if (error != null) {
				return error;
			}
			if (ioFunction.id < 0) {
				return new IOApi.Error("INVALID_PARAMETERS");
			}

			gRPC_Followergir.GetOrderHistoryReq reqHistory = new gRPC_Followergir.GetOrderHistoryReq();
			reqHistory.UserId = user.id;
			reqHistory.Id = ioFunction.id;
			reqHistory.IsNew = ioFunction.isNew;
			gRPC_Followergir.GetOrderHistoryRes resHistory = await IONet.GRPC.GetFollowergir().GetOrderHistoryAsync(reqHistory);

			IOApi.OrderHistory res = new IOApi.OrderHistory();
			if (resHistory.List.Count > 0) {
				res.list = new List<IOApi.OrderHistoryItem>();
				foreach (gRPC_Followergir.OrderHistoryContent item in resHistory.List) {
					IOApi.OrderHistoryItem content = new IOApi.OrderHistoryItem();

					content.id = item.Id;
					content.order = new IOApi.Order();
					content.order.userId = item.Order.UserId;
					content.order.username = item.Order.Username;
					content.order.postId = item.Order.PostId;
					content.type = (byte) item.Type;
					content.count = item.Count;
					content.date = item.Date;
					res.list.Add(content);
				}
			}

			res.hasMore = resHistory.HasMore;
			return res;
		}




		public static async Task<IOObject> OrderUser(UserConfig.User user, IOApi.OrderUser ioFunction, IOApi.Update[] updates) {
			IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.id, Error.CREDIT_LIMIT);
			if (error != null) {
				return error;
			}
			//TODO API not send initialCount
			if (ioFunction.order == null || String.IsNullOrEmpty(ioFunction.order.username)) {
				return new IOApi.Error("INVALID_PARAMETERS");
			}

			error = Utils.FormatOrder(ioFunction.order);
			if (error != null) {
				return error;
			}

			if (ioFunction.count < UserConfig.config.order.minUser) {
				return new IOApi.Error("ORDER_COUNT_MIN");
			}
			if (ioFunction.count > UserConfig.config.order.maxUser) {
				return new IOApi.Error("ORDER_COUNT_MAX");
			}

			gRPC_Member.DecrementCoinReq reqCredit = new gRPC_Member.DecrementCoinReq();
			reqCredit.UserId = user.id;
			reqCredit.Count = ioFunction.count * UserConfig.config.rate.user * 2;
			gRPC_Member.DecrementCoinRes resCredit = await GRPC.GetMember().DecrementCoinAsync(reqCredit);
			if (resCredit.Error == "CREDIT_LIMIT") {
				await UserConfig.AddTooManyRequestAsync(user.id, Error.CREDIT_LIMIT);
				return new IOApi.Error("CREDIT_LIMIT");
			}

			gRPC_Followergir.OrderUserReq reqOrder = new gRPC_Followergir.OrderUserReq();
			reqOrder.UserId = user.id;
			reqOrder.Order = new gRPC_Followergir.Order();
			reqOrder.Order.Username = ioFunction.order.username;
			reqOrder.Order.UserId = ioFunction.order.userId;
			reqOrder.Count = ioFunction.count;
			reqOrder.InitialCount = ioFunction.initialCount;
			//reqOrder.Type = ioFunction.type + "";
			gRPC_Followergir.OrderRes resOrder;
			try {
				resOrder = await GRPC.GetFollowergir().OrderUserAsync(reqOrder);

			} catch (Exception) {
				gRPC_Member.IncrementCoinReq reqCreditBack = new gRPC_Member.IncrementCoinReq();
				reqCreditBack.UserId = user.id;
				reqCreditBack.Count = ioFunction.count * UserConfig.config.rate.user * 2;
				await GRPC.GetMember().IncrementCoinAsync(reqCreditBack);
				return null;
			}

			IOApi.UpdateCoin updateCoin = new IOApi.UpdateCoin();
			updateCoin.coin = resCredit.Coin;
			updateCoin.unixTime = resCredit.UnixTime;

			IOApi.UpdateOrderInfo updateOrderInfo = new IOApi.UpdateOrderInfo();
			updateOrderInfo.unixTime = resOrder.UnixTime;
			updateOrderInfo.orderInfo = new IOApi.OrderInfoItem();
			updateOrderInfo.orderInfo.order = ioFunction.order;
			updateOrderInfo.orderInfo.type = TYPE_USER;
			updateOrderInfo.orderInfo.count = resOrder.Sefaresh;
			updateOrderInfo.orderInfo.remaining = resOrder.Mande;
			updateOrderInfo.orderInfo.lastCartId = resOrder.LastCartId;
			updateOrderInfo.orderInfo.status = Order.ORDER_STATUS_NEW;

			IOApi.UpdateOrderHistory updateOrderHistory = new IOApi.UpdateOrderHistory();
			updateOrderHistory.unixTime = resOrder.UnixTime;
			updateOrderHistory.orderHistory = new IOApi.OrderHistoryItem();
			updateOrderHistory.orderHistory.id = resOrder.CartId;
			updateOrderHistory.orderHistory.order = ioFunction.order;
			updateOrderHistory.orderHistory.count = ioFunction.count;
			updateOrderHistory.orderHistory.type = TYPE_USER;
			updateOrderHistory.orderHistory.date = resOrder.UnixTime;
			updateOrderHistory.lastCartId = resOrder.LastCartId;

			updates[0] = updateCoin;
			updates[1] = updateOrderInfo;
			updates[2] = updateOrderHistory;
			UserConfig.SendUpdateToGroup(user.id, user.connectionId, updateCoin, updateOrderInfo, updateOrderHistory);

			return new IOApi.Ok();
		}



		public static async Task<IOObject> OrderLike(UserConfig.User user, IOApi.OrderLike ioFunction, IOApi.Update[] updates) {
			IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.id, Error.CREDIT_LIMIT);
			if (error != null) {
				return error;
			}

			if (ioFunction.order == null || String.IsNullOrEmpty(ioFunction.order.username) || String.IsNullOrEmpty(ioFunction.order.postId) || ioFunction.order.postId.Length > 25) {
				return new IOApi.Error("INVALID_PARAMETERS");
			}

			error = Utils.FormatOrder(ioFunction.order);
			if (error != null) {
				return error;
			}

			if (ioFunction.count < UserConfig.config.order.minLike) {
				return new IOApi.Error("ORDER_COUNT_MIN");
			}

			if (ioFunction.count > UserConfig.config.order.maxLike) {
				return new IOApi.Error("ORDER_COUNT_MAX");
			}


			gRPC_Member.DecrementCoinReq reqCredit = new gRPC_Member.DecrementCoinReq();
			reqCredit.UserId = user.id;
			reqCredit.Count = ioFunction.count * UserConfig.config.rate.like * 2;
			gRPC_Member.DecrementCoinRes resCredit = await GRPC.GetMember().DecrementCoinAsync(reqCredit);
			if (resCredit.Error == "CREDIT_LIMIT") {
				await UserConfig.AddTooManyRequestAsync(user.id, Error.CREDIT_LIMIT);
				return new IOApi.Error("CREDIT_LIMIT");
			}

			gRPC_Followergir.OrderLikeReq reqOrder = new gRPC_Followergir.OrderLikeReq();
			reqOrder.UserId = user.id;
			reqOrder.Order = new gRPC_Followergir.Order();
			reqOrder.Order.Username = ioFunction.order.username;
			reqOrder.Order.PostId = ioFunction.order.postId;
			reqOrder.Order.UserId = ioFunction.order.userId;
			reqOrder.Count = ioFunction.count;
			reqOrder.InitialCount = ioFunction.initialCount;
			reqOrder.Type = ioFunction.type;
			gRPC_Followergir.OrderRes resOrder;
			try {
				resOrder = await IONet.GRPC.GetFollowergir().OrderLikeAsync(reqOrder);
			} catch (Exception) {
				gRPC_Member.IncrementCoinReq reqCreditBack = new gRPC_Member.IncrementCoinReq();
				reqCreditBack.UserId = user.id;
				reqCreditBack.Count = ioFunction.count * UserConfig.config.rate.like * 2;
				await GRPC.GetMember().IncrementCoinAsync(reqCreditBack);
				return null;
			}


			IOApi.UpdateCoin updateCoin = new IOApi.UpdateCoin();
			updateCoin.coin = resCredit.Coin;
			updateCoin.unixTime = resCredit.UnixTime;

			IOApi.UpdateOrderInfo updateOrderInfo = new IOApi.UpdateOrderInfo();
			updateOrderInfo.unixTime = resOrder.UnixTime;
			updateOrderInfo.orderInfo = new IOApi.OrderInfoItem();
			updateOrderInfo.orderInfo.order = ioFunction.order;
			updateOrderInfo.orderInfo.type = TYPE_LIKE;
			updateOrderInfo.orderInfo.count = resOrder.Sefaresh;
			updateOrderInfo.orderInfo.remaining = resOrder.Mande;
			updateOrderInfo.orderInfo.lastCartId = resOrder.LastCartId;
			updateOrderInfo.orderInfo.status = Order.ORDER_STATUS_NEW;

			IOApi.UpdateOrderHistory updateOrderHistory = new IOApi.UpdateOrderHistory();
			updateOrderHistory.unixTime = resOrder.UnixTime;
			updateOrderHistory.orderHistory = new IOApi.OrderHistoryItem();
			updateOrderHistory.orderHistory.id = resOrder.CartId;
			updateOrderHistory.orderHistory.order = ioFunction.order;
			updateOrderHistory.orderHistory.count = ioFunction.count;
			updateOrderHistory.orderHistory.type = TYPE_LIKE;
			updateOrderHistory.orderHistory.date = resOrder.UnixTime;
			updateOrderHistory.lastCartId = resOrder.LastCartId;

			updates[0] = updateCoin;
			updates[1] = updateOrderInfo;
			updates[2] = updateOrderHistory;
			UserConfig.SendUpdateToGroup(user.id, user.connectionId, updateCoin, updateOrderInfo, updateOrderHistory);

			return new IOApi.Ok();
		}



		public static async Task<IOObject> OrderComment(UserConfig.User user, IOApi.OrderComment ioFunction, IOApi.Update[] updates) {
			IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.id, Error.CREDIT_LIMIT);
			if (error != null) {
				return error;
			}

			if (ioFunction.order == null || String.IsNullOrEmpty(ioFunction.order.username) || String.IsNullOrEmpty(ioFunction.order.postId) || ioFunction.order.postId.Length > 25) {
				return new IOApi.Error("INVALID_PARAMETERS");
			}

			error = Utils.FormatOrder(ioFunction.order);
			if (error != null) {
				return error;
			}

			if (ioFunction.comments == null || ioFunction.comments.Count < UserConfig.config.order.minComment) {
				return new IOApi.Error("ORDER_COUNT_MIN");
			}

			if (ioFunction.comments.Count > UserConfig.config.order.maxComment) {
				return new IOApi.Error("ORDER_COUNT_MAX");
			}


			gRPC_Member.DecrementCoinReq reqCredit = new gRPC_Member.DecrementCoinReq();
			reqCredit.UserId = user.id;
			reqCredit.Count = ioFunction.comments.Count * UserConfig.config.rate.comment * 2;
			gRPC_Member.DecrementCoinRes resCredit = await GRPC.GetMember().DecrementCoinAsync(reqCredit);
			if (resCredit.Error == "CREDIT_LIMIT") {
				await UserConfig.AddTooManyRequestAsync(user.id, Error.CREDIT_LIMIT);
				return new IOApi.Error("CREDIT_LIMIT");
			}


			gRPC_Followergir.OrderCommentReq reqOrder = new gRPC_Followergir.OrderCommentReq();
			reqOrder.UserId = user.id;
			reqOrder.Order = new gRPC_Followergir.Order();
			reqOrder.Order.Username = ioFunction.order.username;
			reqOrder.Order.PostId = ioFunction.order.postId;
			reqOrder.Order.UserId = ioFunction.order.userId;
			foreach (string comment in ioFunction.comments) {
				reqOrder.Comments.Add(comment);
			}
			reqOrder.InitialCount = ioFunction.initialCount;
			reqOrder.Type = ioFunction.type;
			gRPC_Followergir.OrderRes resOrder;
			try {
				resOrder = await IONet.GRPC.GetFollowergir().OrderCommentAsync(reqOrder);

			} catch (Exception) {
				gRPC_Member.IncrementCoinReq reqCreditBack = new gRPC_Member.IncrementCoinReq();
				reqCreditBack.UserId = user.id;
				reqCreditBack.Count = ioFunction.comments.Count * UserConfig.config.rate.comment * 2;
				await GRPC.GetMember().IncrementCoinAsync(reqCreditBack);
				return null;
			}

			IOApi.UpdateCoin updateCoin = new IOApi.UpdateCoin();
			updateCoin.coin = resCredit.Coin;
			updateCoin.unixTime = resCredit.UnixTime;

			IOApi.UpdateOrderInfo updateOrderInfo = new IOApi.UpdateOrderInfo();
			updateOrderInfo.unixTime = resOrder.UnixTime;
			updateOrderInfo.orderInfo = new IOApi.OrderInfoItem();
			updateOrderInfo.orderInfo.order = ioFunction.order;
			updateOrderInfo.orderInfo.type = TYPE_COMMENT;
			updateOrderInfo.orderInfo.count = resOrder.Sefaresh;
			updateOrderInfo.orderInfo.remaining = resOrder.Mande;
			updateOrderInfo.orderInfo.lastCartId = resOrder.LastCartId;
			updateOrderInfo.orderInfo.status = Order.ORDER_STATUS_NEW;

			IOApi.UpdateOrderHistory updateOrderHistory = new IOApi.UpdateOrderHistory();
			updateOrderHistory.unixTime = resOrder.UnixTime;
			updateOrderHistory.orderHistory = new IOApi.OrderHistoryItem();
			updateOrderHistory.orderHistory.id = resOrder.CartId;
			updateOrderHistory.orderHistory.order = ioFunction.order;
			updateOrderHistory.orderHistory.count = ioFunction.comments.Count;
			updateOrderHistory.orderHistory.type = TYPE_COMMENT;
			updateOrderHistory.orderHistory.date = resOrder.UnixTime;
			updateOrderHistory.lastCartId = resOrder.LastCartId;


			updates[0] = updateCoin;
			updates[1] = updateOrderInfo;
			updates[2] = updateOrderHistory;
			UserConfig.SendUpdateToGroup(user.id, user.connectionId, updateCoin, updateOrderInfo, updateOrderHistory);

			return new IOApi.Ok();
		}


		public static async Task<IOObject> OrderStart(UserConfig.User user, IOApi.OrderStart ioFunction, IOApi.Update[] updates) {
			IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.id, Error.INVALID_PARAMETERS, Error.CREDIT_LIMIT);
			if (error != null) {
				return error;
			}

			if (ioFunction.order == null || String.IsNullOrEmpty(ioFunction.order.username) ||
				(ioFunction.type != TYPE_USER && ioFunction.type != TYPE_LIKE && ioFunction.type != TYPE_COMMENT) ||
				(ioFunction.type == TYPE_LIKE || ioFunction.type == TYPE_COMMENT) && String.IsNullOrEmpty(ioFunction.order.postId)
				) {
				await UserConfig.AddTooManyRequestAsync(user.id, Error.INVALID_PARAMETERS);
				return new IOApi.Error("INVALID_PARAMETERS");
			}

			error = Utils.FormatOrder(ioFunction.order);
			if (error != null) {
				return error;
			}

			gRPC_Followergir.OrderStartReq req = new gRPC_Followergir.OrderStartReq();
			req.UserId = user.id;
			req.Order = new gRPC_Followergir.Order();
			req.Order.Username = ioFunction.order.username;
			req.Order.PostId = ioFunction.order.postId;
			req.Type = ioFunction.type;
			gRPC_Followergir.OrderRes resStart = await IONet.GRPC.GetFollowergir().OrderStartAsync(req);

			if (!String.IsNullOrEmpty(resStart.Error)) {
				if (resStart.Error == "ORDER_NOT_FOUND") {
					return new IOApi.Error("ORDER_NOT_FOUND");

				} else if (resStart.Error == "CAN_NOT_START_ORDER") {
					return new IOApi.Error("CAN_NOT_START_ORDER");
				}
				return null;
			}


			IOApi.UpdateOrderInfo updateOrder = new IOApi.UpdateOrderInfo();
			updateOrder.orderInfo = new IOApi.OrderInfoItem();
			updateOrder.orderInfo.order = ioFunction.order;
			updateOrder.orderInfo.type = ioFunction.type;
			updateOrder.orderInfo.status = Order.ORDER_STATUS_START;
			updateOrder.unixTime = resStart.UnixTime;

			updates[0] = updateOrder;
			UserConfig.SendUpdateToGroup(user.id, user.connectionId, updateOrder);

			return new IOApi.Ok();
		}


		/*public override async Task StreamingBothWays(IAsyncStreamReader<gRPC_Followergir.ExampleRequest> requestStream, IServerStreamWriter<gRPC_Followergir.ExampleResponse> responseStream, ServerCallContext context) {
			// Read requests in a background task.
			var readTask = Task.Run(async () => {
				await foreach (var message in requestStream.ReadAllAsync()) {
					// Process request.
					Console.WriteLine(message.UserId);
				}
			});

			// Send responses until the client signals that it is complete.
			while (!readTask.IsCompleted) {
				await responseStream.WriteAsync(new gRPC_Followergir.ExampleResponse());
				//await Task.Delay(TimeSpan.FromSeconds(1), context.CancellationToken);
			}
		}*/



		public static async Task<IOObject> OrderCancel(UserConfig.User user, IOApi.OrderCancel ioFunction, IOApi.Update[] updates) {
			IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.id, Error.INVALID_PARAMETERS, Error.CREDIT_LIMIT);
			if (error != null) {
				return error;
			}

			if (ioFunction.order == null || String.IsNullOrEmpty(ioFunction.order.username) ||
				(ioFunction.type != TYPE_USER && ioFunction.type != TYPE_LIKE && ioFunction.type != TYPE_COMMENT) ||
				(ioFunction.type == TYPE_LIKE || ioFunction.type == TYPE_COMMENT) && String.IsNullOrEmpty(ioFunction.order.postId)
				) {
				await UserConfig.AddTooManyRequestAsync(user.id, Error.INVALID_PARAMETERS);
				return new IOApi.Error("INVALID_PARAMETERS");
			}

			error = Utils.FormatOrder(ioFunction.order);
			if (error != null) {
				return error;
			}

			gRPC_Followergir.OrderCancelReq reqCancel = new gRPC_Followergir.OrderCancelReq();
			reqCancel.UserId = user.id;
			reqCancel.Order = new gRPC_Followergir.Order();
			reqCancel.Order.Username = ioFunction.order.username;
			reqCancel.Order.PostId = ioFunction.order.postId;
			reqCancel.Type = ioFunction.type;
			gRPC_Followergir.OrderRes resCancel = await IONet.GRPC.GetFollowergir().OrderCancelAsync(reqCancel);


			if (!String.IsNullOrEmpty(resCancel.Error)) {
				if (resCancel.Error == "ORDER_NOT_FOUND") {
					return new IOApi.Error("ORDER_NOT_FOUND");

				} else if (resCancel.Error == "CAN_NOT_CANCEL_ORDER") {
					return new IOApi.Error("CAN_NOT_CANCEL_ORDER");
				}
				return new IOApi.Error();
			}

			float orderRate = 0;
			if (ioFunction.type == Order.TYPE_USER) {
				orderRate = UserConfig.config.rate.user;
			} else if (ioFunction.type == Order.TYPE_LIKE) {
				orderRate = UserConfig.config.rate.like;
			} else if (ioFunction.type == Order.TYPE_COMMENT) {
				orderRate = UserConfig.config.rate.comment;
			}

			gRPC_Member.IncrementCoinReq reqCredit = new gRPC_Member.IncrementCoinReq();
			reqCredit.UserId = user.id;
			reqCredit.Count = resCancel.Mande * (orderRate * 2) * (1F - UserConfig.config.orderInfo.cancelRate);
			gRPC_Member.IncrementCoinRes resCredit = await GRPC.GetMember().IncrementCoinAsync(reqCredit);

			IOApi.UpdateCoin updateCoin = new IOApi.UpdateCoin();
			updateCoin.coin = resCredit.Coin;
			updateCoin.unixTime = resCredit.UnixTime;

			IOApi.UpdateOrderInfo updateOrder = new IOApi.UpdateOrderInfo();
			updateOrder.orderInfo = new IOApi.OrderInfoItem();
			updateOrder.orderInfo.order = ioFunction.order;
			updateOrder.orderInfo.type = ioFunction.type;
			updateOrder.orderInfo.remaining = resCancel.Mande;
			updateOrder.orderInfo.status = Order.ORDER_STATUS_CANCEL;
			updateOrder.unixTime = resCancel.UnixTime;

			updates[0] = updateCoin;
			updates[1] = updateOrder;
			UserConfig.SendUpdateToGroup(user.id, user.connectionId, updateCoin, updateOrder);

			return new IOApi.Ok();
		}




		public static async Task<IOObject> OrderError(UserConfig.User user, IOApi.OrderError ioFunction, IOApi.Update[] updates) {
			IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.id,
				Error.INVALID_PARAMETERS,
				Error.ORDER_NOT_FOUND,
				Error.ORDER_HAS_VIEWED,
				Error.TYPE_INVALID);

			if (error != null) {
				return error;
			}

			if (ioFunction.iId < 0L || ioFunction.order == null || String.IsNullOrEmpty(ioFunction.order.username) ||
				(ioFunction.type != TYPE_USER && ioFunction.type != TYPE_LIKE && ioFunction.type != TYPE_COMMENT) ||
				(ioFunction.type == TYPE_LIKE || ioFunction.type == TYPE_COMMENT) && String.IsNullOrEmpty(ioFunction.order.postId)
				) {
				await UserConfig.AddTooManyRequestAsync(user.id, Error.INVALID_PARAMETERS);
				return null;
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

			if (await UserConfig.NeedData(user.id, ioFunction.iId)) {
				if (String.IsNullOrEmpty(ioFunction.data)) {
					return new IOApi.Error("DATA_INVALID");
				}

				bool isDataSaved = await UserConfig.SetData(user.id, ioFunction.iId, ioFunction.data);
				if (!isDataSaved) {
					return new IOApi.Error("DATA_INVALID");
				}
			}

			gRPC_Followergir.OrderErrorReq reqError = new gRPC_Followergir.OrderErrorReq();
			reqError.UserId = user.id;
			reqError.IId = ioFunction.iId;
			reqError.Order = new gRPC_Followergir.Order();
			reqError.Order.Username = ioFunction.order.username;
			reqError.Order.PostId = ioFunction.order.postId;
			reqError.Order.UserId = ioFunction.order.userId;
			reqError.UserIdOrderer = userIdOrderer;
			reqError.Type = ioFunction.type;
			reqError.Error = ioFunction.error;
			gRPC_Followergir.OrderErrorRes resError = await IONet.GRPC.GetFollowergir().OrderErrorAsync(reqError);

			if (resError.Error == "ORDER_NOT_FOUND") {
				await UserConfig.AddTooManyRequestAsync(user.id, Error.ORDER_NOT_FOUND);
			} else if (resError.Error == "ORDER_HAS_VIEWED") {
				await UserConfig.AddTooManyRequestAsync(user.id, Error.ORDER_HAS_VIEWED);
			} else if (resError.Error == "TYPE_INVALID") {
				await UserConfig.AddTooManyRequestAsync(user.id, Error.TYPE_INVALID);
			}

			if (resError.IsOrderStop) {
				IOApi.UpdateOrderInfo updateOrder = new IOApi.UpdateOrderInfo();
				updateOrder.orderInfo = new IOApi.OrderInfoItem();
				updateOrder.orderInfo.type = ioFunction.type;
				updateOrder.orderInfo.order = ioFunction.order;
				updateOrder.orderInfo.error = ioFunction.error;
				updateOrder.orderInfo.status = Order.ORDER_STATUS_UPDATE;
				updateOrder.unixTime = resError.UnixTime;
				UserConfig.SendUpdateToGroup(userIdOrderer, null, updateOrder);
			}

			return null;
		}


	}
}
