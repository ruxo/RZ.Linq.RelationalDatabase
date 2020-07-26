using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
        public static Option<DotnetOperatorName> Parse(string operatorName) => NameMapper.Get(operatorName);

        static readonly ImmutableDictionary<string, DotnetOperatorName> NameMapper =
            (from DotnetOperatorName value in Enum.GetValues(typeof(DotnetOperatorName))
             select KeyValuePair.Create($"op_{value}", value)
            ).ToImmutableDictionary();
    }
}