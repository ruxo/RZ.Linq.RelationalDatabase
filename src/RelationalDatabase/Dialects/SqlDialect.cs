using System;
using System.Collections.Immutable;
using System.Globalization;
using RZ.Foundation.Extensions;

namespace RZ.Linq.RelationalDatabase.Dialects
{
    public abstract class SqlDialect
    {
        public abstract string BuildSelectStatement(SqlLinqBuilder builder);

        public virtual string GetLiteral(object? value) =>
            LiteralMaker.Get(value?.GetType() ?? typeof(SqlDialect))
                        .Map(f => f(value!))
                        .GetOrThrow(() => new NotSupportedException($"Literal for {value} ({value!.GetType().Name}) is not supported."));

        static string ToString(object v) => v.ToString();
        static string StandardStringQuote(string s) => $"'{s.Replace("'", "''")}'";

        static readonly ImmutableDictionary<Type, Func<object, string>> LiteralMaker = new (Type,Func<object,string>)[]
        {
            (typeof(string), s => StandardStringQuote((string)s)),
            (typeof(char), c => StandardStringQuote(c.ToString())),
            (typeof(byte), ToString),
            (typeof(short), ToString),
            (typeof(int), ToString),
            (typeof(long), ToString),
            (typeof(sbyte), ToString),
            (typeof(ushort), ToString),
            (typeof(uint), ToString),
            (typeof(ulong), ToString),
            (typeof(float), f => ((float)f).ToString(CultureInfo.InvariantCulture)),
            (typeof(double), d => ((double)d).ToString(CultureInfo.InvariantCulture)),
            (typeof(decimal), d => ((decimal)d).ToString(CultureInfo.InvariantCulture)),
            (typeof(bool), b => (bool)b? "TRUE" : "FALSE"),
            (typeof(SqlDialect), _ => "NULL"),    // sorry, I'm lazy to define a new type 😅
        }.ToImmutableDictionary(i => i.Item1, i => i.Item2);

        #region Binary expression

        public virtual string BuildBinaryExpression(DotnetOperatorName @operator, string leftValue, string rightValue) =>
            $"{leftValue}{StandardSqlBinaryOperators[@operator]}{rightValue}";

        static readonly ImmutableDictionary<DotnetOperatorName, string> StandardSqlBinaryOperators = new[]
        {
            (DotnetOperatorName.Addition, "+"),
            (DotnetOperatorName.Subtraction, "-"),
            (DotnetOperatorName.Multiply, "*"),
            (DotnetOperatorName.Division, "/"),

            (DotnetOperatorName.BitwiseAnd, "&"),
            (DotnetOperatorName.BitwiseOr, "|"),
            (DotnetOperatorName.ExclusiveOr, "^"),

            (DotnetOperatorName.Equality, "="),
            (DotnetOperatorName.Inequality, "<>"),
            (DotnetOperatorName.LessThan, "<"),
            (DotnetOperatorName.LessThanOrEqual, "<="),
            (DotnetOperatorName.GreaterThan, ">"),
            (DotnetOperatorName.GreaterThanOrEqual, ">="),
        }.ToImmutableDictionary(i => i.Item1, i => i.Item2);

        #endregion
    }
}