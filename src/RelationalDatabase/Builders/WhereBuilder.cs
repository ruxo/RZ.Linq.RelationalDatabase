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

        protected override Expression VisitMember(MemberExpression node) => Expression.Constant(GetFieldName(node));

        protected override Expression VisitParameter(ParameterExpression node) => Expression.Constant(string.Empty);

        protected override Expression VisitConstant(ConstantExpression node) => Expression.Constant(dialect.GetLiteral(node.Value));

        protected override Expression VisitMethodCall(MethodCallExpression node) =>
            Expression.Constant(node.Method.Name switch
            {
                "Contains" => $"{GetFieldName(node.Arguments[1])} IN ({node.Arguments[0].Evaluate<IEnumerable>().Cast<object>().Select(dialect.GetLiteral).Join(',')})",
                _ => throw new NotSupportedException($"Not support method {node}")
            });

        string GetFieldName(Expression node) =>
            node.GetFieldType(sqlBuilder.TryGetTable) switch
            {
                ColumnField c => c.Alias.FieldName(c.Name),
                _ => throw new NotSupportedException($"Not support expression: {node}")
            };

        string EvaluateString(Expression expression) => Visit(expression).Unwrap<string>();
    }
}