using Followergir.IO;
using Followergir.IONet;
using Library;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Followergir.Methods {

	public class List : Api {

		public static async Task<IOObject> GetListOrder(UserConfig.User user, IOApi.GetListOrder ioFunction, IOApi.Update[] updates) {
			if (ioFunction.list == null || ioFunction.list.Count == 0 || ioFunction.list.Count > UserConfig.config.add.lengthList) {
				return new IOApi.Error("INVALID_PARAMETERS");
			}

			gRPC_Followergir.GetOrderListReq reqOrderList = new gRPC_Followergir.GetOrderListReq();
			reqOrderList.UserId = user.id;

			foreach (IOApi.ListOrderItem item in ioFunction.list) {
				gRPC_Followergir.OrderListContent content = new gRPC_Followergir.OrderListContent();
				content.IId = item.iId;
				if (item is IOApi.ListOrderUser) {
					content.Type = Order.TYPE_USER;

				} else if (item is IOApi.ListOrderLike) {
					content.Type = Order.TYPE_LIKE;

				} else if (item is IOApi.ListOrderComment) {
					content.Type = Order.TYPE_COMMENT;
				}
				//content.Order.
				reqOrderList.List.Add(content);
			}
			gRPC_Followergir.GetOrderListRes resOrderList = await GRPC.GetFollowergir().GetOrderListAsync(reqOrderList);


			IOApi.ListOrder res = new IOApi.ListOrder();
			res.list = new List<IOApi.ListOrderItem>();

			foreach (gRPC_Followergir.OrderListContent item in resOrderList.List) {
				IOApi.ListOrderItem content;
				if (item.Type == Order.TYPE_USER) {
					content = new IOApi.ListOrderUser();

				} else if (item.Type == Order.TYPE_LIKE) {
					content = new IOApi.ListOrderLike();

				} else if (item.Type == Order.TYPE_COMMENT) {
					content = new IOApi.ListOrderComment();
					((IOApi.ListOrderComment) content).text = item.Text;
				} else {
					continue;
				}

				content.iId = item.IId;
				content.order = new IOApi.Order();
				content.order.username = item.Order.Username;
				content.order.postId = item.Order.PostId;
				//content.order.userId = item.Order.UserId;//get user instagram not support USER_NOT_FOUND


				content.needData = await UserConfig.NeedData(user.id, item.IId);
				content.hash = UserConfig.EncryptOrderHash(item.OrdererUserId, item.IId);

				res.list.Add(content);
			}

			return res;
		}


	}
}
