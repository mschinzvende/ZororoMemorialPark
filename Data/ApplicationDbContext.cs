using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data.Models;

namespace ZororoMemorialPark.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Define DbSets for your entities here

        public DbSet<tbl_AuditLogs> tbl_AuditLogs { get; set; }
        public DbSet<tbl_Sections> tbl_Sections { get; set; }

        public DbSet<tbl_DevelopmentStages> tbl_DevelopmentStages { get; set; }

        public DbSet<tbl_Graves> tbl_Graves { get; set; }
    }
}
