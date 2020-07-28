using System;
using System.Linq;
using System.Linq.Expressions;
using LanguageExt;

namespace RZ.Linq.RelationalDatabase.Builders
{
    public static class BuilderHelper
    {
        public static T Unwrap<T>(this Expression expression) => (T) ((ConstantExpression) expression).Value;

        public static T Evaluate<T>(this Expression expression) => Expression.Lambda<Func<T>>(expression).Compile()();

        public static string GetFieldName(this Expression expression, Func<Type,Option<TableAlias>> tableSolver, string? tableFieldName = null) =>
            expression.GetFieldType(tableSolver) switch
            {
                TableField t => t.Alias.FieldName(tableFieldName ?? throw new NotSupportedException($"Not support table expression {expression}")),
                ColumnField c => c.Alias.FieldName(c.Alias.Table.Columns.Single(col => col.ColumnName == c.Name).Name),
                _ => throw new NotSupportedException($"Not support expression: {expression}")
            };
    }
}