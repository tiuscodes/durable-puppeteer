namespace DurablePuppeteer.UIAction
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using PuppeteerSharp;

    public class HelloAction : UIAction.UIActionBase<string>
    {
        public string Url { get; set; }
        public HelloAction(string url)
        {
            this.Url = url;
        }

        public override async Task<string> RunAsync(Page page)
        {
            await page.GoToAsync(this.Url);
            var html = await page.GetContentAsync();
            return await Task.FromResult(html);
        }
    }
}
