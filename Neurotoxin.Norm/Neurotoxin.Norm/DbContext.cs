using System;
using System.Linq;
using System.Reflection;
using Neurotoxin.Norm.Annotations;
using Neurotoxin.Norm.Extensions;

namespace Neurotoxin.Norm
{
    public abstract class DbContext : IDisposable
    {
        private readonly IDataEngine _dataEngine;

        protected DbContext(string connectionString)
        {
            _dataEngine = new MssqlDataEngine(connectionString);
            var migrationHistory = new DbSet<ColumnInfo>(_dataEngine);

            var iDbSet = typeof(IDbSet);
            foreach (var pi in GetType().GetProperties().Where(pi => iDbSet.IsAssignableFrom(pi.PropertyType)))
            {
                var table = pi.GetAttribute<TableAttribute>() ??
                            pi.PropertyType.GetGenericArguments().First().GetAttribute<TableAttribute>() ??
                            new TableAttribute(pi.Name);
                var c = migrationHistory.Where(e => e.TableName == table.Name).Where(e => e.TableSchema == table.Schema).Where(e => e.IsNullable).Select(e => e.ColumnName);
                var columns = c.ToList();
                //var columns = migrationHistory.Where(e => e.TableName == table.Name && e.TableSchema == table.Schema).ToList();
                var ctor = pi.PropertyType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
                                                          new [] {table.GetType(), columns.GetType(), typeof (IDataEngine)}, null);
                var dbSet = (IDbSet)ctor.Invoke(new object[] {table, columns, _dataEngine});

                //var dbSet = (IDbSet)Activator.CreateInstance(pi.PropertyType, BindingFlags.NonPublic | BindingFlags.Instance, table, columns, _dataEngine);
                //if (dbSet.Columns != columns)
                //{
                //    migrationHistory.Remove(e => e.TableName == table.Name && e.TableSchema == table.Schema);
                //    foreach (var column in dbSet.Columns)
                //    {
                //        //TODO: it should be an IList
                //        migrationHistory.Add(column);
                //    }
                //    migrationHistory.SaveChanges();
                //}
                pi.SetValue(this, dbSet);
            }
        }

        public void Dispose()
        {
            _dataEngine.Dispose();
        }
    }
}