namespace Neurotoxin.Soulshot.Tests.Models
{
    public class TestContext3 : DbContext
    {
        //public DbSet<EntityBase> TestTable { get; set; }

        public TestContext3(string connectionString) : base(connectionString)
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EntityBase>()
                .ToTable("EntityBase")
                .HasKey(a => a.Id);
        }
    }
}