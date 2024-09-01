using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Library {

	public class StringUtils {

		public static readonly string UTF_8 = "UTF-8";
		public static readonly char[] HEX_CHARACTERS = "0123456789ABCDEF".ToCharArray();
		public static readonly string CHARACTERS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";

		private static readonly Random random = new Random();
		private static readonly Regex regexNumber = new Regex("^[0-9]+$");

		/// <summary>
		/// ASCII encoding replaces non-ascii with question marks, so we use UTF8 to see if multi-byte sequences are there
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static bool IsOnlyASCII(string value) {
			return Encoding.UTF8.GetByteCount(value) == value.Length;
		}

		public static bool IsOnlyNumber(string number) {
			return regexNumber.IsMatch(number);
		}

		public static short ParseShort(string number) {
			try {
				return short.Parse(ToNumberFormat(number), CultureInfo.InvariantCulture.NumberFormat);
			} catch (FormatException) { }
			return 0;
		}

		public static int ParseInt(string number) {
			try {
				return int.Parse(ToNumberFormat(number), CultureInfo.InvariantCulture.NumberFormat);
			} catch (FormatException) { }
			return 0;
		}

		public static long ParseLong(string number) {
			try {
				return long.Parse(ToNumberFormat(number), CultureInfo.InvariantCulture.NumberFormat);
			} catch (FormatException) { }
			return 0L;
		}

		public static float ParseFloat(string number) {
			try {
				return float.Parse(ToNumberFormat(number), CultureInfo.InvariantCulture.NumberFormat);
			} catch (FormatException) { }
			return 0F;
		}

		public static double ParseDouble(string number) {
			try {
				return double.Parse(ToNumberFormat(number), CultureInfo.InvariantCulture.NumberFormat);
			} catch (FormatException) { }
			return 0d;
		}

		public static string ToNumberFormat(string number) {
			if (string.IsNullOrEmpty(number)) {
				return "0";
			}
			number = Regex.Replace(Regex.Replace(number, "[^\\d\\-]+", "", RegexOptions.Multiline), "(?<!^)\\-", "", RegexOptions.Multiline);
			return Regex.Replace(number.Replace("-", ""), "^0+(?!$)", "", RegexOptions.Multiline) == "" ? "0" : number;
		}

		public static int CharacterCount(string str, string character) {
			try {
				return Regex.Matches(str, character, RegexOptions.Multiline).Count;
			} catch (Exception) { }
			return 0;
		}

		public static string ByteArrayToHex(byte[] bytes) {
			if (bytes == null) {
				return null;
			}
			StringBuilder stringBuilder = new StringBuilder(bytes.Length * 2);
			foreach (byte b in bytes) {
				stringBuilder.Append(HEX_CHARACTERS[b >> 4]);
				stringBuilder.Append(HEX_CHARACTERS[b & 0x0F]);
			}
			return stringBuilder.ToString();
		}

		public static byte[] HexToByteArray(string hex) {
			if (string.IsNullOrEmpty(hex)) {
				return null;
			}
			byte[] bytes = new byte[hex.Length / 2];
			for (int i = 0; i < hex.Length; i += 2) {
				bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
			}
			return bytes;
		}

		public static void RandomBytes(byte[] bytes) {
			random.NextBytes(bytes);
		}

		public static int Random(int min, int max) {
			return random.Next(min, max + 1);
		}

		public static string RandomString(int length) {
			return new string(Enumerable.Repeat(CHARACTERS, length).Select(s => s[random.Next(s.Length)]).ToArray());
		}
	}
}
