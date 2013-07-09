using System;
using System.IO;
using System.Net;

namespace NeoCitiesTransformer.SiteContentTransformer.DataRetrieval
{
	public class WebDataRetriever : IRetrieveWebData
	{
		public string GetText(Uri url)
		{
			if (url == null)
				throw new ArgumentNullException("url");
			if (!url.IsAbsoluteUri)
				throw new ArgumentException("The specified url must be absolute");

			var webRequest = WebRequest.Create(url);
			using (var stream = webRequest.GetResponse().GetResponseStream())
			{
				using (var reader = new StreamReader(stream))
				{
					return reader.ReadToEnd();
				}
			}
		}

		public byte[] GetBinary(Uri url)
		{
			if (url == null)
				throw new ArgumentNullException("url");
			if (!url.IsAbsoluteUri)
				throw new ArgumentException("The specified url must be absolute");

			var webRequest = WebRequest.Create(url);
			using (var stream = webRequest.GetResponse().GetResponseStream())
			{
				using (var reader = new BinaryReader(stream))
				{
					const int bufferSize = 4096;
					using (var ms = new MemoryStream())
					{
						byte[] buffer = new byte[bufferSize];
						int count;
						while ((count = stream.Read(buffer, 0, buffer.Length)) != 0)
							ms.Write(buffer, 0, count);
						return ms.ToArray();
					}
				}
			}
		}
	}
}
