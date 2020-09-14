using System;
using GoogleAuthTest.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GoogleAuthTest
{
    public static class ProcessVideoQueueEntry
    {
        [FunctionName("PrecessVideoQueueEntry")]
        public static async System.Threading.Tasks.Task RunAsync([QueueTrigger("youtubevideoanalytics", Connection = "QUEUECONNECTION")]string myQueueItem, ILogger log)
        {
            var youTubeQueueEntry = JsonConvert.DeserializeObject<YouTubeQueueEntry>(myQueueItem);
           
            var azuredb = new AzureDB();
            var youTube = new YouTube(youTubeQueueEntry.channelInfo);

            var metrics = await youTube.GetMetricsForVideo_Async(youTubeQueueEntry.videoId, youTubeQueueEntry.channelInfo.YoutubeChannelID, youTubeQueueEntry.channelInfo.PlatformChannelID);
            var status = azuredb.InsertVideoMetrics(metrics);
            log.LogInformation(status);
        }
    }
}
