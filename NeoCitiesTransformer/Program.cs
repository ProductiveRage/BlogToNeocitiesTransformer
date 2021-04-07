using System.Threading.Tasks;

namespace NeoCitiesTransformer
{
    class Program
    {
        static async Task Main()
        {
            // Don't upload 404.html (manually change its copyright year range if required)
            await Program_GitHubPages.Go();

            //await Program_NeoCities.Go(); // THIS COMES FROM THE LIVE SITE
        }
    }
}