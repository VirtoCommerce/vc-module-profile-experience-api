using EntityFrameworkCore.Triggers;
using Microsoft.EntityFrameworkCore;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Repositories
{
    public class ProfileExperienceApiModuleDbContext : DbContextWithTriggers
    {
        public ProfileExperienceApiModuleDbContext(DbContextOptions<ProfileExperienceApiModuleDbContext> options)
          : base(options)
        {
        }

        protected ProfileExperienceApiModuleDbContext(DbContextOptions options)
            : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //        modelBuilder.Entity<ProfileExperienceApiModuleEntity>().ToTable("MyModule").HasKey(x => x.Id);
            //        modelBuilder.Entity<ProfileExperienceApiModuleEntity>().Property(x => x.Id).HasMaxLength(128);
            //        base.OnModelCreating(modelBuilder);
        }
    }
}

