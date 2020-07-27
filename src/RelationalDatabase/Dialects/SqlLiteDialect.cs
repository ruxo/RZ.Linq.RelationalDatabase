using System;
using System.Linq;
using System.Text;
using RZ.Foundation.Extensions;
using static LanguageExt.Prelude;

namespace RZ.Linq.RelationalDatabase.Dialects
{
    public sealed class SqlLiteDialect : SqlDialect
    {
        public override string BuildSelectStatement(SqlLinqBuilder builder) {
            var selectedFiels = (builder.SelectedFields.Any()? builder.SelectedFields : builder.GetAllFields()).Join(',');
            var sb = new StringBuilder();
            sb.Append("SELECT ");
            sb.Append(selectedFiels);
            sb.Append(" FROM ");
            switch (builder.TableSpace) {
                case SingleTable tab:
                    AppendTable(sb, tab.Single);
                    break;
                case JoinedTable joined:
                    var firstTable = joined.Tables.First();
                    AppendTable(sb, firstTable);
                    joined.Joining.Iter(j => {
                        sb.Append(" INNER JOIN ");
                        AppendTable(sb, j.Target);
                        sb.Append(" ON ");
                        sb.Append(j.Source.FieldName(j.SourceIdField));
                        sb.Append('=');
                        sb.Append(j.Target.FieldName(j.TargetIdField));
                    });
                    break;
                default:
                    throw new InvalidOperationException();
            }

            builder.WhereCondition.IfSome(s => sb.Append($" WHERE {s}"));

            builder.Take.IfSome(n => sb.Append($" LIMIT {n}"));
            builder.Skip.IfSome(n => sb.Append($" OFFSET {n}"));
            return sb.ToString();
        }

        static void AppendTable(StringBuilder sb, TableAlias tab) {
            sb.Append(tab.TableName);
            tab.Alias.IfSome(alias => sb.Append($" {alias}"));
        }
    }
}