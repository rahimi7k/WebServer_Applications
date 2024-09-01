using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using Library;
using Library.Json;
using Store.Controllers;
using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;

namespace Store {

	public class App {

		public static void Main(string[] args) {

			if (args != null && args.Length != 0) {
				foreach (string param in args) {
					if (param == "") {
						//TaskScheduler.MoveToDatabase();
						return;
					}
				}
			}

			Home.OnLoadPackages();
			Update.OnLoadUpdates();

			//-- https://github.com/aspnet/MetaPackages/blob/master/src/Microsoft.AspNetCore/WebHost.cs
			IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);
			hostBuilder.ConfigureWebHostDefaults(new Action<IWebHostBuilder>(delegate (IWebHostBuilder webHostBuilder) {
				webHostBuilder.UseStartup<Startup>();
			}));
			IHost host = hostBuilder.Build();

			host.Run();
		}

		public static IConfiguration Configuration() {
			if (Startup.configuration == null) {
				ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
				//configurationBuilder.SetBasePath(GetDirectory());
				configurationBuilder.AddJsonFile("appsettings.json", true, false);
				return configurationBuilder.Build();
			}
			return Startup.configuration;
		}



		public static bool IsDebug() {
			//string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
			//return environment != null && environment == Environments.Development;
			return Debugger.IsAttached;
		}

		public static bool IsRun() {
			Process currentProcess = Process.GetCurrentProcess();
			foreach (Process process in Process.GetProcesses()) {
				if (process.ProcessName.Contains(currentProcess.ProcessName)) {
					return true;
				}
			}
			return false;
		}

		public static string GetName() {
			return Assembly.GetExecutingAssembly().GetName().Name;
		}

		public static Version GetVersion() {
			return Assembly.GetExecutingAssembly().GetName().Version;
		}

		public static string GetDirectory() {
			/*
			AppDomain.CurrentDomain.BaseDirectory						=> C:\App\
			Assembly.GetEntryAssembly().Location						=> C:\App\App.dll
			Assembly.GetExecutingAssembly().Location					=> C:\App\App.dll
			Directory.GetCurrentDirectory()								=> C:\inetpub\wwwroot\App
			Environment.CurrentDirectory								=> C:\inetpub\wwwroot\App
			Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)	=> C:\App
			*/
			return AppDomain.CurrentDomain.BaseDirectory;
		}

		public static void Exit() {

		}
	}
}
