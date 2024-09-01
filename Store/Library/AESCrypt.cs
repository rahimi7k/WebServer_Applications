using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Library {

	public class AESCrypt {

		public static readonly byte[] IV = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

		private static readonly AesManaged aesManaged = new AesManaged { Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 };

		static AESCrypt() {

		}

		/// <exception cref="System.ArgumentNullException"></exception>
		/// <exception cref="System.Text.EncoderFallbackException"></exception>
		/// <exception cref="System.Security.Cryptography.CryptographicException"></exception>
		/// <exception cref="System.NotSupportedException"></exception>
		/// <exception cref="System.Reflection.TargetInvocationException"></exception>
		public static string Encrypt(string message, byte[] password) {
			SHA256 sHA256 = SHA256.Create();
			byte[] key = sHA256.ComputeHash(password);
			sHA256.Dispose();
			return Convert.ToBase64String(Encrypt(message, key, IV));
		}

		/// <exception cref="System.ArgumentNullException"></exception>
		/// <exception cref="System.Text.EncoderFallbackException"></exception>
		/// <exception cref="System.Security.Cryptography.CryptographicException"></exception>
		/// <exception cref="System.NotSupportedException"></exception>
		public static byte[] Encrypt(string message, byte[] key, byte[] iv) {
			MemoryStream memoryStream = new MemoryStream();
			CryptoStream cryptoStream = new CryptoStream(memoryStream, aesManaged.CreateEncryptor(key, iv), CryptoStreamMode.Write);
			cryptoStream.Write(Encoding.UTF8.GetBytes(message));
			cryptoStream.Flush();
			cryptoStream.Dispose();
			cryptoStream.Close();
			/*StreamWriter streamWriter = new StreamWriter(cryptoStream);
			streamWriter.Write(message);
			streamWriter.Flush();
			streamWriter.Close();*/
			byte[] bytes = memoryStream.ToArray();
			memoryStream.Dispose();
			memoryStream.Close();
			return bytes;
		}

		/// <exception cref="System.ArgumentNullException"></exception>
		/// <exception cref="System.Text.EncoderFallbackException"></exception>
		public static async Task<byte[]> EncryptAsync(string message, byte[] key, byte[] iv) {
			MemoryStream memoryStream = new MemoryStream();
			CryptoStream cryptoStream = new CryptoStream(memoryStream, aesManaged.CreateEncryptor(key, iv), CryptoStreamMode.Write);
			await cryptoStream.WriteAsync(Encoding.UTF8.GetBytes(message));
			await cryptoStream.FlushAsync();
			await cryptoStream.DisposeAsync();
			cryptoStream.Close();
			/*StreamWriter streamWriter = new StreamWriter(cryptoStream);
			await streamWriter.WriteAsync(message);
			await streamWriter.FlushAsync();
			streamWriter.Close();*/
			byte[] bytes = memoryStream.ToArray();
			await memoryStream.DisposeAsync();
			memoryStream.Close();
			return bytes;
		}

		/// <exception cref="System.ArgumentNullException"></exception>
		/// <exception cref="System.FormatException"></exception>
		/// <exception cref="System.OutOfMemoryException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.Reflection.TargetInvocationException"></exception>
		public static string Decrypt(string cipherText, byte[] password) {
			SHA256 sHA256 = SHA256.Create();
			byte[] key = sHA256.ComputeHash(password);
			sHA256.Dispose();
			return Decrypt(Convert.FromBase64String(cipherText), key, IV);
		}

		/// <exception cref="System.ArgumentNullException"></exception>
		/// <exception cref="System.OutOfMemoryException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public static string Decrypt(byte[] cipherText, byte[] key, byte[] iv) {
			MemoryStream memoryStream = new MemoryStream(cipherText);
			CryptoStream cryptoStream = new CryptoStream(memoryStream, aesManaged.CreateDecryptor(key, iv), CryptoStreamMode.Read);
			StreamReader streamReader = new StreamReader(cryptoStream);
			string message = streamReader.ReadToEnd();
			streamReader.Dispose();
			streamReader.Close();
			cryptoStream.Dispose();
			cryptoStream.Close();
			memoryStream.Dispose();
			memoryStream.Close();
			return message;
		}

		/// <exception cref="System.ArgumentNullException"></exception>
		/// <exception cref="System.ArgumentOutOfRangeException"></exception>
		/// <exception cref="System.InvalidOperationException"></exception>
		public static async Task<string> DecryptAsync(byte[] cipherText, byte[] key, byte[] iv) {
			MemoryStream memoryStream = new MemoryStream(cipherText);
			CryptoStream cryptoStream = new CryptoStream(memoryStream, aesManaged.CreateDecryptor(key, iv), CryptoStreamMode.Read);
			StreamReader streamReader = new StreamReader(cryptoStream);
			string message = await streamReader.ReadToEndAsync();
			streamReader.Dispose();
			streamReader.Close();
			await cryptoStream.DisposeAsync();
			cryptoStream.Close();
			await memoryStream.DisposeAsync();
			memoryStream.Close();
			return message;
		}
	}
}
