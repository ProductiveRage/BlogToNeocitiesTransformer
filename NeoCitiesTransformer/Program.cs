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
			Program_GitHubPages.Go();
		}
	}
}
