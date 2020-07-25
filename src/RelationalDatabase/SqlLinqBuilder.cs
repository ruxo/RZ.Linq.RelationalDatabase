using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LanguageExt;
using static LanguageExt.Prelude;

namespace RZ.Linq.RelationalDatabase
{
    [Record]
    public partial class TableAlias
    {
        public readonly Option<string> Alias;
        public readonly TableInfo Table;

        public string TableName => Table.Name;
        public string FieldName(string field) => Alias.Match(a => $"{a}.{field}", () => field);
    }

    [Record]
    public partial struct JoinCondition
    {
        public readonly TableAlias Source;
        public readonly TableAlias Target;
        public readonly string SourceIdField;
        public readonly string TargetIdField;
    }

    [Union]
    public interface TableSpace
    {
        // ReSharper disable UnusedMemberInSuper.Global
        TableSpace SingleTable(TableAlias single);
        TableSpace JoinedTable(Seq<TableAlias> tables, Seq<JoinCondition> joining);
        // ReSharper restore UnusedMemberInSuper.Global
    }

    [Record]
    public partial class SqlLinqBuilder
    {
        public readonly TableSpace TableSpace;
        public readonly Option<string> WhereCondition;
        public readonly ImmutableList<string> SelectedFields;
        public readonly Option<int> Skipped;
        public readonly Option<int> Take;
        public readonly bool Distinct;

        public static SqlLinqBuilder Create(Type mainTable) =>
            New(SingleTable.New(TableAlias.New(None, TableCache.GetTable(mainTable))), None, ImmutableList<string>.Empty, None, None, false);

        #region Join methods

        public SqlLinqBuilder BuildJoin(MethodCallExpression expression) {
            var (sourceAlias, sourceKey) = ExtractJoinKey((UnaryExpression) expression.Arguments[2]);
            var (targetAlias, targetKey) = ExtractJoinKey((UnaryExpression) expression.Arguments[3]);
            var joiningTable = TableCache.GetTable(((ConstantExpression) expression.Arguments[1]).Value.GetType().GetGenericArguments()[0]);

            JoinedTable singleToJoined(TableAlias single) {
                var newSource = single.With(Alias: sourceAlias);
                var newTarget = TableAlias.New(targetAlias, joiningTable);
                return JoinedTable.New(Seq(new[] {newSource, newTarget}), Seq(new[] {JoinCondition.New(newSource, newTarget, sourceKey.Name, targetKey.Name)}));
            }
            var newSpace = TableSpace switch
            {
                SingleTable t => singleToJoined(t.Single),
                _ => throw new NotSupportedException()
            };
            var select = (UnaryExpression) expression.Arguments[4];
            return With(newSpace).Select(select);
        }

        (string Alias, MemberInfo JoinKey) ExtractJoinKey(UnaryExpression expression) {
            var lambda = (LambdaExpression) expression.Operand;
            var alias = lambda.Parameters[0].Name;
            var joinKey = ((MemberExpression) lambda.Body).Member;
            return (alias, joinKey);
        }

        #endregion

        #region Select methods

        public SqlLinqBuilder Select(UnaryExpression expression) {
            var lambda = (LambdaExpression) expression.Operand;
            return With(SelectedFields: lambda.Body switch
            {
                ParameterExpression p => GetSelect(p),
                MemberExpression m => GetSelect(m),
                NewExpression n => GetSelect(n),
                _ => throw new NotSupportedException($"Not support {lambda.Body.GetType()}")
            });
        }

        TableAlias GetTable(Type tableType) =>
            TableSpace switch
            {
                SingleTable t => t.Single.Table.RepresentationType == tableType
                                     ? t.Single
                                     : throw new InvalidOperationException($"Type {tableType.Name} is not in query context!"),
                JoinedTable t => t.Tables.Single(tab => tab.Table.RepresentationType == tableType),
                _ => throw new InvalidOperationException($"Type {tableType.Name} is not in query context!")
            };

        ImmutableList<string> GetSelect(ParameterExpression expr) {
            var tableAlias = GetTable(expr.Type);
            return tableAlias.Table.Columns.Select(c => tableAlias.FieldName(c.Name)).ToImmutableList();
        }

        ImmutableList<string> GetSelect(MemberExpression expr) => ImmutableList.Create(GetColumnName(expr.Member));
        ImmutableList<string> GetSelect(NewExpression expr) => expr.Arguments.Cast<MemberExpression>().Select(m => GetColumnName(m.Member)).ToImmutableList();

        string GetColumnName(MemberInfo memberInfo) {
            var tableAlias = GetTable(memberInfo.DeclaringType);
            return tableAlias.FieldName(tableAlias.Table.Columns.Single(c => c.ColumnName == memberInfo.Name).Name);
        }

        #endregion
    }
}