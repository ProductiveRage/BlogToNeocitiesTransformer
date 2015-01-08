using System;
using System.IO;
using System.Linq;
using System.Text;
using Blog.Models;
using BlogBackEnd.Models;
using NeoCitiesTransformer.Misc;
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

			// Load the Post Data (all files will be compressed to take up as little space as possible in the NeoCities hosting)
			// - Generate "SearchIndex-Titles.js"
			// - Generate "SearchIndex-Content-{0}.txt"
			var posts = (new SingleFolderPostRetriever(postSourceFolder)).Get();
			var titlesFilename = "SearchIndex-Titles.lz.txt";
			Console.WriteLine("Writing " + titlesFilename);
			var titlesJson = JsonConvert.SerializeObject(
				posts.ToDictionary(
					p => p.Id,
					p => new { Title = p.Title.Trim(), Slug = p.Slug }
				)
			);
			File.WriteAllText(
				Path.Combine(
					destination.FullName,
					titlesFilename
				),
				LZStringCompress.CompressToUTF16(titlesJson),
				new UTF8Encoding()
			);
			foreach (var post in posts)
			{
				var contentFilename = "SearchIndex-Content-" + post.Id + ".lz.txt";
				Console.WriteLine("Writing " + contentFilename);
				File.WriteAllText(
					Path.Combine(
						destination.FullName,
						contentFilename
					),
					LZStringCompress.CompressToUTF16(
						post.GetContentAsPlainText()
					),
					new UTF8Encoding()
				);
			}
		}
	}
}
