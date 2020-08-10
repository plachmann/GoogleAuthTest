using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;

using Google.Apis.Books.v1;
using Google.Apis.Books.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace GoogleAuthTest
{
    class Class1
    {

        public async Task<string> Run()
        {
            UserCredential credential;
            string message = "";


            try
            {


                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                                         new ClientSecrets { ClientId = "779584450476-84i5ga1e7j39beng6kjhv1bmoeqipk90.apps.googleusercontent.com", ClientSecret = "vUwEB7vN07UfierN2vdY3L62" },
                                         new[] { "https://www.googleapis.com/auth/yt-analytics.readonly" },
                                         "user",
                                         CancellationToken.None);
                message = credential.UserId;
            }
            catch (Exception ex)
            {
                message = ex.GetType().Name + "                 " + "bob";
                Console.WriteLine("ERROR: " + ex.GetType().Name);
            }

                return "philiscool1: " + message;
            
        }


    }
}
