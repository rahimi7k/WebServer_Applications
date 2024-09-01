using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using WebServer;

namespace Program {

	public class Log {

		private static readonly int SEND_EMAIL_MAX = 10;

		public static void I(object obj) {
			if (App.IsDebug()) {
				Debug.WriteLine("\r\n" + DateTime.Now.ToString() + " I/Log: " + (obj == null ? "null" : obj) + "\r\n");
			} else {
				File(DateTime.Now.ToString("T") + " I/Log: " + (obj == null ? "null" : obj));
			}
		}

		public static void W(object obj) {
			if (App.IsDebug()) {
				Debug.WriteLine("\r\n" + DateTime.Now.ToString() + " W/Log:" + (obj == null ? "null" : obj) + "\r\n");
			} else {
				File(DateTime.Now.ToString("T") + " W/Log: " + (obj == null ? "null" : obj));
				
			}
		}

		public static void E(object obj) {
			if (App.IsDebug()) {
				Debug.WriteLine("\r\n" + DateTime.Now.ToString() + " E/Log: " + (obj == null ? "null" : obj) + "\r\n");
			} else {
				File(DateTime.Now.ToString("T") + " E/Log: " + (obj == null ? "null" : obj));
			}
		}

		private static void File(string log) {
			string path = App.GetDirectory() + "logs";
			if (!Directory.Exists(path)) {
				Directory.CreateDirectory(path);
			}
			StreamWriter streamWriter = new StreamWriter(path + "\\Log " + DateTime.Now.ToString("yyyy-MM-dd") + ".txt", true);
			streamWriter.Write(log + "\r\n\r\n");
			streamWriter.Flush();
			streamWriter.Dispose();
			streamWriter.Close();
		}

		public static void SendEmail(string subject, string message) {
			int countEmail = Setting.GetDWord(Setting.ADDRESS_REGISTRY + "\\Email", subject, 0);
			/*if (countEmail >= SEND_EMAIL_MAX) {
				return;
			}
			Setting.PutDWord(Setting.ADDRESS_REGISTRY + "\\Email", subject, countEmail + 1);*/

			Email.Send("mysaeedjooon@gmail.com", App.GetName() + " (" + subject + ")", message +
				"\r\n--------\r\nApp: " + Assembly.GetExecutingAssembly().Location + "\r\nServer: " + Environment.MachineName);
		}

		public static void SendEmailKorosh(string subject, string message) {
			/*int countEmail = Setting.GetDWord(Setting.ADDRESS_REGISTRY + "\\Email", subject, 0);
			if (countEmail >= SEND_EMAIL_MAX) {
				return;
			}
			Setting.PutDWord(Setting.ADDRESS_REGISTRY + "\\Email", subject, countEmail + 1);*/

			Email.Send("rahimi7k@gmail.com", App.GetName() + " (" + subject + ")", message +
				"\r\n--------\r\nApp: " + Assembly.GetExecutingAssembly().Location + "\r\nServer: " + Environment.MachineName);
		}

		public static int GetLine() {
			StackFrame stackFrame = new StackFrame(1, true);
			return stackFrame.GetFileLineNumber();
		}
	}
}

