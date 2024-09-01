using Followergir.IO;
using Followergir.IONet;
using Library;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Followergir {

	public class App {

		public static readonly string ADDRESS_REGISTRY = "SOFTWARE\\IODynamic\\" + GetName();


		public static void Main(string[] args) {

			if (args != null && args.Length != 0) {
				foreach (string param in args) {
					if (param == "CheckUnFollow") {
						TaskScheduler.CheckUnFollow();
						return;
					}
				}
			}

			//https://www.cloudflare.com/cdn-cgi/trace
			string ress = Network.RequestPost("https://store.iodynamic.com/get_packages", null);
			Console.WriteLine("ress: " + ress);


			IOApi.ListOrderUser sss = new IOApi.ListOrderUser();
			sss.iId = 11;
			//sss.T = "OrderListUser";

			IOApi.ListOrderLike sss2 = new IOApi.ListOrderLike();
			sss2.iId = 22;
			//sss2.T = "OrderListLike";

			IOApi.UnFollow c2 = new IOApi.UnFollow();
			//c2.likeeeee = sss2;



			IOApi.ListOrderLike sss333 = new IOApi.ListOrderLike();
			sss333.iId = 3333;
			//sss333.T = "OrderListComment";
			//sss333.c2 = c2;


			IOApi.GetListOrder res = new IOApi.GetListOrder();
			res.list = new List<IOApi.ListOrderItem>();
			res.list.Add(sss);
			res.list.Add(sss2);

			res.list.Add(sss333);




			var json2000 = "{\"order1\":{\"T\":\"ListOrderUser\",\"iId\":11,\"orderId\":null,\"hash\":null},\"list\":[{\"T\":\"ListOrderUser\",\"iId\":11,\"orderId\":null,\"hash\":null},{\"T\":\"ListOrderLike\",\"iId\":22,\"orderId\":null,\"hash\":null},{\"T\":\"ListOrderComment\",\"text\":\"MMMM\",\"iId\":3333,\"orderId\":null,\"hash\":null}],\"T\":\"GetListOrder\"}";



			// json2 = "{\"T\":\"UnCoin\",\"count\":10,\"coin\":15}";
			// IOApi.UnCoin class1 = (IOApi.UnCoin) SerializedData.DeserializeObject(json2);



			//var json = SerializedData.SerializeObject(class1);
			//Log.E("json class1: " + json);

			/*
			// 
			JsonSerializerOptions opt = new JsonSerializerOptions();

			IOApi.GetListOrder c = JsonSerializer.Deserialize<IOApi.GetListOrder>( (json2);
			Log.E("DeserializeObject class: " + c);
			*/

			//Log.SendEmail("aaa", "bbb");
			//JsonSerializer.Serialize((json2);



			Console.WriteLine("isDebug: " + IsDebug());
			if (IsDebug()) {

			}



			LoadVariable.Store();
			LoadVariable.Message();



			/*FileExtensionContentTypeProvider fileExtensionContentTypeProvider = new FileExtensionContentTypeProvider();
			fileExtensionContentTypeProvider.Mappings.Add(".exe", "application/vnd.microsoft.portable-executable");
			fileExtensionContentTypeProvider.Mappings.Add(".apk", "application/vnd.android.package-archive");

			StaticFileOptions staticFileOptions = new StaticFileOptions();
			staticFileOptions.FileProvider = new PhysicalFileProvider(App.GetDirectory() + "Download");
			staticFileOptions.RequestPath = new PathString("/dl");
			staticFileOptions.ContentTypeProvider = fileExtensionContentTypeProvider;
			staticFileOptions.ServeUnknownFileTypes = false;
			//staticFileOptions.DefaultContentType = 
			app.UseStaticFiles(staticFileOptions);*/


			//-- https://github.com/aspnet/MetaPackages/blob/master/src/Microsoft.AspNetCore/WebHost.cs
			IHostBuilder builder = Host.CreateDefaultBuilder(args);

			builder.ConfigureLogging(new Action<ILoggingBuilder>(delegate (ILoggingBuilder configureLogging) {
				configureLogging.AddFilter("Grpc", LogLevel.Debug);
			}));

			builder.ConfigureWebHostDefaults(new Action<IWebHostBuilder>(delegate (IWebHostBuilder webHostBuilder) {

				webHostBuilder.ConfigureKestrel(new Action<KestrelServerOptions>(delegate (KestrelServerOptions options) {

					if (IsDebug()) {
						options.Listen(new IPAddress(new byte[] { 127, 0, 0, 1 }), 5005, new Action<ListenOptions>(delegate (ListenOptions configure) {
							configure.Protocols = HttpProtocols.Http1;
						}));

					} else {

						options.Listen(new IPAddress(new byte[] { 127, 0, 0, 1 }), 80, new Action<ListenOptions>(delegate (ListenOptions configure) {
							configure.Protocols = HttpProtocols.Http1;
						}));

						//HTTP
						options.Listen(new IPAddress(new byte[] { 10, 0, 0, 4 }), 80, new Action<ListenOptions>(delegate (ListenOptions configure) {
							configure.Protocols = HttpProtocols.Http1;
						}));

						//HTTPS
						options.Listen(new IPAddress(new byte[] { 10, 0, 0, 4 }), 443, new Action<ListenOptions>(delegate (ListenOptions configure) {
							configure.Protocols = HttpProtocols.Http1;
						}));

						//IP_Log
						options.Listen(new IPAddress(new byte[] { 162, 55, 166, 253 }), ServerConfig.PORT_LOG, new Action<ListenOptions>(delegate (ListenOptions configure) {
							configure.Protocols = HttpProtocols.Http1;
						}));

						options.Listen(new IPAddress(new byte[] { 162, 55, 166, 253 }), 80, new Action<ListenOptions>(delegate (ListenOptions configure) {
							configure.Protocols = HttpProtocols.Http1;
						}));


					}
				}));


				webHostBuilder.ConfigureServices(new Action<WebHostBuilderContext, IServiceCollection>(delegate (WebHostBuilderContext context, IServiceCollection configureServices) {

				}));

				webHostBuilder.Configure(new Action<IApplicationBuilder>(delegate (IApplicationBuilder app) {


				}));

				webHostBuilder.UseStartup<Startup>();
			}));

			IHost host = builder.Build();


			//Action.ManageActionUnFollowRequest();

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
