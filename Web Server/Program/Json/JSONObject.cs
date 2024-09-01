using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Program.Json {

	public class JSONObject : JObject {

		public JSONObject() : base() {

		}

		public JSONObject(string json) : base(Parse(json)) {
			
		}

		public JSONObject(JObject jObject) : base(jObject) {

		}

		public string GetString(string name) {
			return this[name].Value<string>();
		}

		public byte GetByte(string name) {
			return this[name].Value<byte>();
		}

		public short GetShort(string name) {
			return this[name].Value<short>();
		}

		public int GetInt(string name) {
			return this[name].Value<int>();
		}

		public long GetLong(string name) {
			return this[name].Value<long>();
		}

		public float GetFloat(string name) {
			return this[name].Value<float>();
		}

		public double GetDouble(string name) {
			return this[name].Value<double>();
		}

		public bool GetBoolean(string name) {
			return this[name].Value<bool>();
		}

		public JSONObject GetJSONObject(string name) {
			return new JSONObject(this[name].Value<JObject>());
		}

		public JSONArray GetJSONArray(string name) {
			return new JSONArray(this[name].Value<JArray>());
		}

		public bool IsNull(string name) {
			JToken jToken = this[name];
			return jToken == null || jToken.Type == JTokenType.Null;
		}

		public override string ToString() {
			return ToString(Formatting.None);
		}
	}
}
