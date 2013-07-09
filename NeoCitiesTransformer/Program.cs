using System;
using System.IO;
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

			// This is the workaround for the can-only-upload-single-files-through-the-NeoCities-interface limitation, it uploads files one at a time
			// but requires a "neocities" cookie with an authorisation token in (acquired after you log in) and a csfr-token form field value. Both
			// of these can be obtained by watching with Fiddler a single file upload being performed manually. There might be a better way to
			// handle all of this but this approach has been enough to get me going.
			var authCookieValue = "GetMeFromCookieDataVisibleInFiddler";
			var csrfToken = "GetMeFromFiddlerAsWellOrByTypingThisIntoTheBrowserConsoleFromTheUploadPage: $('meta[name=csrf-token]')[0].content";
			FileUploader.UploadFiles(
				"neocities",
				authCookieValue,
				csrfToken,
				destination
			);
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
			NeoCitiesGenerator.Regenerate(
				sourceSite,
				destination,
				urlRewriter,
				contentRewriter => new ConditionalCustomPostRewriter(
					contentRewriter,
					sourceUri => urlRewriter(sourceUri).ToString().EndsWith(".html", StringComparison.InvariantCultureIgnoreCase), // Replacement condition
					"<script type=\"text/javascript\" src=\"Scripts-Site.js\"></script>", // Value to replace
					string.Join( // Value to replace it with
						Environment.NewLine + "\t",
						new[]
						{
							// The IndexSearchGenerator.js, SearchTermHighlighter.js and SearchPage.js are external files, written specifically for
							// the NeoCities version of the Blog
							"<script type=\"text/javascript\" src=\"Scripts-Site.js\"></script>",
							"<script type=\"text/javascript\" src=\"IndexSearchGenerator.js\"></script>",
							"<script type=\"text/javascript\" src=\"SearchTermHighlighter.js\"></script>",
							"<script type=\"text/javascript\" src=\"SearchPage.js\"></script>",
						}
					)
				)
			);
		}
	}
}
