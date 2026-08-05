namespace Tanakh.Api.Model
{
    public class UpdatePreferencesRequest
    {
        public required string Token { get; set; }

        public string? PreferredTime { get; set; }

        public bool? SkipShabbatHolidays { get; set; }

        // "pause" | "resume" | null
        public string? Action { get; set; }
    }
}
