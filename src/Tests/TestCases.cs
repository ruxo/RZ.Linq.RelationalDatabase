using System.Linq;
using FluentAssertions;
using RZ.Linq.RelationalDatabase.Tests.Models;
using Xunit;
using Xunit.Abstractions;

namespace RZ.Linq.RelationalDatabase.Tests
{
    public sealed class TestCases
    {
        readonly ITestOutputHelper output;
        public TestCases(ITestOutputHelper output) {
            this.output = output;
        }

        [Fact]
        public void QueryAllWithObject() {
            var linq = new TestLinq<PersonPoco>(output);
            linq.GetQueryString().Should().Be("SELECT * FROM PersonPoco");
        }

        [Fact]
        public void QueryAll() {
            var linq = new TestLinq<PersonPoco>(output);
            var result = (TestLinq<PersonPoco>) from i in linq select i;
            result.GetQueryString().Should().Be("SELECT Id,Name,IsActive,Created FROM PersonPoco");
        }

        [Fact]
        public void QueryModel() {
            var linq = new TestLinq<PersonPoco>(output);
            var order = new TestLinq<Order>(output);
            var _ = (from i in linq
                          join o in order on i.Id equals o.OwnerId
                          where i.IsActive
                          select new { i.Id, i.Name }).ToArray();
        }
    }
}