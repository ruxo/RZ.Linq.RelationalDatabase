using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using RZ.Linq.RelationalDatabase.Dialects;
using Xunit.Abstractions;

namespace RZ.Linq.RelationalDatabase.Tests
{
    public interface IQueryableEngine
    {
        IOrderedQueryable<TEntity> Apply<TEntity>(MethodCallExpression expression);
    }
    public sealed class TestLinq<T> : IOrderedQueryable<T>, IQueryableEngine
    {
        readonly ITestOutputHelper output;
        readonly TestQueryProvider provider;
        readonly SqlLinqBuilder builder;

        public TestLinq(ITestOutputHelper output) : this(output, SqlLinqBuilder.Create(typeof(T))) { }

        TestLinq(ITestOutputHelper output, SqlLinqBuilder builder, Expression? expression = null) {
            this.output = output;
            this.builder = builder;
            ElementType = typeof(T);
            provider = new TestQueryProvider(this);
            Expression = expression ?? Expression.Constant(this);

            output.WriteLine("Create LINQ for type {0} with expression {1}", ElementType.Name, Expression);
            ElementType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                       .Select(p => p.Name)
                       .Concat(ElementType.GetFields(BindingFlags.Public | BindingFlags.Instance).Select(f => f.Name))
                       .Iter(name => output.WriteLine("\t{0}", name));
        }

        public IOrderedQueryable<TEntity> Apply<TEntity>(MethodCallExpression expression) {
            var newBuilder = expression.Method.Name switch
            {
                "Select" => builder.Select((UnaryExpression) expression.Arguments[1]),
                _ => builder
            };
            return new TestLinq<TEntity>(output, newBuilder, expression);
        }

        public string GetQueryString() => new SqlLiteDialect().Build(builder);

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