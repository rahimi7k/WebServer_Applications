using Followergir.Controllers;
using Followergir.IO;
using Followergir.IONet;
using Library.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Followergir.Methods {

	public class Api: Add {



		public static async Task<IOObject> ApiGetSession(UserConfig.User user, IOApi.ApiGetSession ioFunction, IOApi.Update[] updates) {
			/*IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.id, IOError.ORDER_HAS_VIEWED);
			if (error != null) {
				return error;
			}*/

			Dictionary<string, string> param = new Dictionary<string, string>();
			param.Add("id", user.id + "");
			string response = await Network.RequestGetAsync(Network.CreateUrl(Network.SCHEME_HTTP, ServerConfig.IP_IODYNAMIC, ServerConfig.PORT_API, "get_session/"), param);
			WebSocket_Log.Send("API", response);
			try {
				JSONObject json = new JSONObject(response);
				string status = json.GetString("status");
				if (status == "ok") {
					IOApi.ApiSession apiSession = new IOApi.ApiSession();
					apiSession.session = json.GetString("session");
					apiSession.isActive = json.GetBoolean("isActive");
					return apiSession;

				} else {
					if (json.GetString("description") == "NOT_FOUND") {
						return new IOApi.Error("NOT_FOUND");
					}
				}

			} catch (Exception) {}

			return new IOApi.Error("INTERNAL_ERROR");
		}


		public static async Task<IOObject> ApiCreateOrUpdateSession(UserConfig.User user, IOApi.ApiCreateOrUpdateSession ioFunction, IOApi.Update[] updates) {
			/*IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.id, IOError.ORDER_HAS_VIEWED);
			if (error != null) {
				return error;
			}*/

			Dictionary<string, string> param = new Dictionary<string, string>();
			param.Add("id", user.id + "");
			string response = await Network.RequestPostAsync(Network.CreateUrl(Network.SCHEME_HTTP, ServerConfig.IP_IODYNAMIC, ServerConfig.PORT_API, "create_or_update/"), param);
			WebSocket_Log.Send("API", response);
			try {
				JSONObject json = new JSONObject(response);
				string status = json.GetString("status");
				if (status == "ok") {
					IOApi.ApiSession apiSession = new IOApi.ApiSession();
					apiSession.session = json.GetString("session");
					apiSession.isActive = true;
					return apiSession;
				}

			} catch (Exception) { }

			return new IOApi.Error("INTERNAL_ERROR");
		}


		public static async Task<IOObject> ApiUpdateActivation(UserConfig.User user, IOApi.ApiUpdateActivation ioFunction, IOApi.Update[] updates) {
			/*IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.id, IOError.ORDER_HAS_VIEWED);
			if (error != null) {
				return error;
			}*/

			Dictionary<string, string> param = new Dictionary<string, string>();
			param.Add("id", user.id + "");
			string response = await Network.RequestPostAsync(Network.CreateUrl(Network.SCHEME_HTTP, ServerConfig.IP_IODYNAMIC, ServerConfig.PORT_API, "activation/"), param);
			WebSocket_Log.Send("API", response);
			try {
				JSONObject json = new JSONObject(response);
				string status = json.GetString("status");
				if (status == "ok") {
					IOApi.ApiSession apiSession = new IOApi.ApiSession();
					apiSession.isActive = json.GetBoolean("isActive");
					return apiSession;
				}

			} catch (Exception) { }

			return new IOApi.Error("INTERNAL_ERROR");
		}




	}
}
