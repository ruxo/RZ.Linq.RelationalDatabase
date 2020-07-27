using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using RZ.Linq.RelationalDatabase.Dialects;

namespace RZ.Linq.RelationalDatabase
{
    public interface ISqlGenerator
    {
        SqlDialect Dialect { get; }
        SqlLinqBuilder Builder { get; }

        string GetSelectString() => Dialect.BuildSelectStatement(Builder);
    }
    interface IQueryableEngine
    {
        IOrderedQueryable<TEntity> Apply<TEntity>(MethodCallExpression expression);
        TResult Execute<TResult>(MethodCallExpression expression);
    }
    public abstract class SqlTextQueryableBase<T> : IOrderedQueryable<T>, IQueryableEngine, ISqlGenerator
    {
        #region ctors

        protected SqlTextQueryableBase(SqlDialect dialect, SqlLinqBuilder? builder = null, Expression? expression = null) {
            Dialect = dialect;
            Builder = builder ?? SqlLinqBuilder.Create(typeof(T));
            ElementType = typeof(T);
            Provider = new InternalQueryProvider(this);
            Expression = expression ?? Expression.Constant(this);
        }

        #endregion

        public SqlDialect Dialect { get; }
        public SqlLinqBuilder Builder { get; }

        public Type ElementType { get; }
        public Expression Expression { get; }
        public IQueryProvider Provider { get; }

        protected abstract IOrderedQueryable<TEntity> CreateSelf<TEntity>(SqlDialect dialect, SqlLinqBuilder builder, Expression expression);

        public IOrderedQueryable<TEntity> Apply<TEntity>(MethodCallExpression expression) {
            var newBuilder = expression.Method.Name switch
            {
                "Join" => Builder.BuildJoin(expression),
                "Select" => Builder.Select((UnaryExpression) expression.Arguments[1]),
                "Where" => Builder.BuildWhere((UnaryExpression) expression.Arguments[1], Dialect),
                "Take" => Builder.WithTake(GetConstantValue<int>(expression.Arguments[1])),
                "Skip" => Builder.WithSkip(GetConstantValue<int>(expression.Arguments[1])),
                _ => throw new NotSupportedException($"Not support {expression.Method.Name}")
            };
            return CreateSelf<TEntity>(Dialect, newBuilder, expression);
        }

        public virtual TResult Execute<TResult>(MethodCallExpression expression) {
            throw new NotSupportedException($"Not support method {expression.Method.Name}!");
        }

        protected TExpected GetConstantValue<TExpected>(Expression expression) => (TExpected) ((ConstantExpression) expression).Value;

        #region Enumerators

        public virtual IEnumerator<T> GetEnumerator() => Enumerable.Empty<T>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }

    sealed class InternalQueryProvider : IQueryProvider
    {
        readonly IQueryableEngine queryEngine;

        public InternalQueryProvider(IQueryableEngine queryEngine) {
            this.queryEngine = queryEngine;
        }

        public IQueryable CreateQuery(Expression expression) {
            throw new NotImplementedException();
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => queryEngine.Apply<TElement>((MethodCallExpression) expression);

        public object Execute(Expression expression) {
            throw new NotImplementedException();
        }

        public TResult Execute<TResult>(Expression expression) => queryEngine.Execute<TResult>((MethodCallExpression) expression);
    }
}