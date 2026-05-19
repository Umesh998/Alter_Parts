//// Data/ApplicationDbContext.cs
//using Alter_Parts.Migrations;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore;

//public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
//{
//    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
//        : base(options) { }

//    // Your existing EMS tables
//    public DbSet<Part> Parts { get; set; }
//    public DbSet<BomItem> BomItems { get; set; }
//    public DbSet<Project> Projects { get; set; }

//    protected override void OnModelCreating(ModelBuilder builder)
//    {
//        base.OnModelCreating(builder); // ← CRITICAL: must call base first

//        // Rename Identity tables to cleaner names (optional but clean)
//        builder.Entity<ApplicationUser>().ToTable("Users");
//        builder.Entity<IdentityRole>().ToTable("Roles");
//        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
//        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
//        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
//        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
//        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");

//        // Index for faster lookups
//        builder.Entity<ApplicationUser>()
//            .HasIndex(u => u.Department);
//    }
//}