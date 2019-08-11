namespace DBUtility.EF.Core.IRepository
{
    /// <summary>
    /// 工作单元接口
    /// </summary>
    public interface IUnitOfWork
    {
        bool Commit();
    }
}
