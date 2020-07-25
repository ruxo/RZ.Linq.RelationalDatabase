using System;
using System.Text;
using RZ.Foundation.Extensions;

namespace RZ.Linq.RelationalDatabase.Dialects
{
    public sealed class SqlLiteDialect
    {
        public string Build(SqlLinqBuilder builder) {
            var selectedFiels = builder.SelectedFields.Join(',');
            var sb = new StringBuilder();
            sb.Append("SELECT ");
            sb.Append(string.IsNullOrEmpty(selectedFiels) ? "*" : selectedFiels);
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
            return sb.ToString();
        }

        static void AppendTable(StringBuilder sb, TableAlias tab) {
            sb.Append(tab.TableName);
            tab.Alias.IfSome(alias => sb.Append($" {alias}"));
        }
    }
}