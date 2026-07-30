using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Linq;
using System.Text;

namespace Tanakh.Infrastructure.Data.Conversions
{
    // Maps PascalCase C# enum members to lower_snake_case text values, so
    // the stored strings match the CHECK constraint literals written in
    // each IEntityTypeConfiguration<T> (e.g. PendingConfirmation <->
    // "pending_confirmation"). Shared by every text+CHECK enum in this
    // phase - see docs/database.md for why enums are text, not native
    // Postgres enums.
    public static class SnakeCaseEnumConverter<TEnum> where TEnum : struct, Enum
    {
        public static ValueConverter<TEnum, string> Instance { get; } = new(
            value => ToSnakeCase(value.ToString()),
            value => ParseSnakeCase(value));

        public static ValueConverter<TEnum?, string?> NullableInstance { get; } = new(
            value => value.HasValue ? ToSnakeCase(value.Value.ToString()) : null,
            value => value == null ? null : ParseSnakeCase(value));

        private static string ToSnakeCase(string value)
        {
            StringBuilder builder = new();
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (i > 0 && char.IsUpper(c))
                {
                    builder.Append('_');
                }
                builder.Append(char.ToLowerInvariant(c));
            }
            return builder.ToString();
        }

        private static TEnum ParseSnakeCase(string value)
        {
            string pascalCase = string.Concat(value.Split('_').Select(Capitalize));
            return Enum.Parse<TEnum>(pascalCase);
        }

        private static string Capitalize(string part) =>
            part.Length == 0 ? part : char.ToUpperInvariant(part[0]) + part[1..];
    }
}
