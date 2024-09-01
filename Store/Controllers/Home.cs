using Microsoft.AspNetCore.Mvc;
using Store.IONet;
using System;
using Library.Json;
using System.Text;
using System.IO;
using System.Xml;

namespace Store.Controllers {

	[ApiController]
	[Route("")]
	public class Home : Controller {

		public static JSONObject jsonPackage, jsonOffer;


		[HttpGet("my_test")]
		public string My_Test() {
				return "";
		}



		[HttpGet("get_packages")]
		public ActionResult GetStorePackagesGet() {
			if (!Network.GetIp(HttpContext).StartsWith("10.0.0") && Network.GetIp(HttpContext) != "49.12.41.74") {
				return NotFound();
			}

			if (jsonPackage == null) {
				return NoContent();
			}
			return Content(jsonPackage.ToString(), "application/json", Encoding.ASCII);
		}

		[HttpPost("get_packages")]
		public ActionResult GetStorePackagesPost() {
			if (!Network.GetIp(HttpContext).StartsWith("10.0.0") && Network.GetIp(HttpContext) != "49.12.41.74") {
				return NotFound();
			}

			if (jsonPackage == null) {
				return NoContent();
			}
			return Content(jsonPackage.ToString(), "application/json", Encoding.ASCII);
		}


		[HttpPost("get_offers")]
		public ActionResult GetOffersPost([FromForm] string country_code) {
			if (!Network.GetIp(HttpContext).StartsWith("10.0.0")) {
				return NotFound();
			}
			if (jsonOffer == null) {
				return NoContent();
			}
			return Content(jsonOffer.ToString(), "application/json", Encoding.ASCII);
		}

		[HttpGet("reload")]
		public ActionResult Reload() {
			try {
				OnLoadPackages();
				return Content(jsonPackage.ToString(Newtonsoft.Json.Formatting.Indented), "application/json", Encoding.ASCII);
			} catch (Exception e) {
				return Content(e.ToString());
			}
		}


		public static void OnLoadPackages() {

			jsonPackage = null;
			jsonOffer = null;

			StreamReader streamReader = new StreamReader(App.GetDirectory() + "appsettings.json");
			string str = streamReader.ReadToEnd();
			streamReader.Dispose();
			streamReader.Close();
			JSONObject json = new JSONObject(str);

			jsonPackage = json.GetJSONObject("Packages");
			jsonOffer = json.GetJSONObject("Offers");

			//-- test Full GetChildren() work in this PC, not work in server
			/*IConfigurationSection configurationsectionAndroid = App.Configuration().GetSection("StorePackage:Android");
			foreach (IConfigurationSection configurationsection in configurationsectionAndroid.GetSection("Gem").GetChildren()) {
				IOApi.StorePackageContent storePackageContent = new IOApi.StorePackageContent();
				storePackageContent.credit = configurationsection.GetValue<int>("Credit");
				storePackageContent.dollar = configurationsection.GetValue<int>("Dollar");
				storePackageContent.icon = configurationsection.GetValue<byte>("Icon");
				storePackages.listGem.Add(storePackageContent);
			}*/
		}

	}
}
