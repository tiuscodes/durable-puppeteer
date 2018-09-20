using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;

namespace HelloDurableFunctions
{
    public static class ConcurrentBrowserOrchestrators
    {
        [FunctionName("O_ConcurrentBrowser")]
        public static async Task<object> ConcurrentBrowser(
            [OrchestrationTrigger] DurableOrchestrationContext ctx,
            TraceWriter log
            )
        {
            var url = ctx.GetInput<string>();
            var tasks = new List<Task<string>>();
            for (int i = 0; i < 3; i++)
            {
                tasks.Add(ctx.CallActivityAsync<string>("A_NavigateNewPage", url));
            }
            var results = await Task.WhenAll(tasks);
            
            return results;
        }
    }
}
