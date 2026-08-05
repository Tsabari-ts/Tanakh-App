namespace Tanakh.Api.Model
{
    public class AdminSetMaintenanceRequest
    {
        public bool Enabled { get; set; }

        public string? Message { get; set; }
    }
}
