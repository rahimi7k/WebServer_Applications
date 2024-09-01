using Microsoft.AspNetCore.Mvc;
using Library;
using Store.IONet;
using Library.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using Library.SQL;
using System.Data;

namespace Store.Controllers {

	[ApiController]
	[Route("android")]
	public class Android : Controller {

		private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

		private int countCheck = 0;

		[HttpGet]
		public async Task<ActionResult> Index([FromQuery] string code) {
			if (String.IsNullOrEmpty(code)) {
				return BadRequest();

			} else if (code == "Saeed!") {
				Dictionary<string, string> param = new Dictionary<string, string>();
				param.Add("response_type", "code");
				param.Add("access_type", "offline");
				param.Add("client_id", Bazaar.CLIENT_ID);
				param.Add("redirect_uri", Bazaar.REDIRECT_URL);
				return Redirect("https://pardakht.cafebazaar.ir/devapi/v2/auth/authorize/" + Network.ParamsConvert(param, true));
			} else {
				Dictionary<string, string> param = new Dictionary<string, string>();
				param.Add("grant_type", "authorization_code");
				param.Add("client_id", Bazaar.CLIENT_ID);
				param.Add("client_secret", Bazaar.CLIENT_SECRET);
				param.Add("redirect_uri", Bazaar.REDIRECT_URL);
				param.Add("code", code);
				string response = await Network.RequestPostAsync("https://pardakht.cafebazaar.ir/devapi/v2/auth/token/", param);
				await semaphore.WaitAsync();
				try {
					JSONObject json = new JSONObject(response);
					string refreshToken = json.GetString("refresh_token");
					Bazaar.accessToken = json.GetString("access_token");

					StreamWriter streamWriter = new StreamWriter(App.GetDirectory() + "Bazaar.json", false);
					streamWriter.Write(response);
					streamWriter.Flush();
					streamWriter.Dispose();
					streamWriter.Close();

					semaphore.Release();
					return Content("New token saved.");
				} catch (Exception ex) {
					semaphore.Release();
					return Content("Can't save new token\r\nres: " + response + "\r\n\r\nException: " + ex);
				}
			}
		}

		[HttpGet("check_buy")]
		public async Task<ActionResult> CheckBuyGet([FromQuery] byte market, [FromQuery] string app, [FromQuery] string sku, [FromQuery] string token) {
			return await CheckBuy(market, app, sku, token);
		}

		[HttpPost("check_buy")]
		public async Task<ActionResult> CheckBuyPost([FromForm] byte market, [FromForm] string app, [FromForm] string sku, [FromForm] string token) {
			return await CheckBuy(market, app, sku, token);
		}


		[HttpPost("get_add")]
		public async Task<ActionResult> GetAdd([FromForm] byte market, [FromForm] string token) {
			if (!Network.GetIp(HttpContext).StartsWith("10.0.0") && !Network.GetIp(HttpContext).StartsWith("49.12.41.74")) {
				return NotFound();
			}

			Database database = new Database(ServerConfig.DATABASE_MAIN);
			database.DisableClose();
			database.Prepare("SELECT TOP(1) UserId FROM AndroidStore WHERE Market = @Market AND Token = @Token;");
			database.BindValue("@Market", market, SqlDbType.TinyInt);
			database.BindValue("@Token", token, SqlDbType.VarChar);
			List<Row> rows = await database.ExecuteSelectAsync();
			return Content(rows.Count == 0 ? "ROW_NO_IN_DB" : "USED");
		}


		[HttpPost("set_add")]
		public async Task<ActionResult> SetAdd([FromForm] byte market, [FromForm] string token, [FromForm] long userId) {
			if (!Network.GetIp(HttpContext).StartsWith("10.0.0") && !Network.GetIp(HttpContext).StartsWith("49.12.41.74")) {
				return NotFound();
			}
			try {
				Database database = new Database(ServerConfig.DATABASE_MAIN);
				database.Prepare("INSERT INTO AndroidStore (Market, Token, UserId) VALUES (@Market, @Token, @UserId);");
				database.BindValue("@Market", market, SqlDbType.TinyInt);
				database.BindValue("@Token", token, SqlDbType.VarChar);
				database.BindValue("@UserId", userId, SqlDbType.BigInt);
				await database.ExecuteInsertAsync();
			} catch (Exception) {
				//Email
			}

			return NoContent();
		}


