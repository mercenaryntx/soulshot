using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Neurotoxin.Soulshot.Annotations;
using Neurotoxin.Soulshot.Extensions;
using Neurotoxin.Soulshot.Query;

namespace Neurotoxin.Soulshot
{
    public abstract class DataEngineBase : IDataEngine
    {
        public ColumnMapper ColumnMapper { get; set; }

        protected DataEngineBase()
        {
            ColumnMapper = new ColumnMapper();
        }

        public bool TableExists<TEntity>()
        {
            var t = typeof(TEntity);
            return TableExists(t.GetTableAttribute());
        }

        public virtual void CreateTable(TableAttribute table, IEnumerable<ColumnInfo> columns, bool generateConstraints = true)
        {
            var create = new CreateTableExpression(new TableExpression(table));
            
            foreach (var column in columns)
            {
                create.AddColumn(new ColumnDefinitionExpression(column));
            }

            if (generateConstraints)
                create.AddConstraint(GetPrimaryKeyConstraint(table, columns, ExpressionType.Default));
            
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
            AppendConstraints(table, GetPrimaryKeyConstraint(table, columns, ExpressionType.Add));
        }

        public virtual void AppendConstraints(TableAttribute table, ConstraintExpression constraint)
        {
            var alter = new AlterTableExpression(new TableExpression(table));
            alter.AddConstraint(constraint);
            ExecuteNonQuery(alter);
        }

        public virtual void RemoveConstraint(TableAttribute table, IEnumerable<string> constraints)
        {
            var alter = new AlterTableExpression(new TableExpression(table));
            foreach (var constraint in constraints)
            {
                alter.DropConstraint(constraint);
            }
            ExecuteNonQuery(alter);
        }

        private ConstraintExpression GetPrimaryKeyConstraint(TableAttribute table, IEnumerable<ColumnInfo> columns, ExpressionType nodeType)
        {
            var pk = new ConstraintExpression("PK_" + table.FullName.Replace(".", "_"), nodeType)
            {
                ConstraintType = ConstraintType.PrimaryKey,
                IndexType = IndexType.Clustered
            };
            foreach (var column in columns.Where(c => c.IsIdentity))
            {
                pk.AddColumn(new ColumnOrderExpression(column.ToColumnExpression(), ListSortDirection.Ascending));
            }
            return pk.Columns != null ? pk : null;
        }

        public ColumnInfoCollection UpdateTable<TEntity>(TableAttribute table, ColumnInfoCollection actualColumns, ColumnInfoCollection storedColumns)
        {
            if (TableExists(table))
            {
                if (storedColumns != null && !actualColumns.Equals(storedColumns))
                {
                    var tmpTable = new TableAttribute(table.Name + "_tmp", table.Schema);
                    CreateTable(tmpTable, actualColumns, false);
                    CopyValues(table, tmpTable, actualColumns.Where(c => storedColumns.Any(cc => cc.ColumnName == c.ColumnName)));
                    var foreignReferences = GetForeignReferences(table).GroupBy(k => new TableAttribute(k.TableName, k.TableSchema));
                    foreach (var group in foreignReferences)
                    {
                        RemoveConstraint(group.Key, group.Select(c => c.ConstraintName));
                    }
                    DeleteTable(table);
                    RenameTable(tmpTable, table);
                    AppendConstraints(table, actualColumns);
                    foreach (var group in foreignReferences)
                    {
                        var constraint = group.Select(c => new ConstraintExpression(c.ConstraintName, ExpressionType.Add)
                            {
                                ConstraintType = ConstraintType.ForeignKey,
                                Columns = new ColumnExpression(c.SourceColumn),
                                ReferenceTable = new TableExpression(table),
                                ReferenceColumn = new ColumnExpression(c.TargetColumn)
                            });
                        foreach (var expression in constraint)
                        {
                            AppendConstraints(group.Key, expression);
                        }
                    }
                }
            }
            else
            {
                CreateTable(table, actualColumns);
            }
            return actualColumns;
        }

        private void CopyValues(TableAttribute fromTable, TableAttribute toTable, IEnumerable<ColumnInfo> columns)
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

        protected InsertExpression CreateInsertExpression(object entity, TableAttribute table, ColumnInfoCollection columns)
        {
            var values = new ValuesExpression();
            var insert = new InsertExpression(new TableExpression(table)) { Values = values };
            var type = entity.GetType();
            if (type.Module.ScopeName == "FooBar") type = type.BaseType;
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
            return insert;
        }

        protected virtual void Insert(IEntityProxy entity, TableAttribute table, ColumnInfoCollection columns)
        {
            var insert = CreateInsertExpression(entity, table, columns);
            ExecuteNonQuery(insert);
        }

        protected virtual void Update(IEntityProxy entity, TableAttribute table, IEnumerable<ColumnInfo> columns)
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
                var pi = type.GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                var column = columns.First(c => c.DescribesProperty(pi));
                //update.AddSet(new SetExpression(column.ToColumnExpression(), Expression.Constant(pi.GetValue(entity))));
                update.AddSet(column.ToEqualExpression(entity));
            }
            ExecuteNonQuery(update);
        }

        protected virtual void Delete(IEntityProxy entity, TableAttribute table, IEnumerable<ColumnInfo> columns)
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

        public abstract void ExecuteNonQueryExpression(Expression expression);
        public abstract IEnumerable ExecuteQueryExpression(Type elementType, Expression expression);
        public abstract object ExecuteScalarExpression(Expression expression, Type type);

        protected object MapType(Type type, Dictionary<string, object> values, ColumnInfoCollection columns)
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
            var instance = (IEntityProxy)Activator.CreateInstance(proxyType);

            foreach (var kvp in values.Skip(skip))
            {
                var name = kvp.Key;
                var value = kvp.Value;

                columns.SetValue(instance, name, value, ColumnMapper);
            }
            instance.ClearDirty();
            return instance;
        }

        protected abstract IEnumerable<ConstraintInfo> GetForeignReferences(TableAttribute table);

        public abstract bool TableExists(TableAttribute table);
        public abstract void RenameTable(TableAttribute oldName, TableAttribute newName);
        public abstract void DeleteTable(TableAttribute table);
        public abstract void CommitChanges(IEnumerable entities, TableAttribute table, ColumnInfoCollection columns);
        public abstract void BulkInsert(IEnumerable entities, TableAttribute table, ColumnInfoCollection columns);
        public abstract string GetLiteral(object value);
        protected abstract void ExecuteNonQuery(Expression expression);
        protected abstract IEnumerable ExecuteQuery(Type elementType, Expression expression);
        protected abstract object ExecuteScalar(Expression expression, Type type);
        public abstract void Dispose();
    }
}