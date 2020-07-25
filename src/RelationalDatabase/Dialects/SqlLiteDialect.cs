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
                    sb.Append(tab.Single.Name);
                    tab.Single.Alias.IfSome(alias => sb.Append($" {alias}"));
                    break;
                default:
                    throw new InvalidOperationException();
            }

            builder.WhereCondition.IfSome(s => sb.Append($" WHERE {s}"));
            return sb.ToString();
        }
    }
}