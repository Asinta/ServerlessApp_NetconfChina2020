# Building Serverless Application with Azure Functions

## Pre-request

- [Visual Studio Code](https://code.visualstudio.com/)
- [Azure Cli](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Azure Function Core Tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local?tabs=macos%2Ccsharp%2Cbash#v2)
- [Azure Account](https://azure.microsoft.com/en-us/account/)
- [Azure Tools Extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.vscode-node-azure-pack)
- login to azure account

## What are we going to build?

We are going to build an azure functions app from zero. In this app, we will add two functions. One is used for fetching feeds from [.NET Blog](https://devblogs.microsoft.com/dotnet/) and send results into a queue. The second one is used to receive those feeds from the queue and send an email to inform myself to the new feeds posted.

During this demo, we are going to explore several techniques such as:
- how to create an Azure Functions App using azure cli;
- how to create functions to respond to certain triggers and binding to certain output using .net core;
- how to configure app settings to keep code clean;
- how to retrieve secrets from Azure KeyVault service;
- how to publish local project to Azure;
- how to monitor your app running.

So let's get started.

## Overview

We are going to call this Azure Functions App `AggregatorForMe`, and in order to show you the most important part of this workflow, I will keep business logic as simple as possible.

Basically, we are going to work through following steps:
1. using `azure cli` to create basic resources to hold our app;
2. using `azure function core tools` to init and create local functions;
3. coding for first function: triggered by time, output to queue;
4. local debugging;
5. coding for second function: triggered by queue, send email to myself;
6. refactor our code using app settings;
7. get access to `Azure KeyVault`;
8. publish our app;
9. monitor app logstream.

## 1. Create Azure Functions App

We are going to need a **resource group** to hold our resources during this workshop, as well as a **storage account** to hold our code and data, at last, we are going to create the **Azure Functions App**.

```zsh
# NOTE: Prerequest
# func --version > 3.x
# az --version > 2.4
# dotnet --list-sdks = 3.1.x

# NOTE: make sure you have already login to azure using `az login`
# az login

# 1. Create resource group
az group create --name netconfchina2020-rg --location eastasia

# 2. Create storage account
az storage account create --name netconfchina2020sa --resource-group netconfchina2020-rg

# 3. Create Azure Functions App
az functionapp create \
    --name AggregatorNetForMe \
    --resource-group netconfchina2020-rg \
    --storage-account netconfchina2020sa \
    --consumption-plan-location eastasia \
    --runtime dotnet \
    --functions-version 3
```

Ok, now it's time to check these resources on Azure Portal.

## 2. Create local project

We are going to use `azure function core tools` to create local project.

```zsh
# func init will automatically create project folder for us.
func init AggregatorLocalDemo --dotnet
```

Let's go through the created files:

- `*.csproj` of course is the project file.
- `hots.json` is the configuration for our project.
- `local.settings.json` is the configure file for local development

If you are using Mac to develop, you should change the value of `AzureWebJobsStorage` in `local.settings.json` from origin data to the real storage account connection string, which you can find it using the following command:

```zsh
az storage account show-connection-string --name netconfchina2020sa --query "connectionString"
```

## 3. Add first function: Fetch feeds from [.NET Blog](https://devblogs.microsoft.com/dotnet/)

In the first function, we will complete a `TimeTrigger` function, which will fetch the rss feeds and parse it into our custom data type.

### Add function

Firstly, we are going to add a new function called `NetBlogsFetcher`:

```zsh
# And choose TimeTrigger for this function.
func new --name NetBlogsFetcher
```

If we look at the codes in `NetBlogsFetcher.cs` file, we can see function uses attributes to point out the `FunctionName`, `TriggerType`, since we want to send data into a queue, we need a **output binding to a queue**.

### Add queue output binding

Secondly, let's add output queue binding support for our function:

```zsh
# the version here is to match the version of `Microsoft.NET.Sdk.Functions` package.
dotnet add package Microsoft.Azure.WebJobs.Extensions.Storage --version 3.0.7
```

then modify function signature to:

```C#
// 1. give the queue a name, it will automatically create this queue for us.
// 2. tell which storage account this function will use, default is the value of `AzureWebJobsStorage`.
// 3. use ICollector<T> type instance to operate with, type T is because we don't know the type of data we are going to send into the queue, will fix this later.
public static void Run(
    [TimerTrigger("0 */5 * * * *")]TimerInfo myTimer,
    [Queue("netblogs"), StorageAccount("AzureWebJobsStorage")] ICollector<T> queue,
    ILogger log)
```

### Implement fetching data from rss

Now let's create a folder called `NetBlogsClient` and create two new files in it: `NetBlog.cs` and `NetBlogsClient.cs`:

In `NetBlog.cs` file, since we don't know the data structure of the feed, just define an empty class here:

```C#
public class NetBlog
{

}
```

In `NetBlogsClient.cs` file, we create a class to do the fetching work:
```C#
using System.Collections.Generic;
using System.Xml;

public class NetBlogsClient
{
    public ICollection<NetBlog> Blogs { get; set; }
    public string RssUrl { get; set; }

    public NetBlogClient(string rssUrl)
    {
        RssUrl = rssUrl;
        Blogs = new HashSet<NetBlog>();
    }

    public void FetchFeedsFromNetBlog()
    {
        using var xmlReader = XmlReader.Create(RssUrl);
        // How to load rss feeds?
    }
}
```

For loading feeds, we will use the following package:

```zsh
dotnet add package System.ServiceModel.Syndication --version 5.0.0
```

Now we can implement `FetchFeedsFromNetBlog` as following:

```C#
public void FetchFeedsFromNetBlog()
{
    using var xmlReader = XmlReader.Create(RssUrl);
    var feeds = SyndicationFeed.Load(xmlReader);

    // what data structure is feeds?
}
```

We will start our local debugging to check the content of feeds to implement our data model:

```C#
public class NetBlog
{
    public string Title { get; set; }
    public string Summary { get; set; }
    public DateTimeOffset PubDate { get; set; }
    public Uri Uri { get; set; }
}
```

The final `FetchFeedsFromNetBlog` is like this:

```C#
public void FetchFeedsFromNetBlog()
{
    using var xmlReader = XmlReader.Create(RssUrl);
    var feeds = SyndicationFeed.Load(xmlReader);

    foreach (var feed in feeds.Items)
    {
        Blogs.Add(new NetBlog
        {
            Title = feed.Title.Text,
            Summary = feed.Summary.Text,
            PubDate = feed.PublishDate,
            Uri = feed.Links[0].Uri
        });
    }
}
```

### Send data into queue

In order to send data into queue, we simply use `queue.Add(T instance)` method is enough. The `Add` method will do the serialization work for us, so please make sure what you are going to put in the queue is json serializable. So we will create a new data type to do it since `ICollection` type is not json serializable:

```C#
public class NetBlogsContainer
{
    public int Count { get; set; }
    public ICollection<NetBlog> Blogs { get; set; }

    public NetBlogsContainer(int count, ICollection<NetBlog> blogs)
    {
        Count = count;
        Blogs = blogs;
    }
}
```

Following is our final `Run` method:

```C#
[FunctionName("NetBlogFetcher")]
public static void Run(
    [TimerTrigger("0 */5 * * * *")]TimerInfo myTimer,
    [Queue("netblogs"), StorageAccount("AzureWebJobsStorage")] ICollector<NetBlogsContainer> queue,
    ILogger log)
{
    log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

    var rssUrl = "https://devblogs.microsoft.com/dotnet/feed/";

    var blogClient = new NetBlogClient(rssUrl);
    blogClient.FetchFeedsFromNetBlog();

    var blogsContainer = new NetBlogsContainer(blogClient.Blogs.Count, blogClient.Blogs);
    queue.Add(blogsContainer);

    log.LogInformation($"fetching from rss execution end.");
}
```

Let's hit `F5` to see if data has been successfully sent into the queue called `netblogs`. Great!


## 4. Add second function: receiving data from the queue and send email to myself

### Add function

This time will create a function which has `QueueTrigger`:

```zsh
# And choose QueueTrigger for this function.
func new --name EmailSender
```

Then modify the function signature:

```C#
// use your queue name and custom data type.
public static void Run(
            [QueueTrigger("netblogs", Connection = "AzureWebJobsStorage")] NetBlogsContainer blogsContainer,
            ILogger log)
{
    log.LogInformation($"C# Queue trigger function processed with {blogsContainer.Count} feeds!");
    var blogs = blogsContainer;

    // how to send emails?
}
```

### Send email

We have some options here: either we can use `System.Net.Mail` package, or we can use `SendGrid` service, which is also an optional output binding for function. But I've got some issues with my account, so in this demo, we simply use the first choice with creating a file `EmailClient.cs` in a new foler `EmailClient`.

```C#
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;

public class EmailClient
{
    private readonly ILogger _logger;

    public string From { get; set; }
    public string To { get; set; }
    public string SmtpServer { get; set; }
    public int SmtpPort { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }

    public EmailClient(
        string from,
        string to,
        string smtpServer,
        int smtpPort,
        string username,
        string password,
        ILogger logger)
    {
        From = from;
        To = to;
        SmtpServer = smtpServer;
        SmtpPort = smtpPort;
        Username = username;
        Password = password;
        _logger = logger;
    }

    public void SendEmail(string subject, string body)
    {
        using var mailMessage = new MailMessage(From, To)
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
            Priority = MailPriority.High
        };

        var mailSender = new SmtpClient(SmtpServer, SmtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(Username, Password)
        };

        try
        {
            mailSender.Send(mailMessage);
        }
        catch (System.Exception ex)
        {
            _logger.LogError($"Send mail with error: {ex.Message}");
        }
    }
}
```

Now let's figure out what's in our `Run` method:

```C#
[FunctionName("EmailClient")]
public static void Run(
    [QueueTrigger("netblogs", Connection = "AzureWebJobsStorage")] NetBlogsContainer blogsContainer,
    ILogger log)
{
    log.LogInformation($"C# Queue trigger function processed with {blogsContainer.Count} feeds!");

    var blogs = blogsContainer;

    var fromEmail = "YOUR_SENDER_EMAIL_ADDRESS";
    var toEmail = "YOUR_RECEIVE_EMAIL_ADDRESS";

    // since I'm using hotmail, so find the smtp server/port from outlook website.
    var smtpServer = "smtp-mail.outlook.com";
    int smtpPort = 587;

    var username = fromEmail;
    var password = "YOUR_SENDER_EMAIL_LOGIN_PASSWORD";

    var emailClient = new EmailSender(fromEmail, toEmail, smtpServer, smtpPort, username, password, log);
    emailClient.SendEmail("New feeds coming!", blogs.ToString());

    log.LogInformation("Email Sent!");
}
```

We also need to do some modification to our data model to let `blogs.ToString()` work:

```C#
// in `NetBlog` class
public override string ToString()
{
    return $"<h2>{Title}</h2><br><a href={Uri}>{Uri}</a><br><p>{Summary}</p><br><p>{PubDate}</p><br/>";
}

// in `NetBlogsContainer` class
public override string ToString()
{
    var content = new StringBuilder();
    foreach (var item in Blogs)
    {
        content.AppendLine(item.ToString());
        content
    }
    return $"<h2>{Count} feeds created!</h2><br>" + content.ToString();
}
```

If you test this project locally, you should be able to receive an email contains the new feeds. Whoo! But wait, there are some unconfortable codes here.

First is we don't want our password to be written like this, secondly we have a lot of configuration items hard-coding here, I want to get rid of them.

## 5. Refactor

### Separate app settings from hard-coding

We will use **Azure Functions App Settings** to hold our configuration items values.

```zsh
az functionapp config appsettings set \
    --name AggregatorForMe \
    --resource-group netconfchina2020-rg \
    --settings \
    "NET_BLOG_RSS_URL=https://devblogs.microsoft.com/dotnet/feed/" \
    "EMAIL_FROM=YOUR_SENDER_EMAIL_ADDRESS" \
    "EMAIL_TO=YOUR_RECEIVE_EMAIL_ADDRESS" \
    "HOTMAIL_SMTP_SERVER=YOUR_SMTP_SERVER_ADDRESS" \
    "HOTMAIL_SMTP_PORT=YOUR_SMTP_SERVER_PORT"
```

Check on Azure portal we can see these settings have been successfully created. So how can we get them in code:

```C#
// in `NetBlogsFetcher.cs`
var rssUrl = System.Environment.GetEnvironmentVariable("NET_BLOG_RSS_URL", EnvironmentVariableTarget.Process);

// in `EmailSender.cs`
var from = System.Environment.GetEnvironmentVariable("EMAIL_FROM", EnvironmentVariableTarget.Process);
var to = System.Environment.GetEnvironmentVariable("EMAIL_TO", EnvironmentVariableTarget.Process);
var smtpServer = System.Environment.GetEnvironmentVariable("HOTMAIL_SMTP_SERVER", EnvironmentVariableTarget.Process);
var smtpPort = System.int.Parse(Environment.GetEnvironmentVariable("HOTMAIL_SMTP_PORT", EnvironmentVariableTarget.Process));
var username = from
```

Now let's deal with our password.

### Using Azure KeyVault to store your sensitive data and use in Azure Functions App

Firstly let's create a `KeyVault` in our current resource group:

```zsh
az keyvault create --name "netconfchina2020-kv" --resource-group "netconfchina2020-rg"
```

Secondly add our senstive data as secrets:

```zsh
az keyvault secret set --vault-name "netconfchina2020-kv" --name "hotmailPassword" --value <YOUR_PASSWORD>
```

Thirdly we need to turn on managed identities for our Functions App:

```zsh
az webapp identity assign --resource-group netconfchina2020-rg --name AggregatorForMe 
```

Then we will add access policy for our app:

```zsh
# get appId first
az ad sp list --display-name "AggregatorForMe" --query "[0].appId"

# authorize actions to the secrets form app
az keyvault set-policy \
    --name "netconfchina2020-kv" \
    --spn <YOUR_APP_ID> \
    --secret-permissions get list
```

Then configure secrets as app settings for our app:

```zsh
# get secret's uri
az keyvault secret show --vault-name "netconfchina2020-kv" --name hotmailPassword --query "id"

# configure secret as app settings
az functionapp config appsettings set \
    --name AggregatorForMe \
    --resource-group netconfchina2020-rg \
    --settings HOTMAIL_PASSWORD="@Microsoft.KeyVault(SecretUri=YOUR_SECRET_URI)"
```

Finally you can use this value the same way as app settings:

```C#
var password = System.Environment.GetEnvironmentVariable("HOTMAIL_PASSWORD", EnvironmentVariableTarget.Process);
```

## 5. Publish and demo time

Publish local project to Azure is easy with `Azure Function Core Tools` cli, but before you publish your local project, please consider whether you want to change your cron job frequency.

```zsh
func azure functionapp publish AggregatorForMe
```

## 6. Monitor

You can check logstream either in console or in browser using:

```zsh
func azure functionapp logstream AggregatorForMe [--browser]
```

## 7. Clean up

Since our resources all resides in `netconfchina2020-rg` group, simply delete it:

```zsh
az group delete --name netconfchina2020-rg
```

## Conclusion

This is the most simple way to use `Azure Functions App`, you can explore more exciting and interesting things with it! Check out on [https://docs.microsoft.com/en-us/azure/azure-functions/](https://docs.microsoft.com/en-us/azure/azure-functions/) and build your very first functions app!

WARNING: DO NOT BREAK ANY LAW!
