namespace DurablePuppeteer.UIAction
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using PuppeteerSharp;

    public class NavigateSearch : UIActionBase<int>
    {
        private string targetpage = "https://www.property-guru.co.nz/gurux/render.php?action=main";
        private string SearchQuery = "";
        private string SearchType = "";
        private ElementHandle eltextsearch;
        private ElementHandle elchkresidential;
        private bool LoggedIn = false;
        private Browser _conn = null;
        private bool IsConnected = false;
        private bool KeepPageOpen = false;
        //private int DefaultTimeout = 3000;
        private bool ResidentialOnly = true;
        public int PagesFound { get; set; }

        Page Page = null;
        public NavigateSearch(string Query, string Type)
        {
            this.SearchQuery = Query;
            this.SearchType = Type;
        }

        public NavigateSearch(string Query, string Type, bool keeppageopen = false)
        {
            this.SearchQuery = Query;
            this.SearchType = Type;
            this.KeepPageOpen = keeppageopen;
        }

        private void SetPage(Page page)
        {
            this.Page = page;
            this.Page.SetRequestInterceptionAsync(true);
            this.Page.Request += this.DenyRequests;
        }

        private async Task SetConnectionAsync(Browser conn)
        {
            if (!conn.IsClosed)
            {
                Console.WriteLine($"Connection opened: {this.SearchQuery}");
                var page = await conn.NewPageAsync();
                this.Page = page;
                this.IsConnected = true;
            }
        }

        public override async Task<int> RunAsync(Page page)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Console.WriteLine($"[ {DateTime.Now.ToShortTimeString()} ] Started Search : {this.SearchQuery} ( {this.SearchType} )");
            this.SetPage(page);
            await this.InitialiseAsync();
            await this.ProcessAsync();
            await this.Commit();
            sw.Stop();
            Console.WriteLine($"[ {DateTime.Now.ToShortTimeString()} ] Completed Search : {this.SearchQuery} @ {sw.ElapsedMilliseconds}ms");
            return this.PagesFound;
        }

        private async Task<int> GetTotalPages()
        {
            int i = -1;
            var ellastpagelink = await this.Page.QuerySelectorAsync("a[title='Last page']");
            if (ellastpagelink != null)
            {
                var href = await this.Page.EvaluateFunctionAsync<string>("el => el.href ", ellastpagelink);
                var start = href.IndexOf("(") + 1;
                var end = href.IndexOf(",");
                var totalpages = href.Substring(start, end - start);
                i = int.Parse(totalpages);
            }

            return i;
        }

        private async Task<int> WaitFirstGetTotalPages()
        {
            await this.Page.WaitForSelectorAsync("a[title='Last page']", new WaitForSelectorOptions { Timeout = this.DefaultTimeout });
            await this.Page.WaitForTimeoutAsync(this.DefaultTimeout);
            var result = await this.GetTotalPages();
            return result;
        }

        private async Task Commit()
        {
            this.PagesFound = await this.WaitFirstGetTotalPages();
            if (!this.KeepPageOpen)
            {
                await this.Page.CloseAsync();
            }
            else
            {
                this.Page.Request -= this.DenyRequests;
            }

            if (this.IsConnected)
            {
                try
                {
                    this._conn.Disconnect();
                    Console.WriteLine($"Connection closed: {this.SearchQuery}");
                }
                catch { }
            }
        }

        public async Task WaitForLoad()
        {
            var loader1 = Retry<Task>(
                async () =>
            {
                var loader = await this.Page.QuerySelectorAsync("div#contentLoading");
                var result = await this.Page.WaitForFunctionAsync($"el => el.outerHTML.includes('none')", new WaitForFunctionOptions { Polling = WaitForFunctionPollingOption.Mutation, Timeout = this.DefaultTimeout }, loader);
            }, 2,
                $"[Page] Wait till load complete");
            var loader2 = Retry<Task>(
                async () =>
            {
                var loader =await  this.Page.QuerySelectorAsync("div#loading");
                var result = await this.Page.WaitForFunctionAsync($"el => el.outerHTML.includes('none')", new WaitForFunctionOptions { Polling = WaitForFunctionPollingOption.Mutation, Timeout = this.DefaultTimeout }, loader);
            }, 2,
                $"[Page] Wait till load complete");
            await Task.WhenAll(loader1, loader2);
        }

        public async Task OpenDropDown()
        {
            var loader2 = Retry<Task>(
                async () =>
                {
                    await this.Page.EvaluateExpressionHandleAsync($"fireInitialSearch('{this.SearchQuery}')");
                    await this.Page.WaitForSelectorAsync("#autocomplete_choices", new WaitForSelectorOptions { Visible = true, Hidden = false, Timeout= DefaultTimeout });
                }, 2,
                $"[Page] Wait till load complete");

            await loader2;
        }

        public async Task ProcessAsync()
        {
            // Enter search query
            await OpenDropDown();
            await this.ClickSearchOption();
            await this.WaitForLoad();

        }

        private async Task ClickSearchOption()
        {
            var dropdown = await this.Page.QuerySelectorAsync("#autocomplete_choices");
            if(dropdown == null)
            {
                throw new Exception("NavigateSearch: Dropdown suggestions list failed to trigger");
            }
            List<string> optionselectors = new List<string>();
            string input;
            switch (this.SearchType.ToLower())
            {
                case "suburb":
                    input = "SUBR";
                    optionselectors.Add($"//*[@id='autocomplete_choices']/ul/li[contains(substring-after(@class,'LOCL'),'{input}')]");
                    break;
                case "locality":
                    input = "LOCL";
                    optionselectors.Add($"//*[@id='autocomplete_choices']/ul/li[contains(substring-after(@class,'LOCL'),'{input}')]");
                    break;
                case "district":
                    input = "TERR";
                    optionselectors.Add($"//*[@id='autocomplete_choices']/ul/li[contains(substring-after(@class,'LOCL'),'{input}')]");
                    break;
            }
            await this.Page.WaitForTimeoutAsync(this.DefaultTimeout);
            for (int i = 0; i < optionselectors.Count;i++)
            {
                var option = await this.Page.XPathAsync(optionselectors[i]);
                if (option.Length == 0)
                {
                    if ((i + 1) < optionselectors.Count)
                    {
                        break; // Try next iteration
                    }

                    throw new Exception($"NavigateSearch: Search selector for type '{this.SearchType}' returned null");
                }

                await this.Page.EvaluateFunctionAsync("o => o.click()", option[0]);
                if (this.ResidentialOnly)
                {
                    await this.Page.ClickAsync("input[value='Residential']");// Click check box
                }
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
                await Retry<Task>(async () => { await this.Page.GoToAsync(this.targetpage, new NavigationOptions { Timeout = this.DefaultTimeout, WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded } }); }, 3, $"[Navigation] {this.SearchQuery}");
            }

            await this.Page.WaitForTimeoutAsync(this.DefaultTimeout);
            // Assert is logged in
            // await Retry<Task>(async () => { await this.Page.WaitForSelectorAsync("#userDetailsHeaderTab > div.boxHeaderHeading", new WaitForSelectorOptions { Timeout = DefaultTimeout }); }, 2, $"[Query] {this.SearchQuery}");
            var name = await this.Page.EvaluateExpressionAsync<string>("analytics._user._getTraits().name");
            if (name != null)
            {
                if (name.Length > 0)
                {
                    this.LoggedIn = true;
                }
            }

            if (!this.LoggedIn)
            {
                throw new Exception("NavigateSearch: Login oracle was not found - User is logged out");
            }
        }

        private static async Task<T> Retry<T>(Func<T> func, int retryCount, string id)
        {
            while (true)
            {
                try
                {
                    var result = await Task.Run(func);
                    return result;
                }
                catch when (retryCount-- > 0) { }
            }
        }
    }
}
