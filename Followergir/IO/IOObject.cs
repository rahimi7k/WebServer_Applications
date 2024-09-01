using System;
using System.Reflection;

namespace Followergir.IO {

	public abstract class IOObject {
		/// <summary>
		/// <b>Class Type Name</b>
		/// </summary>
		public string T;

		public override string ToString() {
			string str = "\r\nClass Name: " + GetType().Name + "\r\n";
			foreach (FieldInfo fieldInfo in GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)) {
				str += fieldInfo.Name + "\t= ";
				object obj = fieldInfo.GetValue(this);
				if (obj == null) {
					str += "null";

				} else if (obj is char) {
					str += (char) obj == 0x00 ? "0x00" : obj;
				} else {
					str += obj;
				}
				str += "\r\n";
			}
			return str;
		}
	}
}
