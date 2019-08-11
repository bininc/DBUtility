using CommLiby;
using CommLiby.Database.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using DBHelper.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBUtility.EF
{
    public class Column
    {
        /// <summary>
        /// 列名
        /// </summary>
        public string ColName { get; private set; }
        /// <summary>
        /// 列类型（数据库中）
        /// </summary>
        public string ColType { get; private set; }
        /// <summary>
        /// 不为空
        /// </summary>
        public bool NotNull { get; private set; }
        /// <summary>
        /// 是否是主键
        /// </summary>
        public bool PrimaryKey { get; private set; }
        /// <summary>
        /// 是否自动增长
        /// </summary>
        public bool AutoID { get; private set; }
        /// <summary>
        /// 字符串字段长度
        /// </summary>
        public int StringLenth { get; private set; } = -1;
        /// <summary>
        /// 不和数据库列映射
        /// </summary>
        public bool NotMapped { get; private set; }
        /// <summary>
        /// 整数部分长度
        /// </summary>
        public byte IntergerLength { get; private set; }
        /// <summary>
        /// 小数部分长度
        /// </summary>
        public byte PointLength { get; private set; }

        public PropertyInfo Property { get; private set; }
        /// <summary>
        /// 属性类型
        /// </summary>
        public Type Type => Property?.PropertyType;
        /// <summary>
        /// 属性名称
        /// </summary>
        public string Name => Property?.Name;
        /// <summary>
        /// 是否是外键
        /// </summary>
        public bool IsForeginKey { get; private set; }
        /// <summary>
        /// 外键
        /// </summary>
        public string FK { get; private set; }

        private dynamic customColumn;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="property"></param>
        public Column(PropertyInfo property, DataBaseType dbType)
        {
            if (property == null)
            {
                NotMapped = true;
                return;
            }
            Type type = property.PropertyType;
            if (type.IsClass && !type.FullName.StartsWith("System.", StringComparison.OrdinalIgnoreCase))
            {
                if (property.GetAccessors()[0].IsVirtual)
                {   //外键
                    IsForeginKey = true;
                }
                else
                {
                    NotMapped = true;
                    return;
                }
            }
            Property = property;

            object[] attrs = property.GetCustomAttributes(false);
            if (attrs.Length > 0)
            {
                foreach (object attr in attrs)
                {
                    if (attr is System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute)
                    {
                        NotMapped = true;
                        break;
                    }
                    else if (attr is System.ComponentModel.DataAnnotations.KeyAttribute)
                    {
                        PrimaryKey = true;
                        NotNull = true;
                    }
                    else if (attr is System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedAttribute)
                    {
                        System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedAttribute generated = (System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedAttribute)attr;
                        if (generated.DatabaseGeneratedOption == System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)
                        {
                            AutoID = true;
                        }
                    }
                    else if (attr is System.ComponentModel.DataAnnotations.RequiredAttribute)
                    {
                        NotNull = true;
                    }
                    else if (attr is System.ComponentModel.DataAnnotations.StringLengthAttribute)
                    {
                        System.ComponentModel.DataAnnotations.StringLengthAttribute length = (System.ComponentModel.DataAnnotations.StringLengthAttribute)attr;
                        StringLenth = length.MaximumLength;
                    }
                    else if (attr is NumberRangeAttribute)
                    {
                        NumberRangeAttribute numberRange = (NumberRangeAttribute)attr;
                        IntergerLength = numberRange.Interger;
                        PointLength = numberRange.Point;
                    }
                    else if (attr is System.ComponentModel.DataAnnotations.Schema.ForeignKeyAttribute)
                    {   //指定字段外键
                        dynamic foreignKey = (System.ComponentModel.DataAnnotations.Schema.ForeignKeyAttribute)attr;

                        if (IsForeginKey)
                        {
                            Column fkCol = Tool.GetForeginKeyColmn(type, dbType, foreignKey.Name);
                            if (fkCol != null)
                            {
                                if (!string.IsNullOrWhiteSpace(foreignKey.Name))
                                {
                                    Column c = Tool.GetForeginKeyColmn(property.DeclaringType, dbType, foreignKey.Name);
                                    if (c != null)
                                        Property = c.Property;
                                    ColName = Tool.GetDbColName(foreignKey.Name, dbType);
                                    ColType = fkCol.ColType;
                                    FK = $"references {type.Name}({fkCol.ColName})";
                                }
                                else
                                {
                                    Property = fkCol.Property;
                                    ColName = $"{type.Name}_{fkCol.Name}";
                                    ColType = fkCol.ColType;
                                    FK = $"references {type.Name}({fkCol.ColName})";
                                }
                            }
                            else
                            {
                                NotMapped = true;
                            }
                        }
                        return;
                    }
                    else if (attr is System.ComponentModel.DataAnnotations.Schema.ColumnAttribute)
                    {
                        customColumn = (System.ComponentModel.DataAnnotations.Schema.ColumnAttribute)attr;
                    }
                }
            }

            if (IsForeginKey && string.IsNullOrWhiteSpace(FK))
            {   //匹配字段外键
                Column fkCol = Tool.GetForeginKeyColmn(type, dbType, null);
                if (fkCol != null)
                {
                    Property = fkCol.Property;
                    ColName = $"{type.Name}_{fkCol.Name}";
                    ColType = fkCol.ColType;
                    FK = $"references {type.Name}({fkCol.ColName})";
                }
                else
                {
                    NotMapped = true;
                }
                return;
            }

            if (NotMapped) return;

            ColName = null;
            if (!string.IsNullOrEmpty(customColumn?.Name))
                ColName = Tool.GetDbColName(customColumn.Name, dbType);
            else
                ColName = Tool.GetDbColName(property.Name, dbType);

            if (!string.IsNullOrEmpty(customColumn?.TypeName))
            {
                ColType = customColumn.TypeName;
                return;
            }

            if (type == typeof(string))
            {
                if (dbType == DataBaseType.Sqlite)
                {
                    if (StringLenth != -1)
                    {
                        ColType = $"NVARCHAR({StringLenth})";
                    }
                    else
                        ColType = "TEXT";
                }
            }
            else if (type == typeof(int) || type == typeof(uint))
            {
                if (dbType == DataBaseType.Sqlite)
                {
                    if (AutoID)
                        ColType = "INTEGER";
                    else
                        ColType = "INT";
                    ColType += GetNumberLength();
                }
            }
            else if (type == typeof(long))
            {
                if (dbType == DataBaseType.Sqlite)
                {
                    ColType = "INTEGER";
                    ColType += GetNumberLength();
                }
            }
            else if (type == typeof(DateTime))
            {
                if (dbType == DataBaseType.Sqlite)
                {
                    ColType = "DATETIME";
                }
            }
            else if (type == typeof(double))
            {
                if (dbType == DataBaseType.Sqlite)
                {
                    ColType = "DOUBLE";
                    ColType += GetNumberLength();
                }
            }
            else if (type == typeof(float))
            {
                if (dbType == DataBaseType.Sqlite)
                {
                    ColType = "FLOAT";
                    ColType += GetNumberLength();
                }
            }
            else if (type == typeof(byte))
            {
                if (dbType == DataBaseType.Sqlite)
                {
                    ColType = "TINYINT";
                    ColType += GetNumberLength();
                }
            }
            else if (type == typeof(short) || type == typeof(ushort))
            {
                if (dbType == DataBaseType.Sqlite)
                {
                    ColType = "SMALLINT";
                    ColType += GetNumberLength();
                }
            }
            else if (type == typeof(bool))
            {
                if (dbType == DataBaseType.Sqlite)
                {
                    ColType = "BOOLEAN";
                }
            }

            if (!type.IsValueType|| (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)))
                NotNull = false;
            else
                NotNull = true;
        }

        private string GetNumberLength()
        {
            StringBuilder lenStr = new StringBuilder();
            if (IntergerLength != 0)
            {
                lenStr.AppendFormat("({0}", IntergerLength);
                if (PointLength != 0)
                    lenStr.AppendFormat(",{0}", PointLength);
                lenStr.Append(")");
            }

            return lenStr.ToString();
        }
    }
}
