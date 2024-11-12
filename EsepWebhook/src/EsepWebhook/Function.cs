using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Newtonsoft.Json;

// Use the SystemTextJson serializer
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook
{
    public class Function
    {
        private static readonly HttpClient client = new HttpClient();

        public async Task<string> FunctionHandler(object input, ILambdaContext context)
        {
            context.Logger.LogLine("FunctionHandler received: " + input);

            dynamic json = JsonConvert.DeserializeObject<dynamic>(input.ToString());
            string issueUrl = json?.issue?.html_url;
            
            if (string.IsNullOrEmpty(issueUrl))
            {
                context.Logger.LogLine("Issue URL not found in the request body.");
                return "Issue URL not found in the request body.";
            }

            string payload = $"{{\"text\":\"A new issue has been created: {issueUrl}\"}}";
            var slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");

            if (string.IsNullOrEmpty(slackUrl))
            {
                context.Logger.LogLine("SLACK_URL environment variable not configured.");
                return "SLACK_URL environment variable not configured.";
            }

            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(slackUrl, content);

            context.Logger.LogLine("Slack response status: " + response.StatusCode);
            context.Logger.LogLine("Slack response body: " + await response.Content.ReadAsStringAsync());

            return await response.Content.ReadAsStringAsync();
        }
    }
}
