using System.Collections.Generic;
using Tanakh.Api.Csv;

namespace Tanakh.Tests
{
    public class CsvWriterTests
    {
        [Fact]
        public void Write_Produces_Header_And_Row()
        {
            string csv = CsvWriter.Write(
                ["id", "name"],
                new List<IReadOnlyList<string?>> { new List<string?> { "1", "Alice" } });

            Assert.Equal("id,name\r\n1,Alice\r\n", csv);
        }

        [Theory]
        [InlineData("a,b", "\"a,b\"")]
        [InlineData("a\"b", "\"a\"\"b\"")]
        [InlineData("a\nb", "\"a\nb\"")]
        [InlineData("plain", "plain")]
        public void Write_Escapes_Values_Needing_Quoting(string input, string expectedEscaped)
        {
            string csv = CsvWriter.Write(
                ["value"],
                new List<IReadOnlyList<string?>> { new List<string?> { input } });

            Assert.Equal($"value\r\n{expectedEscaped}\r\n", csv);
        }

        [Fact]
        public void Write_Treats_Null_Value_As_Empty()
        {
            string csv = CsvWriter.Write(
                ["value"],
                new List<IReadOnlyList<string?>> { new List<string?> { null } });

            Assert.Equal("value\r\n\r\n", csv);
        }
    }
}
