using DBUtility.EF.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBUtility.EF.Core
{
    public class DbContextEx : IDisposable
    {
        public readonly ApplicationDbContext db;

        public DbContextEx(string nameOrConnectionString, params string[] assemblys)
        {
            this.db = new ApplicationDbContext(nameOrConnectionString, assemblys);
        }

        public void Dispose()
        {
            db?.Dispose();
        }
    }
}
