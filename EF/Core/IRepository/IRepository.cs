﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DBUtility.EF.Core.IRepository
{
    /// <summary>
    /// 仓储接口
    /// </summary>
    /// <typeparam name="T">实体模型</typeparam>
    public interface IRepository<T> where T : class
    {
        #region 单模型CRUD操作
        /// <summary>
        /// 增加一条记录
        /// </summary>
        /// <param name="entity">实体模型</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        bool Save(T entity, bool isCommit = true);

        /// <summary>
        /// 增加一条记录
        /// </summary>
        /// <param name="entity">实体模型</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        bool Save<Tmodel>(Tmodel entity, bool isCommit = true) where Tmodel : class;
        /// <summary>
        /// 增加一条记录（异步方式）
        /// </summary>
        /// <param name="entity">实体模型</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        //Task<bool> SaveAsync(T entity, bool isCommit = true);
        /// <summary>
        /// 更新一条记录
        /// </summary>
        /// <param name="entity">实体模型</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        bool Update(T entity, bool isCommit = true);
        /// <summary>
        /// 更新一条记录
        /// </summary>
        /// <param name="entity">实体模型</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        bool Update<Tmodel>(Tmodel entity, bool isCommit = true) where Tmodel : class;
        /// <summary>
        /// 更新一条记录（异步方式）
        /// </summary>
        /// <param name="entity">实体模型</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        //Task<bool> UpdateAsync(T entity, bool isCommit = true);
        /// <summary>
        /// 增加或更新一条记录
        /// </summary>
        /// <param name="entity">实体模型</param>
        /// <param name="isSave">是否增加</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        bool SaveOrUpdate(T entity, bool isSave, bool isCommit = true);
        /// <summary>
        /// 增加或更新一条记录
        /// </summary>
        /// <param name="entity">实体模型</param>
        /// <param name="isSave">是否增加</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        bool SaveOrUpdate<Tmodel>(Tmodel entity, bool isSave, bool isCommit = true) where Tmodel : class;
        /// <summary>
        /// 增加或更新一条记录（异步方式）
        /// </summary>
        /// <param name="entity">实体模型</param>
        /// <param name="isSave">是否增加</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        //Task<bool> SaveOrUpdateAsync(T entity, bool isSave, bool isCommit = true);
        /// <summary>
        /// 通过Lamda表达式获取实体
        /// </summary>
        /// <param name="predicate">Lamda表达式（p=>p.Id==Id）</param>
        /// <returns></returns>
        T Get(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// 通过Lamda表达式获取实体
        /// </summary>
        /// <param name="predicate">Lamda表达式（p=>p.Id==Id）</param>
        /// <returns></returns>
        Tmodel Get<Tmodel>(Expression<Func<Tmodel, bool>> predicate) where Tmodel : class;
        /// <summary>
        /// 通过Lamda表达式获取实体（异步方式）
        /// </summary>
        /// <param name="predicate">Lamda表达式（p=>p.Id==Id）</param>
        /// <returns></returns>
        //Task<T> GetAsync(Expression<Func<T, bool>> predicate);
        /// <summary>
        /// 删除一条记录
        /// </summary>
        /// <param name="entity">实体模型</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        bool Delete(T entity, bool isCommit = true);
        /// <summary>
        /// 删除一条记录（异步方式）
        /// </summary>
        /// <param name="entity">实体模型</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        //Task<bool> DeleteAsync(T entity, bool isCommit = true);
        #endregion

        #region 多模型 操作
        /// <summary>
        /// 增加多条记录，同一模型
        /// </summary>
        /// <param name="T1">实体模型集合</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        bool SaveList(List<T> T1, bool isCommit = true);
        /// <summary>
        /// 增加多条记录，同一模型（异步方式）
        /// </summary>
        /// <param name="T1">实体模型集合</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        //Task<bool> SaveListAsync(List<T> T1, bool isCommit = true);

        /// <summary>
        /// 增加多条记录，独立模型
        /// </summary>
        /// <param name="T">实体模型集合</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        bool SaveList<T1>(List<T1> T, bool isCommit = true) where T1 : class;
        /// <summary>
        /// 增加多条记录，独立模型（异步方式）
        /// </summary>
        /// <param name="T">实体模型集合</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        //Task<bool> SaveListAsync<T1>(List<T1> T, bool isCommit = true) where T1 : class;

        /// <summary>
        /// 更新多条记录，同一模型
        /// </summary>
        /// <param name="T1">实体模型集合</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        bool UpdateList(List<T> T1, bool isCommit = true);
        /// <summary>
        /// 更新多条记录，同一模型（异步方式）
        /// </summary>
        /// <param name="T1">实体模型集合</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        //Task<bool> UpdateListAsync(List<T> T1, bool isCommit = true);

        /// <summary>
        /// 更新多条记录，独立模型
        /// </summary>
        /// <param name="T">实体模型集合</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        bool UpdateList<T1>(List<T1> T, bool isCommit = true) where T1 : class;
        /// <summary>
        /// 更新多条记录，独立模型（异步方式）
        /// </summary>
        /// <param name="T">实体模型集合</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        //Task<bool> UpdateListAsync<T1>(List<T1> T, bool isCommit = true) where T1 : class;

        /// <summary>
        /// 删除多条记录，同一模型
        /// </summary>
        /// <param name="T1">实体模型集合</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        bool DeleteList(List<T> T1, bool isCommit = true);
        /// <summary>
        /// 删除多条记录，同一模型（异步方式）
        /// </summary>
        /// <param name="T1">实体模型集合</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        //Task<bool> DeleteListAsync(List<T> T1, bool isCommit = true);

        /// <summary>
        /// 删除多条记录，独立模型
        /// </summary>
        /// <param name="T">实体模型集合</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        bool DeleteList<T1>(List<T1> T, bool isCommit = true) where T1 : class;
        /// <summary>
        /// 删除多条记录，独立模型（异步方式）
        /// </summary>
        /// <param name="T">实体模型集合</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        //Task<bool> DeleteListAsync<T1>(List<T1> T, bool isCommit = true) where T1 : class;

        /// <summary>
        /// 通过Lamda表达式，删除一条或多条记录
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="isCommit"></param>
        /// <returns></returns>
        bool Delete(Expression<Func<T, bool>> predicate, bool isCommit = true);
        /// <summary>
        /// 通过Lamda表达式，删除一条或多条记录（异步方式）
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="isCommit"></param>
        /// <returns></returns>
        //Task<bool> DeleteAsync(Expression<Func<T, bool>> predicate, bool isCommit = true);

        #endregion

        #region 获取多条数据操作
        /// <summary>
        /// 返回IQueryable集合，延时加载数据
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        IQueryable<T> LoadAll(Expression<Func<T, bool>> predicate);
        /// <summary>
        /// 返回IQueryable集合，延时加载数据
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        IQueryable<Tmodel> LoadAll<Tmodel>(Expression<Func<Tmodel, bool>> predicate) where Tmodel : class;
        /// <summary>
        /// 返回IQueryable集合，延时加载数据（异步方式）
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        //Task<IQueryable<T>> LoadAllAsync(Expression<Func<T, bool>> predicate);

        // <summary>
        /// 返回List<T>集合,不采用延时加载
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        List<T> LoadListAll(Expression<Func<T, bool>> predicate);
        // <summary>
        /// 返回List<T>集合,不采用延时加载
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        List<Tmodel> LoadListAll<Tmodel>(Expression<Func<Tmodel, bool>> predicate) where Tmodel : class;
        // <summary>
        /// 返回List<T>集合,不采用延时加载（异步方式）
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        //Task<List<T>> LoadListAllAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// T-Sql方式：返回IQueryable<T>集合
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="para">Parameters参数</param>
        /// <returns></returns>
        IQueryable<T> LoadAllBySql(string sql, params DbParameter[] para);
        /// <summary>
        /// T-Sql方式：返回IQueryable<T>集合（异步方式）
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="para">Parameters参数</param>
        /// <returns></returns>
        //Task<IQueryable<T>> LoadAllBySqlAsync(string sql, params DbParameter[] para);

        /// <summary>
        /// T-Sql方式：返回List<T>集合
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="para">Parameters参数</param>
        /// <returns></returns>
        List<Tmodel> LoadListAllBySql<Tmodel>(string sql, params DbParameter[] para) where Tmodel : class;
        /// <summary>
        /// T-Sql方式：返回List<T>集合
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="para">Parameters参数</param>
        /// <returns></returns>
        List<T> LoadListAllBySql(string sql, params DbParameter[] para);
        /// <summary>
        /// T-Sql方式：返回List<T>集合（异步方式）
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="para">Parameters参数</param>
        /// <returns></returns>
        //Task<List<T>> LoadListAllBySqlAsync(string sql, params DbParameter[] para);

        /// <summary>
        /// 可指定返回结果、排序、查询条件的通用查询方法，返回实体对象集合
        /// </summary>
        /// <typeparam name="TEntity">实体对象</typeparam>
        /// <typeparam name="TOrderBy">排序字段类型</typeparam>
        /// <typeparam name="TResult">数据结果，与TEntity一致</typeparam>
        /// <param name="where">过滤条件，需要用到类型转换的需要提前处理与数据表一致的</param>
        /// <param name="orderby">排序字段</param>
        /// <param name="selector">返回结果（必须是模型中存在的字段）</param>
        /// <param name="isAsc">排序方向，true为正序false为倒序</param>
        /// <returns>实体集合</returns>
        List<TResult> QueryEntity<TEntity, TOrderBy, TResult>(Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, TOrderBy>> orderby, Expression<Func<TEntity, TResult>> selector, bool isAsc)
           where TEntity : class
           where TResult : class;
        /// <summary>
        /// 可指定返回结果、排序、查询条件的通用查询方法，返回实体对象集合（异步方式）
        /// </summary>
        /// <typeparam name="TEntity">实体对象</typeparam>
        /// <typeparam name="TOrderBy">排序字段类型</typeparam>
        /// <typeparam name="TResult">数据结果，与TEntity一致</typeparam>
        /// <param name="where">过滤条件，需要用到类型转换的需要提前处理与数据表一致的</param>
        /// <param name="orderby">排序字段</param>
        /// <param name="selector">返回结果（必须是模型中存在的字段）</param>
        /// <param name="isAsc">排序方向，true为正序false为倒序</param>
        /// <returns>实体集合</returns>
        /*Task<List<TResult>> QueryEntityAsync<TEntity, TOrderBy, TResult>(Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, TOrderBy>> orderby, Expression<Func<TEntity, TResult>> selector, bool isAsc)
             where TEntity : class
             where TResult : class;*/

        /// <summary>
        /// 可指定返回结果、排序、查询条件的通用查询方法，返回Object对象集合
        /// </summary>
        /// <typeparam name="TEntity">实体对象</typeparam>
        /// <typeparam name="TOrderBy">排序字段类型</typeparam>
        /// <param name="where">过滤条件，需要用到类型转换的需要提前处理与数据表一致的</param>
        /// <param name="orderby">排序字段</param>
        /// <param name="selector">返回结果（必须是模型中存在的字段）</param>
        /// <param name="isAsc">排序方向，true为正序false为倒序</param>
        /// <returns>自定义实体集合</returns>
        List<object> QueryObject<TEntity, TOrderBy>(Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, TOrderBy>> orderby, Func<IQueryable<TEntity>, List<object>> selector, bool isAsc)
           where TEntity : class;
        /// <summary>
        /// 可指定返回结果、排序、查询条件的通用查询方法，返回Object对象集合（异步方式）
        /// </summary>
        /// <typeparam name="TEntity">实体对象</typeparam>
        /// <typeparam name="TOrderBy">排序字段类型</typeparam>
        /// <param name="where">过滤条件，需要用到类型转换的需要提前处理与数据表一致的</param>
        /// <param name="orderby">排序字段</param>
        /// <param name="selector">返回结果（必须是模型中存在的字段）</param>
        /// <param name="isAsc">排序方向，true为正序false为倒序</param>
        /// <returns>自定义实体集合</returns>
        /*Task<List<object>> QueryObjectAsync<TEntity, TOrderBy>(Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, TOrderBy>> orderby, Func<IQueryable<TEntity>, List<object>> selector, bool isAsc)
            where TEntity : class;*/
        #endregion

        #region 验证是否存在
        /// <summary>
        /// 验证当前条件是否存在相同项
        /// </summary>
        bool IsExist(Expression<Func<T, bool>> predicate);
        /// <summary>
        /// 验证当前条件是否存在相同项
        /// </summary>
        bool IsExist<Tmodel>(Expression<Func<Tmodel, bool>> predicate) where Tmodel : class;
        /// <summary>
        /// 验证当前条件是否存在相同项（异步方式）
        /// </summary>
        //Task<bool> IsExistAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// 根据SQL验证实体对象是否存在
        /// </summary>
        bool IsExist(string sql, params DbParameter[] para);
        /// <summary>
        /// 根据SQL验证实体对象是否存在（异步方式）
        /// </summary>
        //Task<bool> IsExistAsync(string sql, params DbParameter[] para);
        #endregion

        /// <summary>
        /// 更新部分字段
        /// </summary>
        /// <returns></returns>
        bool Update<Tmodel>(Tmodel entity, string[] fileds, bool isCommit = true) where Tmodel : class;

        /// <summary>
        /// 根据SQL语句返回DataTable
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        DataTable SqlQueryForDataTable(string sql, params DbParameter[] parameters);
        /// <summary>
        /// 根据SQL语句返回一个值
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="sqlType"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        object SqlQueryForScalar(string sql, CommandType sqlType = CommandType.Text, params DbParameter[] parameters);
    }
}
