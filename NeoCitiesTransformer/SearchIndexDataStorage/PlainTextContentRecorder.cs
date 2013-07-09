using System;
using System.IO;
using System.Linq;
using Blog.Models;
using BlogBackEnd.Models;
using Newtonsoft.Json;

namespace NeoCitiesTransformer.SearchIndexDataStorage
{
	public static class PlainTextContentRecorder
	{
		/// <summary>
		/// This generates the "SearchIndex-Titles.js" file and plain text representations of Posts that are required to display search result content (the
		/// JsonSearchIndexDataRecorder generates the javascript search index data which identifies matches but this content is required to map that on to
		/// Post titles and sections of content). These two classes are very specific to my Blog site implementation.
		/// </summary>
		public static void Write(DirectoryInfo postSourceFolder, DirectoryInfo destination)
		{
			if (postSourceFolder == null)
				throw new ArgumentNullException("postSourceFolder");
			postSourceFolder.Refresh();
			if (!postSourceFolder.Exists)
				throw new ArgumentException("postSourceFolder does not exist");
			if (destination == null)
				throw new ArgumentNullException("destination");
			destination.Refresh();
			if (!destination.Exists)
				throw new ArgumentException("destination does not exist");

			// Load the Post Data
			// - Generate "SearchIndex-Titles.js"
			// - Generate "SearchIndex-Content-{0}.txt"
			var posts = (new SingleFolderPostRetriever(postSourceFolder)).Get();
			File.WriteAllText(
				Path.Combine(
					destination.FullName,
					"SearchIndex-Titles.js"
				),
				JsonConvert.SerializeObject(
					posts.ToDictionary(
						p => p.Id,
						p => p.Title.Trim()
					)
				)
			);
			foreach (var post in posts)
			{
				File.WriteAllText(
					Path.Combine(
						destination.FullName,
						"SearchIndex-Content-" + post.Id + ".txt"
					),
					post.GetContentAsPlainText()
				);
			}
		}
	}
}
