using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Neurotoxin.Soulshot.Query
{
    public class SqlCommandTextVisitor : ExpressionVisitor
    {
        private const string Missing = "<MISSING>";
        private readonly StringBuilder _commandBuilder = new StringBuilder();
        private bool _useAliases = true;
        private bool _isNullEnabled;
        private readonly TypeSwitch<Expression> _typeSwitch;

        private static readonly Dictionary<Type, Func<object, string>> TypeToSqlMapping = new Dictionary<Type, Func<object, string>>
        {
            { typeof(bool), o => (bool) o ? "1" : "0" },
            { typeof(DateTime), o => $"'{o:yyyy-MM-dd HH:mm:ss}'" },
            { typeof(double), o => ((double) o).ToString("0.00", CultureInfo.InvariantCulture) },
            { typeof(float), o => ((float) o).ToString("0.00", CultureInfo.InvariantCulture) },
            { typeof(decimal), o => ((decimal) o).ToString("0.00", CultureInfo.InvariantCulture) },
            { typeof(Enum), o => ((int)o).ToString() },
            { typeof(Guid), o => $"'{o}'" },
            { typeof(TimeSpan), o => $"'{o}'" },
            { typeof(string), o => $"N'{((string)o).Replace("'","''")}'" },
            { typeof(short), o => o.ToString() },
            { typeof(int), o => o.ToString() },
            { typeof(long), o => o.ToString() },
        }; 

        public string CommandText => _commandBuilder.ToString();
        public List<Exception> Errors { get; } = new List<Exception>();
        public ParameterizedQueryMode ParameterizedQueryMode { get; }

        public SqlCommandTextVisitor(ParameterizedQueryMode parameterizedQueryMode = ParameterizedQueryMode.ParameterizedQuery)
        {
            ParameterizedQueryMode = parameterizedQueryMode;
            _typeSwitch = new TypeSwitch<Expression>()
                .Case<CreateTableExpression>(VisitCreateTable)
                .Case<AlterTableExpression>(VisitAlterTable)
                .Case<DropTableExpression>(VisitDropTable)
                .Case<ConstraintExpression>(VisitConstraint)
                .Case<SelectExpression>(VisitSelect)
                .Case<DeleteExpression>(VisitDelete)
                .Case<UpdateExpression>(VisitUpdate)
                .Case<SetExpression>(VisitSet)
                .Case<InsertExpression>(VisitInsert)
                .Case<ValuesExpression>(VisitValues)
                .Case<ListingExpression>(VisitListing)
                .Case<WhereExpression>(VisitWhere)
                .Case<ColumnExpression>(VisitColumn)
                .Case<ColumnDefinitionExpression>(VisitColumnDefinition)
                .Case<TableExpression>(VisitTable)
                .Case<AsteriskExpression>(VisitAsterisk)
                .Case<SqlPartExpression>(VisitSqlPart)
                .Case<CountExpression>(VisitCount)
                .Case<MaxExpression>(VisitMax)
                .Case<ContainsExpression>(VisitContains)
                .Case<LikeExpression>(VisitLike)
                .Case<OrderByExpression>(VisitOrderBy)
                .Case<ColumnOrderExpression>(VisitColumnOrder)
                .Case<CreateIndexExpression>(VisitCreateIndex)
                .Case<ObjectNameExpression>(VisitObjectName)
                .Case<JoinExpression>(VisitJoin)
                .Case<ConvertExpression>(VisitConvert)
                .Case<InExpression>(VisitIn)
                .Case<ParameterExpression>(VisitParameter)
                .Default(base.Visit);
        }

        public override Expression Visit(Expression node)
        {
            return _typeSwitch.Switch(node);
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
                            Errors.Add(new NotSupportedException(node.ConstraintType.ToString()));
                            Append(Missing);
                            break;
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
                    Errors.Add(new NotSupportedException("Invalid node type: " + node.NodeType));
                    Append(Missing);
                    break;
            }
            return node;
        }

        protected virtual Expression VisitSelect(SelectExpression node)
        {
            Append("SELECT");
            if (node.Distinct)
            {
                Append("DISTINCT ");
            }
            if (node.Top != null)
            {
                Append("TOP ");
                Visit(node.Top);
            }
            Visit(node.Selection);
            Append("FROM");
            Visit(node.From);
            if (node.Joins != null)
            {
                foreach (var joinExpression in node.Joins)
                {
                    VisitJoin(joinExpression);
                }
            }
            if (node.Where != null)
            {
                _isNullEnabled = true;
                Append("WHERE");
                Visit(node.Where);
            }
            Visit(node.OrderBy);
            if (node.Union != null)
            {
                Append("UNION");
                VisitSelect(node.Union);
            }
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
            if (node.Set == null)
            {
                Errors.Add(new FormatException("List of columns to be updated needs to be specified."));
                Append(Missing);
            }
            else
            {
                Visit(node.Set);
            }
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
            Visit(node.Left);
            if (node.Right != null)
            {
                Append(ToSign(ExpressionType.And));
                Visit(node.Right);
            }
            return node;
        }

        protected virtual Expression VisitColumn(ColumnExpression node)
        {
            var doSpace = true;
            if (_useAliases && !string.IsNullOrEmpty(node.Table?.Alias))
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
            if (c.IsIdentity) Append("IDENTITY(1,1)");
            if (!c.IsNullable) Append("NOT");
            Append("NULL");
            if (c.DefaultValue != null && !c.IsIdentity)
            {
                Append("DEFAULT");
                Append(GetLiteralInner(c.DefaultValue));
            }
            return node;
        }

        protected virtual Expression VisitTable(TableExpression node)
        {
            var pattern = _useAliases ? "{0} {1}" : "{0}";
            Append(string.Format(pattern, node.Table.Name, node.Alias));
            switch (node.TableHint)
            {
                case TableHint.None:
                    break;
                case TableHint.Snapshot:
                    Append("WITH (SNAPSHOT)");
                    break;
                case TableHint.NoLock:
                    Append("WITH (NOLOCK)");
                    break;
                default:
                    Errors.Add(new NotSupportedException("Not supported table hint: " + node.TableHint));
                    Append(Missing);
                    break;
            }
            return node;
        }

        protected virtual Expression VisitAsterisk(AsteriskExpression node)
        {
            if (_useAliases && !string.IsNullOrEmpty(node.Table?.Alias))
            {
                Append(node.Table.Alias);
                Append(".", false);
            }
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
            if (node.IsNot) Append("NOT");
            Append("IN (");
            var i = 0;
            foreach (var v in node.Content)
            {
                if (i++!=0) Append(", ", false);
                Append(GetLiteralInner(v));
                
            }
            if (i == 0)
            {
                Errors.Add(new FormatException("Sequence contains no element"));
                Append("<MISSING>");
            }
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
                    Errors.Add(new NotSupportedException("Invalid join type: " + node.JoinType));
                    Append(Missing);
                    break;
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
            Append(node.ToType);
            Append(",");
            Visit(node.Column);
            Append(")", false);
            return node;
        }

        protected virtual Expression VisitIn(InExpression node)
        {
            Visit(node.Left);
            if (node.IsNot) Append("NOT");
            Append("IN (");
            var visitor = new SqlCommandTextVisitor(ParameterizedQueryMode);
            visitor.Visit(node.Right);
            Append(visitor.CommandText, false);
            //            Visit(node.Right);
            Append(")", false);
            return node;
        }

        protected virtual Expression VisitParameter(ParameterExpression node)
        {
            switch (ParameterizedQueryMode)
            {
                case ParameterizedQueryMode.ParameterizedQuery:
                    Append($"@p{node.Index}");
                    break;
                case ParameterizedQueryMode.StringFormat:
                    Append($"{{{node.Index}}}");
                    break;
                default:
                    Errors.Add(new NotSupportedException("Invalid parameterized query mode: " + ParameterizedQueryMode));
                    Append(Missing);
                    break;
            }

            return node;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            VisitBinaryBranch(node.Left, node.NodeType);
            if (_isNullEnabled && node.Right.NodeType == ExpressionType.Constant && ((ConstantExpression) node.Right).Value == null)
            {
                switch (node.NodeType)
                {
                    case ExpressionType.Equal:
                        Append("is null");
                        break;
                    case ExpressionType.NotEqual:
                        Append("is not null");
                        break;
                    default:
                        Errors.Add(new NotSupportedException("Invalid node type " + node.NodeType));
                        Append(Missing);
                        break;
                }
            }
            else
            {
                Append(ToSign(node.NodeType));
                VisitBinaryBranch(node.Right, node.NodeType);
            }
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
            Append(GetLiteralInner(node.Value));
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
                    Errors.Add(new NotImplementedException(type.ToString()));
                    return Missing;
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

        private string GetLiteralInner(object value)
        {
            try
            {
                return GetLiteral(value);
            }
            catch (Exception ex)
            {
                Errors.Add(ex);
                return Missing;
            }
        }

        public static string GetLiteral(object value)
        {
            if (value == null) return "NULL";
            var type = value.GetType();
            if (!TypeToSqlMapping.ContainsKey(type))
                throw new NotSupportedException($"Unmappable type: {type} (value: {value})");
            return TypeToSqlMapping[type].Invoke(value);
        }
    }
}