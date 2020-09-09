using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

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
            var res = "Howdy";

            // Crossroad Church(Main Channel):  UCEdRBpSpVgfuybR3lzgxa-Q
            // Crossroad Church Music:  UC5w5QDnJIpeJR_KnLZSEg1Q
            // Crossroad Kids' Club:  UCmMySSzKknjgAVVCOPXu_qg
            try
            {
                var azuredb = new AzureDB();

                var channels = await azuredb.GetChannelIds();

                string bob = JsonConvert.SerializeObject(channels, Formatting.Indented);
                res += bob + "\n\n";

                foreach (var channel in channels)
                {
                    var youTube = new YouTube(channel);

                    //get some metrics for the channel
                    var channelMetrics = await youTube.GetMetricsForChannel(channel.YoutubeChannelID, channel.PlatformChannelID);
                    res += azuredb.InsertChannelMetrics(channelMetrics);
                    string formattedJson = JsonConvert.SerializeObject(channelMetrics, Formatting.Indented);
                    res += formattedJson + "\n\n";

                    //get metrics for the videos
                    var videoList = await youTube.SearchForVideosAsync2(channel.PlatformChannelID);
                    foreach (var video in videoList)
                    {
                        var metrics = await youTube.GetMetricsForVideo_Async(video, channel.YoutubeChannelID, channel.PlatformChannelID);
                        res += azuredb.InsertVideoMetrics(metrics);

                        //res += "title: " + metrics.Title + "\n";
                    }
                }
            }
            catch(Exception ex)
            {
                res += "Exception: " + ex.Message;
            }
            return new OkObjectResult(res);
        }


        [FunctionName("WriteToQueue")]
        [return: Queue("myqueue")]
        public static async Task<IActionResult> Run2(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log, ExecutionContext context)
        {
            string res = "returnstring";
            try
            {
                // https://docs.microsoft.com/en-us/azure/storage/queues/storage-tutorial-queues?tabs=dotnet
                var connectionString = "DefaultEndpointsProtocol=https;AccountName=storageaccountinsigb34c;AccountKey=3Qyt6BFfSEl9AdNczDlfGkA5FWdjQXbY1pLwLVKYSVoVDkM9ZqpaDOHE8ZXev1WHA8M4wdrgXoq6NcyyZTnwtA==;EndpointSuffix=core.windows.net";
                QueueClient queue = new QueueClient(connectionString, "youtubevideoanalytics");

                if (null != await queue.CreateIfNotExistsAsync())
                {
                    Console.WriteLine("The queue was created.");
                }

                await queue.SendMessageAsync("Message from my azure function");
            }
            catch(Exception ex)
            {
                res = "Error: " + ex.Message;
            }

            return new OkObjectResult(res);
        }

        
        static async Task InsertMessageAsync(QueueClient theQueue, string newMessage)
        {
            if (null != await theQueue.CreateIfNotExistsAsync())
            {
                Console.WriteLine("The queue was created.");
            }

            await theQueue.SendMessageAsync(newMessage);
        }
    }
}
