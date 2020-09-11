using System;
using GoogleAuthTest.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GoogleAuthTest
{
    public static class PrecessVideoQueueEntry
    {
        [FunctionName("PrecessVideoQueueEntry")]
        public static async System.Threading.Tasks.Task RunAsync([QueueTrigger("youtubevideoanalytics", Connection = "QUEUECONNECTION")]string myQueueItem, ILogger log)
        {
            var youTubeQueueEntry = JsonConvert.DeserializeObject<YouTubeQueueEntry>(myQueueItem);
            //log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
            //log.LogInformation($"videoid: {x.videoId}   channelid: {x.channelInfo.PlatformChannelID} id: {x.channelInfo.ClientID}  ");
            var azuredb = new AzureDB();
            var youTube = new YouTube(youTubeQueueEntry.channelInfo);

            var metrics = await youTube.GetMetricsForVideo_Async(youTubeQueueEntry.videoId, youTubeQueueEntry.channelInfo.YoutubeChannelID, youTubeQueueEntry.channelInfo.PlatformChannelID);
            azuredb.InsertVideoMetrics(metrics);
        }
    }
}
