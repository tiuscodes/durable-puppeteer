namespace DurablePuppeteer.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class WorkerArgs
    {
        // public UIAction.UIAction Action { get; set; }
        public PuppeteerSharp.Page Page { get; set; }
    }
}
