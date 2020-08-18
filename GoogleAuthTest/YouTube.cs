﻿using System;
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

        public async Task<List<string>> GetVideosAsync()
        {
            // Create the service
            YouTubeService youtubeService = new YouTubeService(
                new BaseClientService.Initializer()
                {
                    HttpClientInitializer = serviceAccountCredentials,
                }
            );

            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = "child"; // Replace with your search term.
            searchListRequest.MaxResults = 50;

            // Call the search.list method to retrieve results matching the specified query term.
            var searchListResponse = await searchListRequest.ExecuteAsync();

            List<string> videos = new List<string>();
            List<string> channels = new List<string>();
            List<string> playlists = new List<string>();

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
    }
}