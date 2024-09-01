using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.IO;
using Followergir.IONet;
using Followergir;
using Library.Json;
using Library;

namespace Followergir.Controllers.Store {

	[ApiController]
	[Route("store/android")]
	public class Android : Controller {

		private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

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

		public static async Task<Buy> CheckBazaar(string app, string sku, string token) {
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
				return null;
			}
			Buy buy = new Buy();

			Dictionary<string, string> param = new Dictionary<string, string>();
			param.Add("access_token", Bazaar.accessToken);
			string response = await Network.RequestGetAsync("https://pardakht.cafebazaar.ir/devapi/v2/api/validate/" + app + "/inapp/" + sku + "/purchases/" + token + "/", param);
			try {
				JSONObject json = new JSONObject(response);
				if (json.ContainsKey("purchaseState")) {
					buy.isBuy = json.GetInt("purchaseState") == 0;//-- if 0 Buy is Ok
					buy.isUsed = json.GetInt("consumptionState") == 0;//if 1 Not Used

				} else if (json.ContainsKey("error_description")) {
					string error = json.GetString("error_description");
					if (error == "Access token has expired." || error == "Access token is not valid.") {
						Bazaar.accessToken = null;
						await semaphore.WaitAsync();
						await Bazaar.GetNewAccessToken();
						semaphore.Release();
						//if (countCheck == 0) {
						//	countCheck += 1;
							return await CheckBazaar(app, sku, token);
						//}
					} else if (error == "The requested purchase is not found!") {
						buy.isBuy = false;
						buy.isUsed = false;

					} else if (error == "Product is not found.") {//-- SKU not exist!
						buy.isBuy = false;
						buy.isUsed = false;

					} else if (error == "You don't have access to this app.") {

					} else {
						Log.SendEmail("Store!", "CheckBazaar 3\r\nApp: " + app + "\r\nToken: " + token + "\r\nSKU: " + sku + "\r\nres: " + response);
					}
				} else {
					/*if (countCheck <= 1) {
						countCheck += 2;
						return await CheckBazaar(app, sku, token);
					} else {*/
						Log.SendEmail("Store!", "CheckBazaar 1\r\nApp: " + app + "\r\nToken: " + token + "\r\nSKU: " + sku + "\r\nres: " + response);
					//}
				}
			} catch (Exception ex) {
				Log.SendEmail("Store!", "CheckBazaar 2\r\nApp: " + app + "\r\nToken: " + token + "\r\nSKU: " + sku + "\r\nException: " + ex + "\r\nres: " + response);
				return null;
			}
			return buy;
		}


		public static async Task<Buy> CheckMyket(string app, string sku, string token) {
			Buy buy = new Buy();

			string response = await Network.RequestGetAsync("https://developer.myket.ir/api/applications/" + app + "/purchases/products/" + sku + "/tokens/" + token, null, Myket.header);
			try {
				JSONObject json = new JSONObject(response);
				buy.isBuy = json.GetInt("purchaseState") == 0;//-- if 0 Buy is Ok
				buy.isUsed = json.GetInt("consumptionState") == 1;//if 0 Not Used
			} catch (Exception ex) {
				Log.SendEmail("Store!", "CheckMyket\r\nApp: " + app + "\r\nToken: " + token + "\r\nSKU: " + sku + "\r\nException: " + ex + "\r\nres: " + response);
				return null;
			}
			return buy;
		}

		public class Buy {
			public bool isBuy = false;
			public bool isUsed = false;
		}


		private class Bazaar {

			public static readonly string CLIENT_ID = "uTdYdjekjJmiW2PVXLhuDjyODWhAkdpcnBh7NTUX";
			public static readonly string CLIENT_SECRET = "jbjy9dFnwOlEvthyTjwWgKTuW2hROimWBM5uKaA06L80YXB267QWiP69aH3n";
			public static readonly string REDIRECT_URL = "https://iofollowergir.com/store/android/";

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
