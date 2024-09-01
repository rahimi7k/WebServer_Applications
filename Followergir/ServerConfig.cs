using Microsoft.AspNetCore.Http;
using System;
using System.Xml.Linq;

namespace Followergir {

	public class ServerConfig {

		/**
		 *  Environment.MachineName =>	SAEED-PC
		 *  Dns.GetHostName() =>		Saeed-PC
		 */
		private static readonly string[] IP_HOME = { "162.55.166.253", "167.235.200.54" };
		private static readonly string IP_HOME_RANGE = "185.129.";
		private static readonly string PASSWORD = "04BaSWV*gVU9#RU1n1@jGLeMo";
 	

		public static readonly string IP_WEB_SERVER = App.IsDebug() ? "65.108.153.5" : "10.0.0.2";
		public static readonly string IP_PROCESS = App.IsDebug() ? "162.55.166.253" : "127.0.0.1";
		public static readonly string IP_DATABASE = App.IsDebug() ? "88.99.191.195" : "10.0.0.3";
		public static readonly string IP_IODYNAMIC = App.IsDebug() ? "78.47.45.81" : "10.0.0.5";

		public static readonly int PORT_STORE = 7000;
		public static readonly int PORT_API = 7002;
		public static readonly int PORT_LOG = 8000;
		public static readonly int PORT_GRPC_MEMBER = 10000;
		public static readonly int PORT_GRPC_FOLLOWERGIR = 10001;

		public static readonly string DATABASE = "Server=" + (App.IsDebug() ? IP_PROCESS + ",45100" : Environment.MachineName) + ";Database=Main;User Id=sa;Password=481516kR2342$#;";
		public static readonly string DATABASE_FOLLOWERGIR_OLD = "server=localhost;user=root;database=followergr_instagram;port=3306;password=481516kR2342$#";

		public static readonly string MAIL_HOST = "10.0.0.2";
		public static readonly string MAIL_ADDRESS = "no-reply@iofollowergir.com";
		public static readonly string MAIL_PASSWORD = "481516kR2342$#";

		public static readonly string IODYNAMIC_MAIL_HOST = "10.0.0.5";
		public static readonly string AUTH_MAIL_ADDRESS = "authentication@iodynamic.com";
		public static readonly string AUTH_MAIL_PASSWORD = "481516kR2342$#";


		private static long startingTime;





		public static void setTimer() {
			startingTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		}

		public static void endTimer() {
			Console.WriteLine(DateTimeOffset.Now.ToUnixTimeMilliseconds() - startingTime);
		}

		public static void endTimer(string name) {
			Console.WriteLine(name + " -- " + (DateTimeOffset.Now.ToUnixTimeMilliseconds() - startingTime));
		}




		public static bool isHome(HttpContext httpContext, string password) {

			if (password != PASSWORD) {
				return false;
			}

			//string ip = httpContext.Request.Headers["X-Forwarded-For"];
			string ip = httpContext.Connection.RemoteIpAddress + "";

			if (ip == IP_HOME[0] || ip == IP_HOME[1] || ip.StartsWith(IP_HOME_RANGE)) {
				return true;
			}

			return false;
		}


	}

}
