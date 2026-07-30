namespace Tanakh.Infrastructure.Options
{
    public class TanakhDataOptions
    {
        public const string SectionName = "TanakhData";

        // Legitimately unset in the common case - CacheProvider falls back to
        // ContentRootPath/Data when this isn't overridden.
        public string? DataDirectory { get; set; }
    }
}
