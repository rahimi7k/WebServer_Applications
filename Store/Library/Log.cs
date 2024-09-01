using Store;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Library {

	public class Log {

		private static readonly string PATH = App.GetDirectory() + "logs";

		public static void I(object obj) {
			if (App.IsDebug()) {
				Debug.WriteLine("\r\n" + GetTag() + (obj == null ? "null" : obj) + "\r\n");
			} else {
				File(GetTag() + (obj == null ? "null" : obj));
			}
		}

		public static void W(object obj) {
			if (App.IsDebug()) {
				Debug.WriteLine("\r\n" + GetTag() + (obj == null ? "null" : obj) + "\r\n");
			} else {
				File(GetTag() + (obj == null ? "null" : obj));
			}
		}

		public static void E(object obj) {
			if (App.IsDebug()) {
				Debug.WriteLine("\r\n" + GetTag() + (obj == null ? "null" : obj) + "\r\n");
			} else {
				File(GetTag() + (obj == null ? "null" : obj));
			}
		}

		private static void File(string log) {
			if (!Directory.Exists(PATH)) {
				Directory.CreateDirectory(PATH);
			}
			StreamWriter streamWriter = new StreamWriter(PATH + "\\Log " + DateTime.Now.ToString("yyyy-MM-dd") + ".txt", true);
			streamWriter.Write(log + "\r\n\r\n");
			streamWriter.Flush();
			streamWriter.Dispose();
			streamWriter.Close();
		}

		public static void SendEmail(string subject, string message) {
			try {
				Email.Send("mysaeedjooon@gmail.com", App.GetName() + " (" + subject + ")", message +
					"\r\n--------\r\nApp: " + Assembly.GetExecutingAssembly().Location + "\r\nServer: " + Environment.MachineName);
			} catch (Exception) { }
		}

		public static void SendEmailKorosh(string subject, string message) {
			try {
				Email.Send("rahimi7k@gmail.com", App.GetName() + " (" + subject + ")", message +
					"\r\n--------\r\nApp: " + Assembly.GetExecutingAssembly().Location + "\r\nServer: " + Environment.MachineName);
			} catch (Exception) { }
		}

		private static string GetTag() {
			StackFrame stackFrame = new StackFrame(2, true);

			string methodName;
			try {
				methodName = new StackFrame(1, false).GetMethod().Name;
			} catch (Exception) {
				methodName = "Unknown";
			}

			string fileName;
			try {
				fileName = Path.GetFileName(stackFrame.GetFileName());
			} catch (Exception) {
				fileName = "Unknown";
			}
			return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.FFF") + " " + methodName + "/" + fileName + " " + stackFrame.GetFileLineNumber() + ": ";
		}
	}
}
