using System.Linq;
// ReSharper disable UnusedParameter.Global

namespace RZ.Linq.RelationalDatabase
{
    public static class CommonSql
    {
        /// <summary>
        /// Represent COUNT() in SQL
        /// </summary>
        /// <param name="fieldOrTable">Valid value is either a table type or a field</param>
        public static int Count(object fieldOrTable) => 0 /* Dummy value */;

        public static string ToSelectStatement<T>(this IQueryable<T> queryable) => ((ISqlGenerator) queryable).GetSelectString();
    }
}