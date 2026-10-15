using System.Text;

namespace Weather.WebUI.Hosting;

/// <summary>
/// Serves the crawler discovery files (<c>robots.txt</c>, <c>sitemap.xml</c>) built from the current
/// request origin, so the absolute URLs are correct in every environment without configuration.
/// </summary>
internal static class SeoEndpoints
{
    // Routes that hold indexable, public content. Error and not-found routes are excluded deliberately.
    private static readonly string[] IndexablePaths = ["/", "/favorites"];

    public static void MapSeoEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/robots.txt", (HttpContext context) => Results.Text(BuildRobots(context), "text/plain"));
        app.MapGet("/sitemap.xml", (HttpContext context) => Results.Text(BuildSitemap(context), "application/xml"));
    }

    private static string BuildRobots(HttpContext context) =>
        $"""
        User-agent: *
        Allow: /
        Disallow: /api/
        Disallow: /_csp/
        Disallow: /Error
        Disallow: /not-found

        Sitemap: {Origin(context)}/sitemap.xml
        """;

    private static string BuildSitemap(HttpContext context)
    {
        string origin = Origin(context);
        StringBuilder builder = new();
        builder.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        builder.AppendLine("""<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">""");

        foreach (string path in IndexablePaths)
        {
            builder.AppendLine($"  <url><loc>{origin}{path}</loc></url>");
        }

        builder.AppendLine("</urlset>");
        return builder.ToString();
    }

    private static string Origin(HttpContext context) =>
        $"{context.Request.Scheme}://{context.Request.Host}";
}
