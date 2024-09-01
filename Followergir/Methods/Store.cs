using Followergir.Controllers;
using Followergir.IO;
using Followergir.IONet;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Library.Json;
using Library;

namespace Followergir.Methods {

	public class Store : Order {

		public static List<IOApi.OfferItem> offerEN, offerIR;
		public static IOApi.StorePackages pakageEN, pakageIR;

		private static readonly ThreadLock<String> threadLock = new ThreadLock<String>();

		public static async Task<IOObject> GetStorePackages(UserConfig.User user, IOApi.GetStorePackages ioFunction, IOApi.Update[] updates) {
			if (user.language == "fa" || user.operatingSystem == "android") {
				return pakageIR;

			} else if (user.language == "en") {
				return pakageEN;
			}
			return null;
		}

		public static async Task<IOObject> CheckBuyWeb(UserConfig.User user, IOApi.CheckBuyWeb ioFunction, IOApi.Update[] updates) {
			IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.id, Error.PURCHASE_FAILED, Error.PURCHASE_HAS_USED);
			if (error != null) {
				return error;
			}

			if (String.IsNullOrEmpty(ioFunction.id)) {
				return new IOApi.Error("INVALID_PARAMETERS");
			}

			Dictionary<string, string> param = new Dictionary<string, string>();
			param.Add("id", ioFunction.id);
			param.Add("app", "F");
			param.Add("userId", user.id + "");
			string response = await Network.RequestPostAsync(Network.CreateUrl(Network.SCHEME_HTTP, ServerConfig.IP_IODYNAMIC, ServerConfig.PORT_STORE, "web/check_buy/"), param);
			Log.E(response.ToString());
			bool isBuy = false, isUsed = true;
			double coin = 0d;
			try {
				JSONObject json = new JSONObject(response);
				isBuy = json.GetBoolean("is_buy");
				if (isBuy) {
					isUsed = json.GetBoolean("is_used");
					if (!isUsed) {
						coin = json.GetDouble("coin");
					}
				}
			} catch (Exception) {
				return new IOApi.Error("CAN_NOT_CHECK_PURCHASE");
			}

			if (!isBuy) {
				await UserConfig.AddTooManyRequestAsync(user.id, Error.PURCHASE_FAILED);
				return new IOApi.Error("PURCHASE_NOT_FOUND");
			}
			if (isUsed) {
				await UserConfig.AddTooManyRequestAsync(user.id, Error.PURCHASE_HAS_USED);
				return new IOApi.Error("PURCHASE_HAS_USED");
			}

			gRPC_Member.IncrementCoinReq reqIncrementCredit = new gRPC_Member.IncrementCoinReq();
			reqIncrementCredit.UserId = user.id;
			reqIncrementCredit.Count = coin;
			gRPC_Member.IncrementCoinRes resIncrementCredit = await GRPC.GetMember().IncrementCoinAsync(reqIncrementCredit);
			if (!String.IsNullOrEmpty(resIncrementCredit.Error)) {
				return new IOApi.Error("INTERNAL_ERROR");
			}

			response = await Network.RequestPostAsync(Network.CreateUrl(Network.SCHEME_HTTP, ServerConfig.IP_IODYNAMIC, ServerConfig.PORT_STORE, "web/set_add/"), param);
			Log.E(response.ToString());
			try {
				JSONObject json = new JSONObject(response);
				string status = json.GetString("status");
				if (status != "ok") {
					Log.SendEmailKorosh("Shop", "Error set_buy, response:" + response);
				}
			} catch (Exception e) {
				Log.SendEmailKorosh("Shop", "Error set_buy, response:" + response + "\r\n" + "error: " + e);
			}

			IOApi.UpdateCoin updateCoin = new IOApi.UpdateCoin();
			updateCoin.coin = resIncrementCredit.Coin;
			updateCoin.unixTime = resIncrementCredit.UnixTime;
			updates[0] = updateCoin;
			UserConfig.SendUpdateToGroup(user.id, user.connectionId, updateCoin);

			return new IOApi.Ok();
		}



		public static async Task<IOObject> CheckBuyAndroid(UserConfig.User user, IOApi.CheckBuyAndroid ioFunction, IOApi.Update[] updates) {
			IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.id, Error.PURCHASE_FAILED, Error.PURCHASE_HAS_USED);
			if (error != null) {
				return error;
			}

