using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Google.Apis.YouTubeAnalytics.v2;
using Google.Apis.YouTubeReporting.v1;
using GoogleAuthTest.Models;
using System.Linq;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using System.Net;
using Newtonsoft.Json.Linq;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Util.Store;

namespace GoogleAuthTest
{
    // service account authentication works for the youtubedata api 
    // youtube analytics api will not work with a service account. owner of the channel must authenticate.
    // These 3 stackoverflow posts led me to a way to authenticate for the youtube analytics api
    //       https://stackoverflow.com/questions/41016537/youtube-apis-access-mutiple-youtube-channels-brand-accounts-using-google-adm
    //       https://stackoverflow.com/questions/38981720/get-refresh-token-in-google-oauth2-0-net
    //       https://stackoverflow.com/questions/38390197/how-to-create-a-instance-of-usercredential-if-i-already-have-the-value-of-access

    class YouTube
    {
        private ServiceAccountCredential serviceAccountCredentials;
        private YouTubeService youtubeService;
        private YouTubeAnalyticsService youtubeAnalyticsService;
        private YouTubeReportingService youtubeReportingService;

        public YouTube(YouTubeChannelInfo channelInfo)
        {
            DoAuthentication();
            youtubeService = new YouTubeService(
               new BaseClientService.Initializer()
               {
                   HttpClientInitializer = serviceAccountCredentials,
               }
           );

            youtubeAnalyticsService = new YouTubeAnalyticsService(
                new BaseClientService.Initializer()
                {
                    HttpClientInitializer = GetCredentialsForAnalytics(channelInfo),
                }
            );

            youtubeReportingService = new YouTubeReportingService(
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
                Scopes = new[] { YouTubeService.Scope.YoutubeReadonly, 
                                 YouTubeAnalyticsService.Scope.YoutubeReadonly, 
                                 YouTubeAnalyticsService.Scope.YtAnalyticsReadonly,
                                 YouTubeReportingService.Scope.YtAnalyticsReadonly}
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
                    videos.Add(searchlistItem.Id.VideoId );
                }
            }
            catch (Exception ex)
            {
                videos.Add("Exception: " + ex.Message);
            }
            return videos;
        }

        public async Task<YouTubeChannelMetricsRecord> GetMetricsForChannel(int youtubeChannelID, string platformChannelID)
        {
            var youTubeChannelMetricsRecord = new YouTubeChannelMetricsRecord
            {
                YouTubeChannelID = youtubeChannelID,
                PlatformChannelID = platformChannelID
            };

            try
            {
                var channelMetricsRequest = youtubeService.Channels.List("statistics");
                channelMetricsRequest.Id = platformChannelID;

                var channelMetricsResponse = await channelMetricsRequest.ExecuteAsync();
                var item = channelMetricsResponse.Items.First();

                youTubeChannelMetricsRecord.SubscriberCount = item.Statistics.SubscriberCount != null ? Convert.ToInt32(item.Statistics.SubscriberCount.ToString()) : 0;
                youTubeChannelMetricsRecord.VideoCount = item.Statistics.VideoCount != null ? Convert.ToInt32(item.Statistics.VideoCount.ToString()) : 0;
                youTubeChannelMetricsRecord.ViewCount = item.Statistics.ViewCount != null ? Convert.ToInt32(item.Statistics.ViewCount.ToString()) : 0;
            }
            catch(Exception ex)
            {
                throw ex;
            }
            return youTubeChannelMetricsRecord;
        }

