using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Company.Function
{
    public class TimerTriggerSendMail
    {
        [FunctionName("TimerTriggerSendMail")]
        public void Run([TimerTrigger("0 0 18 * * 1-5")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function starting at: {DateTime.Now}");

            Process(log).Wait();

            log.LogInformation($"C# Timer trigger function finished at: {DateTime.Now}");
        }

        static async Task<string> GetAuthTokenAsync(string apiUrl, string username, string password)
        {
            using (HttpClient client = new HttpClient())
            {
                var credentials = new { Username = username, Password = password };
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(credentials);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using (HttpResponseMessage response = await client.PostAsync(apiUrl, content))
                {
                    response.EnsureSuccessStatusCode();
                    string responseContent = await response.Content.ReadAsStringAsync();
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<TokenResponse>(responseContent)?.Token;
                }
            }
        }

        static async Task Process(ILogger log)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .Build();

            string authenticationApiUrl = Environment.GetEnvironmentVariable("Endpoint.Credentials"); 
            string targetApiUrl = Environment.GetEnvironmentVariable("Endpoint.SendMail");
            string username = Environment.GetEnvironmentVariable("Credentials.Username");
            string password = Environment.GetEnvironmentVariable("Credentials.Password");

            try
            {
                log.LogInformation($"URI Authentication: {authenticationApiUrl}");
                log.LogInformation($"URI Api: {targetApiUrl}");

                log.LogInformation($"Calling credentials method: {DateTime.Now}");

                string authToken = await GetAuthTokenAsync(authenticationApiUrl, username, password);
                if (!string.IsNullOrEmpty(authToken))
                {
                    log.LogInformation($"Authentication Successful. Token: " + authToken);
                    
                    await CallTargetApiAsync(targetApiUrl, authToken);

                    log.LogInformation($"Target API call successful.");
                }
                else
                {
                    log.LogInformation($"Authentication failed. Unable to get the token.");
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Error: " + ex.Message);
                throw new Exception(ex.Message);
            }
        }

        static async Task CallTargetApiAsync(string apiUrl, string authToken)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

                using (HttpResponseMessage response = await client.GetAsync(apiUrl))
                {
                    response.EnsureSuccessStatusCode();
                }
            }
        }

        // Assuming a class like this to represent the token response
        private class TokenResponse
        {
            public string Token { get; set; }
        }
    }
}
