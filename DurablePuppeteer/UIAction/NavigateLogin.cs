namespace DurablePuppeteer.UIAction
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs.Host;
    using PuppeteerSharp;

    public class Login : UIAction.UIActionBase<CookieParam[]>
    {
        private string Username = "";
        private string Pasword = "";
        private string loginurl = "https://www.property-guru.co.nz/gurux/";
        private ElementHandle elusername;
        private ElementHandle elpassword;
        private ElementHandle elbutton;

        public CookieParam[] cookies { get; set; }

        Page Page = null;

        public Login()
        {
            this.Username = "jared.cooksley@raywhite.com";
            this.Pasword = "Jared2017";
        }

        public Login(string username, string password)
        {
            this.Username = username;
            this.Pasword = password;
        }

        public override async Task<CookieParam[]> RunAsync(Page page)
        {
            this.SetPageAsync(page);
            await this.InitialiseAsync();
            await this.ProcessAsync();
            await this.Commit();
            return this.cookies;
        }
        public async Task<CookieParam[]> RunAsync(Page page, TraceWriter log)
        {
            this.log = log;
            this.SetPageAsync(page);
            await this.InitialiseAsync();
            await this.ProcessAsync();
            await this.Commit();
            return this.cookies;
        }

        private async void SetPageAsync(Page page)
        {
            this.Page = page;
            //this.Page.Request += this.DenyRequests;
            //await this.Page.SetRequestInterceptionAsync(false);
        }

        private async Task InitialiseAsync()
        {

            if (this.Page == null)
            {
                throw new NullReferenceException("PuppeteerSharp.Page instance is null");
            }

            var response = await this.Page.GoToAsync(this.loginurl, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded },
            });
            this.elusername = await this.Page.QuerySelectorAsync("#user");
            this.elpassword = await this.Page.QuerySelectorAsync("input[name='password']");
            this.elbutton = await this.Page.QuerySelectorAsync("input.btn:nth-child(1)");

            if (this.elusername == null || this.elpassword == null || this.elbutton == null)
            {
                throw new Exception("NavigateLogin: Form input items missing.");
            }
            else
            {
                Console.WriteLine("Initialise Success.");
            }
        }

        private async Task ProcessAsync()
        {
            var usernameinput = await this.Page.QuerySelectorAsync("#user");
            await this.Page.EvaluateFunctionAsync($"yup => yup.value = {this.Username}", usernameinput);
            var passwordinput = await this.Page.QuerySelectorAsync("input[name='password']");
            await this.Page.EvaluateFunctionAsync($"mmhm => mmhm.value = {this.Pasword}", passwordinput);
            var form = await this.Page.QuerySelectorAsync("#loginForm");
            if (form == null)
            {
                throw new Exception("NavigateLogin: Page missing required login form.");
            }

            await this.Page.EvaluateFunctionAsync($"f => f.submit()", form);
            await this.Page.WaitForNavigationAsync(new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Load } });
            await this.Page.SetRequestInterceptionAsync(true);
            this.Page.Request += this.DenyRequests;
        }

        private async Task Commit()
        {
            // finalise operations
            if (this.Page.Url == "http://www.property-guru.co.nz/gurux/render.php?action=main")
            {
                // success
                this.cookies = await this.Page.GetCookiesAsync();
            }

            this.Page.Request -= this.DenyRequests;
            await this.Page.CloseAsync();
            this.Page = null;
        }
    }
}
