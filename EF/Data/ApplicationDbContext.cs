using CommLiby.Database.EF;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Reflection;
using DBUtility.EF.Core;
using System.Text;
using CommLiby;
using CommonLib;
using Newtonsoft.Json;
using System.Linq.Expressions;
using DBHelper.Common;
using System.Data.Entity.Infrastructure;

namespace DBUtility.EF.Data
{
    public class ApplicationDbContext : DbContext
    {
        string[] assemblys = null;
        public ApplicationDbContext(string nameOrConnectionString, params string[] assemblys) : base(nameOrConnectionString)
        {
            this.assemblys = assemblys;
            Database.SetInitializer<ApplicationDbContext>(new CreateDatabaseIfNotExists<ApplicationDbContext>());
        }

        public override int SaveChanges()
        {
            bool saveFailed;
            do
            {
                saveFailed = false;
                try
                {
                    return base.SaveChanges();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    saveFailed = true;
                    // 获取抛出DbUpdateConcurrencyException异常的实体
                    var entry = ex.Entries.Single();

                    // 设置实体的EntityState为Detached，放弃更新或删除抛出异常的实体
                    entry.State = EntityState.Detached;

                }
            } while (saveFailed);

            return 0;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();  //移除表名后面加s

            List<Type> list = new List<Type>();
            list.Add(typeof(EFConfig));

            if (assemblys != null && assemblys.Length > 0)
            {
                foreach (var item in assemblys)
                {
                    var assembly = Assembly.Load(item);
                    var entityTypes = from type in assembly.GetTypes()
                                      where type.GetCustomAttributes(typeof(AutoDbSetAttribute), true).Length > 0
                                      select type;
                    list.AddRange(entityTypes);
                }
            }

            if (list.Any())
            {
                var entityMethod = typeof(DbModelBuilder).GetMethod("Entity");
                var initEntitysMethod = GetType().GetMethod("InitEntitys", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (var type in list)
                {
                    initEntitysMethod.MakeGenericMethod(type).Invoke(this, null);

                    var config = entityMethod.MakeGenericMethod(type).Invoke(modelBuilder, null);
                    var ignoreMethod = config.GetType().GetMethod("Ignore");    //忽略未映射字段
                    var cols = Tool.GetTableColumns(type, Database.GetDbType()).Where(c => c.NotMapped);
                    foreach (Column col in cols)
                    {
                        Type funcType = Expression.GetFuncType(type, col.Type);//typeof(Func<,>).MakeGenericType();
                        ParameterExpression parameter = Expression.Parameter(type, "p");
                        MethodInfo lambdaMethod = typeof(Expression).GetMethods().First(m => m.IsGenericMethod && m.Name == "Lambda" && m.GetParameters().Length == 2);
                        var ignoreParam = lambdaMethod.MakeGenericMethod(funcType).Invoke(null, new object[] { Expression.Property(parameter, col.Name), new ParameterExpression[] { parameter } });
                        ignoreMethod.MakeGenericMethod(col.Type).Invoke(config, new object[] { ignoreParam });
                    }
                }
            }

            base.OnModelCreating(modelBuilder);
        }

        void InitEntitys<T>() where T : new()
        {
            T classInstance = new T();
            Type type = typeof(T);
            //获取表名
            string tableName = Tool.GetTableName(type);
            if (tableName == null) return;

            bool forceUpdateTable = false;
            FieldInfo tmp = type.GetField("ForceUpdateTable");
            if (tmp != null)
                forceUpdateTable = (bool)tmp.GetValue(classInstance);

            bool clearData = false;
            tmp = null;
            tmp = type.GetField("ClearData");
            if (tmp != null)
                clearData = (bool)tmp.GetValue(classInstance);

            string defaultJsonData = null;
            tmp = null;
            tmp = type.GetField("DefaultJsonData");
            if (tmp != null)
                defaultJsonData = (string)tmp.GetValue(classInstance);


            //判断表是否存在
            bool tableExists = Database.TableExists(tableName);
            if (!tableExists)
            {   //表不存在创建表
                tableExists = CreateTable<T>();
            }
            if (tableExists)
            {
                string oldVer = "";

                DataTable efconfig = Database.SqlQueryForDataTatable("select * from efconfig;");
                if (efconfig != null)
                {
                    DataRow[] rows = efconfig.Select($"key='{tableName}'");
                    if (rows.Length > 0)
                    {
                        oldVer = rows[0].GetDataRowStringValue("value");
                    }
                }

                string curVer = CommonLib.Common.GetAppVersion();
                if (oldVer != curVer)
                {
                    AlterTable<T>(forceUpdateTable);

                    if (clearData)
                        DeleteData<T>(null, null);

                    if (!string.IsNullOrWhiteSpace(defaultJsonData))
                    {
                        T[] datas = JsonConvert.DeserializeObject<T[]>(defaultJsonData);
                        InsertOrUpdate<T>(null, datas);
                    }

                    EFConfig ver = new EFConfig() { key = tableName, value = curVer, uptime = DateTime.Now };
                    InsertOrUpdate<EFConfig>("key", ver);
                }
            }
        }

        bool CreateTable<T>() where T : new()
        {
            T classInstance = new T();
            Type classtype = classInstance.GetType();
            //获取表名
            string tableName = Tool.GetTableName(classtype);
            if (tableName == null) return false;

            //获取列属性
            List<Column> columns = Tool.GetTableColumns(classtype, Database.GetDbType());

            StringBuilder sbsql = new StringBuilder($"create table {tableName} ( ");
            for (int i = 0; i < columns.Count; i++)
            {
                Column col = columns[i];
                PropertyInfo property = col.Property;
                if (col.NotMapped) continue;

                sbsql.AppendFormat("{0} {1} ", col.ColName, col.ColType);

                if (!col.IsForeginKey)
                {

                    if (col.PrimaryKey)
                        sbsql.Append("primary key ");
                    if (col.AutoID)
                    {
                        if (Database.GetDbType() == DataBaseType.Sqlite)
                            sbsql.Append("autoincrement ");
                    }

                    if (col.NotNull)
                        sbsql.Append("not null ");

                    if (!col.AutoID)
                    {
                        if (col.Type != typeof(DateTime))
                        {
                            object dval = property.GetValue(classInstance, null);
                            if (dval != null)
                            {
                                if (col.Type == typeof(string))
                                    dval = $"'{dval}'";
                                if (col.Type == typeof(bool))
                                    dval = Convert.ToByte((bool)dval);
                                sbsql.AppendFormat("default ({0}) ", dval);
                            }
                            else
                            {
                                if (col.NotNull)
                                    if (col.Type == typeof(string))
                                        sbsql.Append("default ('') ");
                            }
                        }
                    }
                }
                else
                {
                    sbsql.Append(col.FK);
                }

                sbsql.Append(",");
            }
            sbsql.Remove(sbsql.Length - 1, 1);
            sbsql.Append(");");

            return Database.SqlExecuteNonQuery(sbsql.ToString()) >= 0;
        }

        bool AlterTable<T>(bool force = false) where T : new()
        {
            T classInstance = new T();
            Type classtype = classInstance.GetType();
            //获取表名
            string tableName = Tool.GetTableName(classtype);
            if (tableName == null) return false;

            DataTable oldtable = null;
            if (Database.GetDbType() == DataBaseType.Sqlite)
                oldtable = Database.SqlQueryForDataTatable($"pragma table_info ('{tableName}')");
            if (oldtable.IsEmpty()) return false;

            StringBuilder sbSql = new StringBuilder($"alter table {tableName} ");
            StringBuilder sbAlter = new StringBuilder();

            StringBuilder insSql = new StringBuilder($"insert into {tableName}(");
            StringBuilder selSql = new StringBuilder("select ");

            bool needModify = force;
            var cols = Tool.GetTableColumns(classtype, Database.GetDbType());
            foreach (Column col in cols)
            {
                if (col.NotMapped) continue;
                string name = col.Name;
                if (col.IsForeginKey)
                    name = Tool.getColName(col.ColName, Database.GetDbType());

                DataRow[] rows = oldtable.Select($"name='{name}'");
                if (rows.Length > 0)
                {
                    insSql.AppendFormat("{0},", col.ColName);
                    selSql.AppendFormat("{0},", col.ColName);
                    if (needModify || col.IsForeginKey) continue;

                    string oldType = rows[0].GetDataRowStringValue("type");
                    bool oldPk = rows[0].GetDataRowIntValue("pk") == 1;
                    bool oldNotnull = rows[0].GetDataRowIntValue("notnull") == 1;
                    string oldDflt_value = rows[0].GetDataRowStringValue("dflt_value");
                    if (oldDflt_value == "''")
                        oldDflt_value = null;

                    if (oldType != col.ColType)
                    {
                        needModify = true;
                    }
                    if (!needModify && col.PrimaryKey != oldPk)
                    {
                        needModify = true;
                    }
                    if (!needModify && col.NotNull != oldNotnull)
                    {
                        needModify = true;
                    }
                    object dval = col.Property.GetValue(classInstance, null);
                    if (col.AutoID) dval = null;
                    if (col.Type == typeof(DateTime))
                    {
                        if (((DateTime)dval) == default(DateTime))
                            dval = null;
                    }
                    else if (col.Type == typeof(bool))
                    {
                        dval = Convert.ToByte((bool)dval);
                    }

                    if (!needModify && dval?.ToString() != oldDflt_value)
                    {
                        needModify = true;
                    }
                }
                else
                {
                    if (needModify) continue;   //表如果需要重新建立 无需再添加列

                    sbAlter.Append("add column ");
                    sbAlter.AppendFormat("{0} {1} ", col.ColName, col.ColType);
                    if (!col.IsForeginKey)
                    {
                        if (col.PrimaryKey)
                        {
                            needModify = true;
                            continue;
                        }
                        if (col.AutoID)
                        {
                            if (Database.GetDbType() == DataBaseType.Sqlite)
                                sbAlter.Append("autoincrement ");
                        }
                        if (col.NotNull)
                            sbAlter.Append("not null ");

                        if (!col.AutoID)
                        {
                            if (col.Type != typeof(DateTime))
                            {
                                object dval = col.Property.GetValue(classInstance, null);
                                if (dval != null)
                                {
                                    if (col.Type == typeof(string))
                                        dval = $"'{dval}'";
                                    if (col.Type == typeof(bool))
                                        dval = Convert.ToByte((bool)dval);
                                    sbAlter.AppendFormat("default ({0}) ", dval);
                                }
                                else
                                {
                                    if (col.NotNull)
                                        if (col.Type == typeof(string))
                                            sbAlter.Append("default ('') ");
                                }
                            }
                        }
                    }
                    else
                    {
                        sbAlter.Append(col.FK);
                    }
                }

                if (sbAlter.Length == 0) continue;

                if (Database.GetDbType() == DataBaseType.Sqlite)
                {
                    sbAlter.Append(";");
                    bool suc = Database.SqlExecuteNonQuery(sbSql.ToString() + sbAlter.ToString()) >= 0;
                    if (suc)
                    {
                        sbAlter.Clear();
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            if (needModify)
            {
                Database.SqlExecuteNonQuery($"drop table if exists {tableName}_old;");
                sbSql.AppendFormat("rename to {0}_old;", tableName);
                Database.SqlExecuteNonQuery(sbSql.ToString());

                bool suc = CreateTable<T>();
                if (suc)
                {
                    insSql.Remove(insSql.Length - 1, 1);
                    selSql.Remove(selSql.Length - 1, 1);

                    insSql.Append(") ");
                    selSql.AppendFormat(" from {0}_old;", tableName);
                    insSql.Append(selSql);
                    suc = Database.SqlExecuteNonQuery(insSql.ToString()) >= 0;
                    if (suc)
                    {
                        suc = Database.SqlExecuteNonQuery($"drop table {tableName}_old;") >= 0;
                    }
                }


                return suc;
            }

            if (sbAlter.Length == 0) return true;

            return false;
        }

        bool InsertData<T>(params T[] datas)
        {
            if (datas == null || datas.Length == 0) return false;

            Type classtype = typeof(T);
            //获取表名
            string tableName = Tool.GetTableName(classtype);
            if (tableName == null) return false;

            //获取列
            var cols = Tool.GetTableColumns(classtype, Database.GetDbType());

            List<string> sqlList = new List<string>();
            foreach (T data in datas)
            {
                StringBuilder sbSql = new StringBuilder($"insert into {tableName} (");
                StringBuilder sbValues = new StringBuilder("values (");
                foreach (Column col in cols)
                {
                    if (col.NotMapped) continue;    //跳过未映射列
                    if (col.AutoID) continue;   //跳过自增列
                    object val = val = col.Property.GetValue(data, null);
                    if (val == null) continue;

                    sbSql.AppendFormat("{0},", col.ColName);

                    string value = null;
                    if (col.Type == typeof(string))
                        value = $"'{val}'";
                    else if (col.Type == typeof(DateTime))
                        value = $"'{((DateTime)val).ToFormatDateTimeStr()}'";
                    else if (col.Type == typeof(bool))
                        value = ((bool)val) ? "1" : "0";
                    else
                        value = val.ToString();
                    sbValues.AppendFormat("{0},", value);
                }
                sbSql.Remove(sbSql.Length - 1, 1);
                sbSql.Append(") ");
                sbValues.Remove(sbValues.Length - 1, 1);
                sbValues.Append(");");

                sbSql.Append(sbValues);
                sqlList.Add(sbSql.ToString());
            }

            return Database.ExecuteSqlsTran(sqlList) > 0;
        }

        bool UpdateData<T>(string key, params T[] datas)
        {
            if (datas == null || datas.Length == 0) return false;
            Type classtype = typeof(T);
            if (string.IsNullOrWhiteSpace(key))
                key = Tool.GetForeginKeyColmn(classtype, Database.GetDbType(), null).Name;

            //获取表名
            string tableName = Tool.GetTableName(classtype);
            if (tableName == null) return false;

            //获取列
            var cols = Tool.GetTableColumns(classtype, Database.GetDbType());

            List<string> sqlList = new List<string>();
            foreach (T data in datas)
            {
                StringBuilder sbSql = new StringBuilder($"update {tableName} set ");
                string keyVal = null;
                foreach (Column col in cols)
                {
                    if (col.NotMapped) continue;    //跳过未映射列

                    object val = col.Property.GetValue(data, null);
                    if (val == null) continue;

                    string value = null;
                    if (col.Type == typeof(string))
                        value = $"'{val}'";
                    else if (col.Type == typeof(DateTime))
                        value = $"'{((DateTime)val).ToFormatDateTimeStr()}'";
                    else if (col.Type == typeof(bool))
                        value = ((bool)val) ? "1" : "0";
                    else
                        value = val.ToString();

                    if (col.Name == key)
                    {
                        keyVal = value;
                        continue;
                    }

                    if (col.AutoID) continue;   //跳过自增列
                    sbSql.AppendFormat("{0}={1},", col.ColName, value);
                }
                sbSql.Remove(sbSql.Length - 1, 1);
                if (keyVal != null)
                {
                    sbSql.AppendFormat(" where {0}={1};", key, keyVal);
                }
                sqlList.Add(sbSql.ToString());
            }

            return Database.ExecuteSqlsTran(sqlList) >= 0;
        }

        void InsertOrUpdate<T>(string key, params T[] datas)
        {
            if (datas == null || datas.Length == 0) return;
            Type classtype = typeof(T);
            if (string.IsNullOrWhiteSpace(key))
                key = Tool.GetForeginKeyColmn(classtype, Database.GetDbType(), null).Name;

            //获取表名
            string tableName = Tool.GetTableName(classtype);
            if (tableName == null) return;

            //获取列
            var props = Tool.GetTableProps(classtype);
            props = props.Where(p => p.Name == key);
            if (!props.Any()) return;

            PropertyInfo prop = props.First();
            foreach (T data in datas)
            {
                string value = null;
                object val = prop.GetValue(data, null);
                if (val == null)
                    value = ",NULL";
                else
                {
                    if (prop.PropertyType == typeof(string))
                        value = val.ToString();
                    else if (prop.PropertyType == typeof(DateTime))
                        value = ((DateTime)val).ToFormatDateTimeStr();
                    else if (prop.PropertyType == typeof(bool))
                        value = ((bool)val) ? ",1" : ",0";
                    else
                        value = val.ToString();
                }

                if (Database.DataExists(tableName, key, value))
                {//更新
                    UpdateData<T>(key, data);
                }
                else
                {//新建
                    InsertData<T>(data);
                }
            }
        }

        bool DeleteData<T>(string key, string value)
        {
            Type type = typeof(T);
            //获取表名
            string tableName = Tool.GetTableName(type);
            if (tableName == null) return false;

            if (string.IsNullOrWhiteSpace(key) && string.IsNullOrWhiteSpace(value))
            {//删除所有数据
                return Database.SqlExecuteNonQuery($"delete from {tableName};") >= 0;
            }
            else if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
            {//删除指定条件数据
                return Database.SqlExecuteNonQuery($"delete from {tableName} where {key}='{value}';") >= 0;
            }
            else
                return false;
        }
    }
}
