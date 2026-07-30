namespace Tanakh.Api.Model
{
    public class ReadingProgressRequest
    {
        // The signed subscriber token from the reminder email link - proves
        // who this progress update belongs to without requiring an account.
        public required string Token { get; set; }

        public required string Book { get; set; }

        public required int Chapter { get; set; }
    }
}
