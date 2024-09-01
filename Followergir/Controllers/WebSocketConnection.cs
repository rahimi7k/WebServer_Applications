using Followergir.Methods;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using Followergir.IO;
using System.Timers;
using Followergir.IONet;
using NATS.Client;
using System.Data.SqlClient;
using System.Linq;
using StackExchange.Redis;
using Library;

namespace Followergir.Controllers {

	[Route("")]
	[ApiController]
	public class WebSocketConnection : ControllerBase, IDisposable {

		// 1024(byte) = 1(kb)

		private static int RECEIVE_BUFFER_SIZE = 1024 * 4; //(byte) -> 4096(byte) or 4kb -- 32,768(bit) or 32(kbit)
		public static bool isFreeze = false;

		public static int webCount = 0;
		public static int androidCount = 0;

		//private static CancellationTokenSource PING_CANCELLATION_TOKEN = new CancellationTokenSource(60000); // 5 Minutes, 5 * 60 * 1000



		public readonly UserConfig.User user = new UserConfig.User();

		public WebSocket webSocket;
		private IAsyncSubscription updateSubscribe;
		private byte[] aesKey, aesIv;

		[HttpGet]
		public async Task Index() {
			try {
				if (!HttpContext.WebSockets.IsWebSocketRequest) {
					HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
					return;
				}

				user.ip = HttpContext.Request.Headers["X-Forwarded-For"];
				if (String.IsNullOrEmpty(user.ip)) {
					user.ip = HttpContext.Connection.RemoteIpAddress + "";
				}

				user.connectionId = Guid.NewGuid().ToString();

				webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

				IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.ip, Error.UNAUTHORIZED);
				if (error != null) {
					await DisconnectAsync(WebSocketCloseStatus.NormalClosure, error.message);
					return;
				}

				System.Timers.Timer timer = new System.Timers.Timer(10000D);
				timer.AutoReset = false;
				timer.Elapsed += new ElapsedEventHandler(async delegate (object sender, ElapsedEventArgs e) {
					Log.E("Timer Disconnect()");
					await DisconnectAsync(WebSocketCloseStatus.NormalClosure, "ESTABLISH_CONNECTION_NOT_COMPLETED");
				});

				timer.Start();
				byte[] bytes = await WaitForReceiveMessageAsync();
				timer.Stop();

				if (bytes == null) {
					timer.Dispose();
					await DisconnectAsync(WebSocketCloseStatus.NormalClosure, "ENCRYPTION_IS_NULL");
					return;
				}

				aesKey = new byte[16];
				for (int i = 0; i < aesKey.Length; i++) {
					aesKey[i] = bytes[i];
				}

				byte[] reqBytes = new byte[bytes.Length - 16];
				for (int i = 0; i < reqBytes.Length; i++) {
					reqBytes[i] = bytes[i + 16];
				}

				aesIv = new byte[16];

				IOApi.Initial initial = (IOApi.Initial) SerializedData.DeserializeObject(reqBytes, aesKey, aesIv);
				if (initial is not IOApi.Initial ||
					String.IsNullOrEmpty(initial.os) || String.IsNullOrEmpty(initial.language) || initial.version <= 0) {
					timer.Dispose();
					await DisconnectAsync(WebSocketCloseStatus.NormalClosure, "ENCRYPTION_WRONG_PARAMETERS");
					return;
				}
				user.operatingSystem = initial.os;
				user.language = initial.language;
				user.version = initial.version;

				StringUtils.RandomBytes(aesIv);
				bytes = AESCrypt.Encrypt("{\"status\":\"ok\"}", aesKey, aesIv);
				bytes = aesIv.Concat(bytes).ToArray();
				await webSocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Binary, true, CancellationToken.None);


				if (isFreeze) {
					await DisconnectAsync(WebSocketCloseStatus.NormalClosure, "FREEZE");
					return;
				}

				Message message = null;
				if (initial.enableAuthentication) {
					//while (!result.CloseStatus.HasValue) {
					while (webSocket.State == WebSocketState.Open) {
						bytes = await WaitForReceiveMessageAsync();
						if (bytes == null || bytes.Length == 1) { // Length 1 for Ping
							continue;
						}

						message = SerializedData.DeserializeMessage(bytes, aesKey, aesIv);
						await OnAuthentication(message);
						if (user.id != 0) {//-- AuthorizationStateReady fill user.id and user.session
							break;
						}
					}

					if (webSocket.State != WebSocketState.Open) {
						timer.Dispose();
						return;
					}
				}

				timer.Start();
				bytes = await WaitForReceiveMessageAsync();
				timer.Dispose();

				message = SerializedData.DeserializeMessage(bytes, aesKey, aesIv);

