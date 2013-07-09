using System;

namespace NeoCitiesTransformer.SiteContentTransformer.ContentRewriting
{
	public interface IRewriteContent
	{
		RewrittenContent Rewrite(string content, Uri sourceUrl);
	}
}
