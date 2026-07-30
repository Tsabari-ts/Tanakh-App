namespace Tanakh.Domain.Entities
{
    public enum EmailEventType
    {
        Delivered,
        Bounce,
        Complaint,
        // Only ever written if open-tracking is explicitly enabled -
        // default is not to track opens. See docs/database.md privacy notes.
        Open
    }
}
