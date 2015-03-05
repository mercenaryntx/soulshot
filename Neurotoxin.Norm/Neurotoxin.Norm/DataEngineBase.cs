using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
            
            foreach (var column in columns)
            {
                create.AddColumn(new ColumnDefinitionExpression(column));
            }

            if (generateConstraints)
            {
                foreach (var constraint in GetConstraints(table, columns, ExpressionType.Default))
                {
                    create.AddConstraint(constraint);
                }
            }
            
            ExecuteNonQuery(create);
            foreach (var column in columns.Where(c => c.IndexType.HasValue))
            {
                var name = string.Format("IX_{0}_{1}", table.FullName.Replace(".","_"), column.ColumnName);
                var ix = new CreateIndexExpression(name, new TableExpression(table))
                {
                    IndexType = column.IndexType.Value
                };
                ix.AddColumn(column.ToColumnExpression());
                ExecuteNonQuery(ix);
            }
        }

        public virtual void AppendConstraints(TableAttribute table, IEnumerable<ColumnInfo> columns)
        {
            var alter = new AlterTableExpression(new TableExpression(table));
            foreach (var constraint in GetConstraints(table, columns, ExpressionType.Add))
            {
                alter.AddConstraint(constraint);
            }
            ExecuteNonQuery(alter);
        }

        private List<ConstraintExpression> GetConstraints(TableAttribute table, IEnumerable<ColumnInfo> columns, ExpressionType nodeType)
        {
            var result = new List<ConstraintExpression>();
            var pk = new ConstraintExpression("PK_" + table.FullName.Replace(".", "_"), nodeType)
            {
                ConstraintType = ConstraintType.PrimaryKey,
                IndexType = IndexType.Clustered
            };
            foreach (var column in columns.Where(c => c.IsIdentity))
            {
                pk.AddColumn(new ColumnOrderExpression(column.ToColumnExpression(), ListSortDirection.Ascending));
            }
            if (pk.Columns != null) result.Insert(0, pk);
            return result;
        }

        public List<ColumnInfo> UpdateTable<TEntity>(TableAttribute table, List<ColumnInfo> actualColumns, List<ColumnInfo> storedColumns)
        {
            if (TableExists(table))
            {
                if (storedColumns != null && !storedColumns.SequenceEqual(actualColumns))
                {
                    //TODO: what if there's constraint change only?
                    var tmpTable = new TableAttribute(table.Name + "_tmp", table.Schema);
                    CreateTable(tmpTable, actualColumns, false);
                    CopyValues(table, tmpTable, actualColumns.Where(c => storedColumns.Any(cc => cc.ColumnName == c.ColumnName)).ToList());
                    DeleteTable(table);
                    RenameTable(tmpTable, table);
                    AppendConstraints(table, actualColumns);
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
                if (column.IsIdentity) insert.IsIdentityInsertEnabled = true;
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

                    var value = pi.GetValue(entity);

                    if (column.IsIdentity)
                    {
                        var defaultValue = Activator.CreateInstance(pi.PropertyType);
                        if (!value.Equals(defaultValue))
                        {
                            insert.IsIdentityInsertEnabled = true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    values.AddColumn(column.ToColumnExpression());
                    values.AddValue(Expression.Constant(value));
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
            return (IEnumerable<TEntity>)ExecuteQuery(typeof(TEntity), expression);
        }

        protected object MapType(Type type, Dictionary<string, object> values, Dictionary<string, string> columns)
        {
            var entityType = type;
            var first = values.First();

            if (columns == null)
            {
                if (values.Count == 1)
                    return ColumnMapper.MapToType(first.Value);
                else 
                    throw new NotSupportedException();
            }

            var skip = 0;
            if (first.Key == ColumnMapper.DiscriminatorColumnName)
            {
                entityType = ColumnMapper.MapType(first.Value);
                skip = 1;
            }

            var proxyType = DynamicProxy.Instance.GetProxyType(entityType);
            var instance = Activator.CreateInstance(proxyType);

            foreach (var kvp in values.Skip(skip))
            {
                var name = kvp.Key;
                var value = kvp.Value;

                var propertyName = columns[name];
                var pi = entityType.GetProperty(propertyName);
                if (pi == null || !pi.CanWrite) continue;

                var mappedValue = ColumnMapper.MapToType(value, pi);
                pi.SetValue(instance, mappedValue);
            }
            return instance;
        }

        public abstract bool TableExists(TableAttribute table);
        public abstract void RenameTable(TableAttribute oldName, TableAttribute newName);
        public abstract void DeleteTable(TableAttribute table);
        public abstract void CommitChanges(IEnumerable entities, TableAttribute table, IEnumerable<ColumnInfo> columns);
        public abstract string GetLiteral(object value);
        public abstract void ExecuteNonQuery(Expression expression);
        public abstract IEnumerable ExecuteQuery(Type elementType, Expression expression);
        public abstract object ExecuteScalar(Expression expression, Type type);
        public abstract void Dispose();
    }
}