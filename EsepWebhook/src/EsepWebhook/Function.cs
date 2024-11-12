using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace EsepWebhook
{
    public class Function
    {
        private static readonly HttpClient client = new HttpClient();

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            // Log the incoming request body for debugging
            context.Logger.LogLine("Received event: " + request?.Body);

            if (string.IsNullOrEmpty(request?.Body))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = 400,
                    Body = "Request body is null or empty."
                };
            }

            // Parse the incoming JSON payload
            JObject requestBody;
            try
            {
                requestBody = JObject.Parse(request.Body);
            }
            catch (Exception ex)
            {
                context.Logger.LogLine("Error parsing JSON: " + ex.Message);
                return new APIGatewayProxyResponse
                {
                    StatusCode = 400,
                    Body = "Invalid JSON format in request body."
                };
            }

            // Extract the issue URL
            var issueUrl = requestBody["issue"]?["html_url"]?.ToString();

            if (string.IsNullOrEmpty(issueUrl))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = 400,
                    Body = "Issue URL not found in the request body."
                };
            }

            // Prepare message for Slack
            var slackMessage = new
            {
                text = $"A new issue has been created: {issueUrl}"
            };
            var payload = new StringContent(
                JObject.FromObject(slackMessage).ToString(),
                Encoding.UTF8,
                "application/json"
            );

            // Get the Slack URL from the environment variable
            var slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
            if (string.IsNullOrEmpty(slackUrl))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = 500,
                    Body = "SLACK_URL environment variable not configured."
                };
            }

            // Send the message to Slack
            var response = await client.PostAsync(slackUrl, payload);

            // Log the response from Slack
            context.Logger.LogLine("Slack response status: " + response.StatusCode);
            context.Logger.LogLine("Slack response body: " + await response.Content.ReadAsStringAsync());


            return new APIGatewayProxyResponse
            {
                StatusCode = (int)response.StatusCode,
                Body = await response.Content.ReadAsStringAsync()
            };
        }
    }
}
