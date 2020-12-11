using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace AggregatorLocalDemo
{
    public static class NetBlogsFetcher
    {
        [FunctionName("NetBlogsFetcher")]
        public static void Run(
            [TimerTrigger("0 */1 * * * *")]TimerInfo myTimer,
            [Queue("netblogs"), StorageAccount("AzureWebJobsStorage")]ICollector<NetBlogsContainer> queue,
            ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var rssUrl = Environment.GetEnvironmentVariable("NET_BLOG_RSS_URL", EnvironmentVariableTarget.Process);

            var client = new NetBlogsClient(rssUrl);
            client.FetchFeedsFromNetBlog();

            queue.Add(client.BlogsContainer);

            log.LogInformation("Fetch feeds end.");
        }
    }
}
