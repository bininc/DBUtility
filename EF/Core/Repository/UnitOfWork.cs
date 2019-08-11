using DBUtility.EF.Core.IRepository;
using DBUtility.EF.Data;
using System;

namespace DBUtility.EF.Core.Repository
{
    /// <summary>
    /// 工作单元实现类
    /// </summary>
    public class UnitOfWork : IUnitOfWork, IDisposable
    {

        #region 数据上下文
        /// <summary>
        /// 数据上下文
        /// </summary>
        private readonly ApplicationDbContext _context;

        public UnitOfWork(DbContextEx context)
        {
            _context = context.db;
        }
        #endregion

        public bool Commit()
        {
            try
            {
                int rows = _context.SaveChanges();
                return rows > 0;
            }
            catch(Exception e)
            {
                LogHelper.WriteError(e, "UnitOfWork.Commit");
                return false;
            }
        }

        public void Dispose()
        {
            if (_context != null)
            {
                _context.Dispose();
                GC.SuppressFinalize(this);
            }
        }
    }
}
