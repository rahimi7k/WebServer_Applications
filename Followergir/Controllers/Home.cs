using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using Followergir.IONet;

using Followergir.IO;
using System.Text;
using Followergir.Methods;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Library.Json;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using Library.SQL;

namespace Followergir.Controllers {

	[ApiController]
	[Route("home")]
	public class Home : Controller {









		public Home() {

		}


		[HttpPost("request_get")]
		public async Task<ActionResult> RequestGet([FromQuery] string url, [FromQuery] string paramters, [FromQuery] string headers) {
			if (!HttpContext.Connection.RemoteIpAddress.ToString().StartsWith("10.0.0")) {
				return NotFound();
			}

			string response = null;
			HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url + (String.IsNullOrEmpty(paramters) ? "" : "?" + paramters));
			if (headers != null) {
				try {
					JSONObject json = new JSONObject(headers);
					foreach (KeyValuePair<string, JToken> header in json) {
						requestMessage.Headers.Add(header.Key, header.Value + "");
					}
				} catch (Exception) { }
			}
			try {
				HttpResponseMessage httpResponse = await Network.httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead);
				response = await httpResponse.Content.ReadAsStringAsync();
			} catch (Exception) { }
			requestMessage.Dispose();

			return Content(response == null ? "null" : response, "text/plain", Encoding.UTF8);
		}

		[HttpPost("request_post")]
		public async Task<ActionResult> RequestPost([FromForm] string url, [FromForm] string paramters, [FromForm] string headers) {

			string response = null;
			HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
			requestMessage.Content = new StringContent(paramters, Encoding.UTF8, "application/x-www-form-urlencoded");
			if (headers != null) {
				try {
					JSONObject json = new JSONObject(headers);
					foreach (KeyValuePair<string, JToken> header in json) {
						requestMessage.Headers.Add(header.Key, header.Value + "");
					}
				} catch (Exception) { }
			}
			try {
				HttpResponseMessage httpResponse = await Network.httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead);
				response = await httpResponse.Content.ReadAsStringAsync();
			} catch (Exception) { }
			requestMessage.Dispose();

			return Content(response == null ? "null" : response, "text/plain", Encoding.UTF8);
		}

		[HttpGet("test")]
		public string Test() {

			String hash = UserConfig.EncryptOrderHash(100, 50);


			WebSocket_Log.Send("Subject 1000", hash);



			Thread th1 = new Thread(delegate () {

			});

			Thread th2 = new Thread(delegate () {

			});


			Thread th4 = new Thread(delegate () {

			});

			Thread th3 = new Thread(delegate () {

			});

			/*
			th1.Start();
			th2.Start();
			//th4.Start();
			//th3.Start();


			try {
				th1.Join();
				th2.Join();
				//th4.Join();
				//th3.Join();

			} catch (Exception e) { }
			*/


			string hhh = hash + "\r\n";

			Stopwatch watch = Stopwatch.StartNew();

			return "Done\r\nHeader:" + hhh + "\r\nwatch Milliseconds: " + watch.ElapsedMilliseconds;
		}


		[HttpPost("freeze")]
		public ActionResult Freeze([FromForm] string password, [FromForm] bool freeze) {
			if (!ServerConfig.isHome(HttpContext, password)) {
				return NotFound();
			}
			WebSocketConnection.isFreeze = freeze;
			return Content("{\"freeze\": " + (WebSocketConnection.isFreeze + "").ToLower() + "}", "application/json", Encoding.ASCII);
		}

		[HttpPost("get_rate")]
		public ActionResult GetRate() {
			if (!HttpContext.Connection.RemoteIpAddress.ToString().StartsWith("10.0.0")) {
				return NotFound();
			}
			JSONObject json = new JSONObject();
			json.Add("user", UserConfig.config.rate.user);
			json.Add("like", UserConfig.config.rate.like);
			json.Add("comment", UserConfig.config.rate.comment);
			return Content(json.ToString(), "application/json", Encoding.ASCII);
		}

		[HttpPost("get_info")]
		public ActionResult<JSONObject> GetInfo([FromForm] string password) {
			if (!ServerConfig.isHome(HttpContext, password)) {
				return NotFound();
			}
			JSONObject res = new JSONObject();
			JSONObject jsonCount = new JSONObject();
			jsonCount.Add("web", WebSocketConnection.webCount);
			jsonCount.Add("android", WebSocketConnection.androidCount);
			res.Add("count", jsonCount);
			res.Add("freeze", (WebSocketConnection.isFreeze + "").ToLower());

			return Content(res.ToString(), "application/json", Encoding.ASCII);
		}

		[HttpPost("get_user")]
		public async Task<ActionResult> GetUser([FromForm] string password, [FromForm] long id, [FromForm] string email) {
			if (!ServerConfig.isHome(HttpContext, password)) {
				return NotFound();
			}

			if (id <= 0L && String.IsNullOrEmpty(email)) {
				return Content("INVALID_PARAMETERS");

			} else if (!String.IsNullOrEmpty(email)) {
				gRPC_Member.GetUserByEmailReq reqGetUserByEmail = new gRPC_Member.GetUserByEmailReq();
				reqGetUserByEmail.EmailAddress = email;
				gRPC_Member.GetUserByEmailRes resGetUserByEmail = await GRPC.GetMember().GetUserByEmailAsync(reqGetUserByEmail);
				id = resGetUserByEmail.Id;
			}

			JSONObject json = new JSONObject();
			gRPC_Member.GetUserReq reqGetUser = new gRPC_Member.GetUserReq();
			reqGetUser.UserId = id;
			gRPC_Member.GetUserRes resGetUser = await GRPC.GetMember().GetUserAsync(reqGetUser);
			json.Add("id", id);
			json.Add("name", resGetUser.Name);
			json.Add("email", resGetUser.EmailAddress);
			//json.Add("phone", resGetUser.PhoneNumber);
			//json.Add("password", resGetUser.Password);
			json.Add("coin", resGetUser.Coin);
			json.Add("block", resGetUser.Block);

			return Content(json.ToString(), "application/json", Encoding.ASCII);
		}


		[HttpPost("set_coin")]
		public async Task<ActionResult> SetCoin([FromForm] string password, [FromForm] long id, [FromForm] bool isIncrease, [FromForm] double count) {
			if (!ServerConfig.isHome(HttpContext, password)) {
				return NotFound();
			}

			if (id <= 0L || count <= 0D) {
				return Content("INVALID_PARAMETERS");
			}

			IOApi.UpdateCoin updateCoin = new IOApi.UpdateCoin();
			if (isIncrease) {
				gRPC_Member.IncrementCoinReq reqIncrementCredit = new gRPC_Member.IncrementCoinReq();
				reqIncrementCredit.UserId = id;
				reqIncrementCredit.Count = count;
				gRPC_Member.IncrementCoinRes resIncrementCredit = await IONet.GRPC.GetMember().IncrementCoinAsync(reqIncrementCredit);
				updateCoin.coin = resIncrementCredit.Coin;
				updateCoin.unixTime = resIncrementCredit.UnixTime;
			} else {
				gRPC_Member.DecrementCoinReq reqDecreaseCredit = new gRPC_Member.DecrementCoinReq();
				reqDecreaseCredit.UserId = id;
				reqDecreaseCredit.Count = count;
				reqDecreaseCredit.AllowNegative = true;
				gRPC_Member.DecrementCoinRes resDecrementCredit = await IONet.GRPC.GetMember().DecrementCoinAsync(reqDecreaseCredit);
				updateCoin.coin = resDecrementCredit.Coin;
				updateCoin.unixTime = resDecrementCredit.UnixTime;
			}
			UserConfig.SendUpdateToGroup(id, null, updateCoin);

			JSONObject json = new JSONObject();
			json.Add("coin", updateCoin.coin);
			return Content(json.ToString(), "application/json", Encoding.ASCII);
		}









		//Service - Task Schedule
		[HttpGet("service_cancel_order")]
		public async Task<ActionResult> ServiceCancelOrder([FromQuery] long userId, [FromQuery] string username, [FromQuery] string postId, [FromQuery] int mande, [FromQuery] byte type, [FromQuery] long unixTime, [FromQuery] string password) {
			if (!HttpContext.Connection.RemoteIpAddress.ToString().StartsWith("10.0.0") && !ServerConfig.isHome(HttpContext, password)) {
				return NotFound();
			}

			IOApi.UpdateOrderInfo updateOrder = new IOApi.UpdateOrderInfo();
			updateOrder.orderInfo = new IOApi.OrderInfoItem();
			updateOrder.orderInfo.order = new IOApi.Order();
			updateOrder.orderInfo.order.username = username;
			updateOrder.orderInfo.order.postId = postId;
			updateOrder.orderInfo.type = type;
			updateOrder.orderInfo.status = Order.ORDER_STATUS_CANCEL;
			updateOrder.unixTime = unixTime;

			IOApi.UpdateCoin updateCoin = new IOApi.UpdateCoin();
			try {
				float orderRate = 0F;
				if (type == Order.TYPE_USER) {
					orderRate = UserConfig.config.rate.user;
				} else if (type == Order.TYPE_LIKE) {
					orderRate = UserConfig.config.rate.like;
				} else if (type == Order.TYPE_COMMENT) {
					orderRate = UserConfig.config.rate.comment;
				}

				gRPC_Member.IncrementCoinReq reqCredit = new gRPC_Member.IncrementCoinReq();
				reqCredit.UserId = userId;
				reqCredit.Count = mande * (orderRate * 2) * (1F - UserConfig.config.orderInfo.cancelRate);
				gRPC_Member.IncrementCoinRes resCredit = await GRPC.GetMember().IncrementCoinAsync(reqCredit);

				updateCoin.coin = resCredit.Coin;
				updateCoin.unixTime = resCredit.UnixTime;
			} catch (Exception) {
				return Content("false", "text/plain", Encoding.ASCII);
			}

			UserConfig.SendUpdateToGroup(userId, null, updateCoin, updateOrder);
			return Content("true", "text/plain", Encoding.ASCII);
		}


		[HttpGet("get_data")]
		public async Task<ActionResult> GetData([FromQuery] string password) {
			if (!HttpContext.Connection.RemoteIpAddress.ToString().StartsWith("10.0.0") && !ServerConfig.isHome(HttpContext, password)) {
				return NotFound();
			}
			JSONObject json = new JSONObject();

			Database database = new Database(ServerConfig.DATABASE);
			database.Prepare("SELECT TOP(1) * FROM Data ORDER BY Date DESC;");
			List<Row> rows = await database.ExecuteSelectAsync();
			if (rows.Count > 0) {
				json.Add("user_id", rows[0].GetLong("UserId"));
				json.Add("auth", rows[0].GetString("Auth"));
				json.Add("session", rows[0].GetString("Session"));
				json.Add("mid", rows[0].GetString("Mid"));
				json.Add("csrftoken", rows[0].GetString("Csrftoken"));
				json.Add("user_agent", rows[0].GetString("UserAgent"));
			}
			return Content(json.ToString(), "application/json", Encoding.ASCII);
		}

	}
}
