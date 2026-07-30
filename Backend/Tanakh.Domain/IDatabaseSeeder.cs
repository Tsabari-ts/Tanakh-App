using System.Threading;
using System.Threading.Tasks;

namespace Tanakh.Domain
{
    public interface IDatabaseSeeder
    {
        // Rolls the schema all the way down and back up to latest - the
        // practical equivalent of "drop, recreate, migrate" without
        // requiring DROP DATABASE-level privileges (migrations_user, per
        // D-12, only owns the schema, not the database itself).
        Task ResetSchemaAsync(CancellationToken cancellationToken = default);

        // Idempotent - does nothing if subscribers already exist, so
        // running --seed twice doesn't duplicate rows.
        Task SeedAsync(CancellationToken cancellationToken = default);
    }
}
