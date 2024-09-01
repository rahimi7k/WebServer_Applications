using System;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using System.IO;

namespace Library {

	public class ByteArrayFormatters {

		private static readonly Type type = typeof(byte[]);

		public class ByteArrayInputFormatter : InputFormatter {
			public ByteArrayInputFormatter() {
				SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/octet-stream"));
			}

			//-- Add this CanReadType not check!
			/*public override bool CanRead(InputFormatterContext context) {
				return context.HttpContext.Request.ContentType == "application/octet-stream";
			}*/

			protected override bool CanReadType(Type type) {
				return ByteArrayFormatters.type == type;
			}

			public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context) {
				MemoryStream memoryStream = new MemoryStream();
				await context.HttpContext.Request.Body.CopyToAsync(memoryStream);
				InputFormatterResult inputFormatterResult = await InputFormatterResult.SuccessAsync(memoryStream.ToArray());
				await memoryStream.FlushAsync();
				await memoryStream.DisposeAsync();
				memoryStream.Close();
				return inputFormatterResult;
			}
		}

		public class ByteArrayOutputFormatter : OutputFormatter {
			public ByteArrayOutputFormatter() {
				SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/octet-stream"));
			}

			/*public override bool CanWriteResult(OutputFormatterCanWriteContext context) {
				return context.HttpContext.Request.ContentType == "application/octet-stream";
			}*/

			protected override bool CanWriteType(Type type) {
				return ByteArrayFormatters.type == type;
			}

			public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context) {
				byte[] bytes = (byte[])context.Object;
				await context.HttpContext.Response.Body.WriteAsync(bytes, 0, bytes.Length);
			}
		}
	}
}
