using DBUtility.EF.Core.IRepository;
using DBUtility.EF.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DBUtility.EF.Core.Repository
{
    /// <summary>
    /// 仓储实现类
    /// </summary>
    /// <typeparam name="T">实体模型</typeparam>
    public class Repository<T> : IRepository<T> where T : class
    {
        #region 数据上下文

        /// <summary>
        /// 数据上下文
        /// </summary>
        private readonly ApplicationDbContext _context;

        public Repository(DbContextEx context)
        {
            _context = context.db;
        }

        #endregion

        #region 公共

        public void AttachEntity<Tmodel>(Tmodel entity) where Tmodel : class
        {
            try
            {
                _context.Set<Tmodel>().Attach(entity);
            }
            catch (Exception ex)
            {
                LogHelper.Debug("实体Attach异常", ex);
            }
        }
        #endregion

        #region 单模型 CRUD 操作

        /// <summary>
        /// 增加一条记录
        /// </summary>
        /// <param name="entity">实体模型</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        public virtual bool Save(T entity, bool isCommit = true)
        {

            _context.Set<T>().Add(entity);

            if (isCommit)
            {
                try
                {
                    return _context.SaveChanges() > 0;
                }
                catch (Exception ex)
                {
                    LogHelper.Error(ex, "Save(T entity");
                    return false;
                }
            }
            else
                return false;
        }

        /// <summary>
        /// 增加一条记录
        /// </summary>
        /// <param name="entity">实体模型</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        public virtual bool Save<Tmodel>(Tmodel entity, bool isCommit = true) where Tmodel : class
        {
            _context.Set<Tmodel>().Add(entity);
            if (isCommit)
                return _context.SaveChanges() > 0;
            else
                return false;
        }

        /// <summary>
        /// 增加一条记录(异步方式)
        /// </summary>
        /// <param name="entity">实体模型</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        /*public virtual async Task<bool> SaveAsync(T entity, bool isCommit = true)
        {
            _context.Set<T>().Add(entity);
            if (isCommit)
                return await TaskEx.Run(() => _context.SaveChanges() > 0);
            else
                return await TaskEx.Run(() => false);
        }*/

        /// <summary>
        /// 更新一条记录
        /// </summary>
        /// <param name="entity">实体模型</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        public virtual bool Update(T entity, bool isCommit = true)
        {
            AttachEntity(entity);
            _context.Entry<T>(entity).State = EntityState.Modified;
            if (isCommit)
                return _context.SaveChanges() > 0;
            else
                return false;
        }

        public bool Update<Tmodel>(Tmodel entity, bool isCommit = true) where Tmodel : class
        {
            AttachEntity(entity);
            _context.Entry<Tmodel>(entity).State = EntityState.Modified;
            if (isCommit)
                return _context.SaveChanges() > 0;
            else
                return false;
        }


        /// <summary>
        /// 更新部分字段
        /// </summary>
        /// <returns></returns>
        public virtual bool Update<Tmodel>(Tmodel entity, string[] fileds, bool isCommit = true) where Tmodel : class
        {
            if (entity == null || fileds == null || fileds.Length == 0) return false;
            try
            {
                //AttachEntity(entity);
                DbEntityEntry<Tmodel> entry = _context.Entry<Tmodel>(entity);
                entry.State = EntityState.Unchanged;
                foreach (string filed in fileds)
                {
                    if (!string.IsNullOrWhiteSpace(filed))
                    {
                        entry.Property(filed).IsModified = true;
                    }
                }

                if (isCommit)
                    return _context.SaveChanges() > 0;
                else
                    return false;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, "Update<Tmodel>(Tmodel entity, string[] fileds");
                return false;
            }
        }


        /// <summary>
        /// 更新一条记录（异步方式）
        /// </summary>
        /// <param name="entity">实体模型</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        /*public virtual async Task<bool> UpdateAsync(T entity, bool isCommit = true)
        {
            AttachEntity(entity);
            _context.Entry<T>(entity).State = EntityState.Modified;
            if (isCommit)
                return await TaskEx.Run(() => { return _context.SaveChanges() > 0; });
            else
                return await TaskEx.Run(() => false);
        }*/

        /// <summary>
        /// 增加或更新一条记录
        /// </summary>
        /// <param name="entity">实体模型</param>
        /// <param name="isSave">是否增加</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        public virtual bool SaveOrUpdate(T entity, bool isSave, bool isCommit = true)
        {
            return isSave ? Save(entity, isCommit) : Update(entity, isCommit);
        }

        public bool SaveOrUpdate<Tmodel>(Tmodel entity, bool isSave, bool isCommit = true) where Tmodel : class
        {
            return isSave ? Save<Tmodel>(entity, isCommit) : Update<Tmodel>(entity, isCommit);
        }

        /// <summary>
        /// 增加或更新一条记录（异步方式）
        /// </summary>
        /// <param name="entity">实体模型</param>
        /// <param name="isSave">是否增加</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        /*public virtual async Task<bool> SaveOrUpdateAsync(T entity, bool isSave, bool isCommit = true)
        {
            return isSave ? await SaveAsync(entity, isCommit) : await UpdateAsync(entity, isCommit);
        }*/

        /// <summary>
        /// 通过Lamda表达式获取实体
        /// </summary>
        /// <param name="predicate">Lamda表达式（p=>p.Id==Id）</param>
        /// <returns></returns>
        public virtual T Get(Expression<Func<T, bool>> predicate)
        {
            try
            {
                return _context.Set<T>().AsNoTracking().SingleOrDefault(predicate);
            }
            catch (Exception ex)
            {
                LogHelper.WriteError(ex);
                if (Debugger.IsAttached)
                    throw;
                return null;
            }
        }

        /// <summary>
        /// 通过Lamda表达式获取实体
        /// </summary>
        /// <param name="predicate">Lamda表达式（p=>p.Id==Id）</param>
        /// <returns></returns>
        public virtual Tmodel Get<Tmodel>(Expression<Func<Tmodel, bool>> predicate) where Tmodel : class
        {
            return _context.Set<Tmodel>().AsNoTracking().SingleOrDefault(predicate);
        }

        /// <summary>
        /// 通过Lamda表达式获取实体（异步方式）
        /// </summary>
        /// <param name="predicate">Lamda表达式（p=>p.Id==Id）</param>
        /// <returns></returns>
        /*public virtual async Task<T> GetAsync(Expression<Func<T, bool>> predicate)
        {
            return await TaskEx.Run(() => _context.Set<T>().AsNoTracking().SingleOrDefault(predicate));
        }*/

        /// <summary>
        /// 删除一条记录
        /// </summary>
        /// <param name="entity">实体模型</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        public virtual bool Delete(T entity, bool isCommit = true)
        {
            if (entity == null) return false;
            AttachEntity(entity);
            _context.Set<T>().Remove(entity);

            if (isCommit)
                return _context.SaveChanges() > 0;
            else
                return false;
        }
        /// <summary>
        /// 删除一条记录（异步方式）
        /// </summary>
        /// <param name="entity">实体模型</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        /*public virtual async Task<bool> DeleteAsync(T entity, bool isCommit = true)
        {
            if (entity == null) return await TaskEx.Run(() => false);
            AttachEntity(entity);
            _context.Set<T>().Remove(entity);
            if (isCommit)
                return await TaskEx.Run(() => _context.SaveChanges() > 0);
            else
                return await TaskEx.Run(() => false); ;
        }*/

        #endregion

        #region 多模型 操作

        /// <summary>
        /// 增加多条记录，同一模型
        /// </summary>
        /// <param name="T1">实体模型集合</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        public virtual bool SaveList(List<T> T1, bool isCommit = true)
        {
            if (T1 == null || T1.Count == 0) return false;

            T1.ToList().ForEach(item =>
            {
                _context.Set<T>().Add(item);
            });

            if (isCommit)
                return _context.SaveChanges() > 0;
            else
                return false;
        }
        /// <summary>
        /// 增加多条记录，同一模型（异步方式）
        /// </summary>
        /// <param name="T1">实体模型集合</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        /*public virtual async Task<bool> SaveListAsync(List<T> T1, bool isCommit = true)
        {
            if (T1 == null || T1.Count == 0) return await TaskEx.Run(() => false);

            T1.ToList().ForEach(item =>
            {
                _context.Set<T>().Add(item);
            });

            if (isCommit)
                return await TaskEx.Run(() => _context.SaveChanges() > 0);
            else
                return await TaskEx.Run(() => false);
        }*/

        /// <summary>
        /// 增加多条记录，独立模型
        /// </summary>
        /// <param name="T1">实体模型集合</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        public virtual bool SaveList<T1>(List<T1> T, bool isCommit = true) where T1 : class
        {
            if (T == null || T.Count == 0) return false;
            var tmp = _context.ChangeTracker.Entries<T>().ToList();
            foreach (var x in tmp)
            {
                var properties = typeof(T).GetProperties();
                foreach (var y in properties)
                {
                    var entry = x.Property(y.Name);
                    entry.CurrentValue = entry.OriginalValue;
                    entry.IsModified = false;
                    y.SetValue(x.Entity, entry.OriginalValue, null);
                }
                x.State = EntityState.Unchanged;
            }
            T.ToList().ForEach(item =>
            {
                _context.Set<T1>().Add(item);
            });
            if (isCommit)
                return _context.SaveChanges() > 0;
            else
                return false;
        }
        /// <summary>
        /// 增加多条记录，独立模型（异步方式）
        /// </summary>
        /// <param name="T1">实体模型集合</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        /*public virtual async Task<bool> SaveListAsync<T1>(List<T1> T, bool isCommit = true) where T1 : class
        {
            if (T == null || T.Count == 0) return await TaskEx.Run(() => false);
            var tmp = _context.ChangeTracker.Entries<T>().ToList();
            foreach (var x in tmp)
            {
                var properties = typeof(T).GetProperties();
                foreach (var y in properties)
                {
                    var entry = x.Property(y.Name);
                    entry.CurrentValue = entry.OriginalValue;
                    entry.IsModified = false;
                    y.SetValue(x.Entity, entry.OriginalValue, null);
                }
                x.State = EntityState.Unchanged;
            }
            T.ToList().ForEach(item =>
            {
                _context.Set<T1>().Add(item);
            });
            if (isCommit)
                return await TaskEx.Run(() => _context.SaveChanges() > 0);
            else
                return await TaskEx.Run(() => false);
        }*/

        /// <summary>
        /// 更新多条记录，同一模型
        /// </summary>
        /// <param name="T1">实体模型集合</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        public virtual bool UpdateList(List<T> T1, bool isCommit = true)
        {
            if (T1 == null || T1.Count == 0) return false;

            T1.ToList().ForEach(item =>
            {
                AttachEntity(item);
                _context.Entry<T>(item).State = EntityState.Modified;
            });

            if (isCommit)
                return _context.SaveChanges() > 0;
            else
                return false;
        }
        /// <summary>
        /// 更新多条记录，同一模型（异步方式）
        /// </summary>
        /// <param name="T1">实体模型集合</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        /*public virtual async Task<bool> UpdateListAsync(List<T> T1, bool isCommit = true)
        {
            if (T1 == null || T1.Count == 0) return await TaskEx.Run(() => false);

            T1.ToList().ForEach(item =>
            {
                AttachEntity(item);
                _context.Entry<T>(item).State = EntityState.Modified;
            });

            if (isCommit)
                return await TaskEx.Run(() => _context.SaveChanges() > 0);
            else
                return await TaskEx.Run(() => false);
        }*/

        /// <summary>
        /// 更新多条记录，独立模型
        /// </summary>
        /// <param name="T1">实体模型集合</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        public virtual bool UpdateList<T1>(List<T1> T, bool isCommit = true) where T1 : class
        {
            if (T == null || T.Count == 0) return false;

            T.ToList().ForEach(item =>
            {
                AttachEntity(item);
                _context.Entry<T1>(item).State = EntityState.Modified;
            });

            if (isCommit)
                return _context.SaveChanges() > 0;
            else
                return false;
        }
        /// <summary>
        /// 更新多条记录，独立模型（异步方式）
        /// </summary>
        /// <param name="T1">实体模型集合</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        /*public virtual async Task<bool> UpdateListAsync<T1>(List<T1> T, bool isCommit = true) where T1 : class
        {
            if (T == null || T.Count == 0) return await TaskEx.Run(() => false);

            T.ToList().ForEach(item =>
            {
                AttachEntity(item);
                _context.Entry<T1>(item).State = EntityState.Modified;
            });

            if (isCommit)
                return await TaskEx.Run(() => _context.SaveChanges() > 0);
            else
                return await TaskEx.Run(() => false);
        }*/

        /// <summary>
        /// 删除多条记录，同一模型
        /// </summary>
        /// <param name="T1">实体模型集合</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        public virtual bool DeleteList(List<T> T1, bool isCommit = true)
        {
            if (T1 == null || T1.Count == 0) return false;

            T1.ToList().ForEach(item =>
            {
                AttachEntity(item);
                _context.Set<T>().Remove(item);
            });

            if (isCommit)
                return _context.SaveChanges() > 0;
            else
                return false;
        }
        /// <summary>
        /// 删除多条记录，同一模型（异步方式）
        /// </summary>
        /// <param name="T1">实体模型集合</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        /*public virtual async Task<bool> DeleteListAsync(List<T> T1, bool isCommit = true)
        {
            if (T1 == null || T1.Count == 0) return await TaskEx.Run(() => false);

            T1.ToList().ForEach(item =>
            {
                AttachEntity(item);
                _context.Set<T>().Remove(item);
            });

            if (isCommit)
                return await TaskEx.Run(() => _context.SaveChanges() > 0);
            else
                return await TaskEx.Run(() => false);
        }*/

        /// <summary>
        /// 删除多条记录，独立模型
        /// </summary>
        /// <param name="T1">实体模型集合</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        public virtual bool DeleteList<T1>(List<T1> T, bool isCommit = true) where T1 : class
        {
            if (T == null || T.Count == 0) return false;

            T.ToList().ForEach(item =>
            {
                AttachEntity(item);
                _context.Set<T1>().Remove(item);
            });

            if (isCommit)
                return _context.SaveChanges() > 0;
            else
                return false;
        }
        /// <summary>
        /// 删除多条记录，独立模型（异步方式）
        /// </summary>
        /// <param name="T1">实体模型集合</param>
        /// <param name="isCommit">是否提交（默认提交）</param>
        /// <returns></returns>
        /*public virtual async Task<bool> DeleteListAsync<T1>(List<T1> T, bool isCommit = true) where T1 : class
        {
            if (T == null || T.Count == 0) return await TaskEx.Run(() => false);

            T.ToList().ForEach(item =>
            {
                AttachEntity(item);
                _context.Set<T1>().Remove(item);
            });

            if (isCommit)
                return await TaskEx.Run(() => _context.SaveChanges() > 0);
            else
                return await TaskEx.Run(() => false);
        }*/

        /// <summary>
        /// 通过Lamda表达式，删除一条或多条记录
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="isCommit"></param>
        /// <returns></returns>
        public virtual bool Delete(Expression<Func<T, bool>> predicate, bool isCommit = true)
        {
            IQueryable<T> entry = (predicate == null) ? _context.Set<T>().AsNoTracking().AsQueryable() : _context.Set<T>().AsNoTracking().Where(predicate);
            List<T> list = entry.ToList();

            if (list != null && list.Count == 0) return false;
            list.ForEach(item =>
            {
                AttachEntity(item);
                _context.Set<T>().Remove(item);
            });

            if (isCommit)
                return _context.SaveChanges() > 0;
            else
                return false;
        }
        /// <summary>
        /// 通过Lamda表达式，删除一条或多条记录（异步方式）
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="isCommit"></param>
        /// <returns></returns>
        /*public virtual async Task<bool> DeleteAsync(Expression<Func<T, bool>> predicate, bool isCommit = true)
        {
            IQueryable<T> entry = (predicate == null) ? _context.Set<T>().AsNoTracking().AsQueryable() : _context.Set<T>().AsNoTracking().Where(predicate);
            List<T> list = entry.ToList();

            if (list != null && list.Count == 0) return await TaskEx.Run(() => false);
            list.ForEach(item =>
            {
                AttachEntity(item);
                _context.Set<T>().Remove(item);
            });

            if (isCommit)
                return await TaskEx.Run(() => _context.SaveChanges() > 0);
            else
                return await TaskEx.Run(() => false);
        }*/

        #endregion

        #region 获取多条数据操作

        /// <summary>
        /// Lamda返回IQueryable集合，延时加载数据
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public virtual IQueryable<T> LoadAll(Expression<Func<T, bool>> predicate)
        {
            return predicate != null ? _context.Set<T>().AsNoTracking().Where(predicate) : _context.Set<T>().AsNoTracking().AsQueryable();
        }
        public IQueryable<Tmodel> LoadAll<Tmodel>(Expression<Func<Tmodel, bool>> predicate) where Tmodel : class
        {
            return predicate != null ? _context.Set<Tmodel>().AsNoTracking().Where(predicate) : _context.Set<Tmodel>().AsNoTracking().AsQueryable();
        }
        /// <summary>
        /// 返回IQueryable集合，延时加载数据（异步方式）
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        /*public virtual async Task<IQueryable<T>> LoadAllAsync(Expression<Func<T, bool>> predicate)
        {
            return predicate != null ? await TaskEx.Run(() => _context.Set<T>().AsNoTracking().Where(predicate)) : await TaskEx.Run(() => _context.Set<T>().AsNoTracking<T>().AsQueryable<T>());
        }*/

        /// <summary>
        /// 返回List<T>集合,不采用延时加载
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public virtual List<T> LoadListAll(Expression<Func<T, bool>> predicate)
        {
            try
            {
                return predicate != null ? _context.Set<T>().AsNoTracking().Where(predicate).ToList() : _context.Set<T>().AsNoTracking().AsQueryable<T>().ToList();
            }
            catch (Exception ex)
            {
                LogHelper.WriteError(ex);
                return null;
            }
        }

        public List<Tmodel> LoadListAll<Tmodel>(Expression<Func<Tmodel, bool>> predicate) where Tmodel : class
        {
            try
            {
                return predicate != null ? _context.Set<Tmodel>().AsNoTracking().Where(predicate).ToList() : _context.Set<Tmodel>().AsNoTracking().AsQueryable<Tmodel>().ToList();
            }
            catch (Exception ex)
            {
                LogHelper.WriteError(ex);
                return null;
            }
        }

        // <summary>
        /// 返回List<T>集合,不采用延时加载（异步方式）
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        /*public virtual async Task<List<T>> LoadListAllAsync(Expression<Func<T, bool>> predicate)
        {
            return predicate != null ? await TaskEx.Run(() => _context.Set<T>().Where(predicate).AsNoTracking().ToList()) : await TaskEx.Run(() => _context.Set<T>().AsQueryable<T>().AsNoTracking().ToList());
        }*/

        /// <summary>
        /// T-Sql方式：返回IQueryable<T>集合
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="para">Parameters参数</param>
        /// <returns></returns>
        public virtual IQueryable<T> LoadAllBySql(string sql, params DbParameter[] para)
        {
            return _context.Set<T>().SqlQuery(sql, para).AsNoTracking().AsQueryable();
        }
        /// <summary>
        /// T-Sql方式：返回IQueryable<T>集合（异步方式）
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="para">Parameters参数</param>
        /// <returns></returns>
        /*public virtual async Task<IQueryable<T>> LoadAllBySqlAsync(string sql, params DbParameter[] para)
        {
            return await TaskEx.Run(() => _context.Set<T>().SqlQuery(sql, para).AsNoTracking().AsQueryable());
        }*/


        /// <summary>
        /// T-Sql方式：返回List<T>集合
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="para">Parameters参数</param>
        /// <returns></returns>
        public virtual List<T> LoadListAllBySql(string sql, params DbParameter[] para)
        {
            var t = _context.Set<T>().SqlQuery(sql, para).AsNoTracking();
            if (t.Any())
                return t.ToList();
            else
                return null;
        }
        /// <summary>
        /// T-Sql方式：返回List<T>集合（异步方式）
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="para">Parameters参数</param>
        /// <returns></returns>
        /*public virtual async Task<List<T>> LoadListAllBySqlAsync(string sql, params DbParameter[] para)
        {
            return await TaskEx.Run(() => _context.Set<T>().SqlQuery(sql, para).AsNoTracking().ToList());
        }*/

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
        public virtual List<TResult> QueryEntity<TEntity, TOrderBy, TResult>
            (Expression<Func<TEntity, bool>> where,
            Expression<Func<TEntity, TOrderBy>> orderby,
            Expression<Func<TEntity, TResult>> selector,
            bool isAsc)
            where TEntity : class
            where TResult : class
        {
            IQueryable<TEntity> query = _context.Set<TEntity>();
            if (where != null)
            {
                query = query.Where(where);
            }

            if (orderby != null)
            {
                query = isAsc ? query.OrderBy(orderby) : query.OrderByDescending(orderby);
            }
            if (selector == null)
            {
                return query.Cast<TResult>().AsNoTracking().ToList();
            }
            return query.Select(selector).AsNoTracking().ToList();
        }
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
        /*public virtual async Task<List<TResult>> QueryEntityAsync<TEntity, TOrderBy, TResult>
            (Expression<Func<TEntity, bool>> where,
            Expression<Func<TEntity, TOrderBy>> orderby,
            Expression<Func<TEntity, TResult>> selector,
            bool isAsc)
            where TEntity : class
            where TResult : class
        {
            IQueryable<TEntity> query = _context.Set<TEntity>();
            if (where != null)
            {
                query = query.Where(where);
            }

            if (orderby != null)
            {
                query = isAsc ? query.OrderBy(orderby) : query.OrderByDescending(orderby);
            }
            if (selector == null)
            {
                return await TaskEx.Run(() => query.Cast<TResult>().AsNoTracking().ToList());
            }
            return await TaskEx.Run(() => query.Select(selector).AsNoTracking().ToList());
        }*/

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
        public virtual List<object> QueryObject<TEntity, TOrderBy>
            (Expression<Func<TEntity, bool>> where,
            Expression<Func<TEntity, TOrderBy>> orderby,
            Func<IQueryable<TEntity>,
            List<object>> selector,
            bool isAsc)
            where TEntity : class
        {
            IQueryable<TEntity> query = _context.Set<TEntity>();
            if (where != null)
            {
                query = query.Where(where);
            }

            if (orderby != null)
            {
                query = isAsc ? query.OrderBy(orderby) : query.OrderByDescending(orderby);
            }
            if (selector == null)
            {
                return query.AsNoTracking().ToList<object>();
            }
            return selector(query);
        }
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
        /*public virtual async Task<List<object>> QueryObjectAsync<TEntity, TOrderBy>
            (Expression<Func<TEntity, bool>> where,
            Expression<Func<TEntity, TOrderBy>> orderby,
            Func<IQueryable<TEntity>,
            List<object>> selector,
            bool isAsc)
            where TEntity : class
        {
            IQueryable<TEntity> query = _context.Set<TEntity>();
            if (where != null)
            {
                query = query.Where(where);
            }

            if (orderby != null)
            {
                query = isAsc ? query.OrderBy(orderby) : query.OrderByDescending(orderby);
            }
            if (selector == null)
            {
                return await TaskEx.Run(() => query.AsNoTracking().ToList<object>());
            }
            return await TaskEx.Run(() => selector(query));
        }*/

        #endregion

        #region 验证是否存在

        /// <summary>
        /// 验证当前条件是否存在相同项
        /// </summary>
        public virtual bool IsExist(Expression<Func<T, bool>> predicate)
        {
            var entry = _context.Set<T>().Where(predicate);
            return (entry.Any());
        }

        public bool IsExist<Tmodel>(Expression<Func<Tmodel, bool>> predicate) where Tmodel : class
        {
            var entry = _context.Set<Tmodel>().Where(predicate);
            return (entry.Any());
        }

        /// <summary>
        /// 验证当前条件是否存在相同项（异步方式）
        /// </summary>
        /*public virtual async Task<bool> IsExistAsync(Expression<Func<T, bool>> predicate)
        {
            var entry = _context.Set<T>().Where(predicate);
            return await TaskEx.Run(() => entry.Any());
        }*/

        /// <summary>
        /// 根据SQL验证实体对象是否存在
        /// </summary>
        public virtual bool IsExist(string sql, params DbParameter[] para)
        {
            return _context.Database.ExecuteSqlCommand(sql, para) > 0;
        }
        /// <summary>
        /// 根据SQL验证实体对象是否存在（异步方式）
        /// </summary>
        /*public virtual async Task<bool> IsExistAsync(string sql, params DbParameter[] para)
        {
            return await TaskEx.Run(() => _context.Database.ExecuteSqlCommand(sql, para) > 0);
        }*/

        #endregion

        public List<Tmodel> LoadListAllBySql<Tmodel>(string sql, params DbParameter[] para) where Tmodel : class
        {
            try
            {
                return _context.Set<Tmodel>().SqlQuery(sql, para).AsNoTracking().ToList();
            }
            catch (Exception ex)
            {
                LogHelper.WriteError(ex);
                return null;
            }
        }

        public DataTable SqlQueryForDataTable(string sql, params DbParameter[] parameters)
        {
            return _context.Database.SqlQueryForDataTatable(sql, parameters);
        }

        public object SqlQueryForScalar(string sql, CommandType sqlType = CommandType.Text, params DbParameter[] parameters)
        {
            return _context.Database.SqlQueryScalar(sql, sqlType, parameters);
        }
    }
}
