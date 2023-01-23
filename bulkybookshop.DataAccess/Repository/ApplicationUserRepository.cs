using bulkybook.DataAccess.Repository;
using Bulkybook.DataAccess.Repository.IRepository;
using bulkybookshop.DataAccess.Data;
using bulkybookshop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bulkybookshop.DataAccess.Repository
{
    public class ApplicationUserRepository : Repository<ApplicationUser>, IApplicationUserRepository
    {
        private readonly ApplicationDbContext _db;

        public ApplicationUserRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        }
}