				IOApi.GetMe getMe = (IOApi.GetMe) message.ioFunction;
				if (getMe == null || getMe.userId <= 0L || String.IsNullOrEmpty(getMe.session)) {
					await UserConfig.AddTooManyRequestAsync(user.ip, Error.BAD_REQUEST);
					await DisconnectAsync(WebSocketCloseStatus.NormalClosure, "BAD_REQUEST");
					return;
				}

				gRPC_Member.GetUserReq reqGetUser = new gRPC_Member.GetUserReq();
				reqGetUser.UserId = getMe.userId;
				gRPC_Member.GetUserRes resGetUser;
				try {
					resGetUser = await GRPC.GetMember().GetUserAsync(reqGetUser);
				} catch (Exception) {
					await DisconnectAsync(WebSocketCloseStatus.NormalClosure, "USER_NOT_LOADED");
					return;
				}

				if (resGetUser.Error == "USER_NOT_FOUND") {
					await UserConfig.AddTooManyRequestAsync(user.ip, Error.USER_NOT_FOUND);
					await DisconnectAsync(WebSocketCloseStatus.NormalClosure, "USER_NOT_FOUND");
					return;
				}

				if (resGetUser.Session != getMe.session) {
					await UserConfig.AddTooManyRequestAsync(user.ip, Error.UNAUTHORIZED);
					await DisconnectAsync(WebSocketCloseStatus.NormalClosure, "UNAUTHORIZED");
					return;
				}

				if (resGetUser.Block) {
					await DisconnectAsync(WebSocketCloseStatus.NormalClosure, "USER_BLOCK");
					return;
				}

				user.id = getMe.userId;


				if (!await UserConfig.addConnection(user)) {
					await UserConfig.AddTooManyRequestAsync(user.ip, Error.MAX_CONNECTION);
					await DisconnectAsync(WebSocketCloseStatus.NormalClosure, "MAX_CONNECTION");
					return;
				}

				IOApi.SyncApp res = new IOApi.SyncApp();
				res.user = new IOApi.UserFull();
				res.user.name = resGetUser.Name;
				res.user.phoneNumber = "";
				res.user.emailAddress = resGetUser.EmailAddress;
				res.user.coin = resGetUser.Coin;

				res.config = UserConfig.config;
				res.listMessage = new List<IOApi.Message>();
				if (user.language == "fa") {
					res.listOffer = Methods.Store.offerIR;
					res.listMessage = UserConfig.messageFa;
				} else {
					res.listOffer = Methods.Store.offerEN;
					res.listMessage = UserConfig.messageEn;
				}

				message.ioFunction = res;
				await SendMessage(message);


		

				updateSubscribe = Nats.connectionUpdate.SubscribeAsync(user.id + "", new EventHandler<MsgHandlerEventArgs>(async delegate (object sender, MsgHandlerEventArgs msg) {

					//Console.WriteLine("OnUpdate(UserId = " + user.id + "): " + msg.Message);

					if (msg.Message.Reply == user.connectionId) {
						return;
					}

					if (msg.Message.Data.Length == 1 && msg.Message.Data[0] == 0x01) { //LOGOUT
						await DisconnectAsync(WebSocketCloseStatus.NormalClosure, "PASSWORD_CHANGE");
						return;
					}

					byte[] bytes = SerializedData.SerializeUpdate(msg.Message.Data, aesKey, aesIv);
					await SendBytes(bytes);
				}));


				/*
				if (user has insta account && proxy is connect) {
				Nats.connectionAction.SubscribeAsync("android_1", "Action", new EventHandler<MsgHandlerEventArgs>(delegate (object sender, MsgHandlerEventArgs msg) {

					Console.WriteLine("OnUpdate(UserId = " + user.id + "): " + msg.Message);

				}));
				}
				*/


				UserConfig.CheckUnFollowAndUnGem(user.id, user.language);


				//while (!result.CloseStatus.HasValue) {
				while (webSocket.State == WebSocketState.Open) {
					bytes = await WaitForReceiveMessageAsync();
					if (bytes == null || bytes.Length == 1) { // Length 1 for Ping
						continue;
					}

					message = SerializedData.DeserializeMessage(bytes, aesKey, aesIv);
					if (message.identifierFunction > 0) {
						await OnFunction(message);

					} else if (!String.IsNullOrEmpty(message.identifierAction)) {
						await OnAction(message);
					} else {

					}
				}

			} catch (Exception e) {
				//Console.WriteLine("Websocket Error: " + e);
				WebSocket_Log.Send("Websocket", e.ToString());
			}

			if (updateSubscribe != null) {
				updateSubscribe.Unsubscribe();
				// subscriptionUpdate.Drain();
				updateSubscribe.Dispose();
			}

