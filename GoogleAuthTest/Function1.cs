using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;


namespace GoogleAuthTest
{
    // read all channel ids from azure db
    // for each channeld get list of videos
    // for each video get metrics
    public static class Function3
    {
        [FunctionName("Function3JSON")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            var res = "";

            // Crossroad Church(Main Channel):  UCEdRBpSpVgfuybR3lzgxa-Q
            // Crossroad Church Music:  UC5w5QDnJIpeJR_KnLZSEg1Q
            // Crossroad Kids' Club:  UCmMySSzKknjgAVVCOPXu_qg

            var azuredb = new AzureDB();
            var youTube = new YouTube();

            var channels = await azuredb.GetChannelIds();
            
            foreach (var channel in channels)
            {
                var videoList = await youTube.SearchForVideosAsync2(channel.PlatformChannelID);
                foreach (var video in videoList)
                {
                    var metrics = await youTube.GetMetricsForVideo(video, channel.YoutubeChannelID);
                    res += azuredb.InsertVideoMetrics(metrics);
                }
            }
            return new OkObjectResult(res);
        }

    }
}