			if (ioFunction.market == 0x00 ||
				String.IsNullOrEmpty(ioFunction.app) ||
				String.IsNullOrEmpty(ioFunction.sku) ||
				String.IsNullOrEmpty(ioFunction.token)) {
				return new IOApi.Error("INVALID_PARAMETERS");
			}

			/*Dictionary<string, string> param = new Dictionary<string, string>();
			param.Add("market", ioFunction.market + "");
			param.Add("app", ioFunction.app);
			param.Add("sku", ioFunction.sku);
			param.Add("token", ioFunction.token);
			string response = await Network.RequestPostAsync("http://" + ServerConfig.IP_IODYNAMIC + ":" + ServerConfig.PORT_STORE + "/android/check_buy/", param);
			bool isBuy, isUsed = false;
			try {
				JSONObject json = new JSONObject(response);
				isBuy = json.GetBoolean("is_buy");
				if (isBuy) {
					isUsed = json.GetBoolean("is_used");
				}
			} catch (Exception) {
				return new IOApi.Error("CAN_NOT_CHECK_PURCHASE");
			}
			*/
			Controllers.Store.Android.Buy buy = null;
			if (ioFunction.market == 0x02) {
				buy = await Controllers.Store.Android.CheckBazaar(ioFunction.app, ioFunction.sku, ioFunction.token);

			} else if (ioFunction.market == 0x03) {
				buy = await Controllers.Store.Android.CheckMyket(ioFunction.app, ioFunction.sku, ioFunction.token);
			}
			if (buy == null) {
				return new IOApi.Error("CAN_NOT_CHECK_PURCHASE");
			}

			if (!buy.isBuy) {
				//Email
				await UserConfig.AddTooManyRequestAsync(user.id, Error.PURCHASE_FAILED);
				return new IOApi.Error("PURCHASE_NOT_FOUND");
			}
			if (buy.isUsed) {
				await UserConfig.AddTooManyRequestAsync(user.id, Error.PURCHASE_HAS_USED);
				return new IOApi.Error("PURCHASE_HAS_USED");
			}

			double coin = 0d;
			ioFunction.sku = ioFunction.sku.Replace("Package", "");
			try {
				foreach (IOApi.StorePackageItem item in pakageIR.list) {
					if (item.id == ioFunction.sku) {
						coin = item.coin;
					}
				}
			} catch (Exception) {}

			if (coin == 0d) {
				return new IOApi.Error("PURCHASE_NOT_FOUND");
			}

			gRPC_Member.IncrementCoinRes resIncrementCredit;
			using (await threadLock.LockAsync(ioFunction.market + ioFunction.token)) {
				Dictionary<string, string> param = new Dictionary<string, string>();
				param.Add("market", ioFunction.market + "");
				param.Add("token", ioFunction.token);
				string res = await Network.RequestPostAsync("http://" + ServerConfig.IP_IODYNAMIC + ":" + ServerConfig.PORT_STORE + "/android/get_add/", param);
				WebSocket_Log.Send("/android/get_add/",  "res " + res);
				if (res == "USED") {
					await UserConfig.AddTooManyRequestAsync(user.id, Error.PURCHASE_HAS_USED);
					return new IOApi.Error("PURCHASE_HAS_USED");

				} else if (res == "ROW_NO_IN_DB") {//OK!
					gRPC_Member.IncrementCoinReq reqIncrementCredit = new gRPC_Member.IncrementCoinReq();
					reqIncrementCredit.UserId = user.id;
					reqIncrementCredit.Count = coin;
					resIncrementCredit = await GRPC.GetMember().IncrementCoinAsync(reqIncrementCredit);
					if (!String.IsNullOrEmpty(resIncrementCredit.Error)) {
						return new IOApi.Error("INTERNAL_ERROR");
					}

					param.Clear();
					param.Add("market", ioFunction.market + "");
					param.Add("token", ioFunction.token);
					param.Add("userId", user.id + "");
					await Network.RequestPostAsync("http://" + ServerConfig.IP_IODYNAMIC + ":" + ServerConfig.PORT_STORE + "/android/set_add/", param);

				} else {
					return new IOApi.Error("CAN_NOT_CHECK_PURCHASE");
				}
			}

			IOApi.UpdateCoin updateCoin = new IOApi.UpdateCoin();
			updateCoin.coin = resIncrementCredit.Coin;
			updateCoin.unixTime = resIncrementCredit.UnixTime;
			updates[0] = updateCoin;

			UserConfig.SendUpdateToGroup(user.id, user.connectionId, updateCoin);

			return new IOApi.Ok();
		}


	}
}
