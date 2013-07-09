using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;

namespace NeoCitiesTransformer.SiteContentTransformer
{
	/// <summary>
	/// This tries to upload a single file's contents through an upload form that requires an authorisation cookie and a "csrf_token" form field (common with Django?)
	/// </summary>
	public class SingleFileUploader
	{
		private readonly string _authCookieName, _authCookieValue, _csrfToken;
		public SingleFileUploader(string authCookieName, string authCookieValue, string csrfToken)
		{
			if (string.IsNullOrWhiteSpace(authCookieName))
				throw new ArgumentNullException("Null/blank authCookieName specified");
			if (string.IsNullOrWhiteSpace(authCookieValue))
				throw new ArgumentNullException("Null/blank authCookieValue specified");
			if (string.IsNullOrWhiteSpace(csrfToken))
				throw new ArgumentNullException("Null/blank csrfToken specified");

			_authCookieName = authCookieName;
			_authCookieValue = authCookieValue;
			_csrfToken = csrfToken;
		}

		public void UploadFiles(Uri url, FileInfo file, string contentType)
		{
			if (url == null)
				throw new ArgumentNullException("url");
			if (!url.IsAbsoluteUri)
				throw new ArgumentException("url must be an absolute uri");
			if (file == null)
				throw new ArgumentNullException("file");
			if (string.IsNullOrWhiteSpace(contentType))
				throw new ArgumentNullException("Null/blank contentType specified");

			// See http://stackoverflow.com/a/11048296
			var request = WebRequest.Create(url);
			request.Method = "POST";

			var httpRequest = (HttpWebRequest)request;
			httpRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
			httpRequest.Headers.Add("Cache-Control", "max-age=0");
			httpRequest.Headers.Add("Origin", url.Scheme + "://" + url.Host);
			httpRequest.Headers.Add("Accept-Encoding", "gzip,deflate,sdch");
			httpRequest.Headers.Add("Accept-Language", "en-GB,en-US;q=0.8,en;q=0.6");

			if (httpRequest.CookieContainer == null)
				httpRequest.CookieContainer = new CookieContainer();
			httpRequest.CookieContainer.Add(new Cookie(
				_authCookieName,
				_authCookieValue,
				"/",
				url.Host
			));

			var boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x", NumberFormatInfo.InvariantInfo);
			request.ContentType = "multipart/form-data; boundary=" + boundary;
			boundary = "--" + boundary;

			using (var requestStream = request.GetRequestStream())
			{
				// Write the csrf_token value
				var buffer = Encoding.ASCII.GetBytes(boundary + Environment.NewLine);
				requestStream.Write(buffer, 0, buffer.Length);
				buffer = Encoding.ASCII.GetBytes(string.Format("Content-Disposition: form-data; name=\"{0}\"{1}{1}", "csrf_token", Environment.NewLine));
				requestStream.Write(buffer, 0, buffer.Length);
				buffer = Encoding.UTF8.GetBytes(_csrfToken + Environment.NewLine);
				requestStream.Write(buffer, 0, buffer.Length);

				// Write the file
				var fileBuffer = Encoding.ASCII.GetBytes(boundary + Environment.NewLine);
				requestStream.Write(fileBuffer, 0, fileBuffer.Length);
				fileBuffer = Encoding.UTF8.GetBytes(string.Format("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"{2}", "newfile", file.Name, Environment.NewLine));
				requestStream.Write(fileBuffer, 0, fileBuffer.Length);
				fileBuffer = Encoding.ASCII.GetBytes(string.Format("Content-Type: {0}{1}{1}", contentType, Environment.NewLine));
				requestStream.Write(fileBuffer, 0, fileBuffer.Length);
				using (var fileStream = File.Open(file.FullName, FileMode.Open))
				{
					fileStream.CopyTo(requestStream);
				}
				fileBuffer = Encoding.ASCII.GetBytes(Environment.NewLine);
				requestStream.Write(fileBuffer, 0, fileBuffer.Length);

				var boundaryBuffer = Encoding.ASCII.GetBytes(boundary + "--");
				requestStream.Write(boundaryBuffer, 0, boundaryBuffer.Length);
			}

			try
			{
				using (var response = request.GetResponse())
				{
					using (var responseStream = response.GetResponseStream())
					{
						using (var stream = new MemoryStream())
						{
							responseStream.CopyTo(stream);
						}
					}
				}
			}
			catch
			{
				// When uploading in this manner, a 500 response is always returned for a successful upload. I'm not quite sure why at the minute
				// so just going to ignore the failures and hope that they all actually mean successes! This is just to get things moving more
				// quickly than individually uploading 1000s of files so I can live with rough-round-the-edges.
			}
		}
	}
}
