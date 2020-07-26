using System.Linq;
using System.Linq.Expressions;
using RZ.Linq.RelationalDatabase.Dialects;

namespace RZ.Linq.RelationalDatabase
{
    public sealed class SqlTextQueryable<T> : SqlTextQueryableBase<T>
    {
        public SqlTextQueryable(SqlDialect dialect) : this(dialect, null, null) { }

        SqlTextQueryable(SqlDialect dialect, SqlLinqBuilder? builder, Expression? expression) : base(dialect, builder, expression) { }

        protected override IOrderedQueryable<TEntity> CreateSelf<TEntity>(SqlDialect dialect, SqlLinqBuilder builder, Expression expression) =>
            new SqlTextQueryable<TEntity>(dialect, builder, expression);
    }
}