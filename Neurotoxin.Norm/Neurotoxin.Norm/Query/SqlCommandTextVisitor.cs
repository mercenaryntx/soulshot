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
            var selectExpression = node as SelectExpression;
            if (selectExpression != null) return VisitSelect(selectExpression);

            var deleteExpression = node as DeleteExpression;
            if (deleteExpression != null) return VisitDelete(deleteExpression);

            var updateExpression = node as UpdateExpression;
            if (updateExpression != null) return VisitUpdate(updateExpression);

            var insertExpression = node as InsertExpression;
            if (insertExpression != null) return VisitInsert(insertExpression);

            var listingExpression = node as ListingExpression;
            if (listingExpression != null) return VisitListing(listingExpression);

            var whereExpression = node as WhereExpression;
            if (whereExpression != null) return VisitWhere(whereExpression);

            var columnExpression = node as ColumnExpression;
            if (columnExpression != null) return VisitColumn(columnExpression);

            var tableExpression = node as TableExpression;
            if (tableExpression != null) return VisitTable(tableExpression);

            var asteriskExpression = node as AsteriskExpression;
            if (asteriskExpression != null) return VisitAsterisk(asteriskExpression);

            return base.Visit(node);
        }

        protected virtual Expression VisitSelect(SelectExpression node)
        {
            _commandBuilder.Append("SELECT");
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
            _commandBuilder.Append("INSERT INTO ");
            Visit(node.Into);
            if (node.Values != null)
            {
                Visit(node.Values);
            }
            else if (node.Select != null)
            {
                _commandBuilder.Append(" ");
                Visit(node.Select);
            }
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
            var pattern = _useAliases ? " {0}.[{1}]" : " [{1}]";
            _commandBuilder.Append(string.Format(pattern, node.Alias, node.ColumnName));
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
