using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Neurotoxin.Soulshot.Extensions;

namespace Neurotoxin.Soulshot.Query
{
    public class SqlCommandTextVisitor : ExpressionVisitor
    {
        private readonly StringBuilder _commandBuilder = new StringBuilder();
        private readonly IDataEngine _dataEngine;
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

            var maxExpression = node as MaxExpression;
            if (maxExpression != null) return VisitMax(maxExpression);

            var containsExpression = node as ContainsExpression;
            if (containsExpression != null) return VisitContains(containsExpression);

            var likeExpression = node as LikeExpression;
            if (likeExpression != null) return VisitLike(likeExpression);

            var orderByExpression = node as OrderByExpression;
            if (orderByExpression != null) return VisitOrderBy(orderByExpression);

            var columnOrderExpression = node as ColumnOrderExpression;
            if (columnOrderExpression != null) return VisitColumnOrder(columnOrderExpression);

            var createIndexExpression = node as CreateIndexExpression;
            if (createIndexExpression != null) return VisitCreateIndex(createIndexExpression);

            var objectNameExpression = node as ObjectNameExpression;
            if (objectNameExpression != null) return VisitObjectName(objectNameExpression);

            var joinExpression = node as JoinExpression;
            if (joinExpression != null) return VisitJoin(joinExpression);

            var setExpression = node as SetExpression;
            if (setExpression != null) return VisitSet(setExpression);

            var convertExpression = node as ConvertExpression;
            if (convertExpression != null) return VisitConvert(convertExpression);

            return base.Visit(node);
        }

        protected virtual Expression VisitCreateTable(CreateTableExpression node)
        {
            Append("CREATE TABLE");
            Visit(node.Table);
            Append("(");
            Visit(node.Columns);
            if (node.Constraints != null) Append(",", false);
            Visit(node.Constraints);
            Append(")", false);
            return node;
        }

        protected virtual Expression VisitAlterTable(AlterTableExpression node)
        {
            Append("ALTER TABLE");
            Visit(node.Table);
            Visit(node.Columns);
            Visit(node.Constraints);
            return node;
        }

        protected virtual Expression VisitDropTable(DropTableExpression node)
        {
            Append("DROP TABLE");
            Visit(node.Table);
            return node;
        }

