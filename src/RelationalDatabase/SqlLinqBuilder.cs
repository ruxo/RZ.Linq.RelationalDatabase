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

        public string Name => Alias.IfNone(Table.Name);
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
        TableSpace NoTable();
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

        ImmutableList<string> GetSelect(ParameterExpression expr) => GetTable(expr.Type).Table.Columns.Select(c => c.Name).ToImmutableList();
        ImmutableList<string> GetSelect(MemberExpression expr) => ImmutableList.Create(GetColumnName(expr.Member));
        ImmutableList<string> GetSelect(NewExpression expr) => expr.Arguments.Cast<MemberExpression>().Select(m => GetColumnName(m.Member)).ToImmutableList();

        string GetColumnName(MemberInfo memberInfo) => GetTable(memberInfo.DeclaringType).Table.Columns.Single(c => c.ColumnName == memberInfo.Name).Name;
    }
}