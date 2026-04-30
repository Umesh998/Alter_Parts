using Alter_Parts.Models;
using Microsoft.EntityFrameworkCore;

namespace Alter_Parts.Data
{
    public class DB : DbContext
    {
        public DB()
        {

        }

        public DB(DbContextOptions<DB> options) : base(options)
        {

        }

        public DbSet<Alter> More_Fruits { get; set; }
    }
}

