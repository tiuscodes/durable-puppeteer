using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace HelloDurableFunctions
{
    public static class ConcurrentBrowserStarter
    {
        [FunctionName("ConcurrentBrowserStarter")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "browser/goto")]
            HttpRequestMessage req, 
            [OrchestrationClient]DurableOrchestrationClient starter, 
            TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // parse query parameter
            string url = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "url", true) == 0)
                .Value;

            // Get request body
            dynamic data = await req.Content.ReadAsAsync<object>();

            // Set name to query string or body data
            url = url ?? data?.url;

            if (url == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body");
            }

            log.Info($"About to start orchestration for {url}");
            var orchestrationId = await starter.StartNewAsync("O_ConcurrentBrowser", url);

            return starter.CreateCheckStatusResponse(req, orchestrationId);
        }
    }
}
