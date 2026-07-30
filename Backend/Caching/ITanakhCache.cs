using System.Diagnostics.CodeAnalysis;

namespace Tanakh.Caching
{
    public interface ITanakhCache
    {
        bool TryGet<T>(string key, [NotNullWhen(true)] out T? value) where T : class;

        void Set<T>(string key, T value) where T : class;
    }
}
