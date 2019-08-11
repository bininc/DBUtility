using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Data.Common;
using DBUtility.EF.Data;
using CommLiby.Database.EF;
using DBHelper.BaseHelper;
using System.Data;
using CommonLib;
using Newtonsoft.Json;
using DBHelper.Common;
using DBHelper;
using CommLiby;

namespace DBUtility.EF
{
    public class EntityHelper
    {
        private readonly string[] assemblys = null;
        private readonly DbConnection _conn;
        private readonly DataBaseType _dbType = DataBaseType.Unknown;
        private readonly IDBHelper _dbHelper;
        private bool _inited = false;
        public EntityHelper(DbConnection connection, params string[] assemblys)
        {
            this._conn = connection;
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            _dbType = DbConnections.GetDbTypeByConn(connection);
            _dbHelper = DBHelper.DBHelper.GetDBHelper(_conn);
            this.assemblys = assemblys;
        }

        public void InitAllEntitys()
        {
            Dictionary<Type, string> list = new Dictionary<Type, string>();
            list.Add(typeof(EFConfig), Assembly.GetExecutingAssembly()?.GetName().Version.ToString());

            if (assemblys != null && assemblys.Length > 0)
            {
                foreach (var item in assemblys)
                {
                    var assembly = Assembly.Load(item);
                    var entityTypes = from type in assembly.GetTypes()
                                      where type.GetCustomAttributes(typeof(AutoDbSetAttribute), true).Length > 0
                                      select type;
                    foreach (var entityType in entityTypes)
                    {
                        list.Add(entityType, assembly.GetName().Version.ToString());
                    }
                }
            }

            if (list.Any())
            {
                var initEntitysMethod = GetType().GetMethod("InitEntitys", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (var type in list)
                {
                    initEntitysMethod.MakeGenericMethod(type.Key).Invoke(this, new object[] { type.Value });
                }
            }
        }

        internal void InitEntitys<T>(string curVer) where T : new()
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
            bool tableExists = _dbHelper.TableExists(tableName);
            if (!tableExists)
            {   //表不存在创建表
                tableExists = CreateTable<T>();
            }
            if (tableExists)
            {
                string oldVer = "";

                DataTable efconfig = _dbHelper.QueryTable("select * from efconfig;");
                if (efconfig != null)
                {
                    DataRow[] rows = efconfig.Select($"key='{tableName}'");
                    if (rows.Length > 0)
                    {
                        oldVer = rows[0].GetDataRowStringValue("value");
                    }
                }
              
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
            List<Column> columns = Tool.GetTableColumns(classtype, _dbType);
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
                        if (_dbType == DataBaseType.Sqlite)
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

            return _dbHelper.ExecuteNonQuery(sbsql.ToString()) >= 0;
        }

        bool AlterTable<T>(bool force = false) where T : new()
        {
            T classInstance = new T();
            Type classtype = classInstance.GetType();
            //获取表名
            string tableName = Tool.GetTableName(classtype);
            if (tableName == null) return false;

            DataTable oldtable = null;
            if (_dbType == DataBaseType.Sqlite)
                oldtable = _dbHelper.QueryTable($"pragma table_info ('{tableName}')");
            if (oldtable.IsEmpty()) return false;

            StringBuilder sbSql = new StringBuilder($"alter table {tableName} ");
            StringBuilder sbAlter = new StringBuilder();

            StringBuilder insSql = new StringBuilder($"insert into {tableName}(");
            StringBuilder selSql = new StringBuilder("select ");

            bool needModify = force;
            var cols = Tool.GetTableColumns(classtype, _dbType);
            foreach (Column col in cols)
            {
                if (col.NotMapped) continue;
                string name = col.Name;
                if (col.IsForeginKey)
                    name = Tool.getColName(col.ColName, _dbType);

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
                            if (_dbType == DataBaseType.Sqlite)
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

                if (_dbType == DataBaseType.Sqlite)
                {
                    sbAlter.Append(";");
                    bool suc = _dbHelper.ExecuteNonQuery(sbSql.ToString() + sbAlter.ToString()) >= 0;
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
                _dbHelper.ExecuteNonQuery($"drop table if exists {tableName}_old;");
                sbSql.AppendFormat("rename to {0}_old;", tableName);
                _dbHelper.ExecuteNonQuery(sbSql.ToString());

                bool suc = CreateTable<T>();
                if (suc)
                {
                    insSql.Remove(insSql.Length - 1, 1);
                    selSql.Remove(selSql.Length - 1, 1);

                    insSql.Append(") ");
                    selSql.AppendFormat(" from {0}_old;", tableName);
                    insSql.Append(selSql);
                    suc = _dbHelper.ExecuteNonQuery(insSql.ToString()) >= 0;
                    if (suc)
                    {
                        suc = _dbHelper.ExecuteNonQuery($"drop table {tableName}_old;") >= 0;
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
            var cols = Tool.GetTableColumns(classtype, _dbType);

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

            return _dbHelper.ExecuteSqlsTran(sqlList) > 0;
        }

        bool UpdateData<T>(string key, params T[] datas)
        {
            if (datas == null || datas.Length == 0) return false;
            Type classtype = typeof(T);
            if (string.IsNullOrWhiteSpace(key))
                key = Tool.GetForeginKeyColmn(classtype, _dbType, null).Name;

            //获取表名
            string tableName = Tool.GetTableName(classtype);
            if (tableName == null) return false;

            //获取列
            var cols = Tool.GetTableColumns(classtype, _dbType);

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

            return _dbHelper.ExecuteSqlsTran(sqlList) >= 0;
        }

        void InsertOrUpdate<T>(string key, params T[] datas)
        {
            if (datas == null || datas.Length == 0) return;
            Type classtype = typeof(T);
            if (string.IsNullOrWhiteSpace(key))
                key = Tool.GetForeginKeyColmn(classtype, _dbType, null).Name;

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

                if (_dbHelper.Exists(tableName, new Dictionary<string, string>() { { key, value } }))
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
                return _dbHelper.ExecuteNonQuery($"delete from {tableName};") >= 0;
            }
            else if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
            {//删除指定条件数据
                return _dbHelper.ExecuteNonQuery($"delete from {tableName} where {key}='{value}';") >= 0;
            }
            else
                return false;
        }
    }
}
