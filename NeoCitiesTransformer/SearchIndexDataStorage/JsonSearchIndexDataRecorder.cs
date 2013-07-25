﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using BlogBackEnd.FullTextIndexing;
using BlogBackEnd.FullTextIndexing.CachingPostIndexers;
using FullTextIndexer.Core.Indexes;
using NeoCitiesTransformer.Misc;
using Newtonsoft.Json;

namespace NeoCitiesTransformer.SearchIndexDataStorage
{
	public static class JsonSearchIndexDataRecorder
	{
		/// <summary>
		/// This generates the "SearchIndex-SummaryDictionary.js" and "SearchIndex-{PostId}-CompleteDictionary.js" files that are used to perform the full
		/// text site search. The first file maps token matches onto Posts by Key, specifying the match Weight. It doesn't contain the source locations
		/// which map the token back onto the source content in order to keep the file size down. The "SearchIndex-{PostId}-CompleteDictionary.js" files
		/// contain the mappings with source locations for a single Post. These only need to be accessed once a Post has been identified as matching the
		/// search term(s). In order to display matched content, the source locations must be mapped onto the plain text content generated by the
		/// PlainTextContentRecorder. These two classes are very specific to my Blog site implementation.
		/// </summary>
		public static void Write(IIndexData<int> searchIndex, DirectoryInfo destination)
		{
			if (searchIndex == null)
				throw new ArgumentNullException("searchIndexFile");
			if (destination == null)
				throw new ArgumentNullException("destination");
			destination.Refresh();
			if (!destination.Exists)
				throw new ArgumentException("destination does not exist");

			// Get Search Index Data
			// - Generate "SearchIndex-SummaryDictionary.js"
			// - Generate all of "SearchIndex-{0}-CompleteDictionary.js"

			// Translate into combined detail data for all Posts
			var matchData = searchIndex.GetAllTokens().Select(token => new JsTokenMatch
			{
				t = token,
				l = searchIndex.GetMatches(token).Select(weightedEntry => new JsSourceLocation
				{
					k = weightedEntry.Key,
					w = weightedEntry.Weight,
					l = weightedEntry.SourceLocations.Select(sourceLocation => new JsSourceLocationDetail
					{
						f = sourceLocation.SourceFieldIndex,
						w = sourceLocation.MatchWeightContribution,
						t = sourceLocation.TokenIndex,
						i = sourceLocation.SourceIndex,
						l = sourceLocation.SourceTokenLength
					})
				})
			});

			// The all-Post Summary data is going to be an associative array of token to Key/Weight matches (no Source Location data). This won't be
			// compressed so that the initial searching can be as quick as possible (the trade-off between valuable space at NeoCities hosting vs the
			// speed of native compression - ie. the gzip that happens over the wire but that doesn't benefit the backend storage - is worth it)
			var allPostsSummaryDictionary = matchData.ToDictionary(
				tokenMatch => tokenMatch.t,
				tokenMatch => tokenMatch.l.Select(weightedEntry => new JsSourceLocation
				{
					k = weightedEntry.k,
					w = weightedEntry.w
				})
			);
			File.WriteAllText(
				Path.Combine(destination.FullName, "SearchIndex-SummaryDictionary.js"),
				SerialiseToJson(allPostsSummaryDictionary),
				new UTF8Encoding()
			);

			// The per-Post Detail data is going to be an associative array of token to Key/Weight matches (with Source Location) but only a single
			// Key will appear in each dictionary. This data WILL be compressed since it takes up a lot of space considering the NeoCities limits.
			var perPostData = new Dictionary<int, IEnumerable<JsTokenMatch>>();
			foreach (var entry in matchData)
			{
				foreach (var result in entry.l)
				{
					var key = result.k;
					if (!perPostData.ContainsKey(key))
						perPostData.Add(key, new JsTokenMatch[0]);
					perPostData[key] = perPostData[key].Concat(new[] { 
						new JsTokenMatch
						{
							t = entry.t,
							l = new[] { result }
						}
					});
				}
			}
			foreach (var postId in perPostData.Keys)
			{
				File.WriteAllText(
					Path.Combine(destination.FullName, "SearchIndex-" + postId + "-CompleteDictionary.lz.txt"),
					LZStringCompress.CompressToUTF16(
						SerialiseToJson(
							perPostData[postId].ToDictionary(
								entry => entry.t,
								entry => entry.l
							)
						)
					),
					new UTF8Encoding()
				);
			}
		}

		/// <summary>
		/// The Blog search index data currently doesn't directly expose the IIndexData reference so it'll have to be extracted with reflection and passed
		/// to the Write method signature above
		/// </summary>
		public static void Write(FileInfo searchIndexFile, DirectoryInfo destination)
		{
			if (searchIndexFile == null)
				throw new ArgumentNullException("searchIndexFile");
			searchIndexFile.Refresh();
			if (!searchIndexFile.Exists)
				throw new ArgumentException("searchIndexFile does not exist");
			if (destination == null)
				throw new ArgumentNullException("destination");

			// Extract the index data from the serialised Post cache file
			PostIndexContent postIndexContent;
			using (var stream = File.Open(searchIndexFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				using (var decompressedStream = new GZipStream(stream, CompressionMode.Decompress))
				{
					postIndexContent = (new BinaryFormatter().Deserialize(decompressedStream) as CachedPostIndexContent).Index;
				}
			}
			Write(
				(IIndexData<int>)postIndexContent.GetType().GetField("_searchIndex", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(postIndexContent),
				destination
			);
		}

		private static string SerialiseToJson(object value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			return JsonConvert.SerializeObject(
				value,
				Formatting.None,
				new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }
			);
		}

		// This should be a dictionary so that the resulting data is an associative array
		private class JsTokenMatch
		{
			public string t { get; set; }
			public IEnumerable<JsSourceLocation> l { get; set; }
		}

		private class JsSourceLocation
		{
			public int k { get; set; } // Key
			public float w { get; set; } // Weight
			public IEnumerable<JsSourceLocationDetail> l { get; set; } // SourceLocations
		}

		private class JsSourceLocationDetail
		{
			public int f { get; set; } // SourceFieldIndex
			public float w { get; set; } // MatchWeightContribution
			public int t { get; set; } // TokenIndex
			public int i { get; set; } // SourceIndex
			public int l { get; set; } // SourceTokenLength
		}
	}
}