		private async Task<ActionResult> CheckBuy(byte market, string app, string sku, string token) {
			if (market == 0x00 || String.IsNullOrEmpty(app) || String.IsNullOrEmpty(token)) {
				return Content("{\"error\":\"INVALID_PARAMETERS\"}", "application/json", Encoding.ASCII);
			}
			if (market == 0x01) {
				return await CheckPlayStore(app, sku, token);

			} else if (market == 0x02) {
				return await CheckBazaar(app, sku, token);

			} else if (market == 0x03) {
				return await CheckMyket(app, sku, token);
			}
			return Content("{\"error\":\"MARKET_NOT_SUPPORT\"}", "application/json", Encoding.ASCII);
		}


		private async Task<ActionResult> CheckPlayStore(string app, string sku, string token) {
			bool isBuy = false;
			bool isUsed = false;

			return Content("{\"is_buy\":" + (isBuy ? "true" : "false") + "}", "application/json", Encoding.ASCII);
		}



		private async Task<ActionResult> CheckBazaar(string app, string sku, string token) {
			bool isBuy = false;
			bool isUsed = false;

			/*using (await semaphore.WaitAsync()) {

			}*/
			await semaphore.WaitAsync();
			if (String.IsNullOrEmpty(Bazaar.accessToken)) {
				await Bazaar.GetNewAccessToken();
			}
			semaphore.Release();

			//string response2 = await Network.RequestGetAsync("https://www.cloudflare.com/cdn-cgi/trace", null);
			//return Content(response2, "application/json", Encoding.ASCII);

			if (String.IsNullOrEmpty(Bazaar.accessToken)) {
				Log.SendEmail("Store!", "CheckBazaar 4\r\nACCESS_TOKEN_EMPTY");
				return Content("{\"error\":\"ACCESS_TOKEN_EMPTY\"}", "application/json", Encoding.ASCII);
			}
			Dictionary<string, string> param = new Dictionary<string, string>();
			param.Add("access_token", Bazaar.accessToken);
			string response = await Network.RequestGetAsync("https://pardakht.cafebazaar.ir/devapi/v2/api/validate/" + app + "/inapp/" + sku + "/purchases/" + token + "/", param);
			try {
				JSONObject json = new JSONObject(response);
				if (json.ContainsKey("purchaseState")) {
					isBuy = json.GetInt("purchaseState") == 0;//-- if 0 Buy is Ok
					isUsed = json.GetInt("consumptionState") == 0;//if 1 Not Used

				} else if (json.ContainsKey("error_description")) {
					string error = json.GetString("error_description");
					if (error == "Access token has expired." || error == "Access token is not valid.") {
						Bazaar.accessToken = null;
						await semaphore.WaitAsync();
						await Bazaar.GetNewAccessToken();
						semaphore.Release();
						if (countCheck == 0) {
							countCheck += 1;
							return await CheckBazaar(app, sku, token);
						}
					} else if (error == "The requested purchase is not found!") {
						isBuy = false;
						isUsed = false;

					} else if (error == "Product is not found.") {//-- SKU not exist!
						isBuy = false;
						isUsed = false;

					} else if (error == "You don't have access to this app.") {

					} else {
						Log.SendEmail("Store!", "CheckBazaar 3\r\nApp: " + app + "\r\nToken: " + token + "\r\nSKU: " + sku + "\r\nres: " + response);
					}
				} else {
					if (countCheck <= 1) {
						countCheck += 2;
						return await CheckBazaar(app, sku, token);
					} else {
						Log.SendEmail("Store!", "CheckBazaar 1\r\nApp: " + app + "\r\nToken: " + token + "\r\nSKU: " + sku + "\r\nres: " + response);
					}
				}
			} catch (Exception ex) {
				Log.SendEmail("Store!", "CheckBazaar 2\r\nApp: " + app + "\r\nToken: " + token + "\r\nSKU: " + sku + "\r\nException: " + ex + "\r\nres: " + response);
			}

			string res = "{\"is_buy\":" + (isBuy ? "true" : "false");
			if (isBuy) {
				res += ",\"is_used\":" + (isUsed ? "true" : "false");
				/*if (!isUsed) {
					res += ",\"coin\":" + Home.jsonPackage.GetJSONObject("IR").GetJSONObject(sku.Replace("Package", "")).GetDouble("coin");
				}*/
			}
			return Content(res + "}", "application/json", Encoding.ASCII);
		}


