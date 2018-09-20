using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
//using OpenScraping.Config;

namespace DurablePuppeteer
{
    public static class WorkerStarter
    {
        [FunctionName("WorkerStarter")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "propertyguru/search")]
            HttpRequest req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            TraceWriter log)
        {
            //AssemblyBindingRedirectHelper.ConfigureBindingRedirects();
            log.Info("C# HTTP trigger function processed a request.");

            // parse query parameter
            string query = req.Query["query"];

            // Get request body
            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            // Set name to query string or body data
            query = query ?? data?.url;

            if (query == null)
            {
                return new BadRequestObjectResult("Please pass a name on the query string or in the request body");
            }

            log.Info($"About to start orchestration for {query}");
            var orchestrationId = await starter.StartNewAsync("NewWorker", query);
            var host = req.HttpContext.Request.Host.Value;
            var path = req.HttpContext.Request.Path.Value;
            var querys = req.HttpContext.Request.QueryString.Value;
            var scheme = req.HttpContext.Request.Scheme;
            var a = new HttpRequestMessage(HttpMethod.Post, $"{scheme}://{host}{path}{querys}");
            var result = await starter.CreateCheckStatusResponse(a, orchestrationId).Content.ReadAsStringAsync();
            var obj = JsonConvert.DeserializeObject(result);
            return new ObjectResult(obj);
        }

        [FunctionName("Test")]
        public static async Task<IActionResult> Test([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "test")]
            HttpRequest req,
    [OrchestrationClient]DurableOrchestrationClient starter,
    TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // parse query parameter
            string query = req.Query["query"];

            // Get request body
            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            // Set name to query string or body data
            query = query ?? data?.url;

            if (query == null)
            {
                return new BadRequestObjectResult("Please pass a name on the query string or in the request body");
            }

            log.Info($"About to start orchestration for {query}");
            var orchestrationId = await starter.StartNewAsync("NewWorker", query);

            var a = new HttpRequestMessage(HttpMethod.Post, "yourappname/yourmethod");
            var result = starter.CreateCheckStatusResponse(a, orchestrationId);
            return new OkObjectResult(result.Content.ReadAsStringAsync());
        }

        [FunctionName("WritePropertyRecordConfig")]
        public static async Task<HttpResponseMessage> WritePropertyRecordConfig(
            [HttpTrigger(AuthorizationLevel.Function,"post",Route ="config/propertyrecord")] HttpRequestMessage req,
            [Blob("config/propertyrecord.json", Connection ="StorageAccount")] TextWriter file,
            TraceWriter log
            )
        {
            //AssemblyBindingRedirectHelper.ConfigureBindingRedirects();
            string data = await req.Content.ReadAsStringAsync();
            
            try
            {
                await file.WriteAsync(data);
                return req.CreateResponse(HttpStatusCode.OK, "Config successfully written.");
            }
            catch(Exception ex)
            {

            }
            return req.CreateResponse(HttpStatusCode.InternalServerError, "Config failed to write.");
        }
    }
}
