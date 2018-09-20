// using OpenScraping;
// using OpenScraping.Config;
namespace DurablePuppeteer.UIAction
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs.Host;
    using OpenScraping;
    using OpenScraping.Config;
    using PuppeteerSharp;

    /// <summary>
    /// Will iterate through an already existing page with results and return the accumulated HTML
    /// </summary>
    public class NavigateCollect : UIActionBase<string>
    {
        private string targetpage = "https://www.property-guru.co.nz/gurux/render.php?action=main";
        private int totalpages = 0;
        private int currpage = 1;
        private int offset = 0;
        private bool LoggedIn = false;
        private Browser _conn = null;
        private bool IsConnected = false;
        private bool KeepPageOpen = false;
        private string HTML = "";
        private string sessionid = "";
        private string workerid = "";
        private bool IsOffset = false;
        private int DefaultTimeout = 3000;
        private ConfigSection config;
        private Page Page = null;

        public NavigateCollect(int pagenumber, string workerid, string sessionid, ConfigSection config)
        {
            this.config = config;
            this.offset = pagenumber;
            this.workerid = workerid;
            this.sessionid = sessionid;
            this.IsOffset = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigateCollect"/> class.
        /// </summary>
        /// <param name="pagenumber"></param>
        /// <param name="workerid"></param>
        /// <param name="sessionid"></param>
        /// <param name="keeppageopen"></param>
        public NavigateCollect(int pagenumber, string workerid, string sessionid, ConfigSection config, bool keeppageopen = false)
        {
            this.config = config;
            this.offset = pagenumber;
            this.workerid = workerid;
            this.sessionid = sessionid;
            this.IsOffset = true;
            this.KeepPageOpen = keeppageopen;
        }

        private async Task SetConnectionAsync(Browser conn)
        {
            if (!conn.IsClosed)
            {
                Console.WriteLine($"Connection opened worker: {this.offset} - {this.workerid}");
                var page = await conn.NewPageAsync();
                this.Page = page;
                this.IsConnected = true;
            }
        }

        private void SetPage(Page page)
        {
            this.Page = page;
            this.Page.SetRequestInterceptionAsync(true);
            this.Page.Request += this.DenyRequests;
        }

        public override async Task<string> RunAsync(Page page)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Console.WriteLine($"[ {DateTime.Now.ToShortTimeString()} ] Started Collector");
            this.SetPage(page);
            await this.InitialiseAsync();
            await this.ProcessAsync();
            var HTML = await this.Commit();
            sw.Stop();
            Console.WriteLine($"[ {DateTime.Now.ToShortTimeString()} ] Completed Collector: @ {sw.ElapsedMilliseconds}ms");
            return HTML;
        }

        public async Task<string> RunAsync(Page page, TraceWriter log)
        {
            this.log = log;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Console.WriteLine($"[ {DateTime.Now.ToShortTimeString()} ] Started Collector");
            this.SetPage(page);
            await this.InitialiseAsync();
            await this.ProcessAsync();
            var HTML = await this.Commit();
            sw.Stop();
            Console.WriteLine($"[ {DateTime.Now.ToShortTimeString()} ] Completed Collector: @ {sw.ElapsedMilliseconds}ms");
            return HTML;
        }

        public async Task<string> Run(Browser conn)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Console.WriteLine($"[ {DateTime.Now.ToShortTimeString()} ] Started Property Record Fetcher");
            await this.SetConnectionAsync(conn);
            await this.InitialiseAsync();
            await this.ProcessAsync();
            var HTML = await this.Commit();
            sw.Stop();
            Console.WriteLine($"[ {DateTime.Now.ToShortTimeString()} ] Completed Property Record Fetcher: @ {sw.ElapsedMilliseconds}ms");
            return HTML;
        }

        private async Task<string> Commit()
        {
            var result = this.HTML;
            if (!this.KeepPageOpen)
            {
                await this.Page.CloseAsync();
            }

            if (this.IsConnected)
            {
                try
                {
                    this._conn.Disconnect();
                    Console.WriteLine($"Connection closed.");
                }
                catch { }
            }

            return result;
        }

        private async Task<int> GetCurrentPage()
        {
            var elcurrentpagelink = await this.Page.XPathAsync("//*[@id='TableTabContent']/div[3]/div[2]/a[@class='activePage']");
            if (elcurrentpagelink.Length > 0)
            {
                var href = await this.Page.EvaluateFunctionAsync<string>("el => el.href ", elcurrentpagelink.First());
                var start = href.IndexOf("(") + 1;
                var end = href.IndexOf(",");
                var currpage = href.Substring(start, end - start);
                return int.Parse(currpage);
            }

            return -1;
        }

        private async Task<int> GetTotalPages()
        {
            var ellastpagelink = await this.Page.QuerySelectorAsync("a[title='Last page']");
            if (ellastpagelink != null)
            {
                var href = await this.Page.EvaluateFunctionAsync<string>("el => el.href ", ellastpagelink);
                var start = href.IndexOf("(") + 1;
                var end = href.IndexOf(",");
                var totalpages = href.Substring(start, end - start);
                var result = int.Parse(totalpages);
                return result;
            }

            return -1;
        }

        private async Task GoToPage(int x)
        {
            await this.Page.EvaluateExpressionAsync($"updateTableAfterSelectingPage(\"{x}\",false)");
            await this.Page.EvaluateExpressionAsync($"updateWhat()");
            var loader1 = Retry<ElementHandle>(
                () =>
            {
                var loader = this.Page.QuerySelectorAsync("div#contentLoading").Result;
                var result = this.Page.WaitForFunctionAsync($"el => el.outerHTML.includes('none')", new WaitForFunctionOptions { Polling = WaitForFunctionPollingOption.Mutation, Timeout = this.DefaultTimeout }, loader).Result;
                return loader;
            }, 2, $"[Page] Wait till load complete");
            var loader2 = Retry<ElementHandle>(
                () =>
            {
                var loader = this.Page.QuerySelectorAsync("div#loading").Result;
                var result = this.Page.WaitForFunctionAsync($"el => el.outerHTML.includes('none')", new WaitForFunctionOptions { Polling = WaitForFunctionPollingOption.Mutation, Timeout = this.DefaultTimeout }, loader).Result;
                return loader;
            }, 2, $"[Page] Wait till load complete");

            await Task.WhenAll(loader1, loader2);
            await this.UnblockOwners();
        }

        private async Task UnblockOwners()
        {
            bool success = false;
            await this.Page.WaitForTimeoutAsync(this.DefaultTimeout);
            var items = await this.Page.XPathAsync("//td[@class='uncopyable']");
            if (items != null)
            {
                await this.Page.EvaluateExpressionAsync("var elements = document.getElementsByClassName('uncopyable');for (var i = 0; i < elements.length; i++) { elements[i].innerHTML = elements[i].getAttribute('uncopyable-content');}");
                await this.Page.WaitForTimeoutAsync(this.DefaultTimeout);
                var item1 = items[0];
                var innerhtml = await this.Page.EvaluateFunctionAsync<string>("el => el.innerHTML", item1);
                if (!innerhtml.Equals(string.Empty))
                {
                    success = true;
                }
            }

            if (!success)
            {
                throw new Exception("NavigateCollect: Failed to unblock owner text");
            }
        }

        private async Task ProcessAsync()
        {
            await this.Page.WaitForTimeoutAsync(this.DefaultTimeout);
            string HTML = await this.Page.GetContentAsync();
            try
            {
                var openScraping = new StructuredDataExtractor(this.config);
                var scrapingResults = openScraping.Extract(html: HTML);
                var records = scrapingResults.ToObject<Model.RootObject>();
                if (records.PropertyRecords.Count > 0)
                {
                    var first = records.PropertyRecords.First().Owner;
                    var last = records.PropertyRecords.Last().Owner;
                    if (string.IsNullOrEmpty(first) && string.IsNullOrEmpty(last))
                    {
                        // suspicious
                        HTML = null;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    HTML = null;
                }
            }
            catch (Exception ex)
            {
                HTML = null;
                throw ex;
            }

            if (HTML.Equals(null))
            {
                throw new Exception("NavigateCollect: No record data was returned");
            }
        }

        private async Task InitialiseAsync()
        {
            if (this.Page == null)
            {
                throw new NullReferenceException("NavigateSearch: Puppeteer.Page instance is null");
            }

            if (!this.Page.Url.StartsWith(this.targetpage))
            {
                await this.Page.SetExtraHttpHeadersAsync(new Dictionary<string, string>() { { "Referrer", "https://www.property-guru.co.nz/gurux/" } });
                await Retry<Task>(async () => { await this.Page.GoToAsync(this.targetpage, new NavigationOptions { Timeout = this.DefaultTimeout, WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded } }); }, 3, $"[Navigation] {this.workerid}");
                await this.Page.WaitForTimeoutAsync(this.DefaultTimeout);
            }

            var name = await this.Page.EvaluateExpressionAsync<string>("analytics._user._getTraits().name");
            if (name.Length > 0)
            {
                this.LoggedIn = true;
            }

            if (!this.LoggedIn)
            {
                throw new Exception("NavigateCollect: Login oracle was not found - User is logged out");
            }

            this.totalpages = await this.GetTotalPages();
            if (this.IsOffset)
            {
                await this.GoToPage(this.offset);
            }

            this.currpage = await this.GetCurrentPage();
            if(this.totalpages < 1 || this.currpage < 1)
            {
                throw new Exception("NavigateCollect: Failed to determine current or total page");
            }

            Console.WriteLine($"[ {DateTime.Now.ToShortTimeString()} ] Initialise Success. Found {this.totalpages} pages.");
        }

        private static async Task<T> Retry<T>(Func<T> func, int retryCount, string id)
        {
            while (true)
            {
                try
                {
                    // Console.WriteLine($"[!] Attempting: {id}");
                    var result = await Task.Run(func);
                    return result;
                }
                catch when (retryCount-- > 0) { }
            }
        }


    }
}
