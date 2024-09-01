using Followergir.Controllers;
using Followergir.IO;
using Followergir.IONet;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Followergir.Methods {

	public class Authentication {

		public static readonly BindingFlags BINDING_FLAGS = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

		public static readonly char[] CHARACTERS = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-_".ToCharArray();

		private static readonly string SMS_PANEL_USERNAME = "";
		private static readonly string SMS_PANEL_PASSWORD = "";

		private static readonly byte STATE_LOGIN = 0x01;
		private static readonly byte STATE_FORGOT_PASSWORD = 0x02;
		private static readonly string SMS_URL = "https://api.kavenegar.com/v1/61775142654E687571536D41445555355864796B77465A4330747148503352667973382F6D7667303268513D/verify/lookup.json";

		//private static readonly RedisValue[] PHONE_CODE = new RedisValue[] { Redis.PHONE, Redis.CODE };

		public static async Task<IOObject> SetAuthenticationPhoneNumber______DELETE(UserConfig.User user, IOApi.SetAuthenticationPhoneNumber ioFunction) {
			IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.ip, Error.USER_ID_INVALID, Error.PHONE_NUMBER_INVALID, Error.CODE_INVALID);
			if (error != null) {
				return error;
			}

			if (String.IsNullOrEmpty(ioFunction.phoneNumber)) {
				return new IOApi.Error("INVALID_PARAMETERS");
			}
			//ioObject.phone = StringUtils.ToNumberFormat(ioObject.phone);//TODO check format

			gRPC_Member.GetUserByPhoneReq reqGetUser = new gRPC_Member.GetUserByPhoneReq();
			reqGetUser.PhoneNumber = ioFunction.phoneNumber;
			gRPC_Member.GetUserByPhoneRes resGetUser = await GRPC.GetMember().GetUserByPhoneAsync(reqGetUser);
			if (resGetUser.Block) {
				return new IOApi.Error("USER_BLOCK");
			}

			gRPC_Member.SetAuthenticationPhoneNumberReq reqAuth = new gRPC_Member.SetAuthenticationPhoneNumberReq();
			reqAuth.PhoneNumber = ioFunction.phoneNumber;
			reqAuth.Ip = user.ip;
			if (resGetUser.Id > 0L) {
				reqAuth.UserId = resGetUser.Id;
			}
			gRPC_Member.SetAuthenticationPhoneNumberRes resAuth = await IONet.GRPC.GetMember().SetAuthenticationPhoneNumberAsync(reqAuth);

			if (!String.IsNullOrEmpty(resAuth.Error)) {
				if (resAuth.Error == "FLOOD_WAIT") {
					return new IOApi.Error(resAuth.Error);
				}

				return null;
			}

			if (resGetUser.Id > 0L) {
				IOApi.AuthorizationStateWaitPassword resPassword = new IOApi.AuthorizationStateWaitPassword();
				resPassword.hash = resAuth.Hash;
				return resPassword;
			}

			if (resAuth.Code.Length == 0) {
				return null;
			}
			await SendSmsAsync(ioFunction.phoneNumber, resAuth.Code, user.language);

			IOApi.AuthorizationStateWaitCode res = new IOApi.AuthorizationStateWaitCode();
			res.hash = resAuth.Hash;
			res.length = resAuth.Code.Length;
			res.timeout = 120;
			return res;

		}


		public static async Task<IOObject> SetAuthenticationEmailAddress(UserConfig.User user, IOApi.SetAuthenticationEmailAddress ioFunction) {

			IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.ip, Error.USER_ID_INVALID, Error.EMAIL_ADDRESS_BANNED, Error.EMAIL_ADDRESS_INVALID, Error.CODE_INVALID);
			if (error != null) {
				return error;
			}

			if (String.IsNullOrEmpty(ioFunction.emailAddress)) {
				return new IOApi.Error("INVALID_PARAMETERS");
			}

			if (!Utils.IsEmailValid(ioFunction.emailAddress)) {
				return new IOApi.Error("INVALID_EMAIL_ADDRESS");
			}

			ioFunction.emailAddress = ioFunction.emailAddress.ToLower();

			string domain = ioFunction.emailAddress.Split('@')[1];
			if (domain != "gmail.com" &&
				domain != "yahoo.com" &&
				domain != "hotmail.com" &&
				domain != "outlook.com" &&
				domain != "mail.com" &&
				domain != "aol.com") {
				return new IOApi.Error("EMAIL_ADDRESS_NOT_SUPPORTED");
			}




			gRPC_Member.GetUserByEmailReq reqGetUser = new gRPC_Member.GetUserByEmailReq();
			reqGetUser.EmailAddress = ioFunction.emailAddress;
			gRPC_Member.GetUserByEmailRes resGetUser = await GRPC.GetMember().GetUserByEmailAsync(reqGetUser);
			if (resGetUser.Block) {
				return new IOApi.Error("USER_BLOCK");
			}

			gRPC_Member.SetAuthenticationEmailAddressReq reqAuth = new gRPC_Member.SetAuthenticationEmailAddressReq();
			reqAuth.EmailAddress = ioFunction.emailAddress;
			reqAuth.Ip = user.ip;
			if (resGetUser.Id > 0L) {
				reqAuth.UserId = resGetUser.Id;
			}
			gRPC_Member.SetAuthenticationEmailAddressRes resAuth = await IONet.GRPC.GetMember().SetAuthenticationEmailAddressAsync(reqAuth);

			if (!String.IsNullOrEmpty(resAuth.Error)) {
				if (resAuth.Error == "FLOOD_WAIT") {
					return new IOApi.Error(resAuth.Error);
				}

				return null;
			}

			if (resGetUser.Id > 0L) {
				IOApi.AuthorizationStateWaitPassword resPassword = new IOApi.AuthorizationStateWaitPassword();
				resPassword.hash = resAuth.Hash;
				return resPassword;
			}

			if (resAuth.Code.Length == 0) {
				return null;
			}
			await SendEmailAsync(ioFunction.emailAddress, resAuth.Code, user.language);

			IOApi.AuthorizationStateWaitCode res = new IOApi.AuthorizationStateWaitCode();
			res.hash = resAuth.Hash;
			res.length = resAuth.Code.Length;
			res.timeout = 120;
			return res;

		}





		public static async Task<IOObject> CheckAuthenticationCode(UserConfig.User user, IOApi.CheckAuthenticationCode ioFunction) {
			IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.ip, Error.HASH_INVALID, Error.CODE_INVALID, Error.HASH_EMPTY, Error.CODE_EMPTY);
			if (error != null) {
				return error;
			}

			if (String.IsNullOrEmpty(ioFunction.hash)) {
				return new IOApi.Error("HASH_EMPTY");
			}
			if (String.IsNullOrEmpty(ioFunction.code)) {
				return new IOApi.Error("CODE_EMPTY");
			}


			gRPC_Member.GetAuthenticationReq reqGetAuth = new gRPC_Member.GetAuthenticationReq();
			reqGetAuth.Hash = ioFunction.hash;
			gRPC_Member.GetAuthenticationRes resGetAuth = await IONet.GRPC.GetMember().GetAuthenticationAsync(reqGetAuth);


			if (String.IsNullOrEmpty(resGetAuth.Code)) {
				await UserConfig.AddTooManyRequestAsync(user.ip, Error.HASH_INVALID);
				return new IOApi.Error("HASH_INVALID");
			}

			if (ioFunction.code != resGetAuth.Code) {
				await UserConfig.AddTooManyRequestAsync(user.ip, Error.CODE_INVALID);
				return new IOApi.Error("CODE_INVALID");
			}

			return new IOApi.Ok();
		}



		public static async Task<IOObject> CheckAuthenticationPassword(UserConfig.User user, IOApi.CheckAuthenticationPassword ioFunction) {
			IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.ip, Error.USER_ID_INVALID, Error.PASSWORD_EMPTY, Error.PASSWORD_NOT_MATCH);
			if (error != null) {
				return error;
			}

			if (String.IsNullOrEmpty(ioFunction.hash)) {
				await UserConfig.AddTooManyRequestAsync(user.ip, Error.HASH_INVALID);
				return new IOApi.Error("HASH_INVALID");
			}
			if (String.IsNullOrEmpty(ioFunction.password)) {
				return new IOApi.Error("PASSWORD_EMPTY");
			}
			if (ioFunction.password.Length < Methods.User.PASSWORD_LENGTH_MIN) {
				return new IOApi.Error("PASSWORD_LENGTH_MIM");
			}
			if (ioFunction.password.Length > Methods.User.PASSWORD_LENGTH_MAX) {
				return new IOApi.Error("PASSWORD_LENGTH_MAX");
			}

			gRPC_Member.AuthenticationPasswordReq reqAuthenticationPassword = new gRPC_Member.AuthenticationPasswordReq();
			reqAuthenticationPassword.Hash = ioFunction.hash;
			reqAuthenticationPassword.Ip = user.ip;
			gRPC_Member.AuthenticationPasswordRes resAuthenticationPassword = await GRPC.GetMember().AuthenticationPasswordAsync(reqAuthenticationPassword);

			if (!String.IsNullOrEmpty(resAuthenticationPassword.Error)) {
				if (resAuthenticationPassword.Error == "HASH_INVALID") {
					await UserConfig.AddTooManyRequestAsync(user.ip, Error.HASH_INVALID);
					return new IOApi.Error("HASH_INVALID");
				}

				return null;
			}


			gRPC_Member.CheckPasswordReq reqCheckPassword = new gRPC_Member.CheckPasswordReq();
			reqCheckPassword.UserId = resAuthenticationPassword.UserId;
			reqCheckPassword.Password = ioFunction.password;
			gRPC_Member.CheckPasswordRes resCheckPassword = await GRPC.GetMember().CheckPasswordAsync(reqCheckPassword);
			if (!String.IsNullOrEmpty(resCheckPassword.Error)) {
				if (resCheckPassword.Error == "PASSWORD_NOT_MATCH") {
					return new IOApi.Error("PASSWORD_NOT_MATCH");
				}

				return null;
			}



			gRPC_Member.SetAuthenticationReadyReq reqReady = new gRPC_Member.SetAuthenticationReadyReq();
			reqReady.Hash = ioFunction.hash;
			gRPC_Member.SetAuthenticationReadyRes resReady = await IONet.GRPC.GetMember().SetAuthenticationReadyAsync(reqReady);

			user.id = resAuthenticationPassword.UserId; // Websocket Detect Authorize is Done
			IOApi.AuthorizationStateReady res = new IOApi.AuthorizationStateReady();
			res.userId = resAuthenticationPassword.UserId;
			res.session = resCheckPassword.Session;
			return res;
		}



		public static async Task<IOObject> SetAuthenticationRegister(UserConfig.User user, IOApi.SetAuthenticationRegister ioFunction) {
			IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.ip, Error.NAME_EMPTY, Error.PASSWORD_EMPTY, Error.HASH_INVALID);
			if (error != null) {
				return error;
			}

			if (String.IsNullOrEmpty(ioFunction.hash)) {
				await UserConfig.AddTooManyRequestAsync(user.ip, Error.HASH_INVALID);
				return new IOApi.Error("HASH_INVALID");
			}
			if (String.IsNullOrEmpty(ioFunction.name)) {
				return new IOApi.Error("NAME_EMPTY");
			}
			if (ioFunction.name.Length < Methods.User.NAME_LENGTH_MIN) {
				return new IOApi.Error("NAME_LENGTH_MIM");
			}
			if (ioFunction.name.Length > Methods.User.NAME_LENGTH_MAX) {
				return new IOApi.Error("NAME_LENGTH_MAX");
			}
			if (String.IsNullOrEmpty(ioFunction.password)) {
				return new IOApi.Error("PASSWORD_EMPTY");
			}
			if (ioFunction.password.Length < Methods.User.PASSWORD_LENGTH_MIN) {
				return new IOApi.Error("PASSWORD_LENGTH_MIM");
			}
			if (ioFunction.password.Length > Methods.User.PASSWORD_LENGTH_MAX) {
				return new IOApi.Error("PASSWORD_LENGTH_MAX");
			}


			gRPC_Member.GetAuthenticationReq reqGetAuth = new gRPC_Member.GetAuthenticationReq();
			reqGetAuth.Hash = ioFunction.hash;
			gRPC_Member.GetAuthenticationRes resGetAuth = await IONet.GRPC.GetMember().GetAuthenticationAsync(reqGetAuth);
			if (String.IsNullOrEmpty(resGetAuth.Code)) {
				await UserConfig.AddTooManyRequestAsync(user.ip, Error.HASH_INVALID);
				return new IOApi.Error("HASH_INVALID");
			}

			if (ioFunction.code != resGetAuth.Code) {
				await UserConfig.AddTooManyRequestAsync(user.ip, Error.CODE_INVALID);
				return new IOApi.Error("CODE_INVALID");
			}

			gRPC_Member.GetUserByEmailReq reqGetUser = new gRPC_Member.GetUserByEmailReq();
			reqGetUser.EmailAddress = resGetAuth.EmailAddress;
			gRPC_Member.GetUserByEmailRes resGetUser = await GRPC.GetMember().GetUserByEmailAsync(reqGetUser);
			long userId = resGetUser.Id;

			if (userId > 0L) { //Already Registered
				IOApi.SetAuthenticationEmailAddress resPassword = new IOApi.SetAuthenticationEmailAddress();
				return resPassword;
			}

			gRPC_Member.CreateUserReq reqCreateUser = new gRPC_Member.CreateUserReq();
			reqCreateUser.Name = ioFunction.name;
			reqCreateUser.EmailAddress = resGetAuth.EmailAddress;
			reqCreateUser.Password = ioFunction.password;
			gRPC_Member.CreateUserRes resCreateUser = await GRPC.GetMember().CreateUserAsync(reqCreateUser);
			userId = resCreateUser.UserId;

			if (userId == 0L) {
				return new IOApi.Error("INTERNAL_ERROR");
			}


			gRPC_Member.SetAuthenticationRegisterReq reqRegister = new gRPC_Member.SetAuthenticationRegisterReq();
			reqRegister.Ip = user.ip;
			gRPC_Member.SetAuthenticationRegisterRes resRegister = await IONet.GRPC.GetMember().SetAuthenticationRegisterAsync(reqRegister);

			gRPC_Member.SetAuthenticationReadyReq reqReady = new gRPC_Member.SetAuthenticationReadyReq();
			reqReady.Hash = ioFunction.hash;
			gRPC_Member.SetAuthenticationReadyRes resReady = await IONet.GRPC.GetMember().SetAuthenticationReadyAsync(reqReady);


			user.id = userId; // Websocket Detect Authorize is Done
			IOApi.AuthorizationStateReady res = new IOApi.AuthorizationStateReady();
			res.userId = userId;
			res.session = resCreateUser.Session;
			return res;
		}





		public static async Task<IOObject> ResendAuthenticationCode(UserConfig.User user, IOApi.ResendAuthenticationCode ioFunction) {
			IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.ip, Error.HASH_EMPTY, Error.HASH_INVALID);
			if (error != null) {
				return error;
			}

			if (ioFunction.state != STATE_LOGIN && ioFunction.state != STATE_FORGOT_PASSWORD) {
				return new IOApi.Error("INVALID_PARAMETERS");
			}

			if (String.IsNullOrEmpty(ioFunction.hash)) {
				return new IOApi.Error("HASH_EMPTY");
			}


			gRPC_Member.GetAuthenticationReq reqGetAuth = new gRPC_Member.GetAuthenticationReq();
			reqGetAuth.Hash = ioFunction.hash;
			gRPC_Member.GetAuthenticationRes resGetAuth = await IONet.GRPC.GetMember().GetAuthenticationAsync(reqGetAuth);

			if (String.IsNullOrEmpty(resGetAuth.Code)) {
				await UserConfig.AddTooManyRequestAsync(user.ip, Error.HASH_INVALID);
				return new IOApi.Error("HASH_INVALID");
			}

			if (ioFunction.state == STATE_LOGIN) {
				gRPC_Member.GetUserByEmailReq reqGetUser = new gRPC_Member.GetUserByEmailReq();
				reqGetUser.EmailAddress = resGetAuth.EmailAddress;
				gRPC_Member.GetUserByEmailRes resGetUser = await GRPC.GetMember().GetUserByEmailAsync(reqGetUser);
				if (resGetUser.Id > 0) {
					return new IOApi.Error("USER_EXIST");
				}
			}

			gRPC_Member.ResendAuthenticationCodeReq reqResend = new gRPC_Member.ResendAuthenticationCodeReq();
			reqResend.Ip = user.ip;
			reqResend.Hash = ioFunction.hash;
			gRPC_Member.ResendAuthenticationCodeRes resResend = await GRPC.GetMember().ResendAuthenticationCodeAsync(reqResend);

			if (!String.IsNullOrEmpty(resResend.Error)) {
				if (resResend.Error == "HASH_INVALID") {
					return new IOApi.Error(resResend.Error);

				} else if (resResend.Error == "TOO_MANY_REQUEST") {// Less than 120s request
					return new IOApi.Error(resResend.Error);

				} else if (resResend.Error == "FLOOD_WAIT") {
					return new IOApi.Error(resResend.Error);
				}

				return null;
			}

			if (resResend.Code.Length == 0) {
				return null;
			}

			await SendEmailAsync(resGetAuth.EmailAddress, resResend.Code, user.language);

			IOApi.AuthorizationStateWaitCode res = new IOApi.AuthorizationStateWaitCode();
			res.length = resResend.Code.Length;
			res.timeout = 120;
			return res;
		}







		public static async Task<IOObject> CancelAuthentication(UserConfig.User user, IOApi.CancelAuthentication ioFunction) {
			IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.ip, Error.HASH_EMPTY, Error.HASH_INVALID);
			if (error != null) {
				return error;
			}
			if (String.IsNullOrEmpty(ioFunction.hash)) {
				await UserConfig.AddTooManyRequestAsync(user.ip, Error.HASH_EMPTY);
				return new IOApi.Error("HASH_EMPTY");
			}

			gRPC_Member.CancelAuthenticationReq reqAuth = new gRPC_Member.CancelAuthenticationReq();
			reqAuth.Hash = ioFunction.hash;
			gRPC_Member.CancelAuthenticationRes resAuth = await GRPC.GetMember().CancelAuthenticationAsync(reqAuth);

			if (resAuth.Error == "HASH_INVALID") {
				await UserConfig.AddTooManyRequestAsync(user.ip, Error.HASH_INVALID);
			}

			return new IOApi.Ok();
		}



		public static async Task<IOObject> SetAuthenticationForgotPassword(UserConfig.User user, IOApi.SetAuthenticationForgotPassword ioFunction) {
			IOApi.Error error = await UserConfig.CheckTooManyRequestAsync(user.ip, Error.USER_ID_INVALID, Error.EMAIL_ADDRESS_BANNED, Error.EMAIL_ADDRESS_INVALID, Error.CODE_INVALID);
			if (error != null) {
				return error;
			}

			if (String.IsNullOrEmpty(ioFunction.hash)) {
				return new IOApi.Error("INVALID_PARAMETERS");
			}

			gRPC_Member.GetAuthenticationReq reqGetAuth = new gRPC_Member.GetAuthenticationReq();
			reqGetAuth.Hash = ioFunction.hash;
			gRPC_Member.GetAuthenticationRes resGetAuth = await IONet.GRPC.GetMember().GetAuthenticationAsync(reqGetAuth);

			if (resGetAuth.UserId == 0L) {
				await UserConfig.AddTooManyRequestAsync(user.ip, Error.HASH_INVALID);
				return new IOApi.Error("HASH_INVALID");
			}

			gRPC_Member.SetAuthenticationForgotPasswordReq reqForgotPassword = new gRPC_Member.SetAuthenticationForgotPasswordReq();
			reqForgotPassword.Hash = ioFunction.hash;
			reqForgotPassword.Ip = user.ip;
			gRPC_Member.SetAuthenticationForgotPasswordRes resForgotPassword = await GRPC.GetMember().SetAuthenticationForgotPasswordAsync(reqForgotPassword);
			if (resForgotPassword.Error == "HASH_INVALID") {
				await UserConfig.AddTooManyRequestAsync(user.ip, Error.HASH_INVALID);
				return new IOApi.Error("HASH_INVALID");
			}

			if (resForgotPassword.Code.Length == 0) {
				return null;
			}

			await SendEmailAsync(resGetAuth.EmailAddress, resForgotPassword.Code, user.language);

			IOApi.AuthorizationStateWaitCode res = new IOApi.AuthorizationStateWaitCode();
			res.length = resForgotPassword.Code.Length;
			res.timeout = 120;
			return res;
		}





		public static async Task<IOObject> SetAuthenticationChangePassword(UserConfig.User user, IOApi.SetAuthenticationChangePassword ioFunction) {
			if (String.IsNullOrEmpty(ioFunction.code) || String.IsNullOrEmpty(ioFunction.hash) || String.IsNullOrEmpty(ioFunction.password)) {
				return new IOApi.Error("INVALID_PARAMETERS");
			}

			if (ioFunction.password.Length > User.PASSWORD_LENGTH_MAX) {
				return new IOApi.Error("PASSWORD_LENGTH_MAX");
			} else if (ioFunction.password.Length < User.PASSWORD_LENGTH_MIN) {
				return new IOApi.Error("PASSWORD_LENGTH_MIM");
			}

			gRPC_Member.GetAuthenticationReq reqGetAuth = new gRPC_Member.GetAuthenticationReq();
			reqGetAuth.Hash = ioFunction.hash;
			gRPC_Member.GetAuthenticationRes resGetAuth = await IONet.GRPC.GetMember().GetAuthenticationAsync(reqGetAuth);
			if (String.IsNullOrEmpty(resGetAuth.Code)) {
				await UserConfig.AddTooManyRequestAsync(user.ip, Error.HASH_INVALID);
				return new IOApi.Error("HASH_INVALID");
			}

			if (ioFunction.code != resGetAuth.Code) {
				await UserConfig.AddTooManyRequestAsync(user.ip, Error.CODE_INVALID);
				return new IOApi.Error("CODE_INVALID");
			}

			gRPC_Member.ChangePasswordReq reqPassword = new gRPC_Member.ChangePasswordReq();
			reqPassword.UserId = resGetAuth.UserId;
			reqPassword.Password = ioFunction.password;
			gRPC_Member.ChangePasswordRes resChange = await GRPC.GetMember().ChangePasswordAsync(reqPassword);

			Nats.connectionUpdate.Publish(resGetAuth.UserId + "", user.connectionId, new byte[] { 0x01 }); //LOGOUT

			gRPC_Member.SetAuthenticationReadyReq reqReady = new gRPC_Member.SetAuthenticationReadyReq();
			reqReady.Hash = ioFunction.hash;
			gRPC_Member.SetAuthenticationReadyRes resReady = await IONet.GRPC.GetMember().SetAuthenticationReadyAsync(reqReady);

			user.id = resGetAuth.UserId; // Websocket Detect Authorize is Done
			IOApi.AuthorizationStateReady res = new IOApi.AuthorizationStateReady();
			res.userId = resGetAuth.UserId;
			res.session = resChange.Session;

			return res;
		}




		private static async Task SendEmailAsync(string emailAddress, string code, string language) {
			SmtpClient smtpClient = new SmtpClient();
			smtpClient.Host = ServerConfig.IODYNAMIC_MAIL_HOST;
			smtpClient.Port = 25;
			smtpClient.EnableSsl = false;
			smtpClient.UseDefaultCredentials = false;
			smtpClient.Credentials = new NetworkCredential(ServerConfig.AUTH_MAIL_ADDRESS, ServerConfig.AUTH_MAIL_PASSWORD);
			smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

			MailMessage mailMessage = new MailMessage();
			mailMessage.From = new MailAddress(ServerConfig.AUTH_MAIL_ADDRESS);
			mailMessage.To.Add(new MailAddress(emailAddress));
			mailMessage.Subject = "Login Verfication Code - IODynamic";
			mailMessage.IsBodyHtml = false;
			mailMessage.Body = "Your verfication code is: " + code;

			await smtpClient.SendMailAsync(mailMessage);
			smtpClient.Dispose();
			mailMessage.Dispose();
		}



		private static async Task SendSmsAsync(string phoneNumber, string code, string language) {
			Dictionary<string, string> param = new Dictionary<string, string>();


			param.Add("receptor", "00" + phoneNumber);
			param.Add("token", code);
			if (language == "fa") {
				param.Add("template", "Verify-Fa");
			} else {
				param.Add("template", "Verify-En");
			}
			//var res = await Network.RequestPostAsync(SMS_URL, param);
			//WebSocket_Log.Send("sms res", res);




			//http://ippanel.com/
			//http://188.0.240.110/
			/*JSONObject json = new JSONObject();
			json.Add("op", "patternV2");
			json.Add("user", "rahimi7k");
			json.Add("pass", "481516kR2342$#");
			json.Add("fromNum", "3000505");
			json.Add("toNum", phoneNumber);
			if (language == "fa") {
				json.Add("patternCode", "tgkmkfefr1");
			} else {
				json.Add("patternCode", "wplza8o7729yu49");
			}
			JSONObject jsonInput = new JSONObject();
			jsonInput.Add("code", code);
			json.Add("inputData", jsonInput);
			await Network.RequestPostAsync("https://ippanel.com/api/select", json);*/
		}

	}

}
