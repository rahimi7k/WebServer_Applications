using Microsoft.Win32;
using Library;
using Library.SQL;
using System;

namespace Store {
	public class ServerConfig {

		public static readonly string IP_IODYNAMIC = "10.0.0.5";
		public static readonly string IP_FOLLOWERGIR_PROCESS = "10.0.0.4";

		public static readonly string DATABASE_MAIN = "Server=" + (App.IsDebug() ? "78.47.45.81,45100" : Environment.MachineName) + ";Database=Main;User Id=sa;Password=481516kR2342$#;";

		public static readonly int PORT_STORE = 7000;



		public static readonly string MAIL_HOST = "10.0.0.2";
		public static readonly string MAIL_ADDRESS = "no-reply@iofollowergir.com";
		public static readonly string MAIL_PASSWORD = "481516kR2342$#";


	}
}
