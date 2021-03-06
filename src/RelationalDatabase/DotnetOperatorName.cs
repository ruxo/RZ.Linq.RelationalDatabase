using System.Collections.Immutable;
using System.Linq.Expressions;
using LanguageExt;
using RZ.Foundation.Extensions;

namespace RZ.Linq.RelationalDatabase
{
    public enum DotnetOperatorName
    {
        Addition,
        BitwiseAnd,
        BitwiseOr,
        Decrement,
        Division,
        Equality,
        ExclusiveOr,
        Explicit,
        False,
        Implicit,
        Increment,
        Inequality,
        LeftShift,
        LessThan,
        LessThanOrEqual,
        LogicalAnd,
        LogicalOr,
        LogicalNot,
        GreaterThan,
        GreaterThanOrEqual,
        Modulus,
        Multiply,
        OnesComplement,
        RightShift,
        Subtraction,
        True,
        UnaryNegation,
        UnaryPlus
    }

    public static class DotnetOperator
    {
        public static Option<DotnetOperatorName> Parse(ExpressionType expressionType) => ExpressionTypeMapper.Get(expressionType);

        static readonly ImmutableDictionary<ExpressionType, DotnetOperatorName> ExpressionTypeMapper = new[]
        {
            (ExpressionType.Add, DotnetOperatorName.Addition),
            (ExpressionType.And, DotnetOperatorName.BitwiseAnd),
            (ExpressionType.AndAlso, DotnetOperatorName.LogicalAnd),
            (ExpressionType.Decrement, DotnetOperatorName.Decrement),
            (ExpressionType.Divide, DotnetOperatorName.Division),
            (ExpressionType.Equal, DotnetOperatorName.Equality),
            (ExpressionType.ExclusiveOr, DotnetOperatorName.ExclusiveOr),
            (ExpressionType.IsFalse, DotnetOperatorName.False),
            (ExpressionType.Increment, DotnetOperatorName.Increment),
            (ExpressionType.NotEqual, DotnetOperatorName.Inequality),
            (ExpressionType.LeftShift, DotnetOperatorName.LeftShift),
            (ExpressionType.LessThan, DotnetOperatorName.LessThan),
            (ExpressionType.LessThanOrEqual, DotnetOperatorName.LessThanOrEqual),
            (ExpressionType.Not, DotnetOperatorName.LogicalNot),
            (ExpressionType.GreaterThan, DotnetOperatorName.GreaterThan),
            (ExpressionType.GreaterThanOrEqual, DotnetOperatorName.GreaterThanOrEqual),
            (ExpressionType.Modulo, DotnetOperatorName.Modulus),
            (ExpressionType.Multiply, DotnetOperatorName.Multiply),
            (ExpressionType.OnesComplement, DotnetOperatorName.OnesComplement),
            (ExpressionType.Or, DotnetOperatorName.BitwiseOr),
            (ExpressionType.OrElse, DotnetOperatorName.LogicalOr),
            (ExpressionType.RightShift, DotnetOperatorName.RightShift),
            (ExpressionType.Subtract, DotnetOperatorName.Subtraction),
            (ExpressionType.IsTrue, DotnetOperatorName.True),
            (ExpressionType.Negate, DotnetOperatorName.UnaryNegation),
            (ExpressionType.UnaryPlus, DotnetOperatorName.UnaryPlus)
        }.ToImmutableDictionary(i => i.Item1, i => i.Item2);
    }
}