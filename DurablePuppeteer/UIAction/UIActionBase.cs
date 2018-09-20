namespace DurablePuppeteer.UIAction
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using PuppeteerSharp;

    public abstract class UIActionBase<T> : UIAction<T>
    {
        internal static readonly System.Text.RegularExpressions.Regex RegexDenyTracking = new System.Text.RegularExpressions.Regex("(api.mixpanel.com|liveperson.net|api.segment.io)", System.Text.RegularExpressions.RegexOptions.Compiled);
        internal static readonly System.Text.RegularExpressions.Regex RegexBlockMeta = new System.Text.RegularExpressions.Regex("/gurux/$");
        public Microsoft.Azure.WebJobs.Host.TraceWriter log;
        public int DefaultTimeout = 7000;

        public abstract Task<T> RunAsync(Page page);

        public async void DenyRequests(object sender, RequestEventArgs e)
        {

            if (log != null)
            {
                this.log.Warning(e.Request.Url);
            }

            if (!RequestConsumer.IsRunning)
            {
                RequestConsumer.Start();
            }

            RequestConsumer.AddRequest(e.Request);
        }
    }
}