		private async Task<ActionResult> CheckMyket(string app, string sku, string token) {
			bool isBuy = false;
			bool isUsed = false;

			string response = await Network.RequestGetAsync("https://developer.myket.ir/api/applications/" + app + "/purchases/products/" + sku + "/tokens/" + token, null, Myket.header);
			try {
				JSONObject json = new JSONObject(response);
				isBuy = json.GetInt("purchaseState") == 0;//-- if 0 Buy is Ok
				isUsed = json.GetInt("consumptionState") == 1;//if 0 Not Used
			} catch (Exception ex) {
				Log.SendEmail("Store!", "CheckMyket\r\nApp: " + app + "\r\nToken: " + token + "\r\nSKU: " + sku + "\r\nException: " + ex + "\r\nres: " + response);
			}

			string res = "{\"is_buy\":" + (isBuy ? "true" : "false");
			if (isBuy) {
				res += ",\"is_used\":" + (isUsed ? "true" : "false");
				/*if (!isUsed) {
					res += ",\"coin\":" + Home.jsonPackage.GetJSONObject("IR").GetJSONObject(sku.Replace("Package", "")).GetDouble("coin");
				}*/
			}
			return Content(res + "}", "application/json", Encoding.ASCII);
		}


		private class Bazaar {

			//private static readonly IPGlobalProperties iPGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
			//iPGlobalProperties.DomainName == "iodynamic.com"

			public static readonly string CLIENT_ID = "i8YmQmQ8Lqx8BEMiDkP3UjGVCj5PPUiGmDjnnGFG";
			public static readonly string CLIENT_SECRET = "X2Knv7ug68uog4OsbeP98G1c7duxyx77wX49fEBiARuFgWolMb6KvonwlcOX";
			public static readonly string REDIRECT_URL = "https://store.iodynamic.com/android/";

			public static string accessToken;

			static Bazaar() {
				try {
					StreamReader streamReader = new StreamReader(App.GetDirectory() + "Bazaar.json");
					string str = streamReader.ReadToEnd();
					streamReader.Dispose();
					streamReader.Close();
					JSONObject json = new JSONObject(str);
					accessToken = json.GetString("access_token");
				} catch (Exception ex) { }
			}

			public static async Task GetNewAccessToken() {
				string refreshToken = null;
				try {
					StreamReader streamReader = new StreamReader(App.GetDirectory() + "Bazaar.json");
					string str = streamReader.ReadToEnd();
					streamReader.Dispose();
					streamReader.Close();
					JSONObject json = new JSONObject(str);
					refreshToken = json.GetString("refresh_token");
				} catch (Exception ex) { }

				if (String.IsNullOrEmpty(refreshToken)) {
					Dictionary<string, string> param = new Dictionary<string, string>();
					param.Add("response_type", "code");
					param.Add("access_type", "offline");
					param.Add("client_id", CLIENT_ID);
					param.Add("redirect_uri", REDIRECT_URL);
					Log.SendEmail("Store!", "RefreshToken not exist!\r\nGo to this link\r\n" + "https://pardakht.cafebazaar.ir/devapi/v2/auth/authorize/" + Network.ParamsConvert(param, true));
				} else {
					Dictionary<string, string> param = new Dictionary<string, string>();
					param.Add("client_id", CLIENT_ID);
					param.Add("client_secret", CLIENT_SECRET);
					param.Add("refresh_token", refreshToken);
					param.Add("grant_type", "refresh_token");
					string response = await Network.RequestPostAsync("https://pardakht.cafebazaar.ir/devapi/v2/auth/token/", param);
					try {
						JSONObject json = new JSONObject(response);
						accessToken = json.GetString("access_token");

						StreamWriter streamWriter = new StreamWriter(App.GetDirectory() + "Bazaar.json", false);
						streamWriter.Write(response);
						streamWriter.Flush();
						streamWriter.Dispose();
						streamWriter.Close();

					} catch (Exception ex) {
						Log.SendEmail("Store!", "Can't save new token\r\nres: " + response + "\r\n\r\nException: " + ex);
					}
				}
			}

			public void GetNewRefreshToken() {

			}
		}

		public class Myket {

			public static readonly Dictionary<string, string> header;

			static Myket() {
				header = new Dictionary<string, string>();
				header.Add("X-Access-Token", "4e888f0a-9b8d-4fec-a5ac-ab10296642c0");
			}
		}

	}
}
