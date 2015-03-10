using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neurotoxin.Soulshot.Annotations;
using Neurotoxin.Soulshot.Tests.Models;
using TestContext = Neurotoxin.Soulshot.Tests.Models.TestContext;

namespace Neurotoxin.Soulshot.Tests
{
    [TestClass]
    public class DbSetTests
    {
        [TestMethod]
        public void ColumnMapping()
        {
            var table = new TableAttribute("Lorem", "ipsum");
            var mapper = new ColumnMapper();
            HashSet<IDbSet> relatedDbSets;
            var columns = mapper.Map<EntityBase>(table, out relatedDbSets);

            Assert.IsTrue(columns.All(c => c.TableName == "Lorem" && c.TableSchema == "ipsum"));

            var discriminator = columns[0];
            Assert.AreEqual(discriminator.ColumnName, ColumnMapper.DiscriminatorColumnName);
            Assert.AreEqual(discriminator.ColumnType, "nvarchar(255)");
            Assert.IsNull(discriminator.DeclaringTypes);
            Assert.IsNull(discriminator.PropertyName);

            /* EntityBase */
            var seqEntityBase = new List<Type> { typeof (EntityBase) };

            var id = columns[1];
            Assert.AreEqual(id.ColumnName, "Id");
            Assert.AreEqual(id.ColumnType, "int");
            Assert.IsTrue(id.DeclaringTypes.SequenceEqual(seqEntityBase));
            Assert.AreEqual(id.PropertyName, "Id");

            var entityId = columns[2];
            Assert.AreEqual(entityId.ColumnName, "EntityId");
            Assert.AreEqual(entityId.ColumnType, "uniqueidentifier");
            Assert.IsTrue(entityId.DeclaringTypes.SequenceEqual(seqEntityBase));
            Assert.AreEqual(entityId.PropertyName, "EntityId");

            var name = columns[3];
            Assert.AreEqual(name.ColumnName, "Name");
            Assert.AreEqual(name.ColumnType, "nvarchar(max)");
            Assert.IsTrue(name.DeclaringTypes.SequenceEqual(seqEntityBase));
            Assert.AreEqual(name.PropertyName, "Name");

            /* Class A */
            var seqClassA = new List<Type> { typeof(ClassA) };
            var seqClassAB = new List<Type> { typeof(ClassA), typeof(ClassB) };

            var number1 = columns[4];
            Assert.AreEqual(number1.ColumnName, "NumberOfSomething");
            Assert.AreEqual(number1.ColumnType, "int");
            Assert.IsTrue(number1.DeclaringTypes.SequenceEqual(seqClassAB));
            Assert.AreEqual(number1.PropertyName, "NumberOfSomething");

            var createdOn = columns[5];
            Assert.AreEqual(createdOn.ColumnName, "CreatedOn");
            Assert.AreEqual(createdOn.ColumnType, "datetime2");
            Assert.IsTrue(createdOn.DeclaringTypes.SequenceEqual(seqClassA));
            Assert.AreEqual(createdOn.PropertyName, "CreatedOn");

            /* Class B */
            var seqClassBC = new List<Type> { typeof(ClassB), typeof(ClassC) };

            var text = columns[6];
            Assert.AreEqual(text.ColumnName, "Text");
            Assert.AreEqual(text.ColumnType, "nvarchar(max)");
            Assert.IsTrue(text.DeclaringTypes.SequenceEqual(seqClassBC));
            Assert.AreEqual(text.PropertyName, "Text");

            /* Class D */
            var seqClassD = new List<Type> { typeof(ClassD) };

            var number2 = columns[7];
            Assert.AreEqual(number2.ColumnName, "ClassDNumberOfSomething");
            Assert.AreEqual(number2.ColumnType, "bigint");
            Assert.IsTrue(number2.DeclaringTypes.SequenceEqual(seqClassD));
            Assert.AreEqual(number2.PropertyName, "NumberOfSomething");

            /* Class E */
            var seqClassE = new List<Type> { typeof(ClassE) };

            var lorem = columns[8];
            Assert.AreEqual(lorem.ColumnName, "Lorem");
            Assert.AreEqual(lorem.ColumnType, "nvarchar(max)");
            Assert.IsTrue(lorem.DeclaringTypes.SequenceEqual(seqClassE));
            Assert.AreEqual(lorem.PropertyName, "Lorem");

            Assert.AreEqual(columns.Count, 9);
        }

        [TestMethod]
        public void WriteAndReadBack()
        {
            var sw = new Stopwatch();
            sw.Start();
            using (var context = new TestContext("Server=.;Initial Catalog=TestDb;Integrated security=True;"))
            {
                Console.WriteLine("Init " + sw.Elapsed);
                sw.Restart();

                var guid = Guid.NewGuid();
                var newEntity = new ClassD
                {
                    EntityId = guid,
                    Name = "Lorem ipsum",
                    NumberOfSomething = 5
                };
                context.TestTable.Add(newEntity);
                Console.WriteLine("Add " + sw.Elapsed);
                sw.Restart();
                context.SaveChanges();
                Console.WriteLine("Persist " + sw.Elapsed);
                sw.Restart();

                var storedEntity = context.TestTable.SingleOrDefault(e => e.EntityId == guid);
                Console.WriteLine("Select " + sw.Elapsed);

                Assert.IsInstanceOfType(storedEntity, typeof(ClassD));

                var classD = (ClassD) storedEntity;
                Assert.AreEqual(classD.Name, newEntity.Name);
                Assert.AreEqual(classD.NumberOfSomething, newEntity.NumberOfSomething);
                Assert.AreNotEqual(classD.Id, 0);
            }
        }