			await UserConfig.removeConnection(user);
			webSocket.Dispose();
		}


		private async Task OnAuthentication(Message message) {
			if (message.ioFunction is not IOApi.Function) {
				message.ioFunction = new IOApi.Error("FUNCTION_NOT_FOUND");
				await SendMessage(message);
				return;
			}

			MethodInfo methodInfo = typeof(Methods.Authentication).GetMethod(message.ioFunction.GetType().Name, Methods.Authentication.BINDING_FLAGS);
			if (methodInfo == null) {
				await UserConfig.AddTooManyRequestAsync(user.ip, Error.FUNCTION_NOT_FOUND);
				message.ioFunction = new IOApi.Error("FUNCTION_NOT_FOUND");
				await SendMessage(message);
				return;
			}

			if (isFreeze) {
				message.ioFunction = new IOApi.Error("FREEZE");
				await SendMessage(message);
				return;
			}

			try {
				message.ioFunction = await (Task<IOObject>) methodInfo.Invoke(null, new Object[] { user, message.ioFunction });
			} catch (Exception e) {
				message.ioFunction = new IOApi.Error("INTERNAL_ERROR");
				WebSocket_Log.Send("OnAuthentication", e.ToString());
			}
			/*if (message.ioFunction is IOApi.AuthorizationStateReady) {
				var ioFunction = (IOApi.AuthorizationStateReady) message.ioFunction;
				user.id = ioFunction.userId;
				user.session = ioFunction.session;
			}*/
			await SendMessage(message);
		}



		private async Task OnFunction(Message message) {
			if (message.ioFunction is not IOApi.Function) {
				message.ioFunction = new IOApi.Error("METHOD_NOT_FOUND");
				await SendMessage(message);
				return;
			}
			MethodInfo methodInfo = typeof(Methods.User).GetMethod(message.ioFunction.GetType().Name, Methods.User.BINDING_FLAGS);
			if (methodInfo == null) {
				message.ioFunction = new IOApi.Error("METHOD_NOT_FOUND");
				await SendMessage(message);
				return;
			}

			if (isFreeze) {
				message.ioFunction = new IOApi.Error("FREEZE");
				await SendMessage(message);
				return;
			}


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

			await SendMessage(message);
		}






		private async Task OnAction(Message message) {
			if (message.ioAction is IOApi.ActionResult) {
				IOApi.ActionResult action = (IOApi.ActionResult) message.ioAction;
				MethodInfo methodInfo = typeof(Methods.Action).GetMethod(message.ioAction.GetType().Name, Methods.Action.BINDING_FLAGS);
				if (methodInfo == null) {
					message.ioAction = new IOApi.Error("METHOD_NOT_FOUND");
				} else {
					try {
						await (Task) methodInfo.Invoke(null, new Object[] { user, action });
					} catch (Exception e) when (e is Grpc.Core.RpcException || e is Exception) { }
				}
			} else {
				message.ioAction = new IOApi.Error("METHOD_NOT_FOUND");
			}

			new Thread(delegate () {

			}).Start();
		}

		private async Task<byte[]> WaitForReceiveMessageAsync() {
			byte[] bytes = new byte[RECEIVE_BUFFER_SIZE];
			int offset = 0;
			int free = bytes.Length;

			while (true) {
				WebSocketReceiveResult result = null;
				try {
					// 5 minutes = 5 * 60 * 1000 = 300,000 milisecond
					result = await webSocket.ReceiveAsync(new ArraySegment<byte>(bytes, offset, free), new CancellationTokenSource(300000).Token);
				} catch (Exception) {}


				if (result == null) {
					return null;
				}

				offset += result.Count;
				free -= result.Count;
				if (result.EndOfMessage) {
					break;
				}
				if (free == 0) {
					// No free space - Resize the outgoing buffer
					var newSize = bytes.Length + RECEIVE_BUFFER_SIZE;
					if (newSize > 4096000) {// Check if the new size exceeds a limit
						throw new Exception("Maximum size exceeded");
					}
					byte[] newBytes = new byte[newSize];
					Array.Copy(bytes, 0, newBytes, 0, offset);
					bytes = newBytes;
					free = bytes.Length - offset;
				}
			}

			byte[] finalBytes = new byte[offset];
			for (int i = 0; i < offset; i++) {
				finalBytes[i] = bytes[i];
			}
			return finalBytes;
		}



		private async Task SendMessage(Message message) {
			byte[] bytes = SerializedData.SerializeMessage(message, aesKey, aesIv);
			await SendBytes(bytes);
		}


		private async Task SendBytes(byte[] bytes) {
			try {
				await webSocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Binary, true, CancellationToken.None);
			} catch (Exception) { }
		}


		private async Task DisconnectAsync(WebSocketCloseStatus closeStatus, string statusDescription) {
			try {
				//await webSocket.CloseOutputAsync(closeStatus, statusDescription, new CancellationTokenSource(1000).Token);
				await webSocket.CloseAsync(closeStatus, statusDescription, CancellationToken.None);
			} catch (Exception) { }
			webSocket.Dispose();
		}

		public void Dispose() {

		}


	}
}
