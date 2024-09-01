using Followergir.Controllers;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Timers;

namespace Followergir.IONet {

	public abstract class Redis {

		//public static readonly string USER_ID = "I";
		public static readonly string NAME = "N";
		public static readonly string PHONE = "P";
		public static readonly string COIN = "C";
		public static readonly string DATE = "D";
		public static readonly string SESSION = "S";
		public static readonly string BLOCK = "B";

		public static readonly string USERNAME = "U";
		public static readonly string ORDER_USER_ID = "OUI";
		public static readonly string SEFARESH = "O";
		public static readonly string MANDE = "R";
		public static readonly string ERROR = "E";
		public static readonly string ERROR_COUNT = "EC";

		public static readonly string CARD_ID = "CI";
		public static readonly string LAST_ORDER_ID = "LOI";

		//public static readonly string TYPE = "T";
		//public static readonly string HASH = "HASH";
		//public static readonly string CODE = "CODE";


		public static ConnectionMultiplexer connectionDefault;

		static Redis() {
			ConfigurationOptions config = new ConfigurationOptions();
			config.EndPoints.Add(ServerConfig.IP_PROCESS, 6379);
			config.DefaultDatabase = 0;
			config.AbortOnConnectFail = false;
			// config.Password = "7g$4%bf8Q_aFL5om-N9O0$l1IV7";
			connectionDefault = ConnectionMultiplexer.Connect(config);
		}

		
	
		public static IDatabase GetDatabaseBlock() {
			return connectionDefault.GetDatabase(1);
		}

		public static IDatabase GetDatabaseLimit() {
			return connectionDefault.GetDatabase(2);
		}

		public static IDatabase GetDatabaseData() {
			return connectionDefault.GetDatabase(3);
		}

		public static IDatabase GetDatabaseUnFollow() {
			return connectionDefault.GetDatabase(4);
		}

	}
}
