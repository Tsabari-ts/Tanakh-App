using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tanakh.Domain
{
    public record MaintenanceStatus(bool Enabled, string? Message);

    public record BannerStatus(string Text, DateTimeOffset ExpiresAt);

    // Backs the two AppSetting singleton rows ("maintenance", "banner").
    // GetBannerAsync deliberately returns the raw stored value regardless
    // of expiry - the admin editor needs to see/extend an expired banner,
    // so expiry filtering happens at the public-facing read (SystemController),
    // not here.
    public interface IAppSettingsService
    {
        Task<MaintenanceStatus> GetMaintenanceAsync(CancellationToken cancellationToken = default);

        Task SetMaintenanceAsync(bool enabled, string? message, CancellationToken cancellationToken = default);

        Task<BannerStatus?> GetBannerAsync(CancellationToken cancellationToken = default);

        Task SetBannerAsync(string text, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);

        Task ClearBannerAsync(CancellationToken cancellationToken = default);
    }
}
