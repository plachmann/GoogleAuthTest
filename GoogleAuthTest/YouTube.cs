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

namespace GoogleAuthTest
{
    class YouTube
    {
        private ServiceAccountCredential serviceAccountCredentials;
        public YouTube()
        {
            DoAuthentication();
        }

        private void DoAuthentication()
        {
            var keyVault = new KeyVault();
            var jsonFromKeyVault = keyVault.GetSecret("SERVICEACCOUNTJSONYOUTUBEONLY");

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
            // Create the service
            YouTubeService youtubeService = new YouTubeService(
                new BaseClientService.Initializer()
                {
                    HttpClientInitializer = serviceAccountCredentials,
                }
            );

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
                // Create the service
                YouTubeService youtubeService = new YouTubeService(
                    new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = serviceAccountCredentials,
                    }
                );

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
    }
}
