namespace Tanakh.Api.Pages
{
    // Confirmation/unsubscribe links in emails land here directly - these are
    // deliberately plain server-rendered pages, not part of the Angular app,
    // so they work even if the SPA itself is unavailable.
    public static class SubscriptionPages
    {
        public static string Render(string title, string message, string? homeUrl = null)
        {
            string homeLink = homeUrl is null
                ? string.Empty
                : $"<p><a href=\"{homeUrl}\">חזרה לאתר</a></p>";

            const string style = "body { font-family: sans-serif; text-align: center; padding: 3rem 1rem; } h1 { font-size: 1.4rem; }";

            return $"""
                <!DOCTYPE html>
                <html dir="rtl" lang="he">
                <head>
                    <meta charset="utf-8" />
                    <title>{title}</title>
                    <style>{style}</style>
                </head>
                <body>
                    <h1>{title}</h1>
                    <p>{message}</p>
                    {homeLink}
                </body>
                </html>
                """;
        }
    }
}
