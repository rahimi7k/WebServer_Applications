using Microsoft.AspNetCore.Mvc;
using Library;
using Library.Json;
using Library.SQL;
using Store.IONet;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Store.Controllers {

	[ApiController]
	[Route("web")]
	public class Web : Controller {

		private static readonly char[] CHARACTERS = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-_".ToCharArray();
		private static readonly string PERFECTMONEY_ACCOUNT = "U24510252";
		private static readonly string PERFECTMONEY_PASS_PHRASE_MD5 = CreateMD5("38kW64ztB1qhNKgDDswGIFfDq");
		private static readonly string PAYEER_ACCOUNT = "1641394474"; //Merchant Id
		private static readonly string PAYEER_PASSWORD = "481516kR2342$#";


		private static readonly ThreadLock<long> threadLockUser = new ThreadLock<long>();

		private static JSONObject jsonRate;


		enum Status {
			Cancelled = 1,
			Done = 2,
			AlreadyDone = 3,
			Error = 4,
			Failed = 5,
			WaitingForApproval = 6,
			MoneyBacked = 7,
			PayedLess = 8,
			Unknown = 9,
			PaymentUnitDifferent = 10,
		};



		[HttpPost("get_packages")]
		public async Task<ActionResult> GetStorePackages([FromForm] string country_code) {
			if (jsonRate == null) {
				string res = await Network.RequestPostAsync("http://" + ServerConfig.IP_FOLLOWERGIR_PROCESS + ":80" + "/home/get_rate/", (Dictionary<String, String>) null);
				try {
					jsonRate = new JSONObject(res);
				} catch (Exception) {
					jsonRate = new JSONObject();
				}
			}

			JSONObject json = new JSONObject();
			if (country_code == "EN") {
				json.Add("packages", Home.jsonPackage.GetJSONObject("EN"));
			} else if (country_code == "IR") {
				json.Add("packages", Home.jsonPackage.GetJSONObject("IR"));
			}
			json.Add("rate", jsonRate);
			return Content(json.ToString(), "application/json", Encoding.ASCII);
		}



		[HttpPost("check_buy")]
		public async Task<ActionResult> CheckBuy([FromForm] string id, [FromForm] string app, [FromForm] long userId) {
			if (!Network.GetIp(HttpContext).StartsWith("10.0.0") && !Network.GetIp(HttpContext).StartsWith("49.12.41.74")) {
				return NotFound();
			}
			if (String.IsNullOrEmpty(id)) {
				return BadRequest("INVALID_PARAMETERS");
			}

			JSONObject json = new JSONObject();
			using (await threadLockUser.LockAsync(userId)) {
				Database database = new Database(ServerConfig.DATABASE_MAIN);
				database.DisableClose();
				database.Prepare("SELECT TOP(1) Coin, IsUsed FROM WebStore WHERE Id = @Id;");
				database.BindValue("@Id", id, SqlDbType.VarChar);
				List<Row> rows = await database.ExecuteSelectAsync();

				if (rows.Count == 0) {
					json.Add("is_buy", false);

				} else {
					json.Add("is_buy", true);
					if (rows[0].GetBoolean("IsUsed")) {
						json.Add("is_used", true);
					} else {
						json.Add("is_used", false);
						json.Add("coin", rows[0].GetDecimal("Coin"));
						database.Prepare("UPDATE WebStore SET IsUsed = 1 WHERE Id = @Id;");
						database.BindValue("@Id", id, SqlDbType.VarChar);
						await database.ExecuteUpdateAsync();
					}
				}
				await database.CloseAsync();
			}
			return Content(json.ToString(), "application/json", Encoding.ASCII);
		}


		[HttpPost("set_add")]
		public async Task<ActionResult> SetBuy([FromForm] string id, [FromForm] string app, [FromForm] long userId) {
			if (!Network.GetIp(HttpContext).StartsWith("10.0.0")) {
				return NotFound();
			}
			if (String.IsNullOrEmpty(id)) {
				return BadRequest("INVALID_PARAMETERS");
			}

			JSONObject json = new JSONObject();
			Database database = new Database(ServerConfig.DATABASE_MAIN);
			database.Prepare("UPDATE WebStore SET IsAdd = 1 WHERE Id = @Id;");
			database.BindValue("@Id", id, SqlDbType.VarChar);
			await database.ExecuteUpdateAsync();
			json.Add("status", "ok");
			return Content(json.ToString(), "application/json", Encoding.ASCII);
		}




		[HttpPost("payment")]
		[Consumes("application/json")]
		public async Task<ActionResult> Payment([FromBody] StartPaymentObject param) {

			if (param.userId <= 0 ||
				param.gateWay == null ||
				param.application == null ||
				String.IsNullOrEmpty(param.packageId)) {
				return Content("{\"error\": \"PARAMETER_IS_MISSING\"}", "application/json", Encoding.ASCII);
			}
			decimal coin = 0;
			decimal price = 0;

			if (param.application == "F") {
				if (param.gateWay == "IdPay" || param.gateWay == "ZarinPal") {

					JSONObject json = null;
					if (param.isOffer) {

					} else {
						json = Home.jsonPackage.GetJSONObject("IR").GetJSONObject(param.packageId);
					}

					if (json.HasValues) {
						coin = Convert.ToDecimal(json.GetDouble("coin"));
						price = Convert.ToDecimal(json.GetDouble("price"));
					} else {
						return Content("{\"error\": \"PACKAGE_NOT_FOUND\"}", "application/json", Encoding.ASCII);
					}

				} else if (param.gateWay == "PerfectMoney" || param.gateWay == "Payeer") {

					JSONObject json = null;
					if (param.isOffer) {

					} else {
						json = Home.jsonPackage.GetJSONObject("EN").GetJSONObject(param.packageId);
					}

					if (json.HasValues) {
						coin = Convert.ToDecimal(json.GetDouble("coin"));
						price = Convert.ToDecimal(json.GetDouble("price"));

					} else {
						return Content("{\"error\": \"PACKAGE_NOT_FOUND\"}", "application/json", Encoding.ASCII);
					}
				}
			}

			if (coin == 0 || price == 0) {
				return Content("{\"error\": \"INVALID_PARAMETERS\"}", "application/json", Encoding.ASCII);
			}

			string hash = "";
			for (int i = 0; i < 8; i++) {
				hash += CHARACTERS[StringUtils.Random(0, CHARACTERS.Length - 1)];
			}
			string id = DateTimeOffset.Now.ToUnixTimeMilliseconds() + hash;

			string responseValue = null;
			string uniqueId = null;


			string description = "Payment For UserId " + param.userId + " -- Id " + id;

			if (param.gateWay == "IdPay") {
				Dictionary<string, string> header = new Dictionary<string, string>();
				header.Add("X-API-KEY", "ab938d84-076b-425f-9233-4c79295a7076");
				// header.Add("X-SANDBOX", "1");
				JSONObject jsonParam = new JSONObject();
				jsonParam.Add("order_id", id); // Here we add order_id, Because idpay return id itself
				jsonParam.Add("amount", price * 10 + ""); // Price is Rial, We need -> price * 10
				jsonParam.Add("name", "");
				jsonParam.Add("phone", "");
				jsonParam.Add("mail", "");
				jsonParam.Add("desc", description);
				jsonParam.Add("callback", "https://store.iodynamic.com/web/check_buy_idpay");
				string res = await Network.RequestPostAsync("https://api.idpay.ir/v1.1/payment", jsonParam, header);
				Log.E("(IDPay Initial Req) res:" + res);
				try {
					JSONObject resJson = new JSONObject(res);
					responseValue = resJson.GetString("link");
					uniqueId = resJson.GetString("id");
				} catch (Exception) {
					return Content("{\"error\": \"GATEWAY_ERROR\"}", "application/json", Encoding.ASCII);
				}


			} else if (param.gateWay == "PerfectMoney") {
				responseValue = "https://store.iodynamic.com/web/show_payment_perfectmoney?id=" + id + "&price=" + price +
				"&desc=" + Uri.EscapeDataString(description);


			} else if (param.gateWay == "Payeer") {

				//ToString("0.00") will change:
				//1.52 -> 1.52, 1.50 -> 1.50, 1.00 -> 1.00, 1.0 -> 1.00, 1 -> 1.00

				//ToString("0.##") will change:
				//1.52 -> 1.52, 1.5 -> 1.5, 1.00 -> 1, 1.0 -> 1, 1 -> 1

				string desctiptionBase64 = Convert.ToBase64String(Encoding.ASCII.GetBytes(description));

				string sign =
					PAYEER_ACCOUNT + ":" +
					id + ":" +
					price.ToString("0.00") + ":" +
					"USD" + ":" +
					desctiptionBase64 + ":" +
					PAYEER_PASSWORD;
				Log.E("sign: " + sign);
				sign = EncodeSha256(sign);
				responseValue = "https://store.iodynamic.com/web/show_payment_payeer?id=" + id +
					"&price=" + price.ToString("0.00") + "&sign=" + sign + "&desc=" + Uri.EscapeDataString(desctiptionBase64);
			}



			Database database = new Database(ServerConfig.DATABASE_MAIN);
			if (uniqueId != null) {
				database.Prepare("INSERT INTO WebStore (UserId, Id, Application, PackageId, Price, Coin, GateWay, UniqueId) VALUES (@UserId, @Id, @Application, @PackageId,  @Price, @Coin, @GateWay, @UniqueId);");
			} else {
				database.Prepare("INSERT INTO WebStore (UserId, Id, Application, PackageId, Price, Coin, GateWay) VALUES (@UserId, @Id, @Application, @PackageId,  @Price, @Coin, @GateWay);");
			}
			database.BindValue("@UserId", param.userId, SqlDbType.BigInt);
			database.BindValue("@Id", id, SqlDbType.VarChar);
			database.BindValue("@Application", param.application, SqlDbType.VarChar);
			database.BindValue("@PackageId", param.packageId, SqlDbType.Int);
			database.BindValue("@Price", price, SqlDbType.Decimal);
			database.BindValue("@Coin", coin, SqlDbType.Decimal);
			database.BindValue("@GateWay", param.gateWay, SqlDbType.VarChar);
			if (uniqueId != null) {
				database.BindValue("@UniqueId", uniqueId, SqlDbType.VarChar);
			}
			await database.ExecuteInsertAsync();

			JSONObject response = new JSONObject();
			response.Add("url", responseValue);
			return Content(response.ToString(), "application/json", Encoding.ASCII);
		}



		[HttpGet("show_payment_payeer")]
		public ActionResult ShowPaymentPayeer([FromQuery] string id, [FromQuery] string price, [FromQuery] string sign, [FromQuery] string desc) {

			id = EntitiesHtml(id);
			price = EntitiesHtml(price);
			sign = EntitiesHtml(sign);
			desc = EntitiesHtml(desc);

			string form =
		"<form method=\"post\" id=\"Payyer_Form\" action=\"https://payeer.com/merchant/\">" +
			"<input type=\"hidden\" name=\"m_shop\" value=\"" + PAYEER_ACCOUNT + "\">" +
			"<input type=\"hidden\" name=\"m_orderid\" value=\"" + id + "\">" +
			"<input type=\"hidden\" name=\"m_amount\" value=\"" + price + "\">" +
			"<input type=\"hidden\" name=\"m_curr\" value=\"USD\">" +
			"<input type=\"hidden\" name=\"m_desc\" value=\"" + desc + "\">" +
			"<input type=\"hidden\" name=\"m_sign\" value=\"" + sign + "\">" +
			"<input type=\"submit\" name=\"m_process\" value=\"send\" style=\"visibility: hidden;\"/>" +
			"</form>" +
		"<script type=\"text/javascript\">" +
		"var form = document.getElementById(\"Payyer_Form\").submit();" +
		"</script> ";

			return Content(form, "text/html", Encoding.UTF8);
		}



		[HttpPost("set_payeer_status")]
		public async Task<ActionResult> SetPayeerStatus(
		[FromForm] string m_operation_id,
		[FromForm] string m_operation_ps,
		[FromForm] string m_operation_date,
		[FromForm] string m_operation_pay_date,
		[FromForm] string m_shop,
		[FromForm(Name = "m_orderid")] string id,
		[FromForm(Name = "m_amount")] decimal price,
		[FromForm] string m_curr,
		[FromForm] string m_desc,
		[FromForm] string m_status,
		[FromForm] string m_sign,
		[FromForm] string m_params,
		[FromForm] string client_email,
		[FromForm] string transfer_id,
		[FromForm] string summa_out,
		[FromForm] string client_account) {

			string ip = Network.GetIp(HttpContext);
			if (ip != "185.71.65.92" && ip != "185.71.65.189" && ip != "149.202.17.210") {
				return NotFound();
			}


			Database database = new Database(ServerConfig.DATABASE_MAIN);
			database.DisableClose();
			database.Prepare("SELECT TOP(1) UserId FROM WebStore WHERE Id = @Id;");
			database.BindValue("@Id", id, SqlDbType.VarChar);
			List<Row> row = await database.ExecuteSelectAsync();
			if (row.Count == 0) {
				database.Close();
				return Content(id + "|error", "text/html", Encoding.UTF8);
			}

			using (await threadLockUser.LockAsync(row[0].GetLong("UserId"))) {

				database.Prepare("SELECT TOP(1) * FROM WebStore WHERE Id = @Id;");
				database.BindValue("@Id", id, SqlDbType.VarChar);
				row = await database.ExecuteSelectAsync();
				if (row.Count == 0) {
					database.Close();
					return Content(id + "|error", "text/html", Encoding.UTF8);
				}

				if (!row[0].IsNull("Status")) {
					database.Close();
					return Content(id + "|error", "text/html", Encoding.UTF8);
				}


				Status? resStatus = null; //With ? we can create a null enum
				if (price != row[0].GetDecimal("Price")) {
					resStatus = Status.PayedLess;

				} else {

					string sign =
					m_operation_id + ":" +
					m_operation_ps + ":" +
					m_operation_date + ":" +
					m_operation_pay_date + ":" +
					PAYEER_ACCOUNT + ":" +
					id + ":" +
					price + ":" +
					"USD" + ":" +
					m_desc + ":" +
					m_status + ":" +
					(String.IsNullOrEmpty(m_params) ? "" : m_params + ":") +
					PAYEER_PASSWORD;
					sign = EncodeSha256(sign);

					if (sign != m_sign) {
						resStatus = Status.Error;
					} else {
						if (m_status == "success") {
							resStatus = Status.Done;
						} else {
							resStatus = Status.Failed;
						}
					}
				}

				database.Prepare("UPDATE WebStore SET Status = @Status, PayerInfo = @PayerInfo, TrackId = @TrackId WHERE Id = @Id;");
				database.BindValue("@Id", id, SqlDbType.VarChar);
				database.BindValue("@Status", (int) resStatus, SqlDbType.TinyInt);
				database.BindValue("@TrackId", m_operation_id, SqlDbType.VarChar);
				database.BindValue("@PayerInfo",
					"client_account: " + client_account + " --\r\n" +
					"client_email: " + client_email + " --\r\n" +
					"transfer_id: " + transfer_id + " --\r\n" +
					"summa_out: " + summa_out + " --\r\n" +
					"m_operation_ps: " + m_operation_ps + " --\r\n", SqlDbType.VarChar);
				await database.ExecuteUpdateAsync();


				if (resStatus == Status.Done) {
					await database.CloseAsync();
					return Content(id + "|success", "text/html", Encoding.UTF8);

				} else {
					await database.CloseAsync();
					return Content(id + "|error", "text/html", Encoding.UTF8);
				}
			}
		}



		[HttpGet("check_buy_payeer")]
		public async Task<ActionResult> CheckBuyPayeer([FromQuery] int status,
		[FromQuery] string m_operation_id,
		[FromQuery] string m_operation_ps,
		[FromQuery] string m_operation_date,
		[FromQuery] string m_operation_pay_date,
		[FromQuery] string m_shop,
		[FromQuery(Name = "m_orderid")] string id,
		[FromQuery(Name = "m_amount")] decimal price,
		[FromQuery] string m_curr,
		[FromQuery] string m_desc,
		[FromQuery] string m_status,
		[FromQuery] string m_sign,
		[FromQuery] string m_params,
		[FromQuery] string client_email,
		[FromQuery] string transfer_id,
		[FromQuery] string summa_out,
		[FromQuery] string client_account) {

			/*
			Log.E("m_operation_id: " + m_operation_id);
			Log.E("m_operation_ps: " + m_operation_ps);
			Log.E("m_operation_date: " + m_operation_date);
			Log.E("m_operation_pay_date: " + m_operation_pay_date);
			Log.E("m_shop: " + m_shop);
			Log.E("id: " + id);
			Log.E("price: " + price);
			Log.E("m_curr: " + m_curr);
			Log.E("m_desc: " + m_desc);
			Log.E("m_status: " + m_status);
			Log.E("m_sign: " + m_sign);
			Log.E("m_params: " + m_params);
			*/

			if (String.IsNullOrEmpty(id) ||
			price <= 0 ||
			String.IsNullOrEmpty(m_curr)) {
				return BadRequest("INVALID_PARAMETERS, Id: " + id);
			}

			Database database = new Database(ServerConfig.DATABASE_MAIN);
			database.DisableClose();
			database.Prepare("SELECT TOP(1) UserId FROM WebStore WHERE Id = @Id;");
			database.BindValue("@Id", id, SqlDbType.VarChar);
			List<Row> row = await database.ExecuteSelectAsync();
			if (row.Count == 0) {
				database.Close();
				return BadRequest("Not_Found, Id: " + id);
			}

			using (await threadLockUser.LockAsync(row[0].GetLong("UserId"))) {

				Status? resStatus = null; //With ? we can create a null enum

				database.Prepare("SELECT TOP(1) * FROM WebStore WHERE Id = @Id;");
				database.BindValue("@Id", id, SqlDbType.VarChar);
				row = await database.ExecuteSelectAsync();
				if (row.Count == 0) {
					return BadRequest("NOT_FOUND, Id: " + id);
				}

				if (row[0].IsNull("Status") ||
					String.IsNullOrEmpty(m_operation_id) ||
					String.IsNullOrEmpty(m_sign) ||
					m_shop != PAYEER_ACCOUNT ||
					String.IsNullOrEmpty(m_status) ||
					row[0].GetByte("Status") != (int) Status.Done) {
					resStatus = Status.Error;
				} else if (price != row[0].GetDecimal("Price")) {
					resStatus = Status.PayedLess;
				}

				if (resStatus != null) {
					return Redirect(GetApllicationLink(row[0].GetString("Application")) + "?status=" + resStatus +
						"&id=" + id +
						"&package_id=" + row[0].GetInt("PackageId") +
						"&price=" + price +
						"&coin=" + row[0].GetDecimal("Coin"));
				}


				string sign =
					m_operation_id + ":" +
					m_operation_ps + ":" +
					m_operation_date + ":" +
					m_operation_pay_date + ":" +
					PAYEER_ACCOUNT + ":" +
					id + ":" +
					price + ":" +
					"USD" + ":" +
					m_desc + ":" +
					m_status + ":" +
					(String.IsNullOrEmpty(m_params) ? "" : m_params + ":") +
					PAYEER_PASSWORD;

				sign = EncodeSha256(sign);
				if (sign != m_sign) {
					resStatus = Status.Error;
				} else {
					if (m_status == "success") {
						resStatus = Status.Done;
					} else {
						resStatus = Status.Failed;
					}
				}

				return Redirect(this.GetApllicationLink(row[0].GetString("Application")) + "?status=" + (int) resStatus +
				"&id=" + id +
				"&package_id=" + row[0].GetInt("PackageId") +
				"&price=" + price +
				"&coin=" + row[0].GetDecimal("Coin"));
			}
		}







		[HttpGet("show_payment_perfectmoney")]
		public ActionResult ShowPaymentPerfectMoney([FromQuery] string id, [FromQuery] decimal price, [FromQuery] string desc) {

			id = EntitiesHtml(id);
			desc = EntitiesHtml(desc);

			string form =
		"<form id=\"PerfectMoney_Form\" action=\"https://perfectmoney.com/api/step1.asp\" method=\"POST\" style=\"visibility:hidden;\">" +
		   "<input type=\"hidden\" name=\"PAYEE_ACCOUNT\" value=\"" + PERFECTMONEY_ACCOUNT + "\">" +
		   "<input type=\"hidden\" name=\"PAYEE_NAME\" value=\"IODynamic\">" +
		   "<input type=\"hidden\" name=\"PAYMENT_ID\" value=\"" + id + "\"><br>" +
		   "<input type=\"hidden\" name=\"PAYMENT_AMOUNT\" value=\"" + price + "\"><br>" +
		   "<input type=\"hidden\" name=\"PAYMENT_UNITS\" value=\"USD\">" +
		   "<input type=\"hidden\" name=\"STATUS_URL\" value=\"\">" +
		   "<input type=\"hidden\" name=\"PAYMENT_URL\" value=\"https://store.iodynamic.com/web/check_buy_perfectmoney\">" +
		   "<input type=\"hidden\" name=\"PAYMENT_URL_METHOD\" value=\"POST\">" +
		   "<input type=\"hidden\" name=\"NOPAYMENT_URL\" value=\"https://store.iodynamic.com/web/check_buy_perfectmoney/?&status=" + Status.Cancelled + "\">" +
		   "<input type=\"hidden\" name=\"NOPAYMENT_URL_METHOD\" value=\"POST\">" +
		   "<input type=\"hidden\" name=\"SUGGESTED_MEMO\" value=\"" + desc + "\">" +
		   "<input type=\"hidden\" name=\"SUGGESTED_MEMO_NOCHANGE\" value=\"" + desc + "\">" +
		   "<input type=\"hidden\" name=\"BAGGAGE_FIELDS\" value=\"\">" +
		   "<input type=\"submit\" name=\"PAYMENT_METHOD\" value=\"Pay Now!\" style=\"visibility:hidden;\">" +
		"</form>" +
		"<script type=\"text/javascript\">" +
		"var form = document.getElementById(\"PerfectMoney_Form\").submit();" +
		"</script> ";

			return Content(form, "text/html", Encoding.UTF8);
		}



		[HttpPost("check_buy_perfectmoney")]
		public async Task<ActionResult> CheckBuyPerfectMoney([FromQuery] int status,
			[FromForm(Name = "PAYMENT_ID")] string id,
			[FromForm] string PAYEE_ACCOUNT,
			[FromForm(Name = "PAYMENT_AMOUNT")] decimal price,
			[FromForm] string V2_HASH,
			[FromForm] string PAYMENT_UNITS,
			[FromForm] string PAYMENT_BATCH_NUM,
			[FromForm] string PAYER_ACCOUNT,
			[FromForm] long TIMESTAMPGMT) {

			Log.E(status);
			Log.E(id);
			Log.E(PAYEE_ACCOUNT);
			Log.E(price);
			Log.E(V2_HASH);
			Log.E(PAYMENT_UNITS);
			Log.E(PAYMENT_BATCH_NUM);
			Log.E(PAYER_ACCOUNT);
			Log.E(TIMESTAMPGMT);

			if (String.IsNullOrEmpty(id) ||
				String.IsNullOrEmpty(PAYEE_ACCOUNT) ||
				price <= 0 ||
				String.IsNullOrEmpty(PAYMENT_UNITS)) {
				return BadRequest("INVALID_PARAMETERS");
			}


			Database database = new Database(ServerConfig.DATABASE_MAIN);
			database.DisableClose();
			database.Prepare("SELECT TOP(1) UserId, Status FROM WebStore WHERE Id = @Id;");
			database.BindValue("@Id", id, SqlDbType.VarChar);
			List<Row> row = await database.ExecuteSelectAsync();
			if (row.Count == 0) {
				database.Close();
				return BadRequest("NOT_FOUND, Id: " + id);
			}

			if (!row[0].IsNull("Status")) {
				database.Close();
				return BadRequest("ALREADY_CHECKED, Id: " + id);
			}

			using (await threadLockUser.LockAsync(row[0].GetLong("UserId"))) {

				Status? resStatus = null; //With ? we can create a null enum

				database.Prepare("SELECT TOP(1) * FROM WebStore WHERE Id = @Id;");
				database.BindValue("@Id", id, SqlDbType.VarChar);
				row = await database.ExecuteSelectAsync();
				if (row.Count == 0) {
					return BadRequest("NOT_FOUND, Id: " + id);
				}

				if (status == (int) Status.Cancelled) {
					resStatus = Status.Cancelled;
				} else if (String.IsNullOrEmpty(PAYMENT_BATCH_NUM) ||
					String.IsNullOrEmpty(V2_HASH) ||
					String.IsNullOrEmpty(PAYER_ACCOUNT) ||
					PAYEE_ACCOUNT != PERFECTMONEY_ACCOUNT) {
					resStatus = Status.Error;
				} else if (row[0].GetDecimal("Price") != price) {
					resStatus = Status.PayedLess;
				}

				if (resStatus != null) {
					database.Prepare("UPDATE WebStore SET Status = @Status, PayerInfo = @PayerInfo WHERE Id = @Id;");
					database.BindValue("@Id", id, SqlDbType.VarChar);
					database.BindValue("@Status", (int) resStatus, SqlDbType.TinyInt);
					database.BindValue("@PayerInfo", "PAYER_ACCOUNT: " + PAYER_ACCOUNT, SqlDbType.VarChar);
					await database.ExecuteUpdateAsync();
					await database.CloseAsync();

					return Redirect(GetApllicationLink(row[0].GetString("Application")) + "?status=" + resStatus +
						"&id=" + id +
						"&package_id=" + row[0].GetInt("PackageId") +
						"&price=" + price +
						"&coin=" + row[0].GetDecimal("Coin"));
				}

				string hash = id + ":" +
					PAYEE_ACCOUNT + ":" +
					price + ":" +
					PAYMENT_UNITS + ":" +
					PAYMENT_BATCH_NUM + ":" +
					PAYER_ACCOUNT + ":" +
					PERFECTMONEY_PASS_PHRASE_MD5 + ":" +
					TIMESTAMPGMT;

				if (CreateMD5(hash) == V2_HASH) {
					resStatus = Status.Done;
				} else {
					resStatus = Status.Error;
				}


				database.Prepare("UPDATE WebStore SET Status = @Status, PayerInfo = @PayerInfo, TrackId = @TrackId WHERE Id = @Id;");
				database.BindValue("@Id", id, SqlDbType.VarChar);
				database.BindValue("@Status", (int) resStatus, SqlDbType.TinyInt);
				database.BindValue("@PayerInfo", "PAYER_ACCOUNT: " + PAYER_ACCOUNT, SqlDbType.VarChar);
				database.BindValue("@TrackId", PAYMENT_BATCH_NUM, SqlDbType.VarChar);
				await database.ExecuteUpdateAsync();

				await database.CloseAsync();
				return Redirect(GetApllicationLink(row[0].GetString("Application")) + "?status=" + ((int) resStatus) +
				"&id=" + id +
				"&package_id=" + row[0].GetInt("PackageId") +
				"&price=" + price +
				"&coin=" + row[0].GetDecimal("Coin"));
			}
		}





		[HttpPost("check_buy_idpay")]
		public async Task<ActionResult> CheckBuyIdPay(
			[FromForm] int status, [FromForm] int track_id, [FromForm] string id, [FromForm] string order_id,
			[FromForm] int amount, [FromForm] string card_no, [FromForm] string hashed_card_no, [FromForm] string date) {

			Log.E(order_id);
			Log.E(id);
			Log.E(track_id);
			Log.E(status);
			Log.E(amount);
			Log.E(card_no);
			Log.E(hashed_card_no);
			Log.E(date);

			if (id == null || status == 0) {
				return BadRequest("INVALID_PARAMETERS");
			}

			Database database = new Database(ServerConfig.DATABASE_MAIN);
			database.DisableClose();
			database.Prepare("SELECT TOP(1) UserId FROM WebStore WHERE Id = @Id;");
			database.BindValue("@Id", order_id, SqlDbType.VarChar);
			List<Row> row = await database.ExecuteSelectAsync();
			if (row.Count == 0) {
				database.Close();
				return BadRequest("NOT_FOUND");
			}

			using (await threadLockUser.LockAsync(row[0].GetLong("UserId"))) {

				database.Prepare("SELECT TOP(1) * FROM WebStore WHERE Id = @Id;");
				database.BindValue("@Id", order_id, SqlDbType.VarChar);
				row = await database.ExecuteSelectAsync();
				if (row.Count == 0) {
					database.Close();
					return BadRequest("UNKNOWN , Id: " + order_id);
				}

				if (!row[0].IsNull("Status")) {
					database.Close();
					return BadRequest("ALREADY_CHECK, Id: " + order_id);
				}

				Status? dbStatus = null;
				string app = row[0].GetString("Application");
				decimal price = row[0].GetDecimal("Price") * 10; // Attention, 1) Database price is decimal .00, 2) It is Toman, we need Rial here
				decimal coin = row[0].GetDecimal("Coin");

				Dictionary<string, string> header = new Dictionary<string, string>();
				header.Add("X-API-KEY", "ab938d84-076b-425f-9233-4c79295a7076");
				JSONObject jsonParam = new JSONObject();
				jsonParam.Add("order_id", order_id);
				jsonParam.Add("id", row[0].GetString("UniqueId"));
				string res = await Network.RequestPostAsync("https://api.idpay.ir/v1.1/payment/verify", jsonParam, header);
				Log.E("(IDPay Buy Res) res:" + res);
				try {
					JSONObject resJson = new JSONObject(res);
					dbStatus = getIdPayDbStatus(resJson.GetInt("status"));

					if (resJson.GetInt("amount") < price) {
						dbStatus = Status.PayedLess;
					}

					database.Prepare("UPDATE WebStore SET Status = @Status, PayerInfo = @PayerInfo, TrackId = @TrackId Where Id = @Id;");
					database.BindValue("@Id", order_id, SqlDbType.VarChar);
					database.BindValue("@Status", (int) dbStatus, SqlDbType.TinyInt);
					database.BindValue("@PayerInfo", "Card Number: " + resJson.GetJSONObject("payment").GetString("card_no"), SqlDbType.VarChar);
					database.BindValue("@TrackId", resJson.GetLong("track_id"), SqlDbType.VarChar);
					await database.ExecuteUpdateAsync();
					await database.CloseAsync();
				} catch (Exception) {
					dbStatus = Status.Unknown;
				}


				return Redirect(this.GetApllicationLink(app) + "?status=" + (int) dbStatus +
					"&id=" + order_id +
					"&package_id=" + row[0].GetInt("PackageId") +
					"&price=" + price.ToString("0.##") +
					"&coin=" + coin);
			}
		}



		private Status getIdPayDbStatus(int status) {
			Status dbStatus;

			switch (status) {
				case (100): {
						dbStatus = Status.Done;
						break;
					}
				case (101): {
						dbStatus = Status.AlreadyDone;
						break;
					}

				case (10): {
						dbStatus = Status.WaitingForApproval;
						break;
					}

				case (7): {
						dbStatus = Status.Cancelled;
						break;
					}

				case (6):
				case (5): {
						dbStatus = Status.MoneyBacked;
						break;
					}

				case (4):
				case (3):
				case (2): {
						dbStatus = Status.Failed;
						break;
					}


				default: {
						dbStatus = Status.Unknown;
						break;
					}
			}


			return dbStatus;
		}




		private static string EntitiesHtml(string text) {
			return text.Replace("&", "&amp;").Replace(">", "&gt;").Replace("<", "&lt;").Replace("\"", "&quot;");
			//return String.replace(/ &/ g, '&amp;').replace(/>/ g, '&gt;').replace(/</ g, '&lt;').replace(/ "/g, '&quot;');
		}



		private string GetApllicationLink(string app) {
			if (app == "F") {
				return "https://followergir.iodynamic.com/store";
				//return "http://localhost:4200/store";
			} else if (app == "M") {
				// return "https://membergir.iodynamic.com/store";
				return "http://localhost:4200/store";
			} else {
				return "https://iodynamic.com/store";
			}
		}


		private static string CreateMD5(string input) {
			using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create()) {
				byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
				byte[] hashBytes = md5.ComputeHash(inputBytes);
				return Convert.ToHexString(hashBytes); // .NET 5 +
			}
		}

		static string EncodeSha256(string str) {
			using (SHA256 sha256Hash = SHA256.Create()) {
				byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(str));
				// Convert byte array to a string   
				StringBuilder builder = new StringBuilder();
				for (int i = 0; i < bytes.Length; i++) {
					builder.Append(bytes[i].ToString("x2"));
				}
				return builder.ToString().ToUpper();
			}
		}


		public class StartPaymentObject {
			public long userId { get; set; }
			public string application { get; set; }
			public string gateWay { get; set; }
			public string packageId { get; set; }
			public bool isOffer { get; set; }

		}


	}
}
