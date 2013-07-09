using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HtmlAgilityPack;

namespace NeoCitiesTransformer.SiteContentTransformer.ContentRewriting
{
	/// <summary>
	/// This rewrites html content to changes url values in various locations, making use of the UrlRewriter - eg. anchor href values, form action values, etc..
	/// </summary>
	public class HtmlContentRewriter : IRewriteContent
	{
		private readonly UrlRewriter _urlRewriter;
		public HtmlContentRewriter(UrlRewriter urlRewriter)
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

			// TODO: Address conditional comments - eg.
			/*
				<!--[if lt IE 9]>
				<link rel="stylesheet" type="text/css" href="/Content/IEBefore9.css" />
				<![endif]-->
			 */

			var doc = new HtmlDocument();
			doc.OptionWriteEmptyNodes = true; // This seems to be required to prevent self-closing tags from being rewritten to not self-close (eg. img)
			doc.LoadHtml(content);

			var referencedRelativeUrls = new List<Uri>();
			var nodeTypesToChange = new[]
			{
				Tuple.Create("//form[@action]", "action"),
				Tuple.Create("//script[@src]", "src"),
				Tuple.Create("//link[@href]", "href"),
				Tuple.Create("//img[@src]", "src"),
				Tuple.Create("//a[@href]", "href")
			};
			foreach (var nodeTypeToChange in nodeTypesToChange)
			{
				var nodes = doc.DocumentNode.SelectNodes(nodeTypeToChange.Item1);
				if (nodes == null)
					continue;

				foreach (var node in nodes)
				{
					var attribute = node.Attributes[nodeTypeToChange.Item2];
					if (attribute == null)
						continue;

					var value = new Uri(attribute.Value, UriKind.RelativeOrAbsolute);
					if (value.IsAbsoluteUri)
						continue;

					var rewrittenUrl = _urlRewriter(value);
					if (rewrittenUrl.ToString().Split('?')[0].Split('#')[0].Trim('\\', '/') == "index.html")
					{
						// If the urlRewriter has left a link pointing to "index.html" then change this to the relative url "/" since "index.html"
						// is the default page for NeoCities and many many other solutions.
						var breakPoint = rewrittenUrl.ToString().IndexOfAny(new[] { '?', '#' });
						attribute.Value = "/" + (breakPoint == -1 ? "" : rewrittenUrl.ToString().Substring(breakPoint));
					}
					else
						attribute.Value = rewrittenUrl.ToString();

					referencedRelativeUrls.Add(value);
				}
			}

			var contentBuilder = new StringBuilder();
			using (var writer = new StringWriter(contentBuilder))
			{
				doc.Save(writer);
			}
			return new RewrittenContent(
				contentBuilder.ToString(),
				referencedRelativeUrls
			);
		}
	}
}
