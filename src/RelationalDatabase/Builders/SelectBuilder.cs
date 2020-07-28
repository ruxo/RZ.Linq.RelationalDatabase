using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LanguageExt;
using RZ.Foundation.Extensions;
using RZ.Linq.RelationalDatabase.Dialects;
using static LanguageExt.Prelude;

namespace RZ.Linq.RelationalDatabase.Builders
{
    sealed class SelectBuilder : ExpressionVisitor
    {
        readonly SqlLinqBuilder builder;
        readonly SqlDialect dialect;

        public SelectBuilder(SqlLinqBuilder builder, SqlDialect dialect) {
            this.builder = builder;
            this.dialect = dialect;
        }

        public IEnumerable<string> Parse(Expression expression) => Unwrap(Visit(expression));

        protected override Expression VisitMember(MemberExpression node) =>
            Expression.Constant(node.GetFieldType(builder.TryGetTable) switch
            {
                TableField t => ListFields(t.Alias),
                ColumnField c => GetField(c.Alias, c.Name),
                _ => throw new NotSupportedException($"Not support expression: {node}")
            });

        protected override Expression VisitNew(NewExpression node) => Expression.Constant(node.Arguments.SelectMany(expr => Unwrap(Visit(expr))));

        protected override Expression VisitParameter(ParameterExpression node) => Expression.Constant(ListFields(builder.GetTable(node.Type)));

        protected override Expression VisitMethodCall(MethodCallExpression node) {
            if (node.Method.Name != nameof(CommonSql.Count) || node.Method.DeclaringType != typeof(CommonSql))
                throw new NotSupportedException($"Not support method call: {node}");

            return Expression.Constant(Enumerable.Repeat($"COUNT({node.Arguments[0].GetFieldName(builder.TryGetTable, "*")})", 1));
        }

        static IEnumerable<string> GetField(TableAlias tableAlias, string name) {
            var fieldName = tableAlias.FieldName(tableAlias.Table.Columns.Single(c => c.ColumnName == name).Name);
            return Enumerable.Repeat(fieldName, 1);
        }

        static IEnumerable<string> Unwrap(Expression expression) => expression.Unwrap<IEnumerable<string>>();

        static IEnumerable<string> ListFields(TableAlias tableAlias) => tableAlias.Table.Columns.Select(c => tableAlias.FieldName(c.Name));
    }
}