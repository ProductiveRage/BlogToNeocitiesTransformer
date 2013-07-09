using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSSParser;

namespace NeoCitiesTransformer.SiteContentTransformer.ContentRewriting
{
	/// <summary>
	/// This rewrites css content to changes url values in various locations, making use of the UrlRewriter - currently it only rewrites style property values sections
	/// of the form "url{whatever}" - eg. "background: black url(background.jpf) top left no-repeat;"
	/// </summary>
	public class CssContentRewriter : IRewriteContent
	{
		private readonly UrlRewriter _urlRewriter;
		public CssContentRewriter(UrlRewriter urlRewriter)
		{
			if (urlRewriter == null)
				throw new ArgumentNullException("urlRewriter");

			_urlRewriter = urlRewriter;
		}

		public RewrittenContent Rewrite(string content, Uri sourceUrl)
		{
			if (content == null)
				throw new ArgumentNullException("content");
			if (sourceUrl == null)
				throw new ArgumentNullException("sourceUrl");
			if (!sourceUrl.IsAbsoluteUri)
				throw new ArgumentException("sourceUrl must be an absolute url");

			var contentBuilder = new StringBuilder();
			var referencedRelativeUrls = new List<Uri>();
			foreach (var segment in Parser.ParseCSS(content))
			{
				if (segment.CharacterCategorisation == CSSParser.ContentProcessors.CharacterCategorisationOptions.Comment)
					continue;

				var startIdentifier = "url(";
				var endIdentifier = ")";
				if (!segment.Value.StartsWith(startIdentifier, StringComparison.InvariantCultureIgnoreCase)
				|| !segment.Value.EndsWith(endIdentifier, StringComparison.InvariantCultureIgnoreCase))
				{
					contentBuilder.Append(segment.Value);
					continue;
				}

				var urlContent = segment.Value.Substring(startIdentifier.Length, segment.Value.Length - (startIdentifier.Length + endIdentifier.Length));
				var possibleQuoteCharacters = new[] { "\"", "'" };
				foreach (var possibleQuoteCharacter in possibleQuoteCharacters)
				{
					if (urlContent.StartsWith(possibleQuoteCharacter))
					{
						urlContent = urlContent.Substring(1);
						if (urlContent.EndsWith(possibleQuoteCharacter))
							urlContent = urlContent.Substring(0, urlContent.Length - 1);
						break;
					}
				}

				var rewrittenUrl = new Uri(urlContent, UriKind.RelativeOrAbsolute);
				if (!rewrittenUrl.IsAbsoluteUri)
				{
					rewrittenUrl = new Uri(
						new Uri(sourceUrl, rewrittenUrl).PathAndQuery,
						UriKind.Relative
					);
					referencedRelativeUrls.Add(rewrittenUrl);
				}

				contentBuilder.Append(startIdentifier);
				contentBuilder.Append("\"");
				contentBuilder.Append(_urlRewriter(rewrittenUrl).ToString());
				contentBuilder.Append("\"");
				contentBuilder.Append(endIdentifier);
			}
			return new RewrittenContent(
				contentBuilder.ToString(),
				referencedRelativeUrls
			);
		}
	}
}
