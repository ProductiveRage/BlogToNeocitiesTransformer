using System;
using System.Collections.Generic;
using System.IO;

namespace NeoCitiesTransformer.SiteContentTransformer
{
	public static class FileUploader
	{
		/// <summary>
		/// This will attempt to upload all files from a specified folder using the SingleFileUploader. Any uploads that are rejected or otherwise fail will not
		/// be reported since the SingleFileUploader expects 500 responses even for successes (since that's what I've been getting uploading to Neocities - it
		/// works but I'm presumably doing something wrong to be getting these responses!)
		/// </summary>
		public static void UploadFiles(string authCookieName, string authCookieValue, string csrfToken, DirectoryInfo sourceFolder)
		{
			if (string.IsNullOrWhiteSpace(authCookieName))
				throw new ArgumentNullException("Null/blank authCookieName specified");
			if (string.IsNullOrWhiteSpace(authCookieValue))
				throw new ArgumentNullException("Null/blank authCookieValue specified");
			if (string.IsNullOrWhiteSpace(csrfToken))
				throw new ArgumentNullException("Null/blank csrfToken specified");
			if (sourceFolder == null)
				throw new ArgumentNullException("sourceFolder");

			var textExtensions = new HashSet<string>(
				new[]
				{
					"html", "htm",
					"md", "markdown",
					"js", "json", "geojson",
					"css",
					"txt", "text", "csv", "tsv",
				},
				StringComparer.InvariantCultureIgnoreCase
			);

			var uploader = new SingleFileUploader(authCookieName, authCookieValue, csrfToken);
			foreach (var file in sourceFolder.EnumerateFiles())
			{
				var isTextFile = textExtensions.Contains(file.Extension.Trim('.').Trim());
				Console.WriteLine(file.Name + " [Text: " + isTextFile + "]");
				uploader.UploadFiles(
					new Uri("http://neocities.org/site_files/upload"),
					file,
					isTextFile ? "text/plain" : "application/octet-stream"
				);
			}
		}
	}
}
