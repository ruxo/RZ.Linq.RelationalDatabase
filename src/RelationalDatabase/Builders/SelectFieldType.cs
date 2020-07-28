using System;
using System.Linq.Expressions;
using System.Reflection;
using LanguageExt;
using RZ.Foundation.Extensions;
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

    public static class SelectFieldTypeExtension
    {
        public static SelectFieldType GetFieldType(this Expression expression, Func<Type,Option<TableAlias>> tableSolver) =>
            expression switch
            {
                ParameterExpression p => TableField.New(tableSolver(p.Type).Get()),
                MemberExpression node => GetProperty(node.Member)
                                         .Bind(p => tableSolver(p.PropertyType).Map(t => (SelectFieldType) TableField.New(t)))
                                         .OrElse(() => tableSolver(node.Member.DeclaringType).Map(t => (SelectFieldType) ColumnField.New(t, node.Member.Name)))
                                         .IfNone(() => (SelectFieldType) Invalid.New()),
                UnaryExpression u => u.Operand.GetFieldType(tableSolver),
                _ => throw new NotSupportedException()
            };

        static Option<PropertyInfo> GetProperty(MemberInfo member) => member is PropertyInfo p ? Some(p) : None;
    }
}