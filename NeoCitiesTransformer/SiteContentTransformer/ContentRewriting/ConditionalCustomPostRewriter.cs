using System;

namespace NeoCitiesTransformer.SiteContentTransformer.ContentRewriting
{
	public class ConditionalCustomPostRewriter : IRewriteContent
	{
		private readonly IRewriteContent _preRewriter;
		private readonly Predicate<Uri> _sourceUrlCondition;
		public ConditionalCustomPostRewriter(IRewriteContent preRewriter, Predicate<Uri> sourceUrlCondition, string valueToBeReplaced, string valueToReplaceWith)
		{
			if (preRewriter == null)
				throw new ArgumentNullException("preRewriter");
			if (sourceUrlCondition == null)
				throw new ArgumentNullException("sourceUrlCondition");
			if (valueToBeReplaced == null)
				throw new ArgumentNullException("valueToBeReplaced");
			if (valueToReplaceWith == null)
				throw new ArgumentNullException("valueToReplaceWith");

			_preRewriter = preRewriter;
			_sourceUrlCondition = sourceUrlCondition;
			ValueToBeReplaced = valueToBeReplaced;
			ValueToReplaceWith = valueToReplaceWith;
		}

		/// <summary>
		/// This will never be null
		/// </summary>
		public string ValueToBeReplaced { get; private set; }

		/// <summary>
		/// This will never be null
		/// </summary>
		public string ValueToReplaceWith { get; private set; }

		public RewrittenContent Rewrite(string content, Uri sourceUrl)
		{
			if (content == null)
				throw new ArgumentNullException("content");
			if (sourceUrl == null)
				throw new ArgumentNullException("sourceUrl");
			if (!sourceUrl.IsAbsoluteUri)
				throw new ArgumentException("sourceUrl must be an absolute url");

			var reWrittenContent = _preRewriter.Rewrite(content, sourceUrl);
			if (!_sourceUrlCondition(sourceUrl))
				return reWrittenContent;

			return new RewrittenContent(
				reWrittenContent.Content.Replace(ValueToBeReplaced, ValueToReplaceWith),
				reWrittenContent.ReferencedRelativeUrls
			);
		}
	}
}
