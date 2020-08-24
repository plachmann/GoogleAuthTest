using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using GoogleAuthTest.Models;
using System.Linq;

namespace GoogleAuthTest
{
    class YouTube
    {
        private ServiceAccountCredential serviceAccountCredentials;
        private YouTubeService youtubeService;
        public YouTube()
        {
            DoAuthentication();
            youtubeService = new YouTubeService(
               new BaseClientService.Initializer()
               {
                   HttpClientInitializer = serviceAccountCredentials,
               }
           );
        }

        private void DoAuthentication()
        {
            var keyVault = new KeyVault();
            var jsonFromKeyVault = keyVault.GetSecret("SERVICEACCOUNTJSONYOUTUBEONLY2");

            Newtonsoft.Json.Linq.JObject cr = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(jsonFromKeyVault);
            string privateKey = (string)cr.GetValue("private_key");
            string clientEMail = (string)cr.GetValue("client_email");

            // Create an explicit ServiceAccountCredential credential
            serviceAccountCredentials = new ServiceAccountCredential(new ServiceAccountCredential.Initializer(clientEMail)
            {
                Scopes = new[] { YouTubeService.Scope.YoutubeReadonly }
            }.FromPrivateKey(privateKey));
        }

        public async Task<List<string>> SearchForVideosAsync2(string channelID)
        {
            List<string> videos = new List<string>();
            
            try { 
            
                var searchListRequest = youtubeService.Search.List("snippet");
                searchListRequest.MaxResults = 50;
                searchListRequest.ChannelId = channelID;
                searchListRequest.Type = "video";
                searchListRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
                
                

                // Call the search.list method to retrieve results matching the specified query term.
                var searchListResponse = await searchListRequest.ExecuteAsync();

                foreach (var searchlistItem in searchListResponse.Items)
                {
                    // Print information about each video.
                    videos.Add(searchlistItem.Id.VideoId );
                }

                
            //}
            }
            catch (Exception ex)
            {

                videos.Add("Exception: " + ex.Message);
            }

            return videos;
        }

        public async Task<YouTubeVideoMetricsRecord> GetMetricsForVideo(string videoID, int youtubeChannelID)
        {
            var youTubeVideoMetricsRecord = new YouTubeVideoMetricsRecord();
            youTubeVideoMetricsRecord.PostContentId = videoID;
            try
            {
                var videoMetricsRequest = youtubeService.Videos.List("snippet,statistics,contentDetails");
                videoMetricsRequest.Id = videoID;

                var videoMetricsResponse = await videoMetricsRequest.ExecuteAsync();

                var item = videoMetricsResponse.Items.First();
                DateTime publishedAt;
                DateTime now = DateTime.Now;

                youTubeVideoMetricsRecord.YouTubeChannelId = youtubeChannelID;
                youTubeVideoMetricsRecord.PostContentId = videoID;
                youTubeVideoMetricsRecord.Title = item.Snippet.Title;
                youTubeVideoMetricsRecord.VideoUrl = "Not Available";
                youTubeVideoMetricsRecord.AuthorDisplayName = item.Snippet.ChannelTitle;
                youTubeVideoMetricsRecord.VideoDurationSeconds = ConvertToSeconds(item.ContentDetails.Duration);
                youTubeVideoMetricsRecord.Views = item.Statistics.ViewCount != null ? Convert.ToInt32(item.Statistics.ViewCount.ToString()) : 0;
                youTubeVideoMetricsRecord.Likes = item.Statistics.LikeCount != null ? Convert.ToInt32(item.Statistics.LikeCount.ToString()) : 0;
                youTubeVideoMetricsRecord.Dislikes = item.Statistics.DislikeCount != null ? Convert.ToInt32(item.Statistics.DislikeCount.ToString()) : 0;
                youTubeVideoMetricsRecord.Replies = item.Statistics.CommentCount != null ? Convert.ToInt32(item.Statistics.CommentCount.ToString()) : 0;
                youTubeVideoMetricsRecord.Shares = 0; //not available
                youTubeVideoMetricsRecord.EngagementTotal = 0; //not available
                youTubeVideoMetricsRecord.PostDateTime = DateTime.TryParse(item.Snippet.PublishedAt, out publishedAt) ? publishedAt : new DateTime(1999, 1, 1);
                youTubeVideoMetricsRecord.CreateDateTime = now;
                youTubeVideoMetricsRecord.UpdateDateTime = now;
            }
            catch(Exception ex)
            {
                youTubeVideoMetricsRecord.Title = ex.Message;
            }
            return youTubeVideoMetricsRecord;
        }

        private int ConvertToSeconds(string ptString)
        {
            TimeSpan ts = System.Xml.XmlConvert.ToTimeSpan(ptString);
            return Convert.ToInt32(ts.TotalSeconds);
        }
    }
}
