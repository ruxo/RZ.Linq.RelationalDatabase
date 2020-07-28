using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using RZ.Foundation.Extensions;
using RZ.Linq.RelationalDatabase.Dialects;

namespace RZ.Linq.RelationalDatabase.Builders
{
    sealed class WhereBuilder : ExpressionVisitor
    {
        readonly SqlLinqBuilder sqlBuilder;
        readonly SqlDialect dialect;

        public WhereBuilder(SqlLinqBuilder sqlBuilder, SqlDialect dialect) {
            this.sqlBuilder = sqlBuilder;
            this.dialect = dialect;
        }

        public string Parse(UnaryExpression expression) {
            var lambda = (LambdaExpression) expression.Operand;
            return EvaluateString(lambda.Body);
        }

        protected override Expression VisitBinary(BinaryExpression node) =>
            Expression.Constant(dialect.BuildBinaryExpression(
                  DotnetOperator.Parse(node.NodeType).GetOrThrow(() => new NotSupportedException($"Operator {node.NodeType} ({node.Method}) is not supported!")),
                  EvaluateString(node.Left),
                  EvaluateString(node.Right)));

        protected override Expression VisitMember(MemberExpression node) => Expression.Constant(node.GetFieldName(sqlBuilder.TryGetTable));

        protected override Expression VisitParameter(ParameterExpression node) => Expression.Constant(string.Empty);

        protected override Expression VisitConstant(ConstantExpression node) => Expression.Constant(dialect.GetLiteral(node.Value));

        protected override Expression VisitMethodCall(MethodCallExpression node) =>
            Expression.Constant(node.Method.Name switch
            {
                nameof(Enumerable.Contains) when node.Method.DeclaringType == typeof(Enumerable) => InStatement(node),
                nameof(string.Contains) when node.Method.DeclaringType == typeof(string) && node.Method.GetParameters().Length == 1 =>
                    GetLikeStatement(LikeWildCard.LeftAndRight, node),
                nameof(string.StartsWith) when node.Method.DeclaringType == typeof(string) && node.Method.GetParameters().Length == 1 =>
                    GetLikeStatement(LikeWildCard.Right, node),
                nameof(string.EndsWith) when node.Method.DeclaringType == typeof(string) && node.Method.GetParameters().Length == 1 =>
                    GetLikeStatement(LikeWildCard.Left, node),
                _ => throw new NotSupportedException($"Not support method {node}")
            });

        string InStatement(MethodCallExpression node, bool not = false) =>
            $"{node.Arguments[1].GetFieldName(sqlBuilder.TryGetTable)} {(not ? "NOT IN" : "IN")} ({node.Arguments[0].Evaluate<IEnumerable>().Cast<object>().Select(dialect.GetLiteral).Join(',')})";

        protected override Expression VisitUnary(UnaryExpression node) =>
            Expression.Constant(node.NodeType switch
            {
                ExpressionType.Not => node.Operand is MethodCallExpression m &&
                                      m.Method.Name == nameof(Enumerable.Contains) &&
                                      m.Method.DeclaringType == typeof(Enumerable)
                                      ? InStatement(m, not: true)
                                      : $"NOT ({Visit(node.Operand).Unwrap<string>()})",
                _ => throw new NotSupportedException($"Not support unary expression {node}")
            });

        string GetLikeStatement(LikeWildCard wildCard, MethodCallExpression node) =>
            $"{node.Object.GetFieldName(sqlBuilder.TryGetTable)} LIKE {dialect.GetLikeText(wildCard, node.Arguments[0].Unwrap<string>())}";

        string EvaluateString(Expression expression) => Visit(expression).Unwrap<string>();
    }
}