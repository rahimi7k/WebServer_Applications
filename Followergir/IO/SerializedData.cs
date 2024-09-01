using Followergir.Controllers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Library.Json;
using Library;

namespace Followergir.IO {

	public class SerializedData {

		//private static readonly char RECORD_SEPARATOR = '\u001E';

		private static readonly string IO_API_CLASS_NAME_SPACE = typeof(IOApi).FullName + '+';

		private static readonly JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
		private static readonly JsonSerializerAbstractSetting jsonSerializerAbstractSetting = new JsonSerializerAbstractSetting();


		public static string SerializeMessage(Message message) {
			string json = "{";

			if (message.identifierFunction != 0) {
				json += "\"F\":{\"I\":" + message.identifierFunction;
				if (message.ioFunction != null) {
					message.ioFunction.T = message.ioFunction.GetType().Name;
					json += ",\"B\":" + JsonConvert.SerializeObject(message.ioFunction, message.ioFunction.GetType(), jsonSerializerSettings);
				}
				json += "}";
			}

			if (message.updates != null) {
				string jsonUpdate = "";
				foreach (IOApi.Update update in message.updates) {
					if (update != null) {
						update.T = update.GetType().Name;
						if (jsonUpdate != "") {
							jsonUpdate += ",";
						}
						jsonUpdate += JsonConvert.SerializeObject(update, update.GetType(), jsonSerializerSettings);
					}
				}
				if (jsonUpdate != "") {
					if (message.identifierFunction != 0) {
						json += ",";
					}
					json += "\"U\":[" + jsonUpdate + "]";
				}
			}

			/*if (!String.IsNullOrEmpty(message.identifierAction)) {
				if (jsonUpdate != "") {//TODO
					jsonUpdate += ",";
				}
				json += "\"A\":{\"I\":\"" + message.identifierAction + "\"";
				if (message.ioAction != null) {
					message.ioAction.T = message.ioAction.GetType().Name;
					json += ",\"B\":" + JsonConvert.SerializeObject(message.ioAction, message.ioAction.GetType(), jsonSerializerSettings);
				}
				json += "}";
			}*/

			json += "}";
			//Log.E("SerializeObject: " + json);
			return json;
		}

		public static byte[] SerializeMessage(Message message, byte[] AES_KEY, byte[] AES_IV) {
			string json = SerializeMessage(message);
			//Log.E("SerializeObject: " + json);
			return AESCrypt.Encrypt(json, AES_KEY, AES_IV);
		}




		public static byte[] SerializeUpdate(params IOApi.Update[] updates) {
			string json = "{";

			string jsonUpdate = "";
			foreach (IOObject update in updates) {
				if (update != null) {
					update.T = update.GetType().Name;
					if (jsonUpdate != "") {
						jsonUpdate += ",";
					}
					jsonUpdate += JsonConvert.SerializeObject(update, update.GetType(), jsonSerializerSettings);
				}
			}
			if (jsonUpdate != "") {
				json += "\"U\":[" + jsonUpdate + "]";
			}
			json += "}";
			return Encoding.UTF8.GetBytes(json);
		}


		public static byte[] SerializeUpdate(byte[] bytes, byte[] AES_KEY, byte[] AES_IV) {
			string json = Encoding.UTF8.GetString(bytes);
			return AESCrypt.Encrypt(json, AES_KEY, AES_IV);
		}



		public static Message DeserializeMessage(string js) {
			JSONObject json = null;
			try {
				json = new JSONObject(js);
			} catch (Exception ex) {
				//Console.WriteLine("DeserializeObject", "Exception: " + ex);
			}
			return DeserializeMessage(json);
		}

		public static Message DeserializeMessage(byte[] bytes, byte[] AES_KEY, byte[] AES_IV) {
			JSONObject json = null;
			try {
				json = new JSONObject(AESCrypt.Decrypt(bytes, AES_KEY, AES_IV));
			} catch (Exception ex) {
				//Console.WriteLine("DeserializeObject", "Exception: " + ex);
			}
			return DeserializeMessage(json);
		}


		public static Message DeserializeMessage(JSONObject json) {
			Message message = new Message();
			if (json == null) {
				return message;
			}
			if (!json.IsNull("F")) {
				try {
					JSONObject jsonFunction = json.GetJSONObject("F");
					message.identifierFunction = jsonFunction.GetInt("I");
					jsonFunction = jsonFunction.GetJSONObject("B");
					message.ioFunction = (IOObject) JsonConvert.DeserializeObject(jsonFunction.ToString(), Type.GetType(IO_API_CLASS_NAME_SPACE + jsonFunction.GetString("T")), jsonSerializerSettings);
				} catch (Exception e) {
					//Console.WriteLine("DeserializeObject Exception: " + e);
				}

			} else if (!json.IsNull("A")) {
				try {
					JSONObject jsonAction = json.GetJSONObject("A");
					message.identifierAction = jsonAction.GetString("I");
					jsonAction = jsonAction.GetJSONObject("B");
					message.ioAction = (IOObject) JsonConvert.DeserializeObject(jsonAction.ToString(), Type.GetType(IO_API_CLASS_NAME_SPACE + jsonAction.GetString("T")), jsonSerializerSettings);
				} catch (Exception) { }
			}

			/*if (!json.IsNull("U")) {
				try {
					JSONArray jsonUpdate = json.GetJSONArray("U");
					Log.E(jsonUpdate + "");
					message.updates = new IOApi.Update[jsonUpdate.Count];
					for (int i = 0; i < jsonUpdate.Count; i++) {
						try {
							IOApi.Update update = (IOApi.Update) JsonConvert.DeserializeObject(jsonUpdate.GetJSONObject(i).ToString(), Type.GetType(IO_API_CLASS_NAME_SPACE + jsonUpdate.GetJSONObject(i).GetString("T")), jsonSerializerSettings);
							message.updates[i] = update;
						} catch (Exception) { }
					}
				} catch (Exception) { }
			}*/

			return message;
		}



