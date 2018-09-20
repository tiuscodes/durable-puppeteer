// using OpenScraping.Config;

namespace DurablePuppeteer
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Azure.WebJobs.Host;
    using Microsoft.WindowsAzure.Storage.Blob;

    public static class WorkerStarter
    {

        [FunctionName("WorkerStarter")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "propertyguru/search")]
            HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // parse query parameter
            string query = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "query", true) == 0)
                .Value;

            // Get request body
            dynamic data = await req.Content.ReadAsAsync<object>();

            // Set name to query string or body data
            query = query ?? data?.url;

            if (query == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body");
            }

            log.Info($"About to start orchestration for {query}");
            var orchestrationId = await starter.StartNewAsync("NewWorker", query);

            return starter.CreateCheckStatusResponse(req, orchestrationId);
        }

        // [FunctionName("Test")]
        //    public static async Task<HttpResponseMessage> Test([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "test")]
        //        HttpRequestMessage req,
        // [OrchestrationClient]DurableOrchestrationClient starter,
        // TraceWriter log)
        //    {
        //        log.Info("C# HTTP trigger function processed a request.");

        // // parse query parameter
        //        string query = req.GetQueryNameValuePairs()
        //            .FirstOrDefault(q => string.Compare(q.Key, "query", true) == 0)
        //            .Value;

        // // Get request body
        //        dynamic data = await req.Content.ReadAsAsync<object>();

        // // Set name to query string or body data
        //        query = query ?? data?.url;

        // if (query == null)
        //        {
        //            return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body");
        //        }

        // log.Info($"About to start orchestration for {query}");
        //        var orchestrationId = await starter.StartNewAsync("NewWorker", query);

        // return starter.CreateCheckStatusResponse(req, orchestrationId);
        //    }

        // [FunctionName("WritePropertyRecordConfig")]
        //    public static async Task<HttpResponseMessage> WritePropertyRecordConfig(
        //        [HttpTrigger(AuthorizationLevel.Function,"post",Route ="config/propertyrecord")] HttpRequestMessage req,
        //        [Blob("config/propertyrecord.json", Connection ="StorageAccount")] TextWriter file,
        //        TraceWriter log
        //        )
        //    {
        //        //AssemblyBindingRedirectHelper.ConfigureBindingRedirects();
        //        string data = await req.Content.ReadAsStringAsync();

        // try
        //        {
        //            await file.WriteAsync(data);
        //            return req.CreateResponse(HttpStatusCode.OK, "Config successfully written.");
        //        }
        //        catch(Exception ex)
        //        {

        // }
        //        return req.CreateResponse(HttpStatusCode.InternalServerError, "Config failed to write.");

        // }
    }
}
