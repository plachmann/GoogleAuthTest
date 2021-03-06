﻿using System;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace GoogleAuthTest
{

    // good key vault info in this vid
    // https://www.youtube.com/watch?v=a18iG50xKd8&t=488s

    class KeyVault
    {
        public string GetSecret(string secretName)
        {
            string keyVaultName = Environment.GetEnvironmentVariable("KEY_VAULT_NAME");
            var kvUri = "https://" + keyVaultName + ".vault.azure.net";

            var client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());
            KeyVaultSecret secret = client.GetSecret(secretName);
            return secret.Value;
        }
    }
}
