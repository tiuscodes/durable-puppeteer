using DurablePuppeteer;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DurablePuppeteerV2.Service
{
    public static class Storage
    {
        private static Sender sender = new Sender(Constants.ServiceBusConenction, Constants.ServiceBusQueue);
        public static void AddPage(int pagenumber, int totalpages, string suburb, string html)
        {
            sender.Post(pagenumber, totalpages, suburb, html);
        }
    }

    public class Sender
    {
        private string connectionstring;
        private string queuename;
        private QueueClient queueclient;
        private BlockingCollection<message> messages = new BlockingCollection<message>();
        public Sender(string connectionstring, string queuename)
        {
            this.connectionstring = connectionstring;
            this.queuename = queuename;
            queueclient = QueueClient.CreateFromConnectionString(connectionstring, queuename);
        }

        private void Consumer()
        {
            while (true)
            {
                if (messages.TryTake(out message item))
                {
                    PostCommit(item.pagenumber, item.totalpages, item.suburb, item.html);
                }
                Task.Delay(1000).Wait();
            }
        }

        private void PostCommit(int pagenumber, int totalpages, string suburb, string html)
        {
            var page = new ScrapedPage
            {
                number = pagenumber,
                total = totalpages,
                suburb = suburb,
                html = html
            };
            var pagejson = JsonConvert.SerializeObject(page);
            queueclient.Send(new BrokeredMessage(pagejson));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Posted page {page.number} of {page.total}: {page.suburb}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void Post(int pagenumber, int totalpages, string suburb, string html)
        {
            messages.Add(new message
            {
                pagenumber = pagenumber,
                totalpages = totalpages,
                suburb = suburb,
                html = html
            });
        }

        class message
        {
            public int pagenumber { get; set; }
            public int totalpages { get; set; }
            public string suburb { get; set; }
            public string html { get; set; }
        }
    }

    public class ScrapedPage
    {
        public string suburb { get; set; }
        public int number { get; set; }
        public int total { get; set; }
        public string html { get; set; }
    }
}
