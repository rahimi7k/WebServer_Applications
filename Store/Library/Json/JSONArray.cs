using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Library.Json {

	public class JSONArray : JArray {

		public JSONArray() : base() {

		}

		public JSONArray(string json) : base(Parse(json)) {

		}

		public JSONArray(JArray jObject) : base(jObject) {

		}

		public string GetString(int index) {
			return this[index].Value<string>();
		}

		public byte GetByte(string name) {
			return this[name].Value<byte>();
		}

		public short GetShort(int index) {
			return this[index].Value<short>();
		}

		public int GetInt(int index) {
			return this[index].Value<int>();
		}

		public long GetLong(int index) {
			return this[index].Value<long>();
		}

		public float GetFloat(int index) {
			return this[index].Value<float>();
		}

		public double GetDouble(int index) {
			return this[index].Value<double>();
		}

		public bool GetBoolean(int index) {
			return this[index].Value<bool>();
		}

		public JSONObject GetJSONObject(int index) {
			return new JSONObject(this[index].Value<JObject>());
		}

		public JSONArray GetJSONArray(int index) {
			return new JSONArray(this[index].Value<JArray>());
		}

		public override string ToString() {
			return ToString(Formatting.None);
		}
	}
}
