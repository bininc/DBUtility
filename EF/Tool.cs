using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using CommLiby.Database.EF;
using CommLiby;
using DBHelper.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBUtility.EF
{
    public class Tool
    {
        public static string GetTableName(Type type)
        {
            if (type == null) return null;

            string tableName = type.Name;
            object[] tableAttrs = type.GetCustomAttributes(typeof(TableAttribute), false);
            if (tableAttrs.Length > 0)
            {
                TableAttribute tbAttr = tableAttrs[0] as TableAttribute;

                if (!string.IsNullOrEmpty(tbAttr?.Schema))
                    tableName = tbAttr.Schema + "." + tbAttr.Name;
                else
                    tableName = tbAttr?.Name;
            }

            return tableName;
        }

        public static IEnumerable<PropertyInfo> GetTableProps(Type type)
        {
            if (type == null) return null;

            var propertys = from p in type.GetProperties()
                            where p.DeclaringType == type
                            select p;
            return propertys;
        }

        public static List<Column> GetTableColumns(Type type, DataBaseType dbType)
        {
            var propertys = GetTableProps(type);
            if (propertys == null) return null;
            List<Column> list = new List<Column>();
            List<Column> fkCols = new List<Column>();
            foreach (var prop in propertys)
            {
                Column col = new Column(prop, dbType);
                if (col.IsForeginKey && !col.NotMapped)
                    fkCols.Add(col);
                list.Add(col);
            }
            fkCols.ForEach(c =>
            {
                var tmps = list.Where(col => col != c && (col.Name == c.Name || col.ColName == c.ColName)).ToList();
                foreach (Column tmp in tmps)
                {
                    list.Remove(tmp);
                }
            });

            return list;
        }

        public static Column GetForeginKeyColmn(Type type, DataBaseType dbType, string colName)
        {
            if (type == null) return null;

            var propertys = from p in type.GetProperties()
                            where p.DeclaringType == type && p.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute), false).Length == 0 && p.Name == colName
                            select p;

            if (propertys.Any())
            {
                return new Column(propertys.First(), dbType);
            }
            else
            {
                propertys = from p in type.GetProperties()
                            where p.DeclaringType == type && p.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute), false).Length == 0 && p.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.KeyAttribute), false).Length > 0
                            select p;
                if (propertys.Any())
                    return new Column(propertys.First(), dbType);
                else
                    return null;
            }
        }

        public static string GetDbColName(string colName, DataBaseType dbType)
        {
            if (string.IsNullOrWhiteSpace(colName)) return null;
            if (dbType == DataBaseType.SqlServer || dbType == DataBaseType.Sqlite)
                return $"[{colName}]";
            else if (dbType == DataBaseType.MySql)
                return $"`{colName}`";
            else if (dbType == DataBaseType.Oracle)
                return $"\"{colName}\"";
            else
                return colName;
        }

        public static string getColName(string dbColName, DataBaseType dbType)
        {
            if (string.IsNullOrWhiteSpace(dbColName)) return null;

            if (dbType == DataBaseType.SqlServer || dbType == DataBaseType.Sqlite)
                return dbColName.Trim('[', ']');
            else if (dbType == DataBaseType.MySql)
                return dbColName.Trim('`');
            else if (dbType == DataBaseType.Oracle)
                return dbColName.Trim('"');
            else
                return dbColName;
        }
    }
}
