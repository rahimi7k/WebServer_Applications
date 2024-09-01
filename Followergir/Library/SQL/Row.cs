using System;
using System.Collections.Generic;

namespace Library.SQL {

	public class Row {

		private readonly Dictionary<string, object> list = new Dictionary<string, object>();

		public string GetString(string name) {
			return (string)list[name];
		}

		public short GetShort(string name) {
			return (short)list[name];
		}

		public int GetInt(string name) {
			return (int)list[name];
		}

		public long GetLong(string name) {
			return (long)list[name];
		}

		public float GetFloat(string name) {
			return (float)list[name];
		}

		public double GetDouble(string name) {
			return (double)list[name];
		}

		public decimal GetDecimal(string name) {
			return (decimal)list[name];
		}

		public bool GetBoolean(string name) {
			return (bool)list[name];
		}

		public byte GetByte(string name) {
			return (byte)list[name];
		}

		public byte[] GetByteArray(string name) {
			return (byte[])list[name];
		}

		public DateTime GetDateTime(string name) {
			return (DateTime)list[name];
		}

		public bool IsNull(string name) {
			return list[name] is DBNull;
		}

		public void Put(string name, object value) {
			list.Add(name, value);
		}

	}
}
