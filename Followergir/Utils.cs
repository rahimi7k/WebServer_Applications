using Followergir.IO;
using Library;
using System;

public class Utils {

	public static IOApi.Error FormatOrder(IOApi.Order order) {

		if (!StringUtils.IsOnlyASCII(order.username) || order.username.Contains(':') || order.username.Contains('/') || order.username.Contains('\\')) {
			return new IOApi.Error("USERNAME_INVALID");
		}
		//order.username = order.username.Replace(":", "").ToLower();
		order.username = order.username.ToLower().Trim();

		if (order.postId != null && order.postId != "") {
			if (!StringUtils.IsOnlyNumber(order.postId) && order.postId.Length > 20) {
				return new IOApi.Error("POST_ID_INVALID");
			}
		}
		return null;
	}

	public static long GetUnixTime() {
		return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
	}



	public static bool IsEmailValid(string email) {
		var trimmedEmail = email.Trim();

		if (trimmedEmail.EndsWith(".")) {
			return false;
		}
		try {
			var addr = new System.Net.Mail.MailAddress(email);
			return addr.Address == trimmedEmail;
		} catch {
			return false;
		}
	}







}
