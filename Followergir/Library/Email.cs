using Followergir;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Library {

	public class Email {

		// private static readonly string HOST = "mail." + IPGlobalProperties.GetIPGlobalProperties().DomainName;

		public static void Send(string to, string subject, string body) {
			MailMessage mailMessage = new MailMessage();
			mailMessage.From = new MailAddress(ServerConfig.MAIL_ADDRESS);
			mailMessage.To.Add(new MailAddress(to));
			mailMessage.Subject = subject;
			mailMessage.IsBodyHtml = false;
			mailMessage.Body = body;
			Send(mailMessage);
		}

		public static void Send(MailMessage mailMessage) {
			SmtpClient smtpClient = new SmtpClient();
			smtpClient.Host = ServerConfig.MAIL_HOST;
			smtpClient.Port = 25;
			smtpClient.EnableSsl = false;
			smtpClient.UseDefaultCredentials = false;
			smtpClient.Credentials = new NetworkCredential(ServerConfig.MAIL_ADDRESS, ServerConfig.MAIL_PASSWORD);
			smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
			smtpClient.Send(mailMessage);
			smtpClient.Dispose();
			mailMessage.Dispose();
		}

		public static async Task SendAsync(string to, string subject, string body) {
			MailMessage mailMessage = new MailMessage();
			mailMessage.From = new MailAddress(ServerConfig.MAIL_ADDRESS);
			mailMessage.To.Add(new MailAddress(to));
			mailMessage.Subject = subject;
			mailMessage.IsBodyHtml = false;
			mailMessage.Body = body;
			await SendAsync(mailMessage);
		}

		public static async Task SendAsync(MailMessage mailMessage) {
			SmtpClient smtpClient = new SmtpClient();
			smtpClient.Host = ServerConfig.MAIL_HOST;
			smtpClient.Port = 25;
			smtpClient.EnableSsl = false;
			smtpClient.UseDefaultCredentials = false;
			smtpClient.Credentials = new NetworkCredential(ServerConfig.MAIL_ADDRESS, ServerConfig.MAIL_PASSWORD);
			smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
			await smtpClient.SendMailAsync(mailMessage);
			smtpClient.Dispose();
			mailMessage.Dispose();
		}

	}
}
