using Microsoft.AspNetCore.Mvc;
using Library.Json;
using System;
using System.IO;
using System.Text;

using System.Collections.Generic;

namespace Store.Controllers {

	[ApiController]
	[Route("update")]
	public class Update : Controller {

		public static JSONObject json;

		[HttpGet]
		public ActionResult IndexGet([FromQuery(Name = "os")] string operatingSystem, [FromQuery] string app) {
			return Index(operatingSystem, app);
		}

		[HttpPost]
		public ActionResult IndexPost([FromForm(Name = "os")] string operatingSystem, [FromForm] string app) {
			return Index(operatingSystem, app);
		}

		private ActionResult Index(string operatingSystem, string app) {
			if (string.IsNullOrEmpty(operatingSystem) || string.IsNullOrEmpty(app)) {
				return BadRequest();
			}

			try {
				return Content(json.GetJSONObject(operatingSystem).GetJSONObject(app).ToString(), "application/json", Encoding.ASCII);
			} catch (Exception) { }

			return BadRequest();

		}

		[HttpGet("reload")]
		public ActionResult Reload() {
			try {
				OnLoadUpdates();
				return Content(json.ToString(Newtonsoft.Json.Formatting.Indented), "application/json", Encoding.ASCII);
			} catch (Exception e) {
				return Content(e.ToString());
			}
		}

		public static void OnLoadUpdates() {
			/*FileSecurity fSecurity = System.IO.File.GetAccessControl("c:\\sss\\Update.json");
			DirectorySecurity dSecurity = fileInfo.Get();
			System.Security.Principal.WellKnownSidType.WorldSid
			WellKnownSidType.WorldSid*/

			StreamReader streamReader = new StreamReader(App.GetDirectory() + "appsettings.json");
			string str = streamReader.ReadToEnd();
			streamReader.Dispose();
			streamReader.Close();
			json = new JSONObject(str).GetJSONObject("Updates");
		}

	}
}
