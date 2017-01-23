using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neurotoxin.Soulshot.Tests
{
    [TestClass]
    public class DbSetTests
    {
        private readonly string _cnstr = "Server=.;Initial Catalog=TestDb;Integrated security=True;";

        [TestMethod]
        public void ReadAll()
        {
            using (var context = new TestContext(_cnstr))
            {
                var sw = new Stopwatch();
                sw.Start();
                var entities = context.TestTable.ToList();
                Console.WriteLine("{0} Select {1}", entities.Count, sw.Elapsed);
            }
        }

        [TestMethod]
        public void Read100()
        {
            using (var context = new TestContext(_cnstr))
            {
                var sw = new Stopwatch();
                sw.Start();
                var entities = context.TestTable.Take(100).ToList();
                Assert.AreEqual(entities.Count, 100);
                Console.WriteLine("{0} Select {1}", entities.Count, sw.Elapsed);
            }
        }

        [TestMethod]
        public void Count()
        {
            using (var context = new TestContext(_cnstr))
            {
                var sw = new Stopwatch();
                sw.Start();
                var c = context.TestTable.Count();
                Console.WriteLine("Count {0}: {1}", c, sw.Elapsed);
            }
        }

        [TestMethod]
        public void SelectScalarList()
        {
            using (var context = new TestContext(_cnstr))
            {
                var sw = new Stopwatch();
                sw.Start();
                var c = context.TestTable.Select(e => e.Name).ToList();
                Console.WriteLine("Count {0}: {1}", c.Count, sw.Elapsed);
            }
        }

        [TestMethod]
        public void SelectGreaterThan()
        {
            using (var context = new TestContext(_cnstr))
            {
                var sw = new Stopwatch();
                sw.Start();
                var c = context.TestTable.Where(e => e.Id > 400).ToList();
                Console.WriteLine("Count {0}: {1}", c.Count, sw.Elapsed);
            }
        }

        [TestMethod]
        public void SelectGreaterThanVariable()
        {
            var x = 400;
            using (var context = new TestContext(_cnstr))
            {
                var sw = new Stopwatch();
                sw.Start();
                var c = context.TestTable.Where(e => e.Id > x).ToList();
                Console.WriteLine("Count {0}: {1}", c.Count, sw.Elapsed);
            }
        }

        [TestMethod]
        public void SelectContains()
        {
            using (var context = new TestContext(_cnstr))
            {
                var sw = new Stopwatch();
                sw.Start();
                var ids = new[] { 100, 200, 300, 400, 500 };
                var c = context.TestTable.Where(e => ids.Contains(e.Id)).ToList();
                Console.WriteLine("Count {0}: {1}", c.Count, sw.Elapsed);
            }
        }

        [TestMethod]
        public void SelectOr()
        {
            using (var context = new TestContext(_cnstr))
            {
                var sw = new Stopwatch();
                sw.Start();
                var c = context.TestTable.Where(e => e.Id == 100 || e.Id == 200 || e.Id == 300).ToList();
                Console.WriteLine("Count {0}: {1}", c.Count, sw.Elapsed);
            }
        }

        [TestMethod]
        public void SelectAndOr()
        {
            using (var context = new TestContext(_cnstr))
            {
                var sw = new Stopwatch();
                sw.Start();
                var c = context.TestTable.Where(e => e.Name == "Lorem ipsum" && (e.Id == 100 || e.Id == 200 || e.Id == 300)).ToList();
                Console.WriteLine("Count {0}: {1}", c.Count, sw.Elapsed);
            }
        }

        [TestMethod]
        public void SelectStartsWith()
        {
            using (var context = new TestContext(_cnstr))
            {
                var sw = new Stopwatch();
                sw.Start();
                var c = context.TestTable.Where(e => e.Name.StartsWith("Lorem ipsum")).ToList();
                Console.WriteLine("Count {0}: {1}", c.Count, sw.Elapsed);
            }
        }

        [TestMethod]
        public void SelectEndsWith()
        {
            using (var context = new TestContext(_cnstr))
            {
                var sw = new Stopwatch();
                sw.Start();
                var c = context.TestTable.Where(e => e.Name.EndsWith("ipsum")).ToList();
                Console.WriteLine("Count {0}: {1}", c.Count, sw.Elapsed);
            }
        }

        [TestMethod]
        public void SelectStringContains()
        {
            using (var context = new TestContext(_cnstr))
            {
                var sw = new Stopwatch();
                sw.Start();
                var c = context.TestTable.Where(e => e.Name.Contains("ipsum")).ToList();
                Console.WriteLine("Count {0}: {1}", c.Count, sw.Elapsed);
            }
        }

        [TestMethod]
        public void SelectOrderBy()
        {
            using (var context = new TestContext(_cnstr))
            {
                var sw = new Stopwatch();
                sw.Start();
                var c = context.TestTable.OrderByDescending(e => e.Id).ThenBy(e => e.Name).ToList();
                Console.WriteLine("Count {0}: {1}", c.Count, sw.Elapsed);
            }
        }

        //[TestMethod]
        //[Microsoft.VisualStudio.TestTools.UnitTesting.Ignore]
        //public void ForeignKeysWriteAndReadBack()
        //{
        //    var sw = new Stopwatch();
        //    sw.Start();
        //    using (var context = new TestContext2(_cnstr))
        //    {
        //        Console.WriteLine("Init: " + sw.Elapsed);
        //        sw.Restart();
        //        var hungary = new Country { Name = "Hungary" };
        //        var address = new Address
        //        {
        //            Street = "Futo utca",
        //            CurrentCity = new City { Name = "Budapest", Country = hungary },
        //            Hometown = new City { Name = "Mosonmagyarovar", Country = hungary }
        //        };
        //        context.Address.Add(address);
        //        context.SaveChanges();
        //        Console.WriteLine("Insert: " + sw.Elapsed);

        //        sw.Restart();
        //        var stored = context.Address.First(a => a.Street == "Futo utca");
        //        Console.WriteLine("Select: " + sw.Elapsed);

        //        Assert.AreNotEqual(stored.Id, 0);
        //        Assert.AreEqual(address.Street, stored.Street);
        //        Assert.AreNotEqual(stored.CurrentCity.Id, 0);
        //        Assert.AreEqual(address.CurrentCity.Name, stored.CurrentCity.Name);
        //        Assert.AreNotEqual(stored.Hometown.Id, 0);
        //        Assert.AreEqual(address.Hometown.Name, stored.Hometown.Name);
        //    }
        //}

        ////[TestMethod]
        ////public void CascadeDelete()
        ////{
        ////    var sw = new Stopwatch();
        ////    sw.Start();
        ////    using (var context = new TestContext2(_cnstr))
        ////    {
        ////        Console.WriteLine("Init: " + sw.Elapsed);
        ////        sw.Restart();

        ////        context.Address.Remove(a => a.Street == "Futo utca");
        ////        Console.WriteLine("Delete: " + sw.Elapsed);
        ////    }            
        ////}

        //[TestMethod]
        //[Microsoft.VisualStudio.TestTools.UnitTesting.Ignore]
        //public void SelectByJoinedTableValue()
        //{
        //    var sw = new Stopwatch();
        //    sw.Start();
        //    using (var context = new TestContext2(_cnstr))
        //    {
        //        Console.WriteLine("Init: " + sw.Elapsed);
        //        sw.Restart();

        //        var stored = context.Address.Where(a => a.CurrentCity.Country.Name == "Hungary" || a.Hometown.Country.Name == "Hungary").Select(a => a.Street).First();
        //        Console.WriteLine("Select: " + sw.Elapsed);
        //    }
        //}

        //[TestMethod]
        //[Microsoft.VisualStudio.TestTools.UnitTesting.Ignore]
        //public void OneToMany()
        //{
        //    var sw = new Stopwatch();
        //    sw.Start();
        //    using (var context = new TestContext2(_cnstr))
        //    {
        //        Console.WriteLine("Init: " + sw.Elapsed);
        //        sw.Restart();
        //        var country = context.Countries.First(c => c.Name == "Hungary");
        //        Console.WriteLine("Select: " + sw.Elapsed);
        //    }
        //}

        //[TestMethod]
        //[Microsoft.VisualStudio.TestTools.UnitTesting.Ignore]
        //public void Update()
        //{
        //    using (var context = new TestContext(_cnstr))
        //    {
        //        var sw = new Stopwatch();
        //        sw.Start();
        //        //context.TestTable.Where(e => e.Id == 1).Update();
        //        //Console.WriteLine("Count {0}: {1}", c.Count, sw.Elapsed);
        //    }

        //}

        //[TestMethod]
        //public void GetTimePart()
        //{
        //    using (var context = new TestContext(_cnstr))
        //    {
        //        var sw = new Stopwatch();
        //        sw.Start();
        //        var ts = new TimeSpan(10, 0, 0);
        //        var c = context.TestTable.Count(e => e.Date.TimeOfDay > ts);
        //        Console.WriteLine("Count {0}: {1}", c, sw.Elapsed);
        //    }
        //}

        //[TestMethod]
        //public void OnModelCreating()
        //{
        //    using (var context = new TestContext3(_cnstr))
        //    {
        //        var sw = new Stopwatch();
        //        sw.Start();
        //        var ts = new TimeSpan(10, 0, 0);
        //        var c = context.Set<EntityBase>().Count(e => e.Date.TimeOfDay > ts);
        //        Console.WriteLine("Count {0}: {1}", c, sw.Elapsed);
        //    }
        //}

        //[TestMethod]
        //public void WriteAndReadBackWithCustomMapper()
        //{
        //    var sw = new Stopwatch();
        //    sw.Start();
        //    using (var context = new TestContext4(_cnstr))
        //    {
        //        Console.WriteLine("Init " + sw.Elapsed);
        //        sw.Restart();

        //        var guid = Guid.NewGuid();
        //        var newEntity = new Sample
        //        {
        //            EntityId = guid,
        //            Name = "Lorem ipsum",
        //            Location = DbGeography.FromText("LINESTRING(-122.360 47.656, -122.343 47.656 )")
        //        };
        //        context.Sample.Add(newEntity);
        //        Console.WriteLine("Add " + sw.Elapsed);
        //        sw.Restart();
        //        context.SaveChanges();
        //        Console.WriteLine("Persist " + sw.Elapsed);
        //        sw.Restart();

        //        var storedEntity = context.Sample.SingleOrDefault(e => e.EntityId == guid);
        //        Console.WriteLine("Select " + sw.Elapsed);

        //        Assert.IsNotNull(storedEntity);
        //        Assert.AreEqual(newEntity.Location.ToString(), storedEntity.Location.ToString());
        //    }
        //}

        //[TestMethod]
        //public void InsertInTransactionSpeed()
        //{
        //    using (var context = new TestContext(_cnstr))
        //    {
        //        var sw = new Stopwatch();
        //        sw.Start();
        //        for (var i = 0; i < 100; i++)
        //        {
        //            var guid = Guid.NewGuid();
        //            var newEntity = new ClassD
        //            {
        //                EntityId = guid,
        //                Name = "Lorem ipsum",
        //                NumberOfSomething = 5
        //            };
        //            context.TestTable.Add(newEntity);
        //            Console.WriteLine("Add " + sw.Elapsed);
        //            sw.Restart();
        //        }
        //        context.SaveChanges();
        //        Console.WriteLine("Persist " + sw.Elapsed);
        //        sw.Restart();
        //    }
        //}

        //[TestMethod]
        //public void InsertNotInTransactionSpeed()
        //{
        //    using (var context = new TestContext(_cnstr))
        //    {
        //        var sum = new Stopwatch();
        //        sum.Start();
        //        var sw = new Stopwatch();
        //        sw.Start();
        //        for (var i = 0; i < 100; i++)
        //        {
        //            var guid = Guid.NewGuid();
        //            var newEntity = new ClassD
        //            {
        //                EntityId = guid,
        //                Name = "Lorem ipsum",
        //                NumberOfSomething = 5
        //            };
        //            context.TestTable.Add(newEntity);
        //            Console.WriteLine("Add " + sw.Elapsed);
        //            sw.Restart();
        //            context.SaveChanges();
        //            Console.WriteLine("Persist " + sw.Elapsed);
        //            sw.Restart();
        //        }
        //        Console.WriteLine("-----");
        //        Console.WriteLine(sum.Elapsed);
        //    }
        //}
    }
}