namespace DurablePuppeteer.UIAction
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using PuppeteerSharp;

    public interface UIAction<T>
    {
        Task<T> RunAsync(Page page);
    }
}
