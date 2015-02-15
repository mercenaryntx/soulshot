using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

        public List<ColumnInfo> UpdateTable<TEntity>(TableAttribute table, List<ColumnInfo> storedColumns)
        {
            var actualColumns = ColumnMapper.Map<TEntity>(table);
            if (TableExists(table))
            {
                if (storedColumns != null && !storedColumns.SequenceEqual(actualColumns))
                {
                    var tmpTable = new TableAttribute(table.Name + "_tmp", table.Schema);
                    RenameTable(table, tmpTable);
                    CreateTable(table, actualColumns);
                    //TODO: copy values
                }
            }
            else
            {
                CreateTable(table, actualColumns);
            }
            return actualColumns;
        }

        public abstract bool TableExists(TableAttribute table);
        protected abstract void CreateTable(TableAttribute table, IEnumerable<ColumnInfo> columns);
        public abstract void RenameTable(TableAttribute oldName, TableAttribute newName);

        public IEnumerable<TEntity> Execute<TEntity>(Expression expression)
        {
            return (IEnumerable<TEntity>)Execute(typeof(TEntity), expression);
        }

        public abstract IEnumerable Execute(Type elementType, Expression expression);
        public abstract void Dispose();
    }
}