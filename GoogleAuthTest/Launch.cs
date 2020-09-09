using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using GoogleAuthTest.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace GoogleAuthTest
{
    public static class Launch
    {
        [FunctionName("Launch_Orchestrator")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();
            try
            {
                var azuredb = new AzureDB();
                var channels = await azuredb.GetChannelIds();
                foreach (var channel in channels)
                {
                    var youTube = new YouTube(channel);

                    //get some metrics for the channel
                    var channelMetrics = await youTube.GetMetricsForChannel(channel.YoutubeChannelID, channel.PlatformChannelID);
                    outputs.Add(azuredb.InsertChannelMetrics(channelMetrics));
                   
                    //get metrics for the videos
                    var videoList = await youTube.SearchForVideosAsync2(channel.PlatformChannelID);
                    foreach (var video in videoList)
                    {
                        var videoInfo = new YouTubeDurable();
                        videoInfo.VideoId = video;
                        videoInfo.ChannelInfo = channel;
                        outputs.Add(await context.CallActivityAsync<string>("Launch_GetVideoMetrics", videoInfo));
                    }
                }
            }
            catch(Exception ex)
            {
                outputs.Add("Error: " + ex.Message);
            }
            

            // Replace "hello" with the name of your Durable Activity Function.
            //outputs.Add(await context.CallActivityAsync<string>("Launch_GetVideoMetrics", "Tokyo"));
            //outputs.Add(await context.CallActivityAsync<string>("Launch_GetVideoMetrics", "Seattle"));
            //outputs.Add(await context.CallActivityAsync<string>("Launch_GetVideoMetrics", "London"));

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName("Launch_GetVideoMetrics")]
        public static string SayHello([ActivityTrigger] YouTubeDurable videoInfo, ILogger log)
        {
            string rc = "";
            try
            {
                log.LogInformation($"Saying hello to {videoInfo.VideoId}.");
                rc= $"Hello {videoInfo.VideoId}!";
            }
            catch(Exception ex)
            {
                rc = "ERROR: " + ex.Message;
            }
            return rc;
            //var azuredb = new AzureDB();
            //var youTube = new YouTube(videoInfo.ChannelInfo);

            //var metrics = youTube.GetMetricsForVideo(videoInfo.VideoId, videoInfo.ChannelInfo.YoutubeChannelID, videoInfo.ChannelInfo.PlatformChannelID);
            //var x = azuredb.InsertVideoMetrics(metrics);

            //log.LogInformation($"Insert: {x}.");
            //return x;
        }

        [FunctionName("Launch_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("Launch_Orchestrator", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}