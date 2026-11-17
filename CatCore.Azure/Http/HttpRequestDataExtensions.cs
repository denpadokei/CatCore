using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.Functions.Worker.Http;

namespace CatCore.Azure.Http
{
	internal static class HttpRequestDataExtensions
	{
		private const int MaxFormBodyLength = 4096;

		/// <summary>
		/// Reads a small application/x-www-form-urlencoded body. The body is capped so the anonymous
		/// HTTP endpoints cannot be abused to allocate arbitrary amounts of memory.
		/// </summary>
		public static async Task<NameValueCollection> ReadFormCollectionAsync(this HttpRequestData request)
		{
			if (request.Body == null)
			{
				return new NameValueCollection();
			}

			using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
			var buffer = new char[MaxFormBodyLength + 1];
			var totalRead = 0;
			while (totalRead < buffer.Length)
			{
				var read = await reader.ReadAsync(buffer, totalRead, buffer.Length - totalRead).ConfigureAwait(false);
				if (read == 0)
				{
					break;
				}

				totalRead += read;
			}

			if (totalRead == 0 || totalRead > MaxFormBodyLength)
			{
				return new NameValueCollection();
			}

			return HttpUtility.ParseQueryString(new string(buffer, 0, totalRead));
		}
	}
}
