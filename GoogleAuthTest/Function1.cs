using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;


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
    public static class Function3
    {
        [FunctionName("Function3JSON")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            var res = "";

            // Crossroad Church(Main Channel):  UCEdRBpSpVgfuybR3lzgxa-Q
            // Crossroad Church Music:  UC5w5QDnJIpeJR_KnLZSEg1Q
            // Crossroad Kids' Club:  UCmMySSzKknjgAVVCOPXu_qg

            var youTube = new YouTube();
            var videos = await youTube.GetAllVideosAsync("UCEdRBpSpVgfuybR3lzgxa-Q");

            res += "List of Videos\n\n\n" + String.Format("Videos:\n{0}\n", string.Join("\n", videos));
            string responseMessage = res;

            return new OkObjectResult(responseMessage);
        }

    }
}
