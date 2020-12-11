using System.ServiceModel.Syndication;
using System.Xml;

public class NetBlogsClient
{
    public NetBlogsContainer BlogsContainer { get; set; }
    public string RssUrl { get; set; }

    public NetBlogsClient(string rssUrl)
    {
        RssUrl = rssUrl;
        BlogsContainer = new NetBlogsContainer();
    }

    public void FetchFeedsFromNetBlog()
    {
        using var xmlReader = XmlReader.Create(RssUrl);
        var feeds = SyndicationFeed.Load(xmlReader);

        foreach (var feed in feeds.Items)
        {
            BlogsContainer.Blogs.Add(new NetBlog
            {
                Title = feed.Title.Text,
                Summary = feed.Summary.Text,
                PubDate = feed.PublishDate,
                Uri = feed.Links[0].Uri
            });
        }
    }
}