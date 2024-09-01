using Program.IONet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace Program {

	public class Email {

		private static readonly string HOST = "mail.iotelegram.com";
		private static readonly string EMAIL = "no-replay@iotelegram.com";
		private static readonly string PASSWORD = "481516kR2342$#";

		public static void Send(string to, string subject, string body) {
			MailMessage mailMessage = new MailMessage();
			mailMessage.From = new MailAddress(EMAIL);
			mailMessage.To.Add(new MailAddress(to));
			mailMessage.Subject = subject;
			mailMessage.IsBodyHtml = false;
			mailMessage.Body = body;
			Send(mailMessage);
		}

		public static void Send(MailMessage mailMessage) {
			SmtpClient smtpClient = new SmtpClient();
			smtpClient.Host = HOST;
			smtpClient.Port = 587;
			smtpClient.EnableSsl = true;
			smtpClient.UseDefaultCredentials = false;
			smtpClient.Credentials = new NetworkCredential(EMAIL, PASSWORD);
			smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
			//smtpClient.Send(mailMessage);
			smtpClient.Dispose();
			mailMessage.Dispose();
		}

		public static async Task SendAsync(string to, string subject, string body) {
			MailMessage mailMessage = new MailMessage();
			mailMessage.From = new MailAddress(EMAIL);
			mailMessage.To.Add(new MailAddress(to));
			mailMessage.Subject = subject;
			mailMessage.IsBodyHtml = false;
			mailMessage.Body = body;
			await SendAsync(mailMessage);
		}

		public static async Task SendAsync(MailMessage mailMessage) {
			SmtpClient smtpClient = new SmtpClient();
			smtpClient.Host = HOST;
			smtpClient.Port = 587;
			smtpClient.EnableSsl = true;
			smtpClient.UseDefaultCredentials = false;
			smtpClient.Credentials = new NetworkCredential(EMAIL, PASSWORD);
			smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
			//await smtpClient.SendMailAsync(mailMessage);
			smtpClient.Dispose();
			mailMessage.Dispose();
		}

	}
}
