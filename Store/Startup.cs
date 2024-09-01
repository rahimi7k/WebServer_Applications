using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Store {

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
					"https://followergir.iodynamic.com",
					"http://localhost:4200");
				options.AddPolicy("IODynamic", corsPolicyBuilder.Build());
			}));

			ServicePointManager.ServerCertificateValidationCallback = delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
				return true;
			};

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



		/// <summary>
		///     This is to take care of SSL certification validation which are not issued by Trusted Root CA.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="certificate">The certificate.</param>
		/// <param name="chain">The chain.</param>
		/// <param name="sslPolicyErrors">The errors.</param>
		/// <returns></returns>
		/// <code></code>
		public static bool RemoteCertValidate(object sender
			, System.Security.Cryptography.X509Certificates.X509Certificate certificate
			, System.Security.Cryptography.X509Certificates.X509Chain chain
			, System.Net.Security.SslPolicyErrors sslPolicyErrors) {
			// If the certificate is a valid, signed certificate, return true.
			if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None) {
				return true;
			}
			return true;

			// Logger.Current.Error("X509Certificate [{0}] Policy Error: '{1}'", certificate.Subject, sslPolicyErrors);


			// If there are errors in the certificate chain, look at each error to determine the cause.
			if ((sslPolicyErrors & System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors) != 0) {
				if (chain != null && chain.ChainStatus != null) {
					foreach (System.Security.Cryptography.X509Certificates.X509ChainStatus status in chain.ChainStatus) {
						if ((certificate.Subject == certificate.Issuer) &&
						   (status.Status == System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.UntrustedRoot)) {
							// Self-signed certificates with an untrusted root are valid. 
							continue;
						} else if (status.Status == System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.NotTimeValid) {
							// Ignore Expired certificates
							continue;
						} else {
							if (status.Status != System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.NoError) {
								// If there are any other errors in the certificate chain, the certificate is invalid,
								// so the method returns false.
								return false;
							}
						}
					} // Next status 

				} // End if (chain != null && chain.ChainStatus != null) 

				// When processing reaches this line, the only errors in the certificate chain are 
				// untrusted root errors for self-signed certificates (, or expired certificates). 
				// These certificates are valid for default Exchange server installations, so return true.
				return true;
			} // End if ((sslPolicyErrors & System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors) != 0) 

			return false;
		}





	}




}