        public async Task<YouTubeVideoMetricsRecord> GetMetricsForVideo(string videoID, int youtubeChannelID,string platformChannelID)
        {
            var youTubeVideoMetricsRecord = new YouTubeVideoMetricsRecord
            {
                PostContentId = videoID
            };

            try
            {
                var videoMetricsRequest = youtubeService.Videos.List("snippet,statistics,contentDetails");
                videoMetricsRequest.Id = videoID;

                var videoMetricsResponse = await videoMetricsRequest.ExecuteAsync();

                var item = videoMetricsResponse.Items.First();
                DateTime publishedAt;
                DateTime now = DateTime.Now;

                youTubeVideoMetricsRecord.PlatformChannelID = platformChannelID;
                youTubeVideoMetricsRecord.YouTubeChannelId = youtubeChannelID;
                youTubeVideoMetricsRecord.PostContentId = videoID;
                youTubeVideoMetricsRecord.Title = item.Snippet.Title;
                youTubeVideoMetricsRecord.AuthorDisplayName = item.Snippet.ChannelTitle;
                youTubeVideoMetricsRecord.VideoDurationSeconds = ConvertToSeconds(item.ContentDetails.Duration);
                youTubeVideoMetricsRecord.Views = item.Statistics.ViewCount != null ? Convert.ToInt32(item.Statistics.ViewCount.ToString()) : 0;
                youTubeVideoMetricsRecord.Likes = item.Statistics.LikeCount != null ? Convert.ToInt32(item.Statistics.LikeCount.ToString()) : 0;
                youTubeVideoMetricsRecord.Dislikes = item.Statistics.DislikeCount != null ? Convert.ToInt32(item.Statistics.DislikeCount.ToString()) : 0;
                youTubeVideoMetricsRecord.Comments = item.Statistics.CommentCount != null ? Convert.ToInt32(item.Statistics.CommentCount.ToString()) : 0;
                youTubeVideoMetricsRecord.PostDateTime = DateTime.TryParse(item.Snippet.PublishedAt, out publishedAt) ? publishedAt : new DateTime(1999, 1, 1);
                youTubeVideoMetricsRecord.CreateDateTime = now;
                youTubeVideoMetricsRecord.UpdateDateTime = now;

                //get the last few metrics from the analytics api.
                youTubeVideoMetricsRecord = await GetAnalyticsForVideo(youTubeVideoMetricsRecord);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            
            return youTubeVideoMetricsRecord;
        }

        private async Task<YouTubeVideoMetricsRecord> GetAnalyticsForVideo(YouTubeVideoMetricsRecord metricsRecord)
        {
            try
            {
                var query = youtubeAnalyticsService.Reports.Query();
                query.Metrics = "shares,estimatedMinutesWatched,averageViewDuration,averageViewPercentage";
                query.Ids = "channel==" + metricsRecord.PlatformChannelID;
                query.StartDate = "1980-01-01";
                query.EndDate = "2099-01-01";
                query.Dimensions = "video";
                query.Filters = "video==" + metricsRecord.PostContentId;

                var queryResponse = await query.ExecuteAsync();
              
                if (queryResponse.Rows != null)
                {
                    if(queryResponse.Rows.Count > 0)
                    {
                        var rowRecord = queryResponse.Rows.First();
                        // element 0 is the video id, skip it
                        metricsRecord.Shares = Convert.ToInt32(rowRecord.ElementAt(1).ToString());
                        metricsRecord.EstimatedMinutesWatched = Convert.ToInt32(rowRecord.ElementAt(2).ToString());
                        metricsRecord.AverageViewDurationSeconds = Convert.ToInt32(rowRecord.ElementAt(3).ToString());
                        metricsRecord.AverageViewPercentage = rowRecord.ElementAt(4).ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return metricsRecord;
        }

        public UserCredential GetCredentialsForAnalytics(YouTubeChannelInfo channelInfo)
        { 
            string[] scopes = new string[] {YouTubeAnalyticsService.Scope.YoutubeReadonly,
                                        YouTubeAnalyticsService.Scope.YtAnalyticsReadonly};

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = channelInfo.ClientID,
                    ClientSecret = channelInfo.ClientSecret
                },
                Scopes = scopes,
                DataStore = new FileDataStore("Store")
            });

            var token = new TokenResponse
            {
                AccessToken = getAccessToken(channelInfo),
                RefreshToken = channelInfo.RefreshToken
            };

            return new UserCredential(flow, Environment.UserName, token);
        }

        private string getAccessToken(YouTubeChannelInfo channelInfo)
        {
            string rc = "";
            string URI = "https://accounts.google.com/o/oauth2/token";
            string myParameters = $"grant_type=refresh_token&client_id={channelInfo.ClientID}&client_secret={channelInfo.ClientSecret}&refresh_token={channelInfo.RefreshToken}";
            try
            {
                using (WebClient wc = new WebClient())
                {
                    wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    string response = wc.UploadString(URI, myParameters);
                    JObject rss = JObject.Parse(response);
                    rc =  "" + rss["access_token"];
                }
            }
            catch(Exception ex)
            {
                rc = ex.Message;
            }
            return rc;
        }


        private int ConvertToSeconds(string ptString)
        {
            TimeSpan ts = System.Xml.XmlConvert.ToTimeSpan(ptString);
            return Convert.ToInt32(ts.TotalSeconds);
        }
    }
}
