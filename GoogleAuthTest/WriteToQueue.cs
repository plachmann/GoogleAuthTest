using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using Azure.Storage.Queues;
using GoogleAuthTest.Models;

namespace GoogleAuthTest
{
    // read all channel ids from azure db
    // for each channeld get list of videos
    // for each video get metrics
    public static class WriteToQueue
    {
        [FunctionName("WriteVideosToQueue")]
        public static async Task<IActionResult> WriteVideosToQueue(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log, ExecutionContext context)
        {
            string output = "Starttime: " + DateTime.Now.ToString() + "\n";
            var azuredb = new AzureDB();
            string connectionString = Environment.GetEnvironmentVariable("QUEUECONNECTION");
            QueueClient queue = new QueueClient(connectionString, "youtubevideoanalytics");

            try
            {
                var channels = await azuredb.GetChannelIds();
                if (null != await queue.CreateIfNotExistsAsync())
                {
                    Console.WriteLine("The queue was created.");
                }
                foreach (var channel in channels)
                {
                    var youTube = new YouTube(channel);

                    //get some metrics for the channel
                    var channelMetrics = await youTube.GetMetricsForChannel(channel.YoutubeChannelID, channel.PlatformChannelID);
                    output += azuredb.InsertChannelMetrics(channelMetrics);
                    string formattedJson = JsonConvert.SerializeObject(channelMetrics, Formatting.Indented);
                    output += formattedJson + "\n\n";

                    //get metrics for the videos
                    var videoList = await youTube.SearchForVideosAsync2(channel.PlatformChannelID, channelMetrics.ChannelPublishDate.Year, log);
                    var count = 0;
                    output += $"\n\n{videoList.Count} videos returned in list.\n\n";
                    foreach (var video in videoList)
                    {
                        var queueEntry = new YouTubeQueueEntry
                        {
                            channelInfo = channel,
                            videoId = video
                        };
                        // strings put on queue need to be base64 encoded
                        var serializedQueueEntry = JsonConvert.SerializeObject(queueEntry);
                        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(serializedQueueEntry);
                        await queue.SendMessageAsync(Convert.ToBase64String(plainTextBytes));
                        count++;
                        //res += $"videoid: {video}\n";
                    }
                    output += $"\n{count} records written to queue";
                }

            }
            catch (Exception ex)
            {
                output = "Error: " + ex.Message;
            }

            output += "Endtime: " + DateTime.Now.ToString() + "\n";
            return new OkObjectResult(output);
        }
    }
}
