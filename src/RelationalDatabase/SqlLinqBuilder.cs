using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LanguageExt;
using RZ.Foundation.Extensions;
using RZ.Linq.RelationalDatabase.Dialects;
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
        public readonly Option<int> Skip;
        public readonly Option<int> Take;
        public readonly bool Distinct;

        public static SqlLinqBuilder Create(Type mainTable) =>
            New(SingleTable.New(TableAlias.New(None, TableCache.GetTable(mainTable))), None, ImmutableList<string>.Empty, None, None, false);

        #region Join methods

        public SqlLinqBuilder BuildJoin(MethodCallExpression expression) {
            var (sourceAlias, sourceKey) = ExtractJoinKey((UnaryExpression) expression.Arguments[2]);
            var (targetAlias, targetKey) = ExtractJoinKey((UnaryExpression) expression.Arguments[3]);
            var joiningTable = TableCache.GetTable(((ConstantExpression) expression.Arguments[1]).Value.GetType().GetGenericArguments()[0]);
            var newTarget = TableAlias.New(targetAlias, joiningTable);

            JoinedTable singleToJoined(TableAlias single) {
                var newSource = single.With(Alias: sourceAlias);
                return JoinedTable.New(Seq(new[] {newSource, newTarget}), Seq(new[] {JoinCondition.New(newSource, newTarget, sourceKey.Name, targetKey.Name)}));
            }
            var newSpace = TableSpace switch
            {
                SingleTable t => singleToJoined(t.Single),
                JoinedTable t => JoinedTable.New(Seq(t.Tables.Append(newTarget)),
                                                 Seq(t.Joining.Append(JoinCondition.New(GetTable(sourceKey.DeclaringType), newTarget, sourceKey.Name, targetKey.Name)))),
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

        public IEnumerable<string> GetAllFields() =>
            TableSpace switch
            {
                SingleTable t => ListFields(t.Single),
                JoinedTable t => t.Tables.SelectMany(ListFields),
                _ => throw new NotSupportedException()
            };

        public SqlLinqBuilder Select(UnaryExpression expression) {
            var lambda = (LambdaExpression) expression.Operand;
            return With(SelectedFields: lambda.Body switch
            {
                ParameterExpression p => GetSelect(p),
                MemberExpression m => GetTableFromProperty(m.Member)
                                        .Map(t => ListFields(t).ToImmutableList())
                                        .IfNone(() => GetSelect(m)),
                NewExpression n => GetSelect(n),
                _ => throw new NotSupportedException($"Not support {lambda.Body.GetType()}")
            });
        }

        Option<TableAlias> GetTableFromProperty(MemberInfo member) => member is PropertyInfo p ? TryGetTable(p.PropertyType) : None;

        TableAlias GetTable(Type tableType) =>
            TryGetTable(tableType).GetOrThrow(() => new InvalidOperationException($"Type {tableType.Name} is not in query context!"));

        Option<TableAlias> TryGetTable(Type tableType) =>
            TableSpace switch
            {
                SingleTable t => t.Single.Table.RepresentationType == tableType ? Some(t.Single) : None,
                JoinedTable t => Optional(t.Tables.SingleOrDefault(tab => tab.Table.RepresentationType == tableType)),
                _ => None
            };

        ImmutableList<string> GetSelect(ParameterExpression expr) => ListFields(GetTable(expr.Type)).ToImmutableList();
        ImmutableList<string> GetSelect(MemberExpression expr) => ImmutableList.Create(GetColumnName(expr.Member));

        ImmutableList<string> GetSelect(NewExpression expr) =>
            expr.Arguments.SelectMany(arg => arg switch
                {
                    MemberExpression m => GetSelect(m),
                    ParameterExpression p => GetSelect(p),
                    _ => throw new NotSupportedException($"Expression type {arg.GetType()} is not supported.")
                })
                .ToImmutableList();

        string GetColumnName(MemberInfo memberInfo) {
            var tableAlias = GetTable(memberInfo.DeclaringType);
            return tableAlias.FieldName(tableAlias.Table.Columns.Single(c => c.ColumnName == memberInfo.Name).Name);
        }

        static IEnumerable<string> ListFields(TableAlias tableAlias) => tableAlias.Table.Columns.Select(c => tableAlias.FieldName(c.Name));

        #endregion

        public SqlLinqBuilder WithTake(int n) => With(Take: n);
        public SqlLinqBuilder WithSkip(int n) => With(Skip: n);

        public SqlLinqBuilder BuildWhere(UnaryExpression expression, SqlDialect dialect) =>
            With(WhereCondition: new WhereBuilder(this, dialect).Parse(expression));
    }
}