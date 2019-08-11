using CommLiby;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using DBHelper.BaseHelper;
using DBHelper.Common;
using DBHelper;
using System.Data.SQLite.EF6;

namespace DBUtility.EF.Core
{
    static class CoreExtension
    {
        /// <summary>
        /// 获得当前数据库类型
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static DataBaseType GetDbType(this System.Data.Entity.Database database)
        {
            DataBaseType dbType = DbConnections.GetDbTypeByConn(database.Connection);
            if (dbType == DataBaseType.Sqlite)
            {
                SQLiteProviderFactory factory = SQLiteProviderFactory.Instance;
            }
            return dbType;
        }

        #region 执行sql语句返回数据

        public static DataTable SqlQueryForDataTatable(this System.Data.Entity.Database database, string sql, params DbParameter[] parameters)
        {
            if (database == null || sql == null) return null;

            IDBHelper dbHelper = DBHelper.DBHelper.GetDBHelper(database.Connection);
            return dbHelper.QueryTable(sql, parameters);
        }

        /// <summary>
        /// 返回一个数据
        /// </summary>
        /// <param name="context">数据库上下文</param>
        /// <param name="sql">SQL语句或者存储过程名字</param>
        /// <param name="sqlType">SQL类型</param>
        /// <param name="parameters">数据库参数</param>
        /// <returns></returns>
        public static object SqlQueryScalar(this System.Data.Entity.Database database, string sql, CommandType sqlType = CommandType.Text, params DbParameter[] parameters)
        {
            if (database == null || sql == null) return null;

            IDBHelper dbHelper = DBHelper.DBHelper.GetDBHelper(database.Connection);
            return dbHelper.QueryScalar(sql, sqlType, parameters);
        }

        /// <summary>
        /// 表是否存在
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static bool TableExists(this System.Data.Entity.Database database, string tableName)
        {
            if (database == null || tableName == null) return false;

            IDBHelper dbHelper = DBHelper.DBHelper.GetDBHelper(database.Connection);
            return dbHelper.TableExists(tableName);
        }

        /// <summary>
        /// 执行SQL语句
        /// </summary>
        /// <returns></returns>
        public static int SqlExecuteNonQuery(this System.Data.Entity.Database database, string sqlText, params DbParameter[] commandParameters)
        {
            int n = 0;
            if (database == null || sqlText == null) return n;

            IDBHelper dbHelper = DBHelper.DBHelper.GetDBHelper(database.Connection);
            n = dbHelper.ExecuteNonQuery(sqlText, commandParameters);

            return n;
        }

        /// <summary>
        /// 执行多条SQL语句，实现数据库事务。
        /// </summary>
        /// <param name="SQLStringList">多条SQL语句</param>
        public static int ExecuteSqlsTran(this System.Data.Entity.Database database, List<string> SQLStringList, bool failStop = true)
        {

            int n = 0;
            if (database == null || SQLStringList == null || SQLStringList.Count == 0) return n;

            IDBHelper dbHelper = DBHelper.DBHelper.GetDBHelper(database.Connection);
            n = dbHelper.ExecuteSqlsTran(SQLStringList, failStop);

            return n;
        }


        public static bool DataExists(this System.Data.Entity.Database database, string tableName, string key, string value)
        {
            if (database == null) return false;

            IDBHelper dbHelper = DBHelper.DBHelper.GetDBHelper(database.Connection);
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add(key, value);
            return dbHelper.Exists(tableName, dic);
        }

        #endregion
    }
}
