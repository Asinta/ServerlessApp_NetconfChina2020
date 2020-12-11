using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace AggregatorLocalDemo
{
    public static class EmailSender
    {
        [FunctionName("EmailSender")]
        public static void Run(
            [QueueTrigger("netblogs", Connection = "AzureWebJobsStorage")] NetBlogsContainer container,
            ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {container.Blogs.Count}");

            var fromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM", EnvironmentVariableTarget.Process);
            var toEmail = Environment.GetEnvironmentVariable("EMAIL_TO", EnvironmentVariableTarget.Process);
            var smtpServer = Environment.GetEnvironmentVariable("HOTMAIL_SMTP_SERVER", EnvironmentVariableTarget.Process);
            var smtpPort = int.Parse(Environment.GetEnvironmentVariable("HOTMAIL_SMTP_PORT", EnvironmentVariableTarget.Process));
            var username = fromEmail;
            var password = Environment.GetEnvironmentVariable("HOTMAIL_PASSWORD", EnvironmentVariableTarget.Process);;

            var client = new HotmailEmailClient(
                fromEmail,
                toEmail,
                smtpServer,
                smtpPort,
                username,
                password,
                log
            );

            client.SendEmail("New feeds updated!", container.ToString());

            log.LogInformation("Email Sent!");
        }
    }
}
