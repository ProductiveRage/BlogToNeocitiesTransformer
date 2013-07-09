using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeoCitiesTransformer.SiteContentTransformer.ContentRewriting;
using NeoCitiesTransformer.SiteContentTransformer.DataRetrieval;

namespace NeoCitiesTransformer.SiteContentTransformer
{
	public static class NeoCitiesGenerator
	{
		public static void Regenerate(Uri root, DirectoryInfo destination)
		{
			Regenerate(
				root,
				destination,
				DefaultUrlRewriter.Get(DefaultUrlRewriter.UrlRewriterQueryStringOptions.IncorporateQueryString),
				null
			);
		}

		public static void Regenerate(
			Uri root,
			DirectoryInfo destination,
			UrlRewriter urlRewriter,
			Func<IRewriteContent, IRewriteContent> optionalRewriteIntercepter)
		{
			if (root == null)
				throw new ArgumentNullException("root");
			if (!root.IsAbsoluteUri)
				throw new ArgumentException("root must be an absolute url");
			if (destination == null)
				throw new ArgumentNullException("destination");
			destination.Refresh();
			if (!destination.Exists)
				throw new ArgumentException("The specified destination folder does not exist");
			if (urlRewriter == null)
				throw new ArgumentNullException("urlRewriter");

			var webRequester = new WebDataRetriever();
			IRewriteContent htmlContentRewriter = new HtmlContentRewriter(urlRewriter);
			if (optionalRewriteIntercepter != null)
				htmlContentRewriter = optionalRewriteIntercepter(htmlContentRewriter);
			IRewriteContent cssContentRewriter = new CssContentRewriter(urlRewriter);
			if (optionalRewriteIntercepter != null)
				cssContentRewriter = optionalRewriteIntercepter(cssContentRewriter);

			var processedUrls = new HashSet<Uri>();
			var urlsToProcess = new HashSet<Uri>
			{
				new Uri("/", UriKind.Relative)
			};
			while (urlsToProcess.Any(u => !processedUrls.Contains(u)))
			{
				foreach (var url in urlsToProcess.ToArray().Where(u => !processedUrls.Contains(u)))
				{
					Console.WriteLine(url);
					Console.WriteLine(urlRewriter(url));
					Console.WriteLine();

					var urlToRequest = new Uri(root, url);
					var rewrittenUrl = urlRewriter(url);
					if (rewrittenUrl.IsAbsoluteUri)
					{
						processedUrls.Add(url);
						continue;
					}

					var pageName = urlRewriter(url).ToString().Split('?')[0].Split('#')[0];

					// If it's html or css content then pass it through the appropriate default content rewriter and then the optionalRewriteIntercepter
					// (if non-null). Otherwise just pull it as binary content, regardless of whether it's an image or a javascript file (no custom
					// rewriting is done in this case, the optionalRewriteIntercepter is not used even if it isn't null)
					IRewriteContent contentRewriter;
					if (pageName.EndsWith(".html", StringComparison.InvariantCultureIgnoreCase))
						contentRewriter = htmlContentRewriter;
					else if (pageName.EndsWith(".css", StringComparison.InvariantCultureIgnoreCase))
						contentRewriter = cssContentRewriter;
					else
						contentRewriter = null;

					if (contentRewriter == null)
					{
						File.WriteAllBytes(
							Path.Combine(
								destination.FullName,
								pageName
							),
							webRequester.GetBinary(urlToRequest)
						);
					}
					else
					{
						var rewrittenContent = contentRewriter.Rewrite(
							webRequester.GetText(urlToRequest),
							urlToRequest
						);
						File.WriteAllText(
							Path.Combine(
								destination.FullName,
								pageName
							),
							rewrittenContent.Content
						);
						foreach (var urlToAdd in rewrittenContent.ReferencedRelativeUrls)
							urlsToProcess.Add(urlToAdd);
					}

					processedUrls.Add(url);
				}
			}

			// TODO: Have to deal with any files referenced by javascript!
		}
	}
}
