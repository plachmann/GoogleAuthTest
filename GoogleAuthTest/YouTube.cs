using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

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

        public async Task<List<string>> SearchForVideosAsync(string searchText, int maxResults, string channelID)
        {
            //// Create the service
            //YouTubeService youtubeService = new YouTubeService(
            //    new BaseClientService.Initializer()
            //    {
            //        HttpClientInitializer = serviceAccountCredentials,
            //    }
            //);

            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = searchText;
            searchListRequest.MaxResults = maxResults;
            searchListRequest.ChannelId = channelID;
            searchListRequest.Type = "video";
            searchListRequest.Order = SearchResource.ListRequest.OrderEnum.Relevance;
            

            // Call the search.list method to retrieve results matching the specified query term.
            var searchListResponse = await searchListRequest.ExecuteAsync();


            List<string> videos = new List<string>();
            List<string> channels = new List<string>();
            List<string> playlists = new List<string>();

            videos.Add("page size: " + searchListResponse.PageInfo.ResultsPerPage.ToString());
            videos.Add("total results: " + searchListResponse.PageInfo.TotalResults.ToString());
            videos.Add("next page token: " + searchListResponse.NextPageToken);

            foreach (var searchResult in searchListResponse.Items)
            {
                switch (searchResult.Id.Kind)
                {
                    case "youtube#video":
                        videos.Add(String.Format("{0} ({1})", searchResult.Snippet.Title, searchResult.Id.VideoId));
                        break;

                    case "youtube#channel":
                        channels.Add(String.Format("{0} ({1})", searchResult.Snippet.Title, searchResult.Id.ChannelId));
                        break;

                    case "youtube#playlist":
                        playlists.Add(String.Format("{0} ({1})", searchResult.Snippet.Title, searchResult.Id.PlaylistId));
                        break;
                }
            }

            return videos;
        }

        public async Task<List<string>> GetAllVideosAsync(string channelID)
        {
            List<string> videos = new List<string>();
            try
            {
                //// Create the service
                //YouTubeService youtubeService = new YouTubeService(
                //    new BaseClientService.Initializer()
                //    {
                //        HttpClientInitializer = serviceAccountCredentials,
                //    }
                //);

                var channelsListRequest = youtubeService.Channels.List("contentDetails");
                channelsListRequest.Id = channelID;

                // Retrieve the contentDetails part of the channel resource for the authenticated user's channel.
                var channelsListResponse = await channelsListRequest.ExecuteAsync();

                foreach (var channel in channelsListResponse.Items)
                {
                    // From the API response, extract the playlist ID that identifies the list
                    // of videos uploaded to the authenticated user's channel.
                    var uploadsListId = channel.ContentDetails.RelatedPlaylists.Uploads;

                    videos.Add("Videos in list " + uploadsListId);

                    var nextPageToken = "";
                    while (nextPageToken != null)
                    {
                        var playlistItemsListRequest = youtubeService.PlaylistItems.List("snippet");
                        playlistItemsListRequest.PlaylistId = uploadsListId;
                        playlistItemsListRequest.MaxResults = 250;
                        playlistItemsListRequest.PageToken = nextPageToken;

                        // Retrieve the list of videos uploaded to the authenticated user's channel.
                        var playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync();

                        foreach (var playlistItem in playlistItemsListResponse.Items)
                        {
                            // Print information about each video.
                            videos.Add(playlistItem.Snippet.Title + " " + playlistItem.Snippet.ResourceId.VideoId + " " + playlistItem.Snippet.PublishedAt.ToString());
                        }

                        nextPageToken = playlistItemsListResponse.NextPageToken;
                    }
                }

                return videos;
            }
            catch(Exception ex)
            {
               
                videos.Add("Exception: " + ex.Message);
            }
            return videos;
        }

        public async Task<List<string>> SearchForVideosAsync2(string channelID)
        {
            List<string> videos = new List<string>();
            var nextPageToken = "";

            try { 
            //while (nextPageToken != null)
            //{
                var searchListRequest = youtubeService.Search.List("snippet");
                searchListRequest.MaxResults = 50;
                searchListRequest.ChannelId = channelID;
                searchListRequest.Type = "video";
                searchListRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
                searchListRequest.PageToken = nextPageToken;

                // Call the search.list method to retrieve results matching the specified query term.
                var searchListResponse = await searchListRequest.ExecuteAsync();

                foreach (var searchlistItem in searchListResponse.Items)
                {
                    // Print information about each video.
                    videos.Add(searchlistItem.Snippet.Title + " " + searchlistItem.Id.VideoId + " " + searchlistItem.Snippet.PublishedAt.ToString());
                }

                nextPageToken = searchListResponse.NextPageToken;
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
            var videoMetricsRequest = youtubeService.Videos.List("snippet,statistics,contentDetails");
            videoMetricsRequest.Id = videoID;

            var videoMetricsResponse = await videoMetricsRequest.ExecuteAsync();

            var item = videoMetricsResponse.Items.First();
            DateTime publishedAt;
            DateTime now = DateTime.Now;
            var youTubeVideoMetricsRecord = new YouTubeVideoMetricsRecord();
            youTubeVideoMetricsRecord.YouTubeChannelId = youtubeChannelID;
            youTubeVideoMetricsRecord.PostContentId = videoID;
            youTubeVideoMetricsRecord.Title =item.Snippet.Title;
            youTubeVideoMetricsRecord.VideoUrl = "Not Available";
            youTubeVideoMetricsRecord.AuthorDisplayName = item.Snippet.ChannelTitle;
            youTubeVideoMetricsRecord.VideoDurationSeconds = ConvertToSeconds(item.ContentDetails.Duration);
            youTubeVideoMetricsRecord.Views = item.Statistics.ViewCount != null ? (ulong)item.Statistics.ViewCount : 0;
            youTubeVideoMetricsRecord.Likes = item.Statistics.LikeCount != null ? (ulong)item.Statistics.LikeCount : 0;
            youTubeVideoMetricsRecord.Dislikes = item.Statistics.DislikeCount != null ? (ulong)item.Statistics.DislikeCount : 0;
            youTubeVideoMetricsRecord.Replies = item.Statistics.CommentCount != null ? (ulong)item.Statistics.CommentCount : 0;
            youTubeVideoMetricsRecord.Shares = 0; //not available
            youTubeVideoMetricsRecord.EngagementTotal = 0; //not available
            youTubeVideoMetricsRecord.PostDateTime = DateTime.TryParse(item.Snippet.PublishedAt, out publishedAt) ? publishedAt: new DateTime(1999,1,1);
            youTubeVideoMetricsRecord.CreateDateTime = now;
            youTubeVideoMetricsRecord.UpdateDateTime = now;
            return youTubeVideoMetricsRecord;
        }

        private int ConvertToSeconds(string ptString)
        {
            return 1;
        }
    }
}
