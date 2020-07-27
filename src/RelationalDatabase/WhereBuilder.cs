using System;
using System.Linq.Expressions;
using RZ.Foundation.Extensions;
using RZ.Linq.RelationalDatabase.Dialects;

namespace RZ.Linq.RelationalDatabase
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

        protected override Expression VisitMember(MemberExpression node) =>
            Expression.Constant($"{GetPrefix(node.Expression)}{node.Member.Name}");

        string GetPrefix(Expression expression) {
            var prop = ExtractReturnString(Visit(expression));
            return string.IsNullOrEmpty(prop) ? prop : $"{prop}.";
        }

        protected override Expression VisitParameter(ParameterExpression node) => Expression.Constant(string.Empty);

        protected override Expression VisitConstant(ConstantExpression node) => Expression.Constant(dialect.GetLiteral(node.Value));

        string ExtractReturnString(Expression expression) => (string) ((ConstantExpression) expression).Value;

        string EvaluateString(Expression expression) => (string) ((ConstantExpression) Visit(expression)).Value;
    }
}