		public static string SerializeObject(IOObject iOObject) {
			iOObject.T = iOObject.GetType().Name;
			return JsonConvert.SerializeObject(iOObject, iOObject.GetType(), jsonSerializerSettings);
		}

		public static IOObject DeserializeObject(string js) {
			try {
				JSONObject json = new JSONObject(js);
				return (IOObject) JsonConvert.DeserializeObject(json.ToString(), Type.GetType(IO_API_CLASS_NAME_SPACE + json.GetString("T")), jsonSerializerSettings);
			} catch (Exception) { }
			return null;
		}


		public static IOObject DeserializeObject(byte[] bytes, byte[] aesKey, byte[] aesIv) {
			try {
				JSONObject json = new JSONObject(AESCrypt.Decrypt(bytes, aesKey, aesIv));
				return (IOObject) JsonConvert.DeserializeObject(json.ToString(), Type.GetType(IO_API_CLASS_NAME_SPACE + json.GetString("T")), jsonSerializerSettings);
			} catch (Exception) { }
			return null;
		}





		public class AbstractContractResolver : DefaultContractResolver {

			protected override JsonConverter ResolveContractConverter(Type objectType) {
				//Console.WriteLine("ResolveContractConverter IsAbstract: " + objectType.IsAbstract);
				if (objectType.IsAbstract) {
					return base.ResolveContractConverter(objectType);
				}
				return null;
			}
		}


		public class AbstractConverter : JsonConverter {

			public override bool CanConvert(Type objectType) {
				//Console.WriteLine("AbstractConverter IsAbstract: " + objectType.IsAbstract);
				return objectType.IsAbstract;
			}

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
				JObject json = JObject.Load(reader);
				//Console.WriteLine("ReadJson:" + json);
				return JsonConvert.DeserializeObject(json.ToString(Formatting.None), Type.GetType(IO_API_CLASS_NAME_SPACE + json["T"].Value<string>()), jsonSerializerAbstractSetting);
			}

			public override bool CanWrite {
				get { return false; }
			}

			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
				//Console.WriteLine("WriteJson:" + writer);

				((IOObject) value).T = value.GetType().Name;
				JsonConvert.SerializeObject(value, value.GetType(), jsonSerializerAbstractSetting);
			}
		}


		private class JsonSerializerSettings : Newtonsoft.Json.JsonSerializerSettings {

			public JsonSerializerSettings() {
				DefaultValueHandling = DefaultValueHandling.Include;//-- if use Ignore dont sent string null or int 0
				NullValueHandling = App.IsDebug() ? NullValueHandling.Include : NullValueHandling.Ignore;//-- Include send null value
				Formatting = Formatting.Indented;//-- replace space json
				FloatFormatHandling = FloatFormatHandling.Symbol;
				FloatParseHandling = FloatParseHandling.Double;
				TypeNameHandling = TypeNameHandling.None;//-- $type add namespace
				TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
				TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;//-- if use Full, add Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
				PreserveReferencesHandling = PreserveReferencesHandling.None;//-- $id AND $ref
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
				CheckAdditionalContent = false;
				ConstructorHandling = ConstructorHandling.Default;
				// ContractResolver = new AbstractContractResolver();
				// Converters.Add(new BaseConverter());
			}
		}

		private class JsonSerializerAbstractSetting : JsonSerializerSettings {

			public JsonSerializerAbstractSetting() {
				DefaultValueHandling = DefaultValueHandling.Include;//-- if use Ignore dont sent string null or int 0
				NullValueHandling = App.IsDebug() ? NullValueHandling.Include : NullValueHandling.Ignore;//-- Include send null value
				Formatting = Formatting.Indented;//-- replace space json
				FloatFormatHandling = FloatFormatHandling.Symbol;
				FloatParseHandling = FloatParseHandling.Double;
				TypeNameHandling = TypeNameHandling.None;//-- $type add namespace
				TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
				TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;//-- if use Full, add Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
				PreserveReferencesHandling = PreserveReferencesHandling.None;//-- $id AND $ref
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
				CheckAdditionalContent = false;
				ConstructorHandling = ConstructorHandling.Default;
				ContractResolver = new AbstractContractResolver();
			}
		}
	}
}
