using Microsoft.AspNetCore.Mvc;
using Program;
using System;
using System.Threading.Tasks;
using System.Text;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;

namespace WebServer.Controllers {

	[ApiController]
	[Route("home")]
	public class Home : Controller {

		private string[] availableServer = App.Configuration().GetSection("AvailableServer").Get<string[]>();

		[HttpGet]
		[HttpPost]
		public async Task<ActionResult> Index([FromForm(Name = "user_id")] long userId) {

			


			// await WebSoketConnection.webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);




			RedisValue serverId;
			IDatabase redisUser = Redis.GetDatabaseUser();

			if (userId > 0) {
	
				serverId = await redisUser.StringGetAsync(userId + "");
				if (serverId.IsNullOrEmpty) {

					
				}

			} else {
				serverId = availableServer[0];
				//serverId = availableServer[StringUtils.Random(0, availableServer.Length - 1)];
			}
			if (serverId.IsNullOrEmpty) {
				serverId = 0;
			}
			return Content("{\"process_id\":" + serverId + "}", "application/json", Encoding.ASCII);
		}

		[HttpGet("connected")]
		[HttpPost("connected")]
		public async Task<ActionResult> Connected([FromForm(Name = "server_id")] string serverId, [FromForm(Name = "user_id")] long userId) {
			Log.E("GetIp()New:" + GetIp());
			if (!GetIp().StartsWith("10.0.0.")) {
				Log.SendEmail("ProceesServerId is not local range", "Ip: " + GetIp() + "\r\nUserId: " + userId);
				return NoContent();
			}

			IDatabase redisUser = Redis.GetDatabaseUser();


			bool isDone = false;

			/*using (IRedLock redLock = await Redis.redlockFactory.CreateLockAsync(userId + "", Redis.LOCK_EXPIRY, Redis.LOCK_WAIT, Redis.LOCK_RETRY)) {
				if (!redLock.IsAcquired) {
					Log.E("Redis Lock Error Server Can not Lock Redis\r\nServer: " + Environment.MachineName);
					Log.SendEmail("Redis Lock Error", "Server Can not Lock Redis\r\nServer: " + Environment.MachineName);
					return Content("{\"ok\":false}", "application/json", Encoding.ASCII);
				}*/

			RedisValue redisValue = await redisUser.StringGetAsync(userId + "");
			Log.E("redisValue: " + redisValue);
			if (redisValue.IsNullOrEmpty) {

				

			} else {
				if (redisValue != serverId) {
					Log.E("User is in 2 Server From redisUser - redisValue is not match with serverId \r\nSecondServerId: " + serverId + "\r\nUserId: " + userId);
					Log.SendEmail("User is in 2 Server", "From redisUser - redisValue is not match with serverId \r\nSecondServerId: " + serverId + "\r\nUserId: " + userId);
				} else {
					isDone = true;
				}
				//}
			}

			return Content("{\"ok\":" + (isDone ? "true" : "false") + "}", "application/json", Encoding.ASCII);
		}



		[HttpPost("disconnected")]
		public async Task Disconnected([FromForm(Name = "user_id")] long userId) {
			if (!GetIp().StartsWith("10.0.0.")) {
				return;
			}

			/*using (IRedLock redLock = await Redis.redlockFactory.CreateLockAsync(userId + "", Redis.LOCK_EXPIRY, Redis.LOCK_WAIT, Redis.LOCK_RETRY)) {
				if (!redLock.IsAcquired) {
					Log.SendEmail("Redis Lock Error", "Server Can not Lock Redis\r\nServer: " + Environment.MachineName);
					return;
				}*/

			StackExchange.Redis.IDatabase redisUser = Redis.GetDatabaseUser();
			await redisUser.KeyExpireAsync(userId + "", TimeSpan.FromSeconds(600D));
			//}
		}

		private string GetIp() {
			string ip = HttpContext.Request.Headers["X-Forwarded-For"];
			if (String.IsNullOrEmpty(ip)) {
				ip = HttpContext.Connection.RemoteIpAddress + "";
			}
			return ip;
		}

		/*
		[HttpGet]
		public async Task<ActionResult> Index([FromQuery(Name = "user_id")] long userId) {
			RedisValue serverId;
			if (userId > 0) {
				IDatabase redisUser = Redis.GetDatabaseWebServerUser();
				serverId = redisUser.StringGet(userId + "");
				if (serverId.IsNullOrEmpty) {

					IDatabase redisServers = Redis.GetDatabaseWebServerDefault();
					serverId = redisServers.SetRandomMember("Servers"); //TODO
					IDatabase redisTemp = Redis.GetDatabaseWebServerTemp();

					using (await threadLock.LockAsync(userId)) {
						bool b = redisTemp.StringSet(userId + "", serverId, TimeSpan.FromSeconds(7000), When.NotExists);
						if (!b) {
							serverId = redisTemp.StringGet(userId + "");
							if (serverId.IsNullOrEmpty) {
								serverId = redisUser.StringGet(userId + "");
							}
						}
					}

				}
			} else {
				IDatabase redisServers = Redis.GetDatabaseWebServerDefault();
				serverId = redisServers.SetRandomMember("Servers");//TODO
			}
			if (serverId.IsNullOrEmpty) {
				serverId = 0;
			}
			return Content("{\"process_id\":" + serverId + "}", "application/json", Encoding.ASCII);
		}
		*/
	}
}
