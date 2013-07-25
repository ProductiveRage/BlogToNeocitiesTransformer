using System;
using System.IO;
using System.Text.RegularExpressions;
using NeoCitiesTransformer.SearchIndexDataStorage;
using NeoCitiesTransformer.SiteContentTransformer;
using NeoCitiesTransformer.SiteContentTransformer.ContentRewriting;

namespace NeoCitiesTransformer
{
	class Program
	{
		static void Main(string[] args)
		{
			var destination = new DirectoryInfo("Output");
			if (!destination.Exists)
				destination.Create();

			var sourceSite = new Uri("http://www.productiverage.com");
			
			var generatingNeoCitiesProductiveRageVersion = false;
			if (!generatingNeoCitiesProductiveRageVersion)
			{
				// If generating for a generic site then this should do the job (or at least be a reasonable starting point)
				FetchGenericSite(sourceSite, destination);
			}
			else
			{
				// For my Blog I need to apply some customisations and generate all of the javascript search index data
				FetchBlog(sourceSite, destination);
				var postSourceFolder = new DirectoryInfo(
					@"C:\Users\Me\Documents\Visual Studio 2010\Projects\Blog\Blog\App_Data\Posts"
				);
				var searchIndexFile = new FileInfo(
					@"C:\Users\Me\Documents\Visual Studio 2010\Projects\Blog\Blog\App_Data\SearchIndex.dat"
				);
				JsonSearchIndexDataRecorder.Write(searchIndexFile, destination);
				PlainTextContentRecorder.Write(postSourceFolder, destination);
			}

			// 2013-07-25 DWR: This is no longer required now that NeoCities support drag-and-drop multiple file upload!
			/*
			// This is the workaround for the can-only-upload-single-files-through-the-NeoCities-interface limitation, it uploads files one at a time
			// but requires a "neocities" cookie with an authorisation token in (acquired after you log in) and a csfr-token form field value. Both
			// of these can be obtained by watching with Fiddler a single file upload being performed manually. There might be a better way to
			// handle all of this but this approach has been enough to get me going.
			var authCookieValue = "BAh7CUkiD3Nlc3Npb25faWQGOgZFVEkiRWRjNmRmNmJkOWY4NWY4Y2UwMGE1%0AMTQ5YThhY2ZhY2JkNmRlZDc3ODUyYTMyMGQ2ZmUxMmUxZmNkMzk4MGI0MmEG%0AOwBGSSIQX2NzcmZfdG9rZW4GOwBGSSIxZ0R6Tm81dld1SHQvbHp6TVJkaFcx%0ARndSS0dVbTdhNkVzNEMrOTNtS1Blcz0GOwBGSSIKZmxhc2gGOwBGewBJIgdp%0AZAY7AEZpAuoo%0A--db7b015d8335629edfb4115e0385105f6c3e37f4";
			var csrfToken = "gDzNo5vWuHt/lzzMRdhW1FwRKGUm7a6Es4C+93mKPes=";
			FileUploader.UploadFiles(
				"neocities",
				authCookieValue,
				csrfToken,
				destination
			);
			 */
		}

		/// <summary>
		/// This is the basic case, where a site's contents are retrieved and the only alterations are the url mappings (eg. flattening urls from "/Read/Me"
		/// to "Read-Me.html")
		/// </summary>
		private static void FetchGenericSite(Uri sourceSite, DirectoryInfo destination)
		{
			NeoCitiesGenerator.Regenerate(sourceSite, destination);
		}

		/// <summary>
		/// This is a version tailored to my Blog, I need to inject some extra scripts into the html pages and need to ensure that the RSS Feed link points
		/// to the main domain since I haven't been able to get that working on the NeoCities site yet.
		/// </summary>
		private static void FetchBlog(Uri sourceSite, DirectoryInfo destination)
		{
			var urlRewriter = DefaultUrlRewriter.Get(
				DefaultUrlRewriter.UrlRewriterQueryStringOptions.IncorporateQueryString,
				Tuple.Create(
					new Uri("/feed", UriKind.Relative),				// Always maps this..
					new Uri("http://www.productiverage.com/feed")	// .. onto this
				)
			);

			// Replace the "Scripts-Site.js" script tag with four distinct script tags (one of which is "Scripts-Site.js" so we're really adding scripts to
			// the existing markup rather than replacing / taking away), apply to all urls that are renamed to "*.html" 
			Func<IRewriteContent, IRewriteContent> scriptInjectingContentRewriter = contentRewriter => new ConditionalCustomPostRewriter(
					contentRewriter,
				sourceUri => urlRewriter(sourceUri).ToString().EndsWith(".html", StringComparison.InvariantCultureIgnoreCase),
				"<script type=\"text/javascript\" src=\"Scripts-Site.js\"></script>",
				string.Join(
						Environment.NewLine + "\t",
						new[]
						{
							// The IndexSearchGenerator.js, SearchTermHighlighter.js and SearchPage.js are external files, written specifically for
							// the NeoCities version of the Blog
							"<script type=\"text/javascript\" src=\"Scripts-Site.js\"></script>",
							"<script type=\"text/javascript\" src=\"IndexSearchGenerator.js\"></script>",
							"<script type=\"text/javascript\" src=\"SearchTermHighlighter.js\"></script>",
							"<script type=\"text/javascript\" src=\"SearchPage.js\"></script>",
							"<script type=\"text/javascript\" src=\"LZString.js\"></script>",
						}
				)
			);

			// Replace the Google Analytics API Key with one specific to the NeoCities version of the Blog, apply to all urls that are mapped to "*.html"
			Func<IRewriteContent, IRewriteContent> googleAnalyticsIdChangingContentRewriter = contentRewriter => new ConditionalCustomPostRewriter(
				contentRewriter,
				sourceUri => urlRewriter(sourceUri).ToString().EndsWith(".html", StringComparison.InvariantCultureIgnoreCase),
				"UA-32312857-1",
				"UA-42383037-1"
			);

			// Replace the "No search term entered" message with the text "Searching.." (the javascript in SearchPage.js will update this content if a
			// search is being performed and only show "No search term entered" if no search term has indeed been specified). A requires-javscript
			// message is also included in a "noscript" tag. This replacement is only required on the search.html page.
			Func<IRewriteContent, IRewriteContent> javascriptSearchMessageContentRewriter = contentRewriter => new ConditionalCustomPostRewriter(
				contentRewriter,
				sourceUri => (sourceUri.PathAndQuery == "/Search"),
				new Regex("<p class=\\\"NoResults\\\">\\s+No search term entered\\.\\.\\s+</p>"),
				"<p class=\"NoResults\">Searching..</p><noscript><p class=\"NoResults\"><em>Note: This functionality requires javascript.</em><br/><br/></p></noscript>"
			);

			NeoCitiesGenerator.Regenerate(
				sourceSite,
				destination,
				urlRewriter,
				contentRewriter => googleAnalyticsIdChangingContentRewriter(
					scriptInjectingContentRewriter(
						javascriptSearchMessageContentRewriter(
							contentRewriter
						)
					)
				)
			);
		}
	}
}