        protected virtual Expression VisitConstraint(ConstraintExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Default:
                case ExpressionType.Add:
                    if (node.NodeType == ExpressionType.Add) Append("ADD");
                    Append("CONSTRAINT");
                    Visit(node.Name);
                    switch (node.ConstraintType)
                    {
                        case ConstraintType.PrimaryKey:
                            Append("PRIMARY KEY");
                            if (node.IndexType.HasFlag(IndexType.Unique)) Append("UNIQUE");
                            Append(node.IndexType.HasFlag(IndexType.Clustered) ? "CLUSTERED" : "NONCLUSTERED");
                            break;
                        case ConstraintType.ForeignKey:
                            Append("FOREIGN KEY");
                            break;
                        default:
                            throw new NotSupportedException(node.ConstraintType.ToString());
                    }
                    Append("(");
                    Visit(node.Columns);
                    Append(")", false);
                    if (node.ReferenceTable != null)
                    {
                        Append("REFERENCES");
                        Visit(node.ReferenceTable);
                        Append("(", false);
                        Visit(node.ReferenceColumn);
                        Append(")", false);
                    }
                    break;
                case ExpressionType.Subtract:
                    Append("DROP CONSTRAINT");
                    Visit(node.Name);
                    break;
                default:
                    throw new NotSupportedException("Invalid node type: " + node.NodeType);
            }
            return node;
        }

        protected virtual Expression VisitSelect(SelectExpression node)
        {
            Append("SELECT");
            if (node.Top != null)
            {
                Append("TOP ");
                Visit(node.Top);
            }
            Visit(node.Selection);
            Append("FROM");
            Visit(node.From);
            if (node.Where != null)
            {
                Append("WHERE");
                Visit(node.Where);
            }
            Visit(node.OrderBy);
            return node;
        }

        protected virtual Expression VisitDelete(DeleteExpression node)
        {
            _useAliases = false;
            Append("DELETE FROM");
            Visit(node.From);
            if (node.Where != null)
            {
                Append("WHERE");
                Visit(node.Where);
            }
            return node;
        }

        protected virtual Expression VisitUpdate(UpdateExpression node)
        {
            _useAliases = false;
            Append("UPDATE");
            //TODO: support from
            Visit(node.Target);
            Append("SET");
            Visit(node.Set);
            if (node.Where != null)
            {
                Append("WHERE");
                Visit(node.Where);
            }
            return node;
        }

        protected virtual Expression VisitSet(SetExpression node)
        {
            VisitBinaryBranch(node.Left, node.NodeType);
            Append(ToSign(node.NodeType));
            VisitBinaryBranch(node.Right, node.NodeType);
            return node;
        }

        protected virtual Expression VisitInsert(InsertExpression node)
        {
            if (node.IsIdentityInsertEnabled)
            {
                Append("SET IDENTITY_INSERT");
                Visit(node.Into);
                Append("ON;\n");
            }
            Append("INSERT INTO");
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
                Append("(");
                Visit(node.Select.Selection);
                Append(")", false);
                Visit(node.Select);
            }

            if (node.IsIdentityInsertEnabled)
            {
                Append("\n");
                Append("SET IDENTITY_INSERT");
                Visit(node.Into);
                Append("OFF;");
            }
            return node;
        }

        protected virtual Expression VisitValues(ValuesExpression node)
        {
            Append("(");
            Visit(node.Columns);
            Append(") VALUES (", false);
            Visit(node.Values);
            Append(")", false);
            return node;
        }

        protected virtual Expression VisitListing(ListingExpression node)
        {
            Visit(node.Left);
            Append(",", false);
            Visit(node.Right);
            return node;
        }

        protected virtual Expression VisitWhere(WhereExpression node)
        {
            var left = CheckBoolean(node.Left);
            var right = CheckBoolean(node.Right);
            Visit(left);
            if (right != null)
            {
                Append(ToSign(ExpressionType.And));
                Visit(right);
            }
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
            var doSpace = true;
            if (_useAliases && node.Table != null && !string.IsNullOrEmpty(node.Table.Alias))
            {
                Append(node.Table.Alias);
                Append(".", false);
                doSpace = false;
            }

            Append("[", doSpace);
            Append(node.ColumnName.Name);
            Append("]", false);

            if (!string.IsNullOrEmpty(node.As) && node.As != node.ColumnName.Name)
            {
                Append("AS '");
                Append(node.As, false);
                Append("'", false);
            }
            return node;
        }

        protected virtual Expression VisitColumnDefinition(ColumnDefinitionExpression node)
        {
            var c = node.Column;
            Append("[");
            Append(c.ColumnName);
            Append("]", false);
            Append(c.ColumnType);
            if (c.ReferenceTable == null)
            {
                if (c.IsIdentity) Append("IDENTITY(1,1)");
                if (!c.IsNullable) Append("NOT");
                Append("NULL");
                if (c.DefaultValue != null && !c.IsIdentity)
                {
                    Append("DEFAULT");
                    Append(_dataEngine.GetLiteral(c.DefaultValue));
                }
            }
            else
            {
                //TODO: use DbSet instead
                var pkFields = _dataEngine.ColumnMapper[c.ReferenceTable.BaseType].Where(cc => cc.IsIdentity).Select(cc => cc.ColumnName);

                if (!c.IsNullable) Append("NOT NULL");
                Append("FOREIGN KEY REFERENCES");
                Append(string.Format("{0}({1})", c.ReferenceTable.BaseType.GetTableAttribute().FullName, string.Join(",", pkFields)));
            }
            return node;
        }

        protected virtual Expression VisitTable(TableExpression node)
        {
            var pattern = _useAliases ? "{0} {1}" : "{0}";
            Append(string.Format(pattern, node.Table.FullNameWithBrackets, node.Alias));
            return node;
        }

        protected virtual Expression VisitAsterisk(AsteriskExpression node)
        {
            Append("*");
            return node;
        }

        protected virtual Expression VisitSqlPart(SqlPartExpression node)
        {
            Append(node.Value);
            return node;
        }

        protected virtual Expression VisitCount(CountExpression node)
        {
            Append("COUNT(1)");
            return node;
        }

        protected virtual Expression VisitMax(MaxExpression node)
        {
            Append("MAX(");
            Visit(node.Column);
            Append(")");
            return node;
        }

        protected virtual Expression VisitContains(ContainsExpression node)
        {
            Visit(node.Column);
            Append("IN (");
            Visit(node.Content);
            Append(")", false);
            return node;
        }

        protected virtual Expression VisitLike(LikeExpression node)
        {
            Visit(node.Column);
            Append("LIKE");
            Visit(node.Value);
            return node;
        }

        protected virtual Expression VisitOrderBy(OrderByExpression node)
        {
            Append("ORDER BY");
            Visit(node.By);
            return node;
        }

        protected virtual Expression VisitColumnOrder(ColumnOrderExpression node)
        {
            Visit(node.Column);
            Append(node.Direction == ListSortDirection.Ascending ? "ASC" : "DESC");
            return node;
        }

        protected virtual Expression VisitCreateIndex(CreateIndexExpression node)
        {
            Append("CREATE");
            VisitIndexType(node.IndexType);
            Append("INDEX");
            Append(node.Name);
            Append("ON");
            Visit(node.Table);
            Append("(");
            Visit(node.Columns);
            Append(")", false);
            return node;
        }

        protected virtual Expression VisitObjectName(ObjectNameExpression node)
        {
            Append("[");
            Append(node.Name);
            Append("]", false);
            return node;
        }

        protected virtual Expression VisitJoin(JoinExpression node)
        {
            switch (node.JoinType)
            {
                case JoinType.Inner:
                    Append("INNER");
                    break;
                case JoinType.Left:
                    Append("LEFT");
                    break;
                case JoinType.Right:
                    Append("RIGHT");
                    break;
                case JoinType.Full:
                    Append("FULL OUTER");
                    break;
                default:
                    throw new NotSupportedException("Invalid join type: " + node.JoinType);
            }
            Append("JOIN");
            Visit(node.Table);
            Append("ON");
            Visit(node.Condition);
            return node;
        }

        protected virtual Expression VisitConvert(ConvertExpression node)
        {
            Append("CONVERT(");
            Append(node.ToType.ToString());
            Append(",");
            Visit(node.Column);
            Append(")", false);
            return node;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            VisitBinaryBranch(node.Left, node.NodeType);
            Append(ToSign(node.NodeType));
            VisitBinaryBranch(node.Right, node.NodeType);
            return node;
        }

        private void VisitBinaryBranch(Expression branch, ExpressionType parentNodeType)
        {
            var doBracket = false;
            var binary = branch as BinaryExpression;
            if (binary != null && binary.NodeType != parentNodeType) doBracket = true;

            if (doBracket) Append("(");
            Visit(branch);
            if (doBracket) Append(")", false);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            Append(_dataEngine.GetLiteral(node.Value));
            return node;
        }

        protected virtual void VisitIndexType(IndexType type)
        {
            if (type.HasFlag(IndexType.Unique)) Append("UNIQUE");
            Append(type.HasFlag(IndexType.Clustered) ? "CLUSTERED" : "NONCLUSTERED");
        }

        protected virtual string ToSign(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return "AND";
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return "OR";
                case ExpressionType.Add:
                    return "+";
                case ExpressionType.Equal:
                    return "=";
                case ExpressionType.NotEqual:
                    return "!=";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.Assign:
                    return "=";
                case ExpressionType.Default:
                    return string.Empty;
                default:
                    throw new NotImplementedException(type.ToString());
            }
        }

        private static readonly char[] Characters = { ' ', '(', '[', '\n' };

        private void Append(string s, bool doSpace = true)
        {
            if (doSpace && _commandBuilder.Length > 0 && !Characters.Contains(_commandBuilder[_commandBuilder.Length - 1]))
            {
                _commandBuilder.Append(" ");    
            }
            _commandBuilder.Append(s);
        }

    }
}