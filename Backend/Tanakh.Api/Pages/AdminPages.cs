using System.Text;
using Tanakh.Domain;

namespace Tanakh.Api.Pages
{
    public static class AdminPages
    {
        public static string RenderDashboard(AdminDashboard dashboard, string actionBaseUrl)
        {
            const string style = "body { font-family: sans-serif; padding: 2rem; direction: rtl; } " +
                                  "table { border-collapse: collapse; width: 100%; margin-bottom: 2rem; } " +
                                  "th, td { border: 1px solid #ccc; padding: 0.4rem 0.6rem; text-align: right; } " +
                                  ".stats { display: flex; gap: 2rem; flex-wrap: wrap; margin-bottom: 2rem; } " +
                                  ".stat { border: 1px solid #ccc; padding: 1rem; min-width: 8rem; } " +
                                  ".stat b { display: block; font-size: 1.5rem; }";

            StringBuilder html = new();
            html.Append($"""
                <!DOCTYPE html>
                <html dir="rtl" lang="he">
                <head>
                    <meta charset="utf-8" />
                    <title>לוח ניהול</title>
                    <style>{style}</style>
                </head>
                <body>
                <h1>לוח ניהול</h1>
                <div class="stats">
                    <div class="stat"><b>{dashboard.ActiveSubscribers}</b>מנויים פעילים</div>
                    <div class="stat"><b>{dashboard.PendingConfirmationSubscribers}</b>ממתינים לאישור</div>
                    <div class="stat"><b>{dashboard.UnsubscribedSubscribers}</b>בוטלו</div>
                    <div class="stat"><b>{dashboard.BouncedSubscribers}</b>נחסמו (bounce)</div>
                    <div class="stat"><b>{dashboard.SignupsLast7Days}</b>הרשמות ב-7 ימים</div>
                    <div class="stat"><b>{dashboard.ConfirmationRatePercent:F1}%</b>שיעור אישור</div>
                    <div class="stat"><b>{dashboard.BounceRatePercent:F1}%</b>שיעור bounce</div>
                </div>

                <h2>פעולות ידניות</h2>
                <form method="post" action="{actionBaseUrl}/unsubscribe" style="margin-bottom:0.5rem">
                    <input type="email" name="email" placeholder="אימייל לביטול הרשמה" required />
                    <button type="submit">בטל הרשמה</button>
                </form>
                <form method="post" action="{actionBaseUrl}/resend-confirmation">
                    <input type="email" name="email" placeholder="אימייל לשליחת אישור מחדש" required />
                    <button type="submit">שלח אישור מחדש</button>
                </form>

                <h2>משלוחים שנכשלו ({dashboard.RecentFailedDeliveries.Count})</h2>
                <table>
                    <tr><th>אימייל</th><th>ניסיונות</th><th>שגיאה אחרונה</th><th></th></tr>
                """);

            foreach (FailedDeliverySummary failure in dashboard.RecentFailedDeliveries)
            {
                html.Append($"""
                    <tr>
                        <td>{failure.Email}</td>
                        <td>{failure.AttemptCount}</td>
                        <td>{failure.LastError}</td>
                        <td>
                            <form method="post" action="{actionBaseUrl}/requeue">
                                <input type="hidden" name="deliveryId" value="{failure.DeliveryId}" />
                                <button type="submit">שלח שוב</button>
                            </form>
                        </td>
                    </tr>
                    """);
            }

            html.Append($"""
                </table>

                <h2>משלוחים מתוזמנים היום ({dashboard.DeliveriesScheduledToday.Count})</h2>
                <table>
                    <tr><th>אימייל</th><th>מתוזמן ל</th><th>סטטוס</th></tr>
                """);

            foreach (ScheduledDeliverySummary scheduled in dashboard.DeliveriesScheduledToday)
            {
                html.Append($"""
                    <tr><td>{scheduled.Email}</td><td>{scheduled.ScheduledFor:yyyy-MM-dd HH:mm} UTC</td><td>{scheduled.Status}</td></tr>
                    """);
            }

            html.Append("</table></body></html>");

            return html.ToString();
        }
    }
}
