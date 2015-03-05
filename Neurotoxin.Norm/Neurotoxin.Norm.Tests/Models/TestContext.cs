﻿namespace Neurotoxin.Norm.Tests.Models
{
    public class TestContext : DbContext
    {
        public DbSet<EntityBase> TestTable { get; set; }

        public TestContext(string connectionString) : base(connectionString)
        {
        }
    }
}