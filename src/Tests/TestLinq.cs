using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using RZ.Linq.RelationalDatabase.Dialects;
using Xunit.Abstractions;

namespace RZ.Linq.RelationalDatabase.Tests
{
    public interface ISqlGenerator
    {
        string GetQueryString();
    }
    public interface IQueryableEngine
    {
        IOrderedQueryable<TEntity> Apply<TEntity>(MethodCallExpression expression);
    }
    public sealed class TestLinq<T> : IOrderedQueryable<T>, IQueryableEngine, ISqlGenerator
    {
        readonly ITestOutputHelper output;
        readonly SqlDialect dialect;
        readonly TestQueryProvider provider;
        readonly SqlLinqBuilder builder;

        public TestLinq(ITestOutputHelper output, SqlDialect dialect) : this(output, dialect, SqlLinqBuilder.Create(typeof(T))) { }

        TestLinq(ITestOutputHelper output, SqlDialect dialect, SqlLinqBuilder builder, Expression? expression = null) {
            this.output = output;
            this.dialect = dialect;
            this.builder = builder;
            ElementType = typeof(T);
            provider = new TestQueryProvider(this);
            Expression = expression ?? Expression.Constant(this);
        }

        public IOrderedQueryable<TEntity> Apply<TEntity>(MethodCallExpression expression) {
            var newBuilder = expression.Method.Name switch
            {
                "Join" => builder.BuildJoin(expression),
                "Select" => builder.Select((UnaryExpression) expression.Arguments[1]),
                "Where" => builder.BuildWhere((UnaryExpression) expression.Arguments[1], dialect),
                _ => throw new NotSupportedException($"Not support {expression.Method.Name}")
            };
            return new TestLinq<TEntity>(output, dialect, newBuilder, expression);
        }

        public string GetQueryString() => dialect.Build(builder);

        public IEnumerator<T> GetEnumerator() => Enumerable.Empty<T>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public Type ElementType { get; }

        public Expression Expression { get; }
        public IQueryProvider Provider => provider;
    }

    public sealed class TestQueryProvider : IQueryProvider
    {
        readonly IQueryableEngine queryEngine;

        public TestQueryProvider(IQueryableEngine queryEngine) {
            this.queryEngine = queryEngine;
        }
        public IQueryable CreateQuery(Expression expression) {
            throw new NotImplementedException();
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => queryEngine.Apply<TElement>((MethodCallExpression) expression);

        public object Execute(Expression expression) {
            throw new NotImplementedException();
        }

        public TResult Execute<TResult>(Expression expression) {
            throw new NotImplementedException();
        }
    }
}