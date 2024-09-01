using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using Followergir.IONet;

using System.Collections.Concurrent;
using Followergir.IO;
using System.Timers;
using System.Text;
using Library.Json;
using NATS.Client;
using System.Data.SqlClient;

namespace Followergir.Controllers {

	[ApiController]
	[Route("api")]
	public class Api : Controller {

		public Api() {

		}

		[HttpPost("function")]
		public async Task<ActionResult> OnFunction([FromForm] long id, [FromForm] string json, [FromForm] string ip, [FromForm] string password) {
			if (!HttpContext.Connection.RemoteIpAddress.ToString().StartsWith("10.0.0") && !ServerConfig.isHome(HttpContext, password)) {
				return NotFound();
			}

			Message message = SerializedData.DeserializeMessage(json);
			if (message.ioFunction is not IOApi.Function) {
				message.ioFunction = new IOApi.Error("METHOD_NOT_FOUND");
				return Content(SerializedData.SerializeMessage(message), "application/json", Encoding.ASCII);
			}
			MethodInfo methodInfo = typeof(Methods.User).GetMethod(message.ioFunction.GetType().Name, Methods.User.BINDING_FLAGS);
			if (methodInfo == null) {
				message.ioFunction = new IOApi.Error("METHOD_NOT_FOUND");
				return Content(SerializedData.SerializeMessage(message), "application/json", Encoding.ASCII);
			}

			if (WebSocketConnection.isFreeze) {
				message.ioFunction = new IOApi.Error("FREEZE");
				return Content(SerializedData.SerializeMessage(message), "application/json", Encoding.ASCII);
			}

			UserConfig.User user = new UserConfig.User();
			user.id = id;
			user.ip = ip;

			message.updates = new IOApi.Update[5];
			try {
				message.ioFunction = await (Task<IOObject>) methodInfo.Invoke(null, new Object[] { user, message.ioFunction, message.updates });

			} catch (Grpc.Core.RpcException e) {
				message.ioFunction = new IOApi.Error("INTERNAL_ERROR");
				//Console.WriteLine("Grpc Error: " + e);
				WebSocket_Log.Send("Grpc", e.ToString());
			} catch (SqlException e) {
				message.ioFunction = new IOApi.Error("INTERNAL_ERROR");
				//Console.WriteLine("SQL Error: " + e);
				WebSocket_Log.Send("SQL", e.ToString());

			} catch (NATSException e) {
				message.ioFunction = new IOApi.Error("INTERNAL_ERROR");
				//Console.WriteLine("SQL Error: " + e);
				WebSocket_Log.Send("NATs", e.ToString());

			} catch (Exception e) {
				message.ioFunction = new IOApi.Error("INTERNAL_ERROR");
				// Console.WriteLine("Application Error: " + e);
				WebSocket_Log.Send("Global", e.ToString());
			}

			return Content(SerializedData.SerializeMessage(message), "application/json", Encoding.ASCII);
		}



		[HttpGet("get_credit")]
		public async Task<ActionResult> GetCredit([FromQuery] long id, [FromQuery] string password) {
			if (!HttpContext.Connection.RemoteIpAddress.ToString().StartsWith("10.0.0") && !ServerConfig.isHome(HttpContext, password)) {
				return NotFound();
			}
			gRPC_Member.GetUserFieldReq reqCredit = new gRPC_Member.GetUserFieldReq();
			reqCredit.UserId = id;
			reqCredit.Keys.Add(Redis.COIN);
			gRPC_Member.GetUserFieldRes resCredit = await GRPC.GetMember().GetUserFieldAsync(reqCredit);
			return Content("{\"status\": \"ok\", \"coin\": " + resCredit.Values[0] + "}", "application/json", Encoding.ASCII);
		}

		[HttpGet("get_order")]
		public async Task<ActionResult> GetOrder([FromQuery] long id, [FromQuery] string username, [FromQuery] string post_id, [FromQuery] byte type, [FromQuery] string ip, [FromQuery] string password) {
			if (!HttpContext.Connection.RemoteIpAddress.ToString().StartsWith("10.0.0") && !ServerConfig.isHome(HttpContext, password)) {
				return NotFound();
			}
			UserConfig.User user = new UserConfig.User();
			user.id = id;
			user.ip = ip;

			gRPC_Followergir.GetOrderReq reqOrder = new gRPC_Followergir.GetOrderReq();
			reqOrder.UserId = id;
			reqOrder.Order = new gRPC_Followergir.Order();
			reqOrder.Order.Username = username;
			reqOrder.Order.PostId = post_id;
			reqOrder.Type = type;
			gRPC_Followergir.GetOrderRes resOrder = await GRPC.GetFollowergir().GetOrderAsync(reqOrder);

			if (resOrder.Sefaresh == 0) {
				return Content("{\"status\": \"ok\", \"error\": \"ORDER_NOT_FOUND\"}", "application/json", Encoding.ASCII);
			}

			return Content("{\"status\": \"ok\", \"order_count\": " + resOrder.Sefaresh + ", \"remaining\": " + resOrder.Mande + "}", "application/json", Encoding.ASCII);
		}



	}
}
