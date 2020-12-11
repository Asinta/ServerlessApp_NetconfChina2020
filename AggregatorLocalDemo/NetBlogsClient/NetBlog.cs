using System;
using System.Collections.Generic;
using System.Text;

public class NetBlog
{
    public string Title { get; set; }
    public string Summary { get; set; }
    public DateTimeOffset PubDate { get; set; }
    public Uri Uri { get; set; }

    public override string ToString()
	{
	    return $"<h2>{Title}</h2><br><a href={Uri}>{Uri}</a><br><p>{Summary}</p><br><p>{PubDate}</p><br/>";
	}
}

public class NetBlogsContainer
{
    public ICollection<NetBlog> Blogs { get; set; }

    public NetBlogsContainer() => Blogs = new HashSet<NetBlog>();

    public override string ToString()
	{
	    var content = new StringBuilder();
	    foreach (var item in Blogs)
	    {
	        content.AppendLine(item.ToString());
	    }
	    return $"<h1>{Blogs.Count} new feeds!</h1><br>" + content.ToString();
	}
}