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
    [Union]
    public interface SelectFieldType
    {
        // ReSharper disable UnusedMemberInSuper.Global
        SelectFieldType Invalid();
        SelectFieldType TableField(TableAlias alias);
        SelectFieldType ColumnField(TableAlias alias, string name);
        // ReSharper restore UnusedMemberInSuper.Global
    }

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
            Expression.Constant(GetFieldType(node) switch
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

            return Expression.Constant(GetFieldType(node.Arguments[0]) switch
            {
                TableField t => Enumerable.Repeat($"COUNT({t.Alias.FieldName("*")})", 1),
                ColumnField c => Enumerable.Repeat($"COUNT({c.Alias.FieldName(c.Name)})", 1),
                _ => throw new NotSupportedException($"Not support expression: {node}")
            });
        }

        SelectFieldType GetFieldType(Expression expression) =>
            expression switch
            {
                ParameterExpression p => TableField.New(builder.GetTable(p.Type)),
                MemberExpression node => GetProperty(node.Member)
                                         .Bind(p => builder.TryGetTable(p.PropertyType).Map(t => (SelectFieldType) TableField.New(t)))
                                         .OrElse(() => builder.TryGetTable(node.Member.DeclaringType)
                                                              .Map(t => (SelectFieldType) ColumnField.New(t, node.Member.Name)))
                                         .IfNone(() => (SelectFieldType) Invalid.New()),
                UnaryExpression u => GetFieldType(u.Operand),
                _ => throw new NotSupportedException()
            };

        static IEnumerable<string> GetField(TableAlias tableAlias, string name) {
            var fieldName = tableAlias.FieldName(tableAlias.Table.Columns.Single(c => c.ColumnName == name).Name);
            return Enumerable.Repeat(fieldName, 1);
        }

        static Option<PropertyInfo> GetProperty(MemberInfo member) => member is PropertyInfo p ? Some(p) : None;

        static IEnumerable<string> Unwrap(Expression expression) => (IEnumerable<string>) ((ConstantExpression) expression).Value;

        static IEnumerable<string> ListFields(TableAlias tableAlias) => tableAlias.Table.Columns.Select(c => tableAlias.FieldName(c.Name));
    }
}