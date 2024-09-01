using StackExchange.Redis;
using System;
using System.Collections.Generic;

namespace WebServer {

	public abstract class Redis {

		private static readonly string ipWebSever = App.IsDebug() ? "127.0.0.1" : "157.90.251.170";

		private static ConnectionMultiplexer connectionMultiplexer;


		static Redis() {

			ConfigurationOptions config = new ConfigurationOptions();
			config.EndPoints.Add(ipWebSever, 6379);
			config.DefaultDatabase = 0;
			// config.Password = "481516kR2342$#";
			connectionMultiplexer = ConnectionMultiplexer.Connect(config);

		}

		/// <summary>
		/// WebServer
		/// </summary>
		/// <returns>Redis Database ServerList <strong>1</strong></returns>
		public static StackExchange.Redis.IDatabase GetDatabaseDefault() {
			return connectionMultiplexer.GetDatabase(1);
		}

		/// <summary>
		/// WebServer
		/// </summary>
		/// <returns>Redis Database User <strong>2</strong></returns>
		public static StackExchange.Redis.IDatabase GetDatabaseUser() {
			return connectionMultiplexer.GetDatabase(2);
		}


		/// <summary>
		/// WebServer
		/// </summary>
		/// <returns>Redis Database TooManyRequest <strong>4</strong></returns>
		public static StackExchange.Redis.IDatabase GetDatabaseTooManyRequest() {
			return connectionMultiplexer.GetDatabase(3);
		}

	}
}
