using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace WebServer {

	public class Startup {

		public static IConfiguration configuration;

		public Startup(IConfiguration configuration) {
			Startup.configuration = configuration;
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
					"https://tg.iodynamic.com",
					"https://membergir.iodynamic.com",
					"https://followergir.iodynamic.com",
					"http://localhost:4200");
				options.AddPolicy("IODynamic", corsPolicyBuilder.Build());
			}));

			services.AddControllers(new Action<MvcOptions>(delegate (MvcOptions options) {

			}));

			IMvcBuilder mvcBuilder = services.AddMvc(new Action<MvcOptions>(delegate (MvcOptions options) {

			}));
			mvcBuilder.SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
			mvcBuilder.ConfigureApiBehaviorOptions(new Action<ApiBehaviorOptions>(delegate (ApiBehaviorOptions options) {
				options.SuppressConsumesConstraintForFormFileParameters = true;
				options.SuppressInferBindingSourcesForParameters = true;
				options.SuppressModelStateInvalidFilter = true;
				options.SuppressMapClientErrors = true;
				options.ClientErrorMapping[404].Link = "https://google.com";
			}));

		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
			if (env.IsDevelopment()) {
				//app.UseDeveloperExceptionPage();
			}

			//app.UseHttpsRedirection();
			//app.UseAuthorization();
			app.UseRouting();

			/*
			app.UseCors(new Action<CorsPolicyBuilder>(delegate (CorsPolicyBuilder cors) {
				cors.AllowAnyHeader()
					.AllowAnyMethod()
					.AllowAnyOrigin()
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
