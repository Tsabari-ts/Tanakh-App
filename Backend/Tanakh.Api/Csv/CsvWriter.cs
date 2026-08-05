using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tanakh.Api.Csv
{
    // Minimal RFC 4180-ish writer for the admin export endpoints - not a
    // general-purpose library, just enough to escape commas/quotes/newlines
    // for the flat DTOs those endpoints return.
    public static class CsvWriter
    {
        public static string Write(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string?>> rows)
        {
            StringBuilder builder = new();
            builder.Append(string.Join(',', headers.Select(Escape)));
            builder.Append("\r\n");

            foreach (IReadOnlyList<string?> row in rows)
            {
                builder.Append(string.Join(',', row.Select(Escape)));
                builder.Append("\r\n");
            }

            return builder.ToString();
        }

        private static string Escape(string? value)
        {
            value ??= string.Empty;

            bool needsQuoting = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
            return needsQuoting ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
        }
    }
}
