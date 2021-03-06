﻿using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Blog.Models;
using NeoCitiesTransformer.Misc;
using NeoCitiesTransformer.SearchIndexDataStorage;
using NeoCitiesTransformer.SiteContentTransformer;
using NeoCitiesTransformer.SiteContentTransformer.ContentRewriting;
using NeoCitiesTransformer.SiteContentTransformer.DataRetrieval;

namespace NeoCitiesTransformer
{
    public class Program_NeoCities
	{
		public static async Task Go()
		{
			var destination = new DirectoryInfo("NeoCities");
			if (!destination.Exists)
				destination.Create();

			var sourceSite = new Uri("https://www.productiverage.com");
			
			var generatingNeoCitiesProductiveRageVersion = true;
			if (!generatingNeoCitiesProductiveRageVersion)
			{
				// If generating for a generic site then this should do the job (or at least be a reasonable starting point)
				FetchGenericSite(sourceSite, destination);
			}
			else
			{
				// For my Blog I need to apply some customisations and generate all of the javascript search index data
				FetchBlog(sourceSite, destination);
				var searchIndexFile = new FileInfo(
					@"D:\github\Blog\Blog\App_Data\SearchIndex.dat"
				);
				JsonSearchIndexDataRecorder.Write(searchIndexFile, destination);
				var postSourceFolder = new DirectoryContents(@"D:\github\Blog\Blog\App_Data\Posts");
				if (!postSourceFolder.Exists)
					throw new ArgumentException("postSourceFolder does not exist");
				PlainTextContentRecorder.Write(await new SingleFolderPostRetriever(postSourceFolder).Get(), destination);
			}
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
			var urlRewriter = DefaultNeocitiesUrlRewriter.Get(
				DefaultNeocitiesUrlRewriter.UrlRewriterQueryStringOptions.IncorporateQueryString,
				Tuple.Create(
					new Uri("/feed", UriKind.Relative),				// Always maps this..
					new Uri("http://www.productiverage.com/feed")	// .. onto this
				)
			);

			// Replace the "Scripts-Site.js" script tag with four distinct script tags (one of which is "Scripts-Site.js" so we're really adding scripts to
			// the existing markup rather than replacing / taking away), apply to all urls that are renamed to "*.html" 
			IRewriteContent scriptInjectingContentRewriter(IRewriteContent contentRewriter) => new ConditionalCustomPostRewriter(
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
			IRewriteContent googleAnalyticsIdChangingContentRewriter(IRewriteContent contentRewriter) => new ConditionalCustomPostRewriter(
				contentRewriter,
				sourceUri => urlRewriter(sourceUri).ToString().EndsWith(".html", StringComparison.InvariantCultureIgnoreCase),
				"UA-32312857-1",
				"UA-42383037-1"
			);

			// Replace "http://ajax.googleapis.com/ajax/libs/jquery/1.5.1/jquery.min.js" with "//ajax.googleapis.com/ajax/libs/jquery/1.5.1/jquery.min.js"
			// so it will work over http or https connections (this will be in the main site when I update it next but I want to fix it on Neocities now
			// since its links default to https and it's refusing to load jQuery in Chrome now)
			IRewriteContent jQueryProtocolLessRequestsForHttps(IRewriteContent contentRewriter) => new ConditionalCustomPostRewriter(
				contentRewriter,
				sourceUri => urlRewriter(sourceUri).ToString().EndsWith(".html", StringComparison.InvariantCultureIgnoreCase),
				"http://ajax.googleapis.com/ajax/libs/jquery/1.5.1/jquery.min.js",
				"//ajax.googleapis.com/ajax/libs/jquery/1.5.1/jquery.min.js"
			);

			// Replace the "No search term entered" message with the text "Searching.." (the javascript in SearchPage.js will update this content if a
			// search is being performed and only show "No search term entered" if no search term has indeed been specified). A requires-javscript
			// message is also included in a "noscript" tag. This replacement is only required on the search.html page.
			IRewriteContent javascriptSearchMessageContentRewriter(IRewriteContent contentRewriter) => new ConditionalCustomPostRewriter(
				contentRewriter,
				sourceUri => (sourceUri.PathAndQuery == "/Search"),
				new Regex("<p class=\\\"NoResults\\\">\\s+No search term entered\\.\\.\\s+</p>"),
				"<p class=\"NoResults\">Searching..</p><noscript><p class=\"NoResults\"><em>Note: This functionality requires javascript.</em><br/><br/></p></noscript>"
			);

			NeoCitiesGenerator.Regenerate(
				sourceSite,
				destination,
				urlRewriter,
				contentRewriter => jQueryProtocolLessRequestsForHttps(
					googleAnalyticsIdChangingContentRewriter(
						scriptInjectingContentRewriter(
							javascriptSearchMessageContentRewriter(
								contentRewriter
							)
						)
					)
				)
			);
			File.WriteAllText(
				Path.Combine(destination.FullName, "AutoComplete.json"),
				new WebDataRetriever().GetText(new Uri(sourceSite, "AutoComplete.json"))
			);
		}
	}
}
