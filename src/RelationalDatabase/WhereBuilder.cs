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
            Expression.Constant(dialect.BuildBinaryExpression(DotnetOperator.Parse(node.Method.Name).Get(),
                                                              EvaluateString(node.Left),
                                                              EvaluateString(node.Right)));

        protected override Expression VisitMember(MemberExpression node) =>
            Expression.Constant(sqlBuilder.TableSpace is SingleTable? node.Member.Name : node.ToString());

        protected override Expression VisitConstant(ConstantExpression node) => Expression.Constant(dialect.GetLiteral(node.Value));

        string EvaluateString(Expression expression) => (string) ((ConstantExpression) Visit(expression)).Value;
    }
}