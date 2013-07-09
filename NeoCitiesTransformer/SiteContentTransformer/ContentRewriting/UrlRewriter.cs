using System;

namespace NeoCitiesTransformer.SiteContentTransformer.ContentRewriting
{
	/// <summary>
	/// This will never return null, it will throw an exception for a null url argument
	/// </summary>
	public delegate Uri UrlRewriter(Uri url);
}
