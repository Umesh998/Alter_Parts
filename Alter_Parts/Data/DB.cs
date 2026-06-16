//using Alter_Parts.Models;
//using Microsoft.EntityFrameworkCore;

//namespace Alter_Parts.Data
//{
//    public class DB : DbContext
//    {
//        public DB()
//        {

//        }

//        public DB(DbContextOptions<DB> options) : base(options)
//        {

//        }

//        public DbSet<Alter> More_Fruits { get; set; }
//    }
//}







using Alter_Parts.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace Alter_Parts.Data
{
    public class DB : IdentityDbContext<ApplicationUser>  // ← only base class changes
    {
        public DB()
        {

        }

        public DB(DbContextOptions<DB> options) : base(options)
        {

        }

        // ── Your existing table — completely untouched ─────────────────────────
        public DbSet<Alter> More_Fruits { get; set; }

        // ── Identity tables are added automatically by IdentityDbContext ───────
        // AspNetUsers, AspNetRoles, AspNetUserRoles,
        // AspNetUserClaims, AspNetRoleClaims, AspNetUserLogins, AspNetUserTokens

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);  // ← required: seeds all Identity schema

            // Optional: rename the default Identity table names to cleaner ones
            builder.Entity<ApplicationUser>().ToTable("Users");
            builder.Entity<IdentityRole>().ToTable("Roles");
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
        }
    }
}