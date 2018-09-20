namespace DurablePuppeteer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Host;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;
    using PuppeteerSharp;
    using OpenScraping.Config;
    using System.Threading;

    public static class WorkerOrchestrators
    {
        [FunctionName("NewWorker")]
        public static async Task<object> NewWorker(
                [OrchestrationTrigger] DurableOrchestrationContext ctx,
                TraceWriter log
            )
        {
            int retryattempts = 1;
            var query = ctx.GetInput<string>();

            var retryOptions = new RetryOptions(
                firstRetryInterval: TimeSpan.FromSeconds(5),
                maxNumberOfAttempts: retryattempts);

            var getcookietask = ctx.CallActivityWithRetryAsync<string>("Authenticate", retryOptions, "credentials");
            getcookietask.ContinueWith(t =>
             {
                 return "Failed";
             }, TaskContinuationOptions.OnlyOnFaulted);
            await getcookietask;
            var cookiesjsonb64 = getcookietask.Result;
            var cookies = CookieConverter.DecodeCookie(cookiesjsonb64);
            if (!ctx.IsReplaying)
            {
                log.Warning($"Successfully retrieved {cookies.Length} cookies.");
            }

            var querygurutask = ctx.CallActivityWithRetryAsync<int>("QueryGuru", retryOptions, new Tuple<string,string>(query, cookiesjsonb64) );
            querygurutask.ContinueWith(t =>
            {
                return "Failed";
            }, TaskContinuationOptions.OnlyOnFaulted);
            var pages = await querygurutask;
            if (!ctx.IsReplaying)
            {
                log.Warning($"Query successfully returned {pages}.");
            }

            var workload = await ctx.CallActivityAsync<List<int>>("CreateWorkload", pages);

            //log.Warning(ctx.InstanceId);
            var worker = ctx.CallSubOrchestratorAsync("WorkerWork", new WorkerWorkArgs { query= query, cookiestext= cookiesjsonb64, workload=workload,total=workload.Count });

            string result = await ctx.WaitForExternalEvent<string>("Finished");
            return result;
        }

        [FunctionName("WorkerWork")]
        public static async Task<string> WorkerWork(
                [OrchestrationTrigger] DurableOrchestrationContext ctx,
                TraceWriter log
            )
        {
            //log.Warning($"[ worker ] {ctx.ParentInstanceId}");
            var args = ctx.GetInput<WorkerWorkArgs>();
            List<int> workload = args.workload;
            var batch = workload.Take(2).ToList(); // Number of simultaneous workers
            var cookiestext = args.cookiestext;
            var query = args.query;
            var total = args.total;

            var retryOptions = new RetryOptions(
            firstRetryInterval: TimeSpan.FromSeconds(5),
            maxNumberOfAttempts: 1);

            Func<Task<WorkerResult>, Task<string>> onFail = async (t) => {
                await ctx.CallActivityAsync("RaiseFinishedEvent", new WorkerWorkResult { instanceid = ctx.ParentInstanceId, result = "Error" });
                return string.Empty;
            };
            var config = await ctx.CallActivityAsync<ConfigSection>("RetrieveScrapingConfig", null);
            var configstring = JsonConvert.SerializeObject(config, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

            List<Task<WorkerResult>> workers = new List<Task<WorkerResult>>();
            var max = batch.Count();
            for (int i = 0; i < max; i++)
            {
                var page = batch[i];
                var worker = ctx.CallActivityAsync<WorkerResult>("RetrievePageContent", new Tuple<string, string, int, string>(query, cookiestext, page, configstring));
                worker.ContinueWith(onFail, TaskContinuationOptions.OnlyOnFaulted);

                await worker;
                workers.Add(worker);
            }

            TimeSpan timeout = TimeSpan.FromSeconds(60);
            DateTime deadline = ctx.CurrentUtcDateTime.Add(timeout);

            using (var cts = new CancellationTokenSource())
            {
                var longtask = Task.WhenAll(workers);
                var timeouttask = ctx.CreateTimer(deadline, cts.Token);
                Task winner = await Task.WhenAny(longtask, timeouttask);
                if (winner == longtask)
                {
                    // success case
                    cts.Cancel();
                    var results = longtask.Result;
                    foreach (var wr in results)
                    {
                        var index = workload.FindIndex(z => z == wr.page);
                        workload.RemoveAt(index);
                        if (!ctx.IsReplaying)
                        {
                            log.Warning($"Completed page {wr.page}");
                        }
                    }
                }
                else
                {
                    // timeout case
                    await ctx.CallActivityAsync("RaiseFinishedEvent", new WorkerWorkResult { instanceid = ctx.ParentInstanceId, result = "Task took too long to complete" });
                    return string.Empty;
                }
            }



            if (workload.Count > 0)
            {
                ctx.ContinueAsNew(new WorkerWorkArgs {workload =workload,cookiestext=cookiestext,query=query,total=total });
            }

            // Call output
            await ctx.CallActivityAsync("RaiseFinishedEvent", new WorkerWorkResult { instanceid = ctx.ParentInstanceId, result = $"Completed {total} pages" });
            return string.Empty;
        }

        // [FunctionName("O_WorkerManager")]
        // public static async Task<object> WorkerManager(
        //    [OrchestrationTrigger]DurableOrchestrationContext ctx,
        //    TraceWriter log
        //    )
        // {
        //    //create UIAction based on input
        //    var url = ctx.GetInput<string>();

        // try
        //    {
        //        var action = new UIAction.HelloAction(url);
        //        //connect to browser
        //        var browser = await ctx.CallActivityAsync<Browser>("A_InitBrowser", Constants.BrowserWSEndpoint);
        //        var json = JsonConvert.SerializeObject(browser, Formatting.None, new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.All });
        //        //set page
        //        var page = await ctx.CallActivityAsync<Page>("A_InitBrowserPage", json);

        // //init arguments
        //        var args = new Model.WorkerArgs() { Action = action, Page = page };
        //        json = JsonConvert.SerializeObject(args, Formatting.None, new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.All });
        //        //run
        //        var result = await ctx.CallActivityAsync<Model.WorkerResult>("A_TestSingle", json);
        //        return result;
        //    }
        //    catch(Exception ex)
        //    {
        //        return ex;
        //    }
            
        // //return result
            
        // }
    }

    public class WorkerWorkArgs
    {
        public string query { get; set; }
        public List<int> workload { get; set; }
        public string cookiestext { get; set; }
        public int total { get; set; }
    }

    public class WorkerWorkResult
    {
        public string instanceid { get; set; }
        public string result { get; set; }
        public int page { get; set; }
    }

    public class WorkerResult
    {
        public string html { get; set; }
        public int page { get; set; }
    }

    public static class CookieConverter
    {
        public static string EncodeCookie(CookieParam[] cookies)
        {
            var json = JsonConvert.SerializeObject(cookies);
            var text = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
            return text;
        }

        public static CookieParam[] DecodeCookie(string cookies)
        {
            var text = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cookies));
            var result = JsonConvert.DeserializeObject<CookieParam[]>(text);
            return result;
        }
    }

    public static class Constants
    {
        // ws://localhost:9222/devtools/browser/dd442ad5-bb5d-4bd3-a1b3-50b522dc1b4b
        // ws://chrome.browserless.io?token=a02a8b2a-fe75-4ca5-b880-08d6f19fc050
        // ws://6a7ba752.ngrok.io/devtools/browser/8c89bc93-d87c-4eed-bdfd-498b35059d27
        public static string BrowserWSEndpoint = "ws://6a7ba752.ngrok.io/devtools/browser/8c89bc93-d87c-4eed-bdfd-498b35059d27";
    }
}
