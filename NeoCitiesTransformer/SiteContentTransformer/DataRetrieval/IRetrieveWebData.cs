using System;

namespace NeoCitiesTransformer.SiteContentTransformer.DataRetrieval
{
	public interface IRetrieveWebData
	{
		byte[] GetBinary(Uri url);
		string GetText(Uri url);
	}
}
