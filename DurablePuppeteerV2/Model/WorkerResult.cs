namespace DurablePuppeteer.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class WorkerResult
    {
        public int PageNumber { get; set; }
        public string PageHTML { get; set; }
        public string WorkerId { get; set; }
        public bool Success { get; set; }
    }
}
