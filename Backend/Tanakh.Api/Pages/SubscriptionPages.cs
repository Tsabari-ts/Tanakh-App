using System;
using System.Net;

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

        public static string RenderPreferencesForm(
            string token, TimeOnly preferredTime, bool skipShabbatHolidays, DateTimeOffset? pausedUntil, string postUrl)
        {
            string encodedToken = WebUtility.HtmlEncode(token);
            string timeValue = preferredTime.ToString("HH:mm");
            string checkedAttr = skipShabbatHolidays ? "checked" : "";

            bool isPaused = pausedUntil is not null && pausedUntil > DateTimeOffset.UtcNow;
            string pauseSection = isPaused
                ? $"""<p>המנוי מושהה עד {pausedUntil:yyyy-MM-dd}.</p><button type="submit" name="action" value="resume">חדש תזכורות עכשיו</button>"""
                : """<button type="submit" name="action" value="pause">השהה לחודש</button>""";

            const string style = "body { font-family: sans-serif; text-align: center; padding: 3rem 1rem; } " +
                                  "h1 { font-size: 1.4rem; } form { display: inline-block; text-align: right; }";

            return $"""
                <!DOCTYPE html>
                <html dir="rtl" lang="he">
                <head>
                    <meta charset="utf-8" />
                    <title>ניהול העדפות תזכורת</title>
                    <style>{style}</style>
                </head>
                <body>
                    <h1>ניהול העדפות תזכורת</h1>
                    <form method="post" action="{postUrl}">
                        <input type="hidden" name="token" value="{encodedToken}" />
                        <p>
                            <label>שעת תזכורת: <input type="time" name="preferredTime" value="{timeValue}" /></label>
                        </p>
                        <p>
                            <label><input type="checkbox" name="skipShabbatHolidays" value="true" {checkedAttr} /> לא לשלוח בשבתות ובחגים</label>
                        </p>
                        <p><button type="submit" name="action" value="save">שמור</button></p>
                        {pauseSection}
                    </form>
                </body>
                </html>
                """;
        }
    }
}
