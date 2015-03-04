﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neurotoxin.Norm.Annotations;
using Neurotoxin.Norm.Tests.Models;
using TestContext = Neurotoxin.Norm.Tests.Models.TestContext;

namespace Neurotoxin.Norm.Tests
{
    [TestClass]
    public class DbSetTests
    {
        [TestMethod]
        public void ColumnMapping()
        {
            var table = new TableAttribute("Lorem", "ipsum");
            var columns = ColumnMapper.Map<EntityBase>(table);

            Assert.IsTrue(columns.All(c => c.TableName == "Lorem" && c.TableSchema == "ipsum"));

            var discriminator = columns[0];
            Assert.AreEqual(discriminator.ColumnName, ColumnMapper.DiscriminatorColumnName);
            Assert.AreEqual(discriminator.ColumnType, "nvarchar(max)");
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

            Assert.AreEqual(columns.Count, 8);
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
        public void Select101()
        {
            using (var context = new TestContext("Server=.;Initial Catalog=TestDb;Integrated security=True;"))
            {
                var sw = new Stopwatch();
                sw.Start();
                var c = context.TestTable.Select(e => e.Name).ToList();
                Console.WriteLine("Count {0}: {1}", c, sw.Elapsed);
            }
        }

    }
}