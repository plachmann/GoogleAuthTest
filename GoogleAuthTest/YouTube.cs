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
    class YouTube
    {
        private ServiceAccountCredential serviceAccountCredentials;
        private YouTubeService youtubeService;
        private YouTubeAnalyticsService youtubeAnalyticsService;
        private YouTubeReportingService youtubeReportingService;

        private string clientid = "411507659347-ub6sojqkhoqmsqq6u7n5foi6fd8gf8il.apps.googleusercontent.com";
        private string clientsecret = "NbEXppWhX9_mWrXIS96KNcDr";
        private string refreshToken = "1//04huizcwkZIqdCgYIARAAGAQSNwF-L9IrEidC80b41oYu9y9Sm1GT7KqX22sFrnSWUOGmHfvG_vHF1N6hdga2SiX5lC17Jyn63_Y";
        public YouTube()
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
                    HttpClientInitializer = GetCredentionsForAnalytics(),
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

        public async Task<string> GetAnalyticsForVideo(string videoID, string channelID)
        {
            var z = "empty";

            try
            {
                var x = youtubeAnalyticsService.Reports.Query();
                x.Metrics = "views";
                x.Ids = "channel==" + channelID;
                x.StartDate = "2020-01-01";
                x.EndDate = "2020-02-01";
                x.Dimensions = "video";
                x.Filters = "video==" + videoID;

                var y = await x.ExecuteAsync();

                z = "row=" + y.Rows.ToString();
            }
            catch(Exception ex)
            {
                z = ex.Message;
            }
             
            return z;
        }

       

        public async Task<string> TryReporting(string channelID)
        {
            var z = "empty";

            try
            {
                var x = youtubeReportingService.ReportTypes.List();
                

                x.OnBehalfOfContentOwner = channelID;

                var y = await x.ExecuteAsync();

                z = "row=" + y.ReportTypes.ToString();
            }
            catch (Exception ex)
            {
                z = ex.Message;
            }

            return z;
        }

        public void restcall()
        {
            var url = "https://api.simplymeasured.com/v1/analytics/81e45232-9dfd-4ea2-939e-444777cb1860/profiles";
            //var token = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX21ldGFkYXRhIjp7ImVtYWlsIjoiY291cnRuZXkud2VsY2hAY3Jvc3Nyb2Fkcy5uZXQiLCJmaXJzdF9uYW1lIjoiQ291cnRuZXkiLCJsYXN0X25hbWUiOiJXZWxjaCJ9LCJ1c2VyX2lkIjoiYXV0aDB8OGE4MWM3YTUtNzY4Yy00OWFmLWFhNWMtNTgwZGI2MzEyN2UxIiwicmF0ZWxpbWl0Ijp7Im1vbnRoIjoxMDAwLCJtaW51dGUiOjUwMH0sImFjY291bnRfaWQiOiI4MWU0NTIzMi05ZGZkLTRlYTItOTM5ZS00NDQ3NzdjYjE4NjAiLCJpc3MiOiJodHRwczovL3NpbXBseW1lYXN1cmVkLXByb2QuYXV0aDAuY29tLyIsInN1YiI6ImF1dGgwfDhhODFjN2E1LTc2OGMtNDlhZi1hYTVjLTU4MGRiNjMxMjdlMSIsImF1ZCI6IlB3RHNMNHJSVjd6dkdzM2hhd1FBcjc1SFpsSVNpZktOIiwiZXhwIjoxNTg3NDIxMDcwLCJpYXQiOjE1ODczODUwNzAsImF6cCI6Im1TZDNJQjNucGd6VzI1bkduMEl4eTFTZUw3VjJFS0tFIn0.CAqIhSPuBFWFFZI7vsqKUXSTpGJZMwfA8JBh2QD2Dqw";

            var token_only = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX21ldGFkYXRhIjp7ImVtYWlsIjoiY291cnRuZXkud2VsY2hAY3Jvc3Nyb2Fkcy5uZXQiLCJmaXJzdF9uYW1lIjoiQ291cnRuZXkiLCJsYXN0X25hbWUiOiJXZWxjaCJ9LCJ1c2VyX2lkIjoiYXV0aDB8OGE4MWM3YTUtNzY4Yy00OWFmLWFhNWMtNTgwZGI2MzEyN2UxIiwicmF0ZWxpbWl0Ijp7Im1vbnRoIjoxMDAwLCJtaW51dGUiOjUwMH0sImFjY291bnRfaWQiOiI4MWU0NTIzMi05ZGZkLTRlYTItOTM5ZS00NDQ3NzdjYjE4NjAiLCJpc3MiOiJodHRwczovL3NpbXBseW1lYXN1cmVkLXByb2QuYXV0aDAuY29tLyIsInN1YiI6ImF1dGgwfDhhODFjN2E1LTc2OGMtNDlhZi1hYTVjLTU4MGRiNjMxMjdlMSIsImF1ZCI6IlB3RHNMNHJSVjd6dkdzM2hhd1FBcjc1SFpsSVNpZktOIiwiZXhwIjoxNTg3NTA3MTQ1LCJpYXQiOjE1ODc0NzExNDUsImF6cCI6Im1TZDNJQjNucGd6VzI1bkduMEl4eTFTZUw3VjJFS0tFIn0.c4EgZeG029NLUlRi6veZMHTbo1sIWPeed0W-cxhhaoc";
            var token = "Bearer " + token_only;

            var client = new WebClient();
            client.Headers.Add(HttpRequestHeader.Authorization, token);

            

            var response = client.DownloadString(url);

            // parse the JSON
            JObject rss = JObject.Parse(response);


            string channel = (string)rss["data"][0]["attributes"]["fields"]["channel"];
            string profileid = (string)rss["data"][0]["attributes"]["fields"]["profile.id"];
            string profilelink = (string)rss["data"][0]["attributes"]["fields"]["profile.link"];
            string profilehandle = (string)rss["data"][0]["attributes"]["fields"]["profile.handle"];
            string profiledisplayname = (string)rss["data"][0]["attributes"]["fields"]["profile.display_name"];
            string audiencecount = (string)rss["data"][0]["attributes"]["metrics"]["profile.audience_count"];

            Console.WriteLine("channel = " + channel);
        }

        public UserCredential GetCredentionsForAnalytics()
        { 
            string[] scopes = new string[] {YouTubeAnalyticsService.Scope.YoutubeReadonly,
                                        YouTubeAnalyticsService.Scope.YtAnalyticsReadonly};

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = this.clientid,
                    ClientSecret = this.clientsecret
                },
                Scopes = scopes,
                DataStore = new FileDataStore("Store")
            });

            var token = new TokenResponse
            {
                AccessToken = getAccessToken(),
                RefreshToken = this.refreshToken
            };

            return new UserCredential(flow, Environment.UserName, token);
        }

        private string getAccessToken()
        {
            string rc = "";
            string URI = "https://accounts.google.com/o/oauth2/token";
            string myParameters = $"grant_type=refresh_token&client_id={clientid}&client_secret={clientsecret}&refresh_token={refreshToken}";
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
