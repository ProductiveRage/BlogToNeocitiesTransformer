using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoCitiesTransformer.SiteContentTransformer.ContentRewriting
{
	public class RewrittenContent
	{
		public RewrittenContent(string content, IEnumerable<Uri> referencedRelativeUrls)
		{
			if (content == null)
				throw new ArgumentNullException("content");
			if (referencedRelativeUrls == null)
				throw new ArgumentNullException("referencedRelativeUrls");

			Content = content;
			ReferencedRelativeUrls = referencedRelativeUrls.ToList();
			if (ReferencedRelativeUrls.Any(url => url == null))
				throw new ArgumentException("Null encountered in referencedRelativeUrls set");
			if (ReferencedRelativeUrls.Any(url => url.IsAbsoluteUri))
				throw new ArgumentException("Absolute urls may not be included in the referencedRelativeUrls set");
		}

		public string Content { get; private set; }

		public IEnumerable<Uri> ReferencedRelativeUrls { get; private set; }
	}
}
