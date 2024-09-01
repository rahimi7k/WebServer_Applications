using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Microsoft.AspNetCore.Authentication;
using System.Threading.Tasks;
using Followergir.IONet;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Followergir.Controllers;
using Microsoft.AspNetCore.Http;
using Followergir.IO;
using System.Collections.Generic;
using System.IO;
using Library;

namespace Followergir {

	public class Startup {

		public static IConfiguration configuration;
		//public static IHubContext<HubConnection> hubContext;

		public Startup(IConfiguration configuration) {
			Startup.configuration = configuration;
			//vvvv.DisconnectTimeout = TimeSpan.FromSeconds(1);

		}

		public void ConfigureServices(IServiceCollection services) {

			services.AddCors(new Action<CorsOptions>(delegate (CorsOptions options) {
				CorsPolicyBuilder corsPolicyBuilder = new CorsPolicyBuilder();
				corsPolicyBuilder.AllowAnyHeader();
				corsPolicyBuilder.AllowAnyMethod();
				corsPolicyBuilder.AllowCredentials();
				corsPolicyBuilder.WithOrigins(
					"https://iodynamic.com",
					"https://www.iodynamic.com",
					"https://followergir.iodynamic.com",
					"https://membergir.iodynamic.com",
					"http://localhost:4200");
				options.AddPolicy("IODynamic", corsPolicyBuilder.Build());
			}));




			services.AddControllers(new Action<MvcOptions>(delegate (MvcOptions options) {
				options.InputFormatters.Insert(0, new ByteArrayFormatters.ByteArrayInputFormatter());
				//options.OutputFormatters.Insert(0, new ByteArrayFormatters.ByteArrayOutputFormatter());

				//options.Filters.Add<ValidationFilter>();
				//options.ModelBinderProviders.Insert(0, new ModelBinderProvider());
			}));

			IMvcBuilder mvcBuilder = services.AddMvc(new Action<MvcOptions>(delegate (MvcOptions options) {

			}));
			mvcBuilder.SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
			mvcBuilder.ConfigureApiBehaviorOptions(new Action<ApiBehaviorOptions>(delegate (ApiBehaviorOptions options) {
				options.SuppressConsumesConstraintForFormFileParameters = true;
				options.SuppressInferBindingSourcesForParameters = true;
				options.SuppressModelStateInvalidFilter = true;
				options.SuppressMapClientErrors = true;
				// options.ClientErrorMapping[404].Link = "https://google.com";
			}));

		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
			if (env.IsDevelopment()) {
				//app.UseDeveloperExceptionPage();
			}

			Microsoft.AspNetCore.Builder.WebSocketOptions webSocketOptions = new Microsoft.AspNetCore.Builder.WebSocketOptions();
			webSocketOptions.KeepAliveInterval = TimeSpan.FromSeconds(120);
			app.UseWebSockets(webSocketOptions);

			app.UseRouting();


			/*
			app.Use(new Func<HttpContext, Func<Task>, Task>(async delegate (HttpContext context, Func<Task> next) {
				Log.E("Befor all http req link: " + context.Request.Path);
				await next();
			}));
			*/
			/*
			app.UseCors(new Action<CorsPolicyBuilder>(delegate (CorsPolicyBuilder cors) {
				cors.WithOrigins("https://iodynamic.com", "https://www.iodynamic.com", "http://localhost:4200")
				.AllowAnyHeader()
					.AllowAnyMethod()
					.AllowCredentials();
			}));
			*/
			app.UseCors("IODynamic");


			app.UseEndpoints(new Action<IEndpointRouteBuilder>(delegate (IEndpointRouteBuilder endpoints) {
				endpoints.MapControllers();

			}));

		}


	}
}
