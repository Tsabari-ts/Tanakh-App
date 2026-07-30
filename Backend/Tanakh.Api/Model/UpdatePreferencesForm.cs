namespace Tanakh.Api.Model
{
    public class UpdatePreferencesForm
    {
        public required string Token { get; set; }

        public string? PreferredTime { get; set; }

        public bool SkipShabbatHolidays { get; set; }

        // "save" | "pause" | "resume"
        public string? Action { get; set; }
    }
}
