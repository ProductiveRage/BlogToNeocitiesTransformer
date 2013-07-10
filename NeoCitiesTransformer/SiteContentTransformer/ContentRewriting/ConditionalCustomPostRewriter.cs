using System;
using System.Text.RegularExpressions;

namespace NeoCitiesTransformer.SiteContentTransformer.ContentRewriting
{
	public class ConditionalCustomPostRewriter : IRewriteContent
	{
		private readonly IRewriteContent _preRewriter;
		private readonly Predicate<Uri> _sourceUrlCondition;
		public ConditionalCustomPostRewriter(IRewriteContent preRewriter, Predicate<Uri> sourceUrlCondition, Regex toBeReplaced, string valueToReplaceWith)
		{
			if (preRewriter == null)
				throw new ArgumentNullException("preRewriter");
			if (sourceUrlCondition == null)
				throw new ArgumentNullException("sourceUrlCondition");
			if (toBeReplaced == null)
				throw new ArgumentNullException("toBeReplaced");
			if (valueToReplaceWith == null)
				throw new ArgumentNullException("valueToReplaceWith");

			_preRewriter = preRewriter;
			_sourceUrlCondition = sourceUrlCondition;
			ToBeReplaced = toBeReplaced;
			ValueToReplaceWith = valueToReplaceWith;
		}
		public ConditionalCustomPostRewriter(IRewriteContent preRewriter, Predicate<Uri> sourceUrlCondition, string valueToBeReplaced, string valueToReplaceWith)
			: this(preRewriter, sourceUrlCondition, new Regex(Regex.Escape(valueToBeReplaced ?? "")), valueToReplaceWith)
		{
			if (valueToBeReplaced == null)
				throw new ArgumentNullException("valueToBeReplaced");
		}

		/// <summary>
		/// This will never be null
		/// </summary>
		public Regex ToBeReplaced { get; private set; }

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
				MakeReplacement(reWrittenContent.Content),
				reWrittenContent.ReferencedRelativeUrls
			);
		}

		/// <summary>
		/// This is only public for testing
		/// </summary>
		public string MakeReplacement(string content)
		{
			if (content == null)
				throw new ArgumentNullException("content");

			return ToBeReplaced.Replace(content, ValueToReplaceWith);
		}
	}
}
