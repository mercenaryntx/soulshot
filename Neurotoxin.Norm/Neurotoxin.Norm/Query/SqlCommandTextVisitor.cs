using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Neurotoxin.Norm.Query
{
    public class SqlCommandTextVisitor : ExpressionVisitor
    {
        private readonly StringBuilder _commandBuilder = new StringBuilder();
        private IDataEngine _dataEngine;
        private bool _useAliases = true;

        public string CommandText
        {
            get { return _commandBuilder.ToString(); }
        }

        public SqlCommandTextVisitor(IDataEngine dataEngine)
        {
            _dataEngine = dataEngine;
        }

        public override Expression Visit(Expression node)
        {
            var alterTableExpression = node as AlterTableExpression;
            if (alterTableExpression != null) return VisitAlterTable(alterTableExpression);

            var createTableExpression = node as CreateTableExpression;
            if (createTableExpression != null) return VisitCreateTable(createTableExpression);

            var dropTableExpression = node as DropTableExpression;
            if (dropTableExpression != null) return VisitDropTable(dropTableExpression);

            var constraintExpression = node as ConstraintExpression;
            if (constraintExpression != null) return VisitConstraint(constraintExpression);

            var selectExpression = node as SelectExpression;
            if (selectExpression != null) return VisitSelect(selectExpression);

            var deleteExpression = node as DeleteExpression;
            if (deleteExpression != null) return VisitDelete(deleteExpression);

            var updateExpression = node as UpdateExpression;
            if (updateExpression != null) return VisitUpdate(updateExpression);

            var insertExpression = node as InsertExpression;
            if (insertExpression != null) return VisitInsert(insertExpression);

            var valuesExpression = node as ValuesExpression;
            if (valuesExpression != null) return VisitValues(valuesExpression);

            var listingExpression = node as ListingExpression;
            if (listingExpression != null) return VisitListing(listingExpression);

            var whereExpression = node as WhereExpression;
            if (whereExpression != null) return VisitWhere(whereExpression);

            var columnExpression = node as ColumnExpression;
            if (columnExpression != null) return VisitColumn(columnExpression);

            var columnDefinitionExpression = node as ColumnDefinitionExpression;
            if (columnDefinitionExpression != null) return VisitColumnDefinition(columnDefinitionExpression);

            var tableExpression = node as TableExpression;
            if (tableExpression != null) return VisitTable(tableExpression);

            var asteriskExpression = node as AsteriskExpression;
            if (asteriskExpression != null) return VisitAsterisk(asteriskExpression);

            var sqlPartExpression = node as SqlPartExpression;
            if (sqlPartExpression != null) return VisitSqlPart(sqlPartExpression);

            var countExpression = node as CountExpression;
            if (countExpression != null) return VisitCount(countExpression);

            return base.Visit(node);
        }

        protected virtual Expression VisitCreateTable(CreateTableExpression node)
        {
            _commandBuilder.Append("CREATE TABLE");
            Visit(node.Table);
            _commandBuilder.Append("(");
            Visit(node.Columns);
            if (node.Constraints != null) _commandBuilder.Append(",");
            Visit(node.Constraints);
            _commandBuilder.Append(")");
            return node;
        }

        protected virtual Expression VisitAlterTable(AlterTableExpression node)
        {
            _commandBuilder.Append("ALTER TABLE");
            Visit(node.Table);
            Visit(node.Columns);
            Visit(node.Constraints);
            return node;
        }

        protected virtual Expression VisitDropTable(DropTableExpression node)
        {
            _commandBuilder.Append("DROP TABLE");
            Visit(node.Table);
            return node;
        }

        protected virtual Expression VisitConstraint(ConstraintExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Add:
                    _commandBuilder.Append(" ADD");
                    break;
                case ExpressionType.Subtract:
                    _commandBuilder.Append(" DROP");
                    break;
            }
            _commandBuilder.Append(" CONSTRAINT [");
            _commandBuilder.Append(node.Name);
            _commandBuilder.Append("]");

            if (node.NodeType != ExpressionType.Subtract)
            {
                if (node.Type.HasFlag(ConstraintType.PrimaryKey)) _commandBuilder.Append(" PRIMARY KEY");
                if (node.Type.HasFlag(ConstraintType.ForeignKey)) throw new NotImplementedException();
                if (node.Type.HasFlag(ConstraintType.Clustered)) _commandBuilder.Append(" CLUSTERED");
                if (node.Type.HasFlag(ConstraintType.NonClustered)) throw new NotImplementedException();
                _commandBuilder.Append("(");
                Visit(node.Columns);
                _commandBuilder.Append(")");
            }
            return node;
        }

        protected virtual Expression VisitSelect(SelectExpression node)
        {
            _commandBuilder.Append("SELECT");
            if (node.Top != null)
            {
                _commandBuilder.Append(" TOP ");
                Visit(node.Top);
            }
            Visit(node.Selection);
            _commandBuilder.Append(" FROM");
            Visit(node.From);
            if (node.Where != null)
            {
                _commandBuilder.Append(" WHERE");
                Visit(node.Where);
            }
            return node;
        }

        protected virtual Expression VisitDelete(DeleteExpression node)
        {
            _useAliases = false;
            _commandBuilder.Append("DELETE FROM");
            Visit(node.From);
            if (node.Where != null)
            {
                _commandBuilder.Append(" WHERE");
                Visit(node.Where);
            }
            return node;
        }

        protected virtual Expression VisitUpdate(UpdateExpression node)
        {
            _commandBuilder.Append("UPDATE");
            Visit(node.Set);
            //TODO: support from
            Visit(node.Target);
            if (node.Where != null)
            {
                _commandBuilder.Append(" WHERE");
                Visit(node.Where);
            }
            return node;
        }

        protected virtual Expression VisitInsert(InsertExpression node)
        {
            if (node.IsIdentityInsertEnabled)
            {
                _commandBuilder.Append("SET IDENTITY_INSERT ");
                Visit(node.Into);
                _commandBuilder.Append(" ON;\n");
            }
            _commandBuilder.Append("INSERT INTO");
            Visit(node.Into);
            if (node.Values != null)
            {
                var useAliasesTmp = _useAliases;
                _useAliases = false;
                Visit(node.Values);
                _useAliases = useAliasesTmp;
            }
            else if (node.Select != null)
            {
                _commandBuilder.Append(" (");
                Visit(node.Select.Selection);
                _commandBuilder.Append(") ");
                Visit(node.Select);
            }

            if (node.IsIdentityInsertEnabled)
            {
                _commandBuilder.Append("\n");
                _commandBuilder.Append("SET IDENTITY_INSERT ");
                Visit(node.Into);
                _commandBuilder.Append(" OFF;");
            }            
            return node;
        }

        protected virtual Expression VisitValues(ValuesExpression node)
        {
            _commandBuilder.Append("(");
            Visit(node.Columns);
            _commandBuilder.Append(") VALUES (");
            Visit(node.Values);
            _commandBuilder.Append(")");
            return node;
        }
        
        protected virtual Expression VisitListing(ListingExpression node)
        {
            Visit(node.Left);
            _commandBuilder.Append(",");
            Visit(node.Right);
            return node;
        }

        protected virtual Expression VisitWhere(WhereExpression node)
        {
            //TODO: handle ORs
            Visit(CheckBoolean(node.Left));
            _commandBuilder.Append(" AND");
            Visit(CheckBoolean(node.Right));
            return node;
        }

        protected virtual Expression CheckBoolean(Expression node)
        {
            var columnExpression = node as ColumnExpression;
            return columnExpression != null
                ? Expression.MakeBinary(ExpressionType.Equal, columnExpression, Expression.Constant(true))
                : node;
        }

        protected virtual Expression VisitColumn(ColumnExpression node)
        {
            var pattern = _useAliases && !string.IsNullOrEmpty(node.Alias) ? " {0}.[{1}]" : " [{1}]";
            _commandBuilder.Append(string.Format(pattern, node.Alias, node.ColumnName));
            return node;
        }

        protected virtual Expression VisitColumnDefinition(ColumnDefinitionExpression node)
        {
            _commandBuilder.Append("[");
            _commandBuilder.Append(node.Column.ColumnName);
            _commandBuilder.Append("] ");
            _commandBuilder.Append(node.Column.ColumnType);
            if (node.Column.IsIdentity) _commandBuilder.Append(" IDENTITY(1,1)");
            if (!node.Column.IsNullable) _commandBuilder.Append(" NOT");
            _commandBuilder.Append(" NULL");
            if (node.Column.DefaultValue != null && !node.Column.IsIdentity)
            {
                _commandBuilder.Append(" DEFAULT ");
                _commandBuilder.Append(_dataEngine.GetLiteral(node.Column.DefaultValue));
            }
            return node;
        }

        protected virtual Expression VisitTable(TableExpression node)
        {
            var pattern = _useAliases ? " {0} {1}" : " {0}";
            _commandBuilder.Append(string.Format(pattern, node.Table.FullNameWithBrackets, node.Alias));
            return node;
        }

        protected virtual Expression VisitAsterisk(AsteriskExpression node)
        {
            _commandBuilder.Append(" *");
            return node;
        }

        protected virtual Expression VisitSqlPart(SqlPartExpression node)
        {
            _commandBuilder.Append(node.Value);
            return node;
        }

        protected virtual Expression VisitCount(CountExpression node)
        {
            _commandBuilder.Append(" COUNT(1)");
            return node;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            Visit(node.Left);
            _commandBuilder.Append(ToSign(node.NodeType));
            Visit(node.Right);
            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            _commandBuilder.Append(" ");
            _commandBuilder.Append(_dataEngine.GetLiteral(node.Value));
            return node;
        }

        protected virtual string ToSign(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return " AND";
                case ExpressionType.Add:
                    return " +";
                case ExpressionType.Equal:
                    return " =";
                case ExpressionType.NotEqual:
                    return " !=";
                default:
                    throw new NotImplementedException(type.ToString());
            }
        }

    }
}
