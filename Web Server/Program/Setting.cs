using Microsoft.Win32;
using WebServer;

namespace Program {

	public class Setting {

		public static readonly string ADDRESS_REGISTRY = Registry.LocalMachine.Name + "\\SOFTWARE\\IODynamic\\" + App.GetName();

		static Setting() {
			/*RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\IODynamic\\" + App.GetName(), RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions);
			if (registryKey == null) {
				registryKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\IODynamic\\" + App.GetName());
			}
			RegistrySecurity keyPermissions = registryKey.GetAccessControl();
			RegistryAccessRule registryAccessRule = new RegistryAccessRule("IIS_IUSRS", RegistryRights.FullControl, InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow);
			keyPermissions.AddAccessRule(registryAccessRule);
			registryKey.SetAccessControl(keyPermissions);
			registryKey.Dispose();
			registryKey.Close();*/
		}

		public static string GetString(string key, string name, string defaultValue) {
			object obj = Registry.GetValue(key, name, defaultValue);
			if (obj != null) {
				return (string) obj;
			}
			return defaultValue;
		}

		public static int GetDWord(string key, string name, int defaultValue) {

			object obj = Registry.GetValue(key, name, defaultValue);
			if (obj != null) {
				return (int) obj;
			}
			return defaultValue;
		}

		public static long GetQWord(string key, string name, long defaultValue) {

			object obj = Registry.GetValue(key, name, defaultValue);
			if (obj != null) {
				return (long) obj;
			}
			return defaultValue;
		}

		public static byte[] GetBinary(string key, string name, byte[] defaultValue) {

			object obj = Registry.GetValue(key, name, defaultValue);
			if (obj != null) {
				return (byte[]) obj;
			}
			return defaultValue;
		}

		public static void PutString(string key, string name, string value) {

			Registry.SetValue(key, name, value, RegistryValueKind.String);
		}

		public static void PutDWord(string key, string name, int value) {

			Registry.SetValue(key, name, value, RegistryValueKind.DWord);
		}

		public static void PutQWord(string key, string name, long value) {

			Registry.SetValue(key, name, value, RegistryValueKind.QWord);
		}

		public static void PutBinary(string key, string name, byte[] value) {

			Registry.SetValue(key, name, value, RegistryValueKind.Binary);
		}

	}
}
