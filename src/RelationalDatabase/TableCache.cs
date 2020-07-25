using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using LanguageExt;
using static LanguageExt.Prelude;

namespace RZ.Linq.RelationalDatabase
{
    [Record]
    public partial class TableInfo
    {
        public readonly string Name;
        public readonly Type RepresentationType;
        public readonly ImmutableArray<TableColumnInfo> Columns;
    }

    [Record]
    public partial class TableColumnInfo
    {
        public readonly string ColumnName;
        public readonly Option<string> RealName;
        public readonly Type DataType;

        public string Name => RealName.IfNone(ColumnName);
    }

    public static class TableCache
    {
        static readonly ConcurrentDictionary<Type, TableInfo> Cache = new ConcurrentDictionary<Type, TableInfo>();

        public static TableInfo GetTable(Type tableType) => Cache.GetOrAdd(tableType, MakeTableInfo);

        static TableInfo MakeTableInfo(Type tableType) {
            var properties = GetColumnsFrom(tableType.GetProperties);
            var fields = GetColumnsFrom(tableType.GetFields);
            return TableInfo.New(GetTableName(tableType), tableType, properties.Concat(fields).ToImmutableArray());
        }

        static string GetTableName(MemberInfo tableType) =>
            Optional(tableType.GetCustomAttribute<TableAttribute>()).Map(attr => attr.Name).IfNone(tableType.Name);

        static IEnumerable<TableColumnInfo> GetColumnsFrom(Func<BindingFlags, IEnumerable<MemberInfo>> getter) =>
            from i in getter(BindingFlags.Public | BindingFlags.Instance)
            let column = Optional(i.GetCustomAttribute<ColumnAttribute>()).Map(c => c.Name)
            select TableColumnInfo.New(column.IfNone(i.Name), RealName: None, i.DeclaringType);
    }
}