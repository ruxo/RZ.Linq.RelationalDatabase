using System;
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
        public void ToSelectHelper() {
            var result = (from p in SqlLiteLinq<PersonPoco>() select p.Name).ToSelectStatement();

            result.Should().Be("SELECT Name FROM PersonPoco");
        }

        [Fact]
        public void QueryAll() {
            var linq = SqlLiteLinq<PersonPoco>();
            var result = (ISqlGenerator) from i in linq select i;
            result.GetSelectString().Should().Be("SELECT Id,Name,IsActive,Created FROM PersonPoco");
        }

        [Fact]
        public void QueryAllInLine() {
            var result = (ISqlGenerator) from i in SqlLiteLinq<PersonPoco>() select i;
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
        public void QueryJoinAndSelectSingleTableInLine() {
            var result = (ISqlGenerator) from i in SqlLiteLinq<PersonPoco>()
                                         join o in SqlLiteLinq<Order>() on i.Id equals o.OwnerId
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
            var result = (ISqlGenerator) from i in SqlLiteLinq<PersonPoco>()
                                         join o in SqlLiteLinq<Order>() on i.Id equals o.OwnerId
                                         select new {i.Id, i.Name, o.OrderId, o.PaidAmount, o.TargetDate };
            result.GetSelectString()
                  .Should()
                  .Be("SELECT i.Id,i.Name,o.OrderId,o.PaidAmount,o.TargetDate FROM PersonPoco i INNER JOIN Order o ON i.Id=o.OwnerId");
        }

        #region Where cases

        [Fact]
        public void QueryWhereSingleField() {
            var linq = SqlLiteLinq<PersonPoco>();
            var result = (ISqlGenerator) from i in linq
                                         where i.Name == "Rux"
                                         select i;
            result.GetSelectString().Should().Be("SELECT Id,Name,IsActive,Created FROM PersonPoco WHERE Name='Rux'");
        }

        [Fact]
        public void QueryJoinAndWhere() {
            var result = (ISqlGenerator) from p in SqlLiteLinq<PersonPoco>()
                                         join o in SqlLiteLinq<Order>() on p.Id equals o.OwnerId
                                         where p.Id == 1 && p.IsActive
                                         select p;
            result.GetSelectString()
                  .Should()
                  .Be("SELECT p.Id,p.Name,p.IsActive,p.Created FROM PersonPoco p INNER JOIN Order o ON p.Id=o.OwnerId WHERE p.Id=1 AND p.IsActive");
        }

        [Fact]
        public void QueryTripleJoinsAndWhere() {
            var result = (ISqlGenerator) from p in SqlLiteLinq<PersonPoco>()
                                         join o in SqlLiteLinq<Order>() on p.Id equals o.OwnerId
                                         join d in SqlLiteLinq<OrderLineItem>() on o.OrderId equals d.OrderId
                                         where p.IsActive && o.OwnerId==1 && d.Quantity > 1
                                         select d.Id;
            result.GetSelectString()
                  .Should()
                  .Be("SELECT d.Id FROM PersonPoco p INNER JOIN Order o ON p.Id=o.OwnerId INNER JOIN order_detail d ON o.OrderId=d.OrderId "+
                      "WHERE p.IsActive AND o.OwnerId=1 AND d.Quantity>1");
        }

        [Fact]
        public void WhereIsNull() {
            (from p in SqlLiteLinq<PersonPoco>()
             where p.Name == null
             select p.Id).ToSelectStatement()
                         .Should()
                         .Be("SELECT Id FROM PersonPoco WHERE Name IS NULL");
        }

        [Fact]
        public void WhereIsNotNull() {
            (from p in SqlLiteLinq<PersonPoco>()
             where p.Name != null
             select p.Id).ToSelectStatement()
                         .Should()
                         .Be("SELECT Id FROM PersonPoco WHERE Name IS NOT NULL");
        }

        [Fact]
        public void WhereNot() {
            (from p in SqlLiteLinq<PersonPoco>()
             where !p.IsActive
             select p.Id).ToSelectStatement()
                         .Should()
                         .Be("SELECT Id FROM PersonPoco WHERE NOT (IsActive)");
        }

        #endregion

        [Fact]
        public void SupportOrderBy() {
            (from p in SqlLiteLinq<PersonPoco>()
             orderby p.Name, p.Id descending, p.IsActive
             select p).ToSelectStatement()
                      .Should()
                      .Be("SELECT Id,Name,IsActive,Created FROM PersonPoco ORDER BY Name,Id DESC,IsActive");
        }

        [Fact]
        public void SupportDistinct() {
            (from p in SqlLiteLinq<PersonPoco>()
             select p).Distinct()
                      .ToSelectStatement()
                      .Should()
                      .Be("SELECT DISTINCT Id,Name,IsActive,Created FROM PersonPoco");
        }

        #region Take & Skip

        [Fact]
        public void QueryTake() {
            var result = (ISqlGenerator) (from p in SqlLiteLinq<PersonPoco>()
                                          select p).Take(5);
            result.GetSelectString().Should().Be("SELECT Id,Name,IsActive,Created FROM PersonPoco LIMIT 5");
        }

        [Fact]
        public void QueryTakeFront() {
            var result = (ISqlGenerator) from p in SqlLiteLinq<PersonPoco>().Take(5)
                                         select p;
            result.GetSelectString().Should().Be("SELECT Id,Name,IsActive,Created FROM PersonPoco LIMIT 5");
        }

        [Fact]
        public void QuerySkip() {
            var result = (ISqlGenerator) (from p in SqlLiteLinq<PersonPoco>()
                                          select p).Skip(5);
            result.GetSelectString().Should().Be("SELECT Id,Name,IsActive,Created FROM PersonPoco OFFSET 5");
        }

        [Fact]
        public void QueryTakeSkip() {
            var result = (ISqlGenerator) (from p in SqlLiteLinq<PersonPoco>()
                                          select p).Take(10).Skip(5);
            result.GetSelectString().Should().Be("SELECT Id,Name,IsActive,Created FROM PersonPoco LIMIT 10 OFFSET 5");
        }

        #endregion

        #region COUNT

        [Fact]
        public void QueryCount() {
            Func<int> call = () => (from p in SqlLiteLinq<PersonPoco>() select p).Count();

            call.Should().Throw<NotSupportedException>("Using special Count method instead!");
        }

        [Fact]
        public void QueryCountWithTable() {
            var result = (ISqlGenerator) from p in SqlLiteLinq<PersonPoco>() select CommonSql.Count(p);

            result.GetSelectString().Should().Be("SELECT COUNT(*) FROM PersonPoco");
        }

        [Fact]
        public void QueryCountWithTableField() {
            var result = (ISqlGenerator) from p in SqlLiteLinq<PersonPoco>() select CommonSql.Count(p.Id);

            result.GetSelectString().Should().Be("SELECT COUNT(Id) FROM PersonPoco");
        }

        #endregion

        #region Contains (IN)

        [Fact]
        public void QueryWithStringContains() {
            var result = (from p in SqlLiteLinq<Product>()
                          where new[] {"Milk", "TV"}.Contains(p.Name)
                          orderby p.Name
                          select p.Id
                         ).ToSelectStatement();
            result.Should().Be("SELECT Id FROM Product WHERE Name IN ('Milk','TV') ORDER BY Name");
        }

        [Fact]
        public void QueryWithIntContains() {
            (from p in SqlLiteLinq<PersonPoco>()
             where new[] {1, 2, 3}.Contains(p.Id)
             select p.Id).ToSelectStatement()
                         .Should()
                         .Be("SELECT Id FROM PersonPoco WHERE Id IN (1,2,3)");
        }

        [Fact]
        public void QueryWithIntNotContains() {
            (from p in SqlLiteLinq<PersonPoco>()
             where !new[] {1, 2, 3}.Contains(p.Id)
             select p.Id).ToSelectStatement()
                         .Should()
                         .Be("SELECT Id FROM PersonPoco WHERE Id NOT IN (1,2,3)");
        }

        #endregion

        #region LIKE

        [Fact]
        public void WhereLike() {
            (from p in SqlLiteLinq<PersonPoco>()
             where p.Name.Contains("Rux's Name")
             select p.Id).ToSelectStatement()
                         .Should()
                         .Be("SELECT Id FROM PersonPoco WHERE Name LIKE '%Rux''s Name%'");
        }

        [Fact]
        public void WhereStartsWith() {
            (from p in SqlLiteLinq<PersonPoco>()
             where p.Name.StartsWith("Rux")
             select p.Id).ToSelectStatement()
                         .Should()
                         .Be("SELECT Id FROM PersonPoco WHERE Name LIKE 'Rux%'");
        }

        [Fact]
        public void WhereEndsWith() {
            (from p in SqlLiteLinq<PersonPoco>()
             where p.Name.EndsWith("Rux")
             select p.Id).ToSelectStatement()
                         .Should()
                         .Be("SELECT Id FROM PersonPoco WHERE Name LIKE '%Rux'");
        }

        #endregion
    }
}