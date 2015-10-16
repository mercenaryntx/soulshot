namespace Neurotoxin.Soulshot.Tests.Models
{
    public class TestContext4 : DbContext
    {
        public DbSet<Sample> Sample { get; set; }

        public TestContext4(string connectionString) : base(connectionString)
        {
        }
    }
}