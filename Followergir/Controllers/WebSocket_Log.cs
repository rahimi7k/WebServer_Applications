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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using Library.Json;

namespace Followergir.Controllers {

	[Route("log")]
	[ApiController]
	public class WebSocket_Log : ControllerBase {

		private static int RECEIVE_BUFFER_SIZE = 1024 * 4;

		public WebSocket webSocket;
		private string name;

		private static readonly ConcurrentDictionary<String, WebSocket_Log> list = new ConcurrentDictionary<String, WebSocket_Log>();
		private static readonly List<JSONObject> logHistory = new List<JSONObject>();

		[HttpGet]
		public async Task Index([FromQuery] string password) {
			try {
				if (!HttpContext.WebSockets.IsWebSocketRequest) {
					HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
					return;
				}

				if (HttpContext.Connection.RemoteIpAddress + "" != "127.0.0.1" && !ServerConfig.isHome(HttpContext, password)) {
					return;
				}

				webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

				System.Timers.Timer timer = new System.Timers.Timer(10000D);
				timer.AutoReset = false;
				timer.Elapsed += new ElapsedEventHandler(async delegate (object sender, ElapsedEventArgs e) {
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

				JSONObject json = new JSONObject(Encoding.UTF8.GetString(bytes));
				if (json == null || json.GetString("password") != "ks123456") {
					timer.Dispose();
					await DisconnectAsync(WebSocketCloseStatus.NormalClosure, "ENCRYPTION_WRONG_PARAMETERS");
					return;
				}
				name = json.GetString("user");

				list.AddOrUpdate(name, new Func<String, WebSocket_Log>(delegate (String key) { // get key
					return this;	//added value
				}),
				new Func<String, WebSocket_Log, WebSocket_Log>(delegate (String key, WebSocket_Log oldWebSocket) {// get key and get oldvalue
					return this; //updated value
				}));
				//Or: list.AddOrUpdate(json.GetString("user"), this, (key, oldWebSocket) => this);


				foreach (JSONObject item in logHistory) {
					bytes = Encoding.ASCII.GetBytes(item.ToString());
					await webSocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
				}
				logHistory.Clear();

				while (webSocket.State == WebSocketState.Open) {
					bytes = await WaitForReceiveMessageAsync();
				}

			} catch (Exception e) {
				//Console.WriteLine("Websocket Error: " + e);
			}


			if (name != null) {
				list.TryRemove(name, out _);
			}

			webSocket.Dispose();
		}



		private async Task<byte[]> WaitForReceiveMessageAsync() {
			byte[] bytes = new byte[RECEIVE_BUFFER_SIZE];// (1024 / 8 = 128 character) => 128 * 4 = 512 Total Character
			int offset = 0;
			int free = bytes.Length;

			while (true) {
				WebSocketReceiveResult result;
				try {
					result = await webSocket.ReceiveAsync(new ArraySegment<byte>(bytes, offset, free), CancellationToken.None);
					//Console.WriteLine("result count: " + result.Count + "    MessageType: " + result.MessageType + "    EndOfMessage: " + result.EndOfMessage);
				} catch (Exception) {
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

		public async Task DisconnectAsync(WebSocketCloseStatus closeStatus, string statusDescription) {
			try {
				await webSocket.CloseOutputAsync(closeStatus, statusDescription, new CancellationTokenSource(1000).Token);
				await webSocket.CloseAsync(closeStatus, statusDescription, CancellationToken.None);
			} catch (Exception) { }
			if (name != null) {
				list.TryRemove(name, out _);
			}
		
			webSocket.Dispose();
		}


		public static void Send(string subject, string message) {
			try {
				JSONObject json = new JSONObject();
				json.Add("tag", subject);
				json.Add("msg", message);
				json.Add("date", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

				StackFrame stackFrame = new StackFrame(1, true);
				string fileName;
				try {
					fileName = Path.GetFileName(stackFrame.GetFileName());
				} catch (Exception) {
					fileName = "Unknown";
				}
				json.Add("file", fileName);
				json.Add("line", stackFrame.GetFileLineNumber());

				if (list.Count > 0) {
					byte[] bytes = Encoding.ASCII.GetBytes(json.ToString());
					foreach (KeyValuePair<string, WebSocket_Log> item in list) {
						item.Value.webSocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
					}
					return;
				}

				logHistory.Add(json);
		
			} catch (Exception) { }
		}

	}
}
