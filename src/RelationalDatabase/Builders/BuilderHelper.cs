using System;
using System.Linq.Expressions;

namespace RZ.Linq.RelationalDatabase.Builders
{
    public static class BuilderHelper
    {
        public static T Unwrap<T>(this Expression expression) => (T) ((ConstantExpression) expression).Value;

        public static T Evaluate<T>(this Expression expression) => Expression.Lambda<Func<T>>(expression).Compile()();
    }
}