using System.Linq;
using FluentAssertions;
using RZ.Linq.RelationalDatabase.Dialects;
using RZ.Linq.RelationalDatabase.Tests.Models;
using Xunit;

namespace RZ.Linq.RelationalDatabase.Tests
{
    public sealed class TestCases
    {
        static SqlTextQueryable<T> SqlLiteLinq<T>() => new SqlTextQueryable<T>(new SqlLiteDialect());

        [Fact]
        public void QueryAllWithObject() {
            var linq = (ISqlGenerator) SqlLiteLinq<PersonPoco>();
            linq.GetSelectString().Should().Be("SELECT Id,Name,IsActive,Created FROM PersonPoco");
        }

        [Fact]
        public void QueryAll() {
            var linq = SqlLiteLinq<PersonPoco>();
            var result = (ISqlGenerator) from i in linq select i;
            result.GetSelectString().Should().Be("SELECT Id,Name,IsActive,Created FROM PersonPoco");
        }

        [Fact]
        public void QuerySingleColumn() {
            var linq = SqlLiteLinq<PersonPoco>();
            var result = (ISqlGenerator) from i in linq select i.Name;
            result.GetSelectString().Should().Be("SELECT Name FROM PersonPoco");
        }

        [Fact]
        public void QueryAllWithCustomName() {
            var linq = SqlLiteLinq<Product>();
            var result = (ISqlGenerator) from i in linq select i;
            result.GetSelectString().Should().Be("SELECT Id,Name,created_at FROM Product");
        }

        [Fact]
        public void QuerySingleColumnWithCustomName() {
            var linq = SqlLiteLinq<Product>();
            var result = (ISqlGenerator) from i in linq select i.Created;
            result.GetSelectString().Should().Be("SELECT created_at FROM Product");
        }

        [Fact]
        public void QueryAllWithCustomTableName() {
            var linq = SqlLiteLinq<OrderLineItem>();
            var result = (ISqlGenerator) from i in linq select i;
            result.GetSelectString().Should().Be("SELECT Id,OrderId,ProductId,Quantity,Unit FROM order_detail");
        }

        [Fact]
        public void QueryAllWithMultipleColumns() {
            var linq = SqlLiteLinq<Product>();
            var result = (ISqlGenerator) from i in linq select new {i.Name, i.Created};
            result.GetSelectString().Should().Be("SELECT Name,created_at FROM Product");
        }

        [Fact]
        public void QueryJoinAndSelectSingleTable() {
            var linq = SqlLiteLinq<PersonPoco>();
            var order = SqlLiteLinq<Order>();
            var result = (ISqlGenerator) from i in linq
                                         join o in order on i.Id equals o.OwnerId
                                         select i;
            result.GetSelectString()
                  .Should()
                  .Be("SELECT i.Id,i.Name,i.IsActive,i.Created FROM PersonPoco i INNER JOIN Order o ON i.Id=o.OwnerId");
        }

        [Fact]
        public void QueryTwoJoinAndSelectSingleTable() {
            var linq = SqlLiteLinq<PersonPoco>();
            var order = SqlLiteLinq<Order>();
            var lineItem = SqlLiteLinq<OrderLineItem>();
            var result = (ISqlGenerator) from i in linq
                                         join o in order on i.Id equals o.OwnerId
                                         join d in lineItem on o.OrderId equals d.OrderId
                                         select new { i.Name, d.ProductId };
            result.GetSelectString()
                  .Should()
                  .Be("SELECT i.Name,d.ProductId FROM PersonPoco i INNER JOIN Order o ON i.Id=o.OwnerId INNER JOIN order_detail d ON o.OrderId=d.OrderId");
        }

        [Fact]
        public void QueryJoinAndSelectMultipleColumns() {
            var linq = SqlLiteLinq<PersonPoco>();
            var order = SqlLiteLinq<Order>();
            var result = (ISqlGenerator) from i in linq
                                         join o in order on i.Id equals o.OwnerId
                                         select new {i.Id, i.Name, o.OrderId, o.PaidAmount, o.TargetDate };
            result.GetSelectString()
                  .Should()
                  .Be("SELECT i.Id,i.Name,o.OrderId,o.PaidAmount,o.TargetDate FROM PersonPoco i INNER JOIN Order o ON i.Id=o.OwnerId");
        }

        [Fact]
        public void QueryWhereSingleField() {
            var linq = SqlLiteLinq<PersonPoco>();
            var result = (ISqlGenerator) from i in linq
                                         where i.Name == "Rux"
                                         select i;
            result.GetSelectString().Should().Be("SELECT Id,Name,IsActive,Created FROM PersonPoco WHERE Name='Rux'");
        }
    }
}