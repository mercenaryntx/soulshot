namespace Neurotoxin.Soulshot.Tests.Models
{
    public class TestContext2 : DbContext
    {
        //public DbSet<Person> People { get; set; }
        public DbSet<Address> Address { get; set; }

        public TestContext2(string connectionString) : base(connectionString)
        {
        }
    }
}