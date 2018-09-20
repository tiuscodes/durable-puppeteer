using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace HelloDurableFunctions
{
    public static class ConcurrentBrowserActivities
    {
        [FunctionName("A_NavigateNewPage")]
        public static async Task<string> ProcessVideo(
        [ActivityTrigger] DurableActivityContext ctx,
        TraceWriter log
        )
        {
            var url = ctx.GetInput<string>();
            var asm = Assembly.Load("Microsoft.Extensions.Options, Culture=neutral, PublicKeyToken=adb9793829ddae60");
            PuppeteerSharp.Browser browser = null;
            try
            {
                browser = await Puppeteer.ConnectAsync(new ConnectOptions { BrowserWSEndpoint = Constants.BrowserWSEndpoint });
                var page = await browser.NewPageAsync();
                await page.GoToAsync(url, new NavigationOptions { Timeout = 4000 });
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                if (browser != null)
                {
                    browser.Disconnect();
                }
            }
            return $"Processed {url} shit all.";
        }

        [FunctionName("A_Worker")]
        public static async Task<string> Worker(
        [ActivityTrigger] DurableActivityContext ctx,
        TraceWriter log
        )
        {

            return "";
        }
    }

    public static class Constants
    {
        public static string BrowserWSEndpoint = "ws://localhost:9222/devtools/browser/d326cbdc-0779-41de-bfb4-18d89e3bd310";
    }
}
