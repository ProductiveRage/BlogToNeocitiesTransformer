using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace NeoCitiesTransformer.SiteContentTransformer.ContentRewriting
{
	public static class DefaultGitHubPagesUrlRewriter
	{
		public static UrlRewriter Get(params Tuple<Uri, Uri>[] customOverrides)
		{
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

				var urlString = url.ToString();
				if (urlString.Contains('?'))
					return null;

				if (urlString.Contains("://"))
					return url;
				else if (urlString.StartsWith("//"))
					return new Uri("https:" + urlString);

				urlString = urlString.Replace('\\', '/');
				if (urlString == "")
					urlString = "index.html";

				return new Uri(urlString, UriKind.RelativeOrAbsolute);
			};
		}
	}
}
