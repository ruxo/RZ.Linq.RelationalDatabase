using System;
using System.Collections.Immutable;
using System.Globalization;
using RZ.Foundation.Extensions;

namespace RZ.Linq.RelationalDatabase.Dialects
{
    public enum LikeWildCard
    {
        Left, Right, LeftAndRight
    }

    public abstract class SqlDialect
    {
        public abstract string BuildSelectStatement(SqlLinqBuilder builder);

        public virtual string GetLiteral(object? value) =>
            value switch
            {
                string s => StringLiteral(s),
                char c => StringLiteral(c.ToString()),
                _ => LiteralMaker.Get(value?.GetType() ?? typeof(SqlDialect))
                                 .Map(f => f(value!))
                                 .GetOrThrow(() => new NotSupportedException($"Literal for {value} ({value!.GetType().Name}) is not supported."))
            };

        public virtual string GetLikeText(LikeWildCard wildCard, string s) =>
            wildCard switch
            {
                LikeWildCard.Left => $"'%{EscapeString(s)}'",
                LikeWildCard.Right => $"'{EscapeString(s)}%'",
                LikeWildCard.LeftAndRight => $"'%{EscapeString(s)}%'",
                _ => throw new NotSupportedException($"Not support wildcard {wildCard}")
            };

        protected virtual string EscapeString(string s) => s.Replace("'", "''");
        protected virtual string StringLiteral(string s) => $"'{EscapeString(s)}'";

        static string ToString(object v) => v.ToString();
        static readonly ImmutableDictionary<Type, Func<object, string>> LiteralMaker = new (Type,Func<object,string>)[]
        {
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

        public virtual string BuildBinaryExpression(DotnetOperatorName @operator, string leftValue, string rightValue) {
            var opText = @operator switch
            {
                DotnetOperatorName.Equality when rightValue == "NULL" => " IS ",
                DotnetOperatorName.Inequality when rightValue == "NULL" => " IS NOT ",
                _ => StandardSqlBinaryOperators[@operator]
            };
            return $"{leftValue}{opText}{rightValue}";
        }

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

            (DotnetOperatorName.LogicalAnd, " AND "),
            (DotnetOperatorName.LogicalOr, " OR ")
        }.ToImmutableDictionary(i => i.Item1, i => i.Item2);

        #endregion
    }
}