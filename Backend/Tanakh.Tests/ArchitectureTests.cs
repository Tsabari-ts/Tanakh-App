using System.Linq;
using System.Reflection;
using NetArchTest.Rules;
using Tanakh.Domain;

namespace Tanakh.Tests
{
    public class ArchitectureTests
    {
        private static readonly Assembly DomainAssembly = typeof(IEmailSender).Assembly;

        [Fact]
        public void Domain_Should_Not_Depend_On_Infrastructure_Api_Or_AspNetCore()
        {
            TestResult result = Types.InAssembly(DomainAssembly)
                .ShouldNot()
                .HaveDependencyOnAny(
                    "Tanakh.Infrastructure",
                    "Tanakh.Api",
                    "Microsoft.AspNetCore",
                    "Microsoft.EntityFrameworkCore")
                .GetResult();

            Assert.True(result.IsSuccessful, FailingTypesMessage(result));
        }

        private static string FailingTypesMessage(TestResult result)
        {
            if (result.IsSuccessful || result.FailingTypes is null)
            {
                return string.Empty;
            }

            return "Domain types with forbidden dependencies: "
                + string.Join(", ", result.FailingTypes.Select(t => t.FullName));
        }
    }
}
