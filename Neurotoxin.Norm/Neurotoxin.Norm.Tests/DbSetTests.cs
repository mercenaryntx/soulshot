using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestContext = Neurotoxin.Norm.Tests.Models.TestContext;

namespace Neurotoxin.Norm.Tests
{
    [TestClass]
    public class DbSetTests
    {
        [TestMethod]
        public void PropertyMapping01_ColumnTypes()
        {
            using (var context = new TestContext("Server=ephubudw2021.budapest.epam.com;Initial Catalog=TestDb;User Id=FCPDB;Password=FCP@DB;"))
            {
                
            }
        }
    }
}