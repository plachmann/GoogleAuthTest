using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using System.Security.Cryptography.X509Certificates;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Analytics.v3;
using Google.Apis.Analytics.v3.Data;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.Collections.Generic;
using Google.Apis.Http;

namespace GoogleAuthTest
{
    //public static class Function1
    //{
    //    [FunctionName("Function1")]
    //    public static async Task<IActionResult> Run(
    //        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
    //        ILogger log)
    //    {
    //        log.LogInformation("C# HTTP trigger function processed a request.");

    //        string name = req.Query["name"];

    //        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    //        dynamic data = JsonConvert.DeserializeObject(requestBody);
    //        name = name ?? data?.name;

    //        string res = "nope";

    //        try
    //        {
    //            var c1 = new Class1();
    //            res = await c1.Run();
                
    //        }
    //        catch (AggregateException ex)
    //        {
    //            foreach (var e in ex.InnerExceptions)
    //            {
    //                Console.WriteLine("ERROR: " + e.Message);
    //            }
    //        }

    //        string responseMessage = string.IsNullOrEmpty(name)
    //            ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response. res=" + res
    //            : $"Hello, {name}. This HTTP triggered function executed successfully.";

    //        return new OkObjectResult(responseMessage);
    //    }

    //}

    //public static class Function2
    //{
    //    [FunctionName("Function2ServiceAccount")]
    //    public static async Task<IActionResult> Run(
    //        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
    //        ILogger log, ExecutionContext context)
    //    {
    //        log.LogInformation("C# HTTP trigger function processed a request.");

    //        string name = req.Query["name"];

    //        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    //        dynamic data = JsonConvert.DeserializeObject(requestBody);
    //        name = name ?? data?.name;

    //        String res = "";
    //        String serviceAccountEmail = "youtubeserviceaccount@alteryxyoutube.iam.gserviceaccount.com";

    //        string certfile = Path.Combine(context.FunctionAppDirectory, "keyfile.p12");
    //        var certificate = new X509Certificate2(certfile, "notasecret", X509KeyStorageFlags.Exportable);
    //        try
    //        { 
                
    //            ServiceAccountCredential credential = new ServiceAccountCredential(
    //               new ServiceAccountCredential.Initializer(serviceAccountEmail)
    //               {
    //                   Scopes = new[] { "https://www.googleapis.com/auth/yt-analytics.readonly" }
    //               }.FromCertificate(certificate));

    //            res = credential.ToString();
    //        }
    //        catch(Exception ex )
    //        {

    //            res = ex.ToString() + "certfile: " + certfile;
    //        }

    //        string responseMessage = string.IsNullOrEmpty(name)
    //            ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response. res=" + res
    //            : $"Hello, {name}. This HTTP triggered function executed successfully.";

    //        return new OkObjectResult(responseMessage);
    //    }

    //}

    public static class Function3
    {
        [FunctionName("Function3JSON")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // get environment variable
            var customSetting = Environment.GetEnvironmentVariable("PHILISCOOL", EnvironmentVariableTarget.Process);

            // get secret value
            var keyVault = new KeyVault();
            var jsonFromKeyVault = "";
            try
            {
                jsonFromKeyVault = keyVault.GetSecret("SERVICEACCOUNTJSONYOUTUBEONLY");
            }
            catch(Exception ex)
            {
                jsonFromKeyVault = ex.Message;
            }

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string res = "Env variable value: " + customSetting + "\n\n\n";
            //res += "Secret Value: " + secretValue + "\n\n\n";
           try
            {
                // Get active credential

                var credPath = System.IO.Path.Combine(context.FunctionDirectory, "..\\youtubecert.json");
                //var json = File.ReadAllText(credPath);
                var json = jsonFromKeyVault;
                Newtonsoft.Json.Linq.JObject cr = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(json); 
                string privateKey = (string)cr.GetValue("private_key");
                string clientEMail = (string)cr.GetValue("client_email");
                res += "Secret Value from File: " + privateKey + "\n\n\n";

                //string privateKey = keyVault.GetSecret("SERVICEACCOUNTPRIVATEKEY");
                //string clientEMail = keyVault.GetSecret("SERVICEACCOUNT");

                //var cr = JsonConvert.DeserializeObject<PersonalServiceAccountCred>(json); // "personal" service account credential

                // Create an explicit ServiceAccountCredential credential
                var xCred = new ServiceAccountCredential(new ServiceAccountCredential.Initializer(clientEMail)
                {
                    Scopes = new[] {
                        AnalyticsService.Scope.AnalyticsReadonly,
                        YouTubeService.Scope.YoutubeReadonly
                        }
                }.FromPrivateKey(privateKey));

                

                // Create the service
                YouTubeService youtubeService = new YouTubeService(
                    new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = xCred,
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

               res = res + String.Format("Videos:\n{0}\n", string.Join("\n", videos));
               res = res + String.Format("Channels:\n{0}\n", string.Join("\n", channels));
               res = res +String.Format("Playlists:\n{0}\n", string.Join("\n", playlists));
            }
            catch (Exception ex)
            {

                res = res + ex.ToString();
            }

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response. res=" + res
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }

    }
}
