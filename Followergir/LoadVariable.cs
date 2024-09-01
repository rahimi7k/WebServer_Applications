using Followergir.IO;
using Followergir.IONet;
using Followergir.Methods;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Library.Json;

namespace Followergir {

	public class LoadVariable {

		public static void Store() {

			string response = Network.RequestPost(Network.CreateUrl(Network.SCHEME_HTTP, ServerConfig.IP_IODYNAMIC, ServerConfig.PORT_STORE, "get_packages/"), null);
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("response packages" + response);
			Console.ResetColor();

			try {
				JSONObject json = new JSONObject(response);
		

				IOApi.StorePackages package = new IOApi.StorePackages();
				package.list = new List<IOApi.StorePackageItem>();
				JSONObject jsonIR = json.GetJSONObject("IR");
				foreach (KeyValuePair<string, JToken> keyVal in jsonIR) {
					IOApi.StorePackageItem storePackageContent = new IOApi.StorePackageItem();
					storePackageContent.id = keyVal.Key;
					storePackageContent.coin = keyVal.Value["coin"].Value<double>();
					storePackageContent.price = keyVal.Value["price"].Value<double>();
					package.list.Add(storePackageContent);
				}
				Methods.Store.pakageIR = package;


				package = new IOApi.StorePackages();
				package.list = new List<IOApi.StorePackageItem>();
				JSONObject jsonEN = json.GetJSONObject("EN");
				foreach (KeyValuePair<string, JToken> keyVal in jsonEN) {
					IOApi.StorePackageItem storePackageContent = new IOApi.StorePackageItem();
					storePackageContent.id = keyVal.Key;
					storePackageContent.coin = keyVal.Value["coin"].Value<double>();
					storePackageContent.price = keyVal.Value["price"].Value<double>();
					package.list.Add(storePackageContent);
				}
				Methods.Store.pakageEN = package;

			} catch (Exception e) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("InitStore, Error: " + e);
				Console.ResetColor();
			}
	
		}



		public static void Message() {

			UserConfig.messageFa = new List<IOApi.Message>();
			UserConfig.messageEn = new List<IOApi.Message>();

			try {
				StreamReader streamReader = new StreamReader(App.GetDirectory() + "appsettings.json");
				string str = streamReader.ReadToEnd();
				streamReader.Dispose();
				streamReader.Close();
				JSONObject json = new JSONObject(str);

				UserConfig.systemMessage.Add("UnFollow_En", json.GetJSONObject("Message").GetJSONObject("En").GetString("UnFollow"));
				UserConfig.systemMessage.Add("UnCredit_En", json.GetJSONObject("Message").GetJSONObject("En").GetString("UnCredit"));

				UserConfig.systemMessage.Add("UnCredit_Fa", json.GetJSONObject("Message").GetJSONObject("Fa").GetString("UnCredit"));
				UserConfig.systemMessage.Add("UnFollow_Fa", json.GetJSONObject("Message").GetJSONObject("Fa").GetString("UnFollow"));


				JSONArray jsonArray = json.GetJSONObject("Message").GetJSONObject("En").GetJSONArray("List");
				for (int i = 0; i < jsonArray.Count; i++) {
					IOApi.Message msg = new IOApi.Message();
					msg.id = jsonArray.GetJSONObject(i).GetString("id");
					msg.text = jsonArray.GetJSONObject(i).GetString("text");
					UserConfig.messageEn.Add(msg);
				}

				jsonArray = json.GetJSONObject("Message").GetJSONObject("Fa").GetJSONArray("List");
				//Console.WriteLine(jsonArray.ToString());
				for (int i = 0; i < jsonArray.Count; i++) {
					IOApi.Message msg = new IOApi.Message();
					msg.id = jsonArray.GetJSONObject(i).GetString("id");
					msg.text = jsonArray.GetJSONObject(i).GetString("text");
					UserConfig.messageFa.Add(msg);
				}

			} catch (Exception e) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("InitMessage, Message Error: " + e);
				Console.ResetColor();
			}



		}




	}
}
