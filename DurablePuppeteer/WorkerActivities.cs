namespace DurablePuppeteer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Host;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;
    using OpenScraping.Config;
    using PuppeteerSharp;

    public static class WorkerActivities
    {
        // A) Create login and return [cookies]
        [FunctionName("Authenticate")]
        public static async Task<string> Authenticate(
                [ActivityTrigger] DurableActivityContext ctx,
                TraceWriter log)
        {
            var credentials = ctx.GetInput<string>();
            var browser = await Puppeteer.ConnectAsync(new ConnectOptions { BrowserWSEndpoint = Constants.BrowserWSEndpoint });
            var page = await browser.NewPageAsync();
            var login = new UIAction.Login();
            CookieParam[] cookies = null;
            cookies = await login.RunAsync(page, log);

            // page is closed implicitly
            browser.Disconnect();
            if (cookies != null)
            {
                return CookieConverter.EncodeCookie(cookies);
            }

            throw new Exception("Failed to authenticate.");
        }

        // A) Accept search query and cookies return [totalpages]
        [FunctionName("QueryGuru")]
        public static async Task<int> QueryGuru(
                [ActivityTrigger] DurableActivityContext ctx,
                TraceWriter log)
        {
            var arguments = ctx.GetInput<Tuple<string, string>>();
            var searchargs = arguments.Item1.Split(':');
            if (searchargs.Length != 2)
            {
                throw new ArgumentException("Activity.QueryGuru: Expected search query to be in format <query>:<type>");
            }

            var query = searchargs[0];
            var type = searchargs[1];
            var cookies = CookieConverter.DecodeCookie(arguments.Item2);

            var browser = await Puppeteer.ConnectAsync(new ConnectOptions { BrowserWSEndpoint = Constants.BrowserWSEndpoint });
            var page = await browser.NewPageAsync();
            await page.SetCookieAsync(cookies);
            var search = new UIAction.NavigateSearch(query, type);
            var pages = await search.RunAsync(page);
            browser.Disconnect();
            return pages;
        }

        // O) Accept search query and cookies and page and configsection then return result
        [FunctionName("RetrievePageContent")]
        public static async Task<WorkerResult> RetrievePageContent(
                [ActivityTrigger] DurableActivityContext ctx,
                TraceWriter log)
        {
            var arguments = ctx.GetInput<Tuple<string, string, int, string>>();
            var searchargs = arguments.Item1.Split(':');
            if (searchargs.Length != 2)
            {
                throw new ArgumentException("Activity.QueryGuru: Expected search query to be in format <query>:<type>");
            }

            var query = searchargs[0];
            var type = searchargs[1];
            var cookies = CookieConverter.DecodeCookie(arguments.Item2);
            var pagenumber = arguments.Item3;
            var config = JsonConvert.DeserializeObject<ConfigSection>(arguments.Item4);

            var browser = await Puppeteer.ConnectAsync(new ConnectOptions { BrowserWSEndpoint = Constants.BrowserWSEndpoint });
            var page = await browser.NewPageAsync();
            await page.SetCookieAsync(cookies);
            var search = new UIAction.NavigateSearch(query, type, true);
            await search.RunAsync(page);
            var collect = new UIAction.NavigateCollect(pagenumber, ctx.InstanceId, ctx.InstanceId, config);
            var result = await collect.RunAsync(page);

            // page is closed implicitly
            browser.Disconnect();

            var customresult = new WorkerResult { html = result, page = pagenumber };
            return customresult;
        }

        // A) Accept number of pages and return a range from 1 to number of pages
        [FunctionName("CreateWorkload")]
        public static async Task<List<int>> CreateWorkload(
                [ActivityTrigger] DurableActivityContext ctx,
                TraceWriter log)
        {
            var totalpages = ctx.GetInput<int>();
            var workload = Enumerable.Range(1, totalpages).ToList();
            return await Task.FromResult(workload);
        }

        // A) Accept number of pages and return a range from 1 to number of pages
        [FunctionName("RaiseFinishedEvent")]
        public static async Task RaiseFinishedEvent(
                [ActivityTrigger] DurableActivityContext ctx,
                [OrchestrationClient] DurableOrchestrationClient client,
                TraceWriter log)
        {
            var args = ctx.GetInput<WorkerWorkResult>();
            var instanceid = args.instanceid;
            var reason = args.result;
            await client.RaiseEventAsync(instanceid, "Finished", reason);
        }

        [FunctionName("RetrieveScrapingConfig")]
        public static async Task<ConfigSection> RetrieveScrapingConfig(
                [ActivityTrigger] DurableActivityContext ctx,
                [Blob("config/propertyrecord.json", Connection = "StorageAccount")] TextReader file,
                TraceWriter log)
        {
            var configjson = await file.ReadToEndAsync();
            var config = OpenScraping.Config.StructuredDataConfig.ParseJsonString(configjson);
            return await Task.FromResult(config);
        }

    // [FunctionName("A_InitBrowser")]
    //    public static async Task<string> InitBrowser(
    //        [ActivityTrigger] DurableActivityContext ctx,
    //        TraceWriter log)
    //    {
    //        var connectionstring = ctx.GetInput<string>();
    //        var browser = await Puppeteer.ConnectAsync(new ConnectOptions { BrowserWSEndpoint = connectionstring });
    //        var json = JsonConvert.SerializeObject(browser, Formatting.None, new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.All, TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full });
    //        return json;
    //    }

    // [FunctionName("A_InitBrowserPage")]
    //    public static async Task<Page> InitBrowserPage(
    // [ActivityTrigger] DurableActivityContext ctx,
    // TraceWriter log)
    //    {
    //        var browser = ctx.GetInput<Browser>();
    //        var page = await browser.NewPageAsync();
    //        return page;
    //    }


    // [FunctionName("A_ScrapeSingle")]
    //    public static async Task<Model.WorkerResult> ScrapeSingle(
    //        [ActivityTrigger] DurableActivityContext ctx,
    //        TraceWriter log )
    //    {
    //        var workerid = "AAAA";
    //        var workerargsjson = ctx.GetInput<string>();
    //        log.Warning(workerargsjson);
    //        var workerargs = JsonConvert.DeserializeObject<Model.WorkerArgs>(workerargsjson);
    //        var asm = Assembly.Load("Microsoft.Extensions.Options, Culture=neutral, PublicKeyToken=adb9793829ddae60");
    //        PuppeteerSharp.Browser browser = null;
    //        try
    //        {
    //           // await workerargs.Action.RunAsync(workerargs.Page);
    //        }
    //        catch (Exception ex)
    //        {
    //            return new Model.WorkerResult() { WorkerId= workerid, Success=false };
    //        }
    //        finally
    //        {
    //            if (browser != null)
    //            {
    //                browser.Disconnect();
    //            }
    //        }
    //        return new Model.WorkerResult() { WorkerId= workerid, Success=true };
    //    }

    // [FunctionName("A_TestSingle")]
    //    public static async Task<string> TestSingle(
    // [ActivityTrigger] DurableActivityContext ctx,
    // TraceWriter log)
    //    {
    //        var workerid = "AAAA";
    //        var workerargsjson = ctx.GetInput<string>();
    //        log.Warning(workerargsjson);
    //        PuppeteerSharp.Browser browser = null;
    //        try
    //        {
    //            //await Task.Delay(1000);
    //            return "Yes";
    //        }
    //        catch (Exception ex)
    //        {
    //            return ex.Message;
    //        }
    //        finally
    //        {

    // }
    //        return "wtf";
    //    }

        public static bool ChanceSucceed(int percentage)
        {
            Random rand = new Random();
            int chance = rand.Next(1, 101);

            if (chance <= percentage) // probability of x
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