        [TestMethod]
        public void InsertInTransactionSpeed()
        {
            using (var context = new TestContext("Server=.;Initial Catalog=TestDb;Integrated security=True;"))
            {
                var sw = new Stopwatch();
                sw.Start();
                for (var i = 0; i < 100; i++)
                {
                    var guid = Guid.NewGuid();
                    var newEntity = new ClassD
                    {
                        EntityId = guid,
                        Name = "Lorem ipsum",
                        NumberOfSomething = 5
                    };
                    context.TestTable.Add(newEntity);
                    Console.WriteLine("Add " + sw.Elapsed);
                    sw.Restart();
                }
                context.SaveChanges();
                Console.WriteLine("Persist " + sw.Elapsed);
                sw.Restart();
            }
        }

        [TestMethod]
        public void InsertNotInTransactionSpeed()
        {
            using (var context = new TestContext("Server=.;Initial Catalog=TestDb;Integrated security=True;"))
            {
                var sum = new Stopwatch();
                sum.Start();
                var sw = new Stopwatch();
                sw.Start();
                for (var i = 0; i < 100; i++)
                {
                    var guid = Guid.NewGuid();
                    var newEntity = new ClassD
                    {
                        EntityId = guid,
                        Name = "Lorem ipsum",
                        NumberOfSomething = 5
                    };
                    context.TestTable.Add(newEntity);
                    Console.WriteLine("Add " + sw.Elapsed);
                    sw.Restart();
                    context.SaveChanges();
                    Console.WriteLine("Persist " + sw.Elapsed);
                    sw.Restart();
                }
                Console.WriteLine("-----");
                Console.WriteLine(sum.Elapsed);
            }
        }

        [TestMethod]
        public void ReadAll()
        {
            using (var context = new TestContext("Server=.;Initial Catalog=TestDb;Integrated security=True;"))
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
            using (var context = new TestContext("Server=.;Initial Catalog=TestDb;Integrated security=True;"))
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
            using (var context = new TestContext("Server=.;Initial Catalog=TestDb;Integrated security=True;"))
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
            using (var context = new TestContext("Server=.;Initial Catalog=TestDb;Integrated security=True;"))
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
            using (var context = new TestContext("Server=.;Initial Catalog=TestDb;Integrated security=True;"))
            {
                var sw = new Stopwatch();
                sw.Start();
                var c = context.TestTable.Where(e => e.Id > 15000).ToList();
                Console.WriteLine("Count {0}: {1}", c.Count, sw.Elapsed);
            }
        }

        [TestMethod]
        public void SelectContains()
        {
            using (var context = new TestContext("Server=.;Initial Catalog=TestDb;Integrated security=True;"))
            {
                var sw = new Stopwatch();
                sw.Start();
                var ids = new[] {100, 200, 300, 400, 500};
                var c = context.TestTable.Where(e => ids.Contains(e.Id)).ToList();
                Console.WriteLine("Count {0}: {1}", c.Count, sw.Elapsed);
            }
        }

        [TestMethod]
        public void SelectOr()
        {
            using (var context = new TestContext("Server=.;Initial Catalog=TestDb;Integrated security=True;"))
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
            using (var context = new TestContext("Server=.;Initial Catalog=TestDb;Integrated security=True;"))
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
            using (var context = new TestContext("Server=.;Initial Catalog=TestDb;Integrated security=True;"))
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
            using (var context = new TestContext("Server=.;Initial Catalog=TestDb;Integrated security=True;"))
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
            using (var context = new TestContext("Server=.;Initial Catalog=TestDb;Integrated security=True;"))
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
            using (var context = new TestContext("Server=.;Initial Catalog=TestDb;Integrated security=True;"))
            {
                var sw = new Stopwatch();
                sw.Start();
                var c = context.TestTable.OrderByDescending(e => e.Id).ThenBy(e => e.Name).ToList();
                Console.WriteLine("Count {0}: {1}", c.Count, sw.Elapsed);
            }
        }

        [TestMethod]
        public void ForeignKeysWriteAndReadBack()
        {
            var sw = new Stopwatch();
            sw.Start();
            using (var context = new TestContext2("Server=.;Initial Catalog=TestDb;Integrated security=True;"))
            {
                Console.WriteLine("Init: " + sw.Elapsed);
                sw.Restart();
                var hungary = new Country { Name = "Hungary" };
                var address = new Address
                {
                    Street = "Futo utca",
                    CurrentCity = new City { Name = "Budapest", Country = hungary },
                    Hometown = new City { Name = "Mosonmagyarovar", Country = hungary }
                };
                context.Address.Add(address);
                context.SaveChanges();
                Console.WriteLine("Insert: " + sw.Elapsed);
                
                sw.Restart();
                var stored = context.Address.First(a => a.Street == "Futo utca");
                Console.WriteLine("Select: " + sw.Elapsed);

                Assert.AreNotEqual(stored.Id, 0);
                Assert.AreEqual(address.Street, stored.Street);
                Assert.AreNotEqual(stored.CurrentCity.Id, 0);
                Assert.AreEqual(address.CurrentCity.Name, stored.CurrentCity.Name);
                Assert.AreNotEqual(stored.Hometown.Id, 0);
                Assert.AreEqual(address.Hometown.Name, stored.Hometown.Name);
            }
        }

        [TestMethod]
        public void CascadeDelete()
        {
            var sw = new Stopwatch();
            sw.Start();
            using (var context = new TestContext2("Server=.;Initial Catalog=TestDb;Integrated security=True;"))
            {
                Console.WriteLine("Init: " + sw.Elapsed);
                sw.Restart();

                context.Address.Remove(a => a.Street == "Futo utca");
                Console.WriteLine("Delete: " + sw.Elapsed);
            }            
        }

    }
}