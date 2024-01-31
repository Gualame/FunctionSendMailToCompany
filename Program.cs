using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

class Program
{
    static async Task Main(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        string authenticationApiUrl = configuration["Endpoint.Credentials"];
        string targetApiUrl = configuration["Endpoint.SendMail"];
        string username = configuration["Credentials.Username"];
        string password = configuration["Credentials.Password"];

        try
        {
            string authToken = await GetAuthTokenAsync(authenticationApiUrl, username, password);
            if (!string.IsNullOrEmpty(authToken))
            {
                Console.WriteLine("Authentication Successful. Token: " + authToken);
                await CallTargetApiAsync(targetApiUrl, authToken);
                Console.WriteLine("Target API call successful.");
            }
            else
            {
                Console.WriteLine("Authentication failed. Unable to get the token.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
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