using Store;
using Microsoft.Win32;

namespace Library {

	public class Registry {

		public static readonly string ADDRESS_APP = Microsoft.Win32.Registry.LocalMachine.Name + "\\SOFTWARE\\IODynamic\\" + App.GetName();

		static Registry() {
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
			object obj = Microsoft.Win32.Registry.GetValue(key, name, defaultValue);
			if (obj != null) {
				return (string) obj;
			}
			return defaultValue;
		}

		public static int GetDWord(string key, string name, int defaultValue) {

			object obj = Microsoft.Win32.Registry.GetValue(key, name, defaultValue);
			if (obj != null) {
				return (int) obj;
			}
			return defaultValue;
		}

		public static long GetQWord(string key, string name, long defaultValue) {

			object obj = Microsoft.Win32.Registry.GetValue(key, name, defaultValue);
			if (obj != null) {
				return (long) obj;
			}
			return defaultValue;
		}

		public static byte[] GetBinary(string key, string name, byte[] defaultValue) {

			object obj = Microsoft.Win32.Registry.GetValue(key, name, defaultValue);
			if (obj != null) {
				return (byte[]) obj;
			}
			return defaultValue;
		}

		public static void PutString(string key, string name, string value) {

			Microsoft.Win32.Registry.SetValue(key, name, value, RegistryValueKind.String);
		}

		public static void PutDWord(string key, string name, int value) {

			Microsoft.Win32.Registry.SetValue(key, name, value, RegistryValueKind.DWord);
		}

		public static void PutQWord(string key, string name, long value) {

			Microsoft.Win32.Registry.SetValue(key, name, value, RegistryValueKind.QWord);
		}

		public static void PutBinary(string key, string name, byte[] value) {

			Microsoft.Win32.Registry.SetValue(key, name, value, RegistryValueKind.Binary);
		}

	}
}
