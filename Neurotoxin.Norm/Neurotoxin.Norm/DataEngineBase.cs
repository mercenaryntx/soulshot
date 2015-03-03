using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Neurotoxin.Norm.Annotations;
using Neurotoxin.Norm.Extensions;
using Neurotoxin.Norm.Query;

namespace Neurotoxin.Norm
{
    public abstract class DataEngineBase : IDataEngine
    {
        public bool TableExists<TEntity>()
        {
            var t = typeof(TEntity);
            return TableExists(t.GetTableAttribute());
        }

        public List<ColumnInfo> CreateTable<TEntity>()
        {
            return CreateTable<TEntity>(typeof(TEntity).GetTableAttribute());
        }

        public List<ColumnInfo> CreateTable<TEntity>(TableAttribute table)
        {
            var columns = ColumnMapper.Map<TEntity>(table);
            CreateTable(table, columns);
            return columns;
        }

        public virtual void CreateTable(TableAttribute table, IEnumerable<ColumnInfo> columns, bool generateConstraints = true)
        {
            var create = new CreateTableExpression(new TableExpression(table));
            var pk = new ConstraintExpression(Expression.Constant("PK_" + table.FullName), ConstraintType.PrimaryKey | ConstraintType.Clustered);
            foreach (var column in columns)
            {
                create.AddColumn(new ColumnDefinitionExpression(column));
                if (column.IsIdentity)
                {
                    //TODO: proper PK and sort order handling
                    pk.AddColumn(new SqlPartExpression(string.Format("[{0}] ASC", column.ColumnName)));
                }
            }

            if (generateConstraints && pk.Columns != null) create.AddConstraint(pk);
            //TODO: support indexes

            ExecuteNonQuery(create);
        }

        public List<ColumnInfo> UpdateTable<TEntity>(TableAttribute table, List<ColumnInfo> storedColumns)
        {
            var actualColumns = ColumnMapper.Map<TEntity>(table);
            if (TableExists(table))
            {
                if (storedColumns != null && !storedColumns.SequenceEqual(actualColumns))
                {
                    var tmpTable = new TableAttribute(table.Name + "_tmp", table.Schema);
                    CreateTable(tmpTable, actualColumns, false);
                    CopyValues(table, tmpTable, actualColumns.Where(c => storedColumns.Any(cc => cc.ColumnName == c.ColumnName)).ToList());
                    DeleteTable(table);
                    RenameTable(tmpTable, table);
                    //TODO: append constraints
                }
            }
            else
            {
                CreateTable(table, actualColumns);
            }
            return actualColumns;
        }

        private void CopyValues(TableAttribute fromTable, TableAttribute toTable, List<ColumnInfo> columns)
        {
            var select = new SelectExpression(new TableExpression(fromTable));
            var insert = new InsertExpression(new TableExpression(toTable)) { Select = select };
            foreach (var column in columns)
            {
                select.AddSelection(column.ToColumnExpression());
            }
            ExecuteNonQuery(insert);
        }

        protected virtual void Insert(IProxy entity, TableAttribute table, IEnumerable<ColumnInfo> columns)
        {
            var values = new ValuesExpression();
            var insert = new InsertExpression(new TableExpression(table)) { Values = values };
            var type = entity.GetType().BaseType;
            var discriminator = columns.SingleOrDefault(c => c.IsDiscriminatorColumn);
            if (discriminator != null)
            {
                values.AddColumn(discriminator.ToColumnExpression());
                values.AddValue(Expression.Constant(type.FullName));
            }

            foreach (var pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                try
                {
                    var column = columns.SingleOrDefault(c => c.DescribesProperty(pi));
                    if (column == null) continue;

                    //TODO: support identity insert
                    if (column.IsIdentity) continue;

                    values.AddColumn(column.ToColumnExpression());
                    values.AddValue(Expression.Constant(pi.GetValue(entity)));
                }
                catch (Exception ex)
                {
                    Debugger.Break();
                }
            }

            ExecuteNonQuery(insert);
        }

        protected virtual void Update(IProxy entity, TableAttribute table, IEnumerable<ColumnInfo> columns)
        {
            //TODO: do not let an identity field to be dirtied
            var update = new UpdateExpression(new TableExpression(table));
            foreach (var column in columns.Where(c => c.IsIdentity))
            {
                update.AddWhere(column.ToEqualExpression(entity));
            }
            var type = entity.GetType();
            foreach (var property in entity.DirtyProperties)
            {
                var pi = type.GetProperty(property);
                var column = columns.First(c => c.DescribesProperty(pi));
                update.AddSet(new SetExpression(column.ToColumnExpression(), Expression.Constant(pi.GetValue(entity))));
            }
            ExecuteNonQuery(update);
        }

        protected virtual void Delete(IProxy entity, TableAttribute table, IEnumerable<ColumnInfo> columns)
        {
            var delete = new DeleteExpression(new TableExpression(table));
            foreach (var column in columns.Where(c => c.IsIdentity))
            {
                delete.AddWhere(column.ToEqualExpression(entity));
            }
            ExecuteNonQuery(delete);
        }

        public IEnumerable<TEntity> Execute<TEntity>(Expression expression)
        {
            return (IEnumerable<TEntity>)Execute(typeof(TEntity), expression);
        }

        protected object MapType(Type type, Dictionary<string, object> values)
        {
            var entityType = type;
            var first = values.First();
            var skip = 0;
            if (first.Key == ColumnMapper.DiscriminatorColumnName)
            {
                entityType = ColumnMapper.MapType(first.Value);
                skip = 1;
            }

            var columns = ColumnMapper.Cache[type];
            var proxyType = DynamicProxy.Instance.GetProxyType(entityType);
            var instance = Activator.CreateInstance(proxyType);

            foreach (var kvp in values.Skip(skip))
            {
                var name = kvp.Key;
                var value = kvp.Value;

                var propertyName = columns.Single(c => c.ColumnName == name).PropertyName;
                var pi = entityType.GetProperty(propertyName);
                if (pi == null || !pi.CanWrite) continue;
                pi.SetValue(instance, ColumnMapper.MapToPropertyValue(value, pi));
            }
            return instance;
        }

        public abstract bool TableExists(TableAttribute table);
        public abstract void RenameTable(TableAttribute oldName, TableAttribute newName);
        public abstract void DeleteTable(TableAttribute table);
        public abstract void CommitChanges(IEnumerable entities, TableAttribute table, IEnumerable<ColumnInfo> columns);
        public abstract string GetLiteral(object value);
        public abstract void ExecuteNonQuery(Expression expression);
        public abstract IEnumerable Execute(Type elementType, Expression expression);
        public abstract void Dispose();
    }
}