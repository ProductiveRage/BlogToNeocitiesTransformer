using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace NeoCitiesTransformer.SiteContentTransformer.ContentRewriting
{
	public static class DefaultUrlRewriter
	{
		public static UrlRewriter Get(UrlRewriterQueryStringOptions queryStringOptions, params Tuple<Uri, Uri>[] customOverrides)
		{
			if (!Enum.IsDefined(typeof(UrlRewriterQueryStringOptions), queryStringOptions))
				throw new ArgumentOutOfRangeException("queryStringOptions");

			var allowedExtensions = new[]
			{
				"html",
				"htm",
				"jpg",
				"png",
				"gif",
				"svg",
				"md",
				"markdown",
				"js",
				"json",
				"geojson",
				"css",
				"txt",
				"text",
				"csv",
				"tsv",
				"eot",
				"ttf",
				"woff",
				"svg",
				"ico" // Neocities won't allow this but there is no point trying to rename it to "favicon.ico.html" so just retrieve it and ignore the upload error later
			};

			return url =>
			{
				if (url == null)
					throw new ArgumentNullException("url");

				if (customOverrides != null)
				{
					var customOverride = customOverrides.FirstOrDefault(c => c.Item1.Equals(url));
					if (customOverride != null)
						return customOverride.Item2;
				}

				var urlString = url.ToString().Replace('\\', '/').Trim('/');
				if (urlString == "")
					urlString = "index.html";

				string hashContent;
				if (urlString.Contains('#'))
				{
					hashContent = urlString.Substring(urlString.LastIndexOf('#') + 1);
					urlString = urlString.Substring(0, urlString.LastIndexOf('#'));
				}
				else
					hashContent = "";

				string queryString;
				if (urlString.Contains('?'))
				{
					queryString = urlString.Substring(urlString.LastIndexOf('?') + 1);
					urlString = urlString.Substring(0, urlString.LastIndexOf('?'));
				}
				else
					queryString = "";

				if (urlString.EndsWith(".less", StringComparison.InvariantCultureIgnoreCase))
					urlString = urlString.Substring(0, urlString.LastIndexOf('.')) + ".css";

				if (!allowedExtensions.Any(extension => urlString.EndsWith("." + extension, StringComparison.InvariantCultureIgnoreCase)))
					urlString += ".html";

				var recombinedUrlString = urlString;
				if ((queryString != "") && (queryStringOptions == UrlRewriterQueryStringOptions.IncorporateQueryString))
				{
					var breakPoint = recombinedUrlString.LastIndexOf('.');
					recombinedUrlString = recombinedUrlString.Substring(0, breakPoint) + "-" + queryString + recombinedUrlString.Substring(breakPoint);
				}

				// Replace non-alphanumeric characters with "-"
				recombinedUrlString = (new Regex("[^a-zA-Z0-9 -\\.]")).Replace(recombinedUrlString, "-");

				if ((queryString != "") && (queryStringOptions == UrlRewriterQueryStringOptions.MaintainSeparateQueryString))
					recombinedUrlString += "?" + queryString;
				if (hashContent != "")
					recombinedUrlString += "#" + hashContent;

				return new Uri(recombinedUrlString, UriKind.RelativeOrAbsolute);
			};
		}

		public enum UrlRewriterQueryStringOptions
		{
			/// <summary>
			/// This will break down the Query String name-key pairs and make them part of the Url - eg. "search?term=test" may become "search-term-test.html"
			/// </summary>
			IncorporateQueryString,

			/// <summary>
			/// This will leave the Query String unaltered, changing only the Url path - eg. "search?term=test" may become "search.html?term=test"
			/// </summary>
			MaintainSeparateQueryString
		}
	}
}
