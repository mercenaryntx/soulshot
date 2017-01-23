using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Neurotoxin.Soulshot.Annotations;
using Neurotoxin.Soulshot.Extensions;
using PetaPoco;

namespace Neurotoxin.Soulshot.Query
{
    public abstract class LinqToSqlVisitor<TExpression> : ExpressionVisitor
    {
        private readonly Dictionary<Type, TableExpression> _typeTableMapping = new Dictionary<Type, TableExpression>();
        protected TableHint TableHint { get; }
        protected TableExpression From => _typeTableMapping.First().Value;
        protected Type EntityType { get; }
        protected SelectExpression Select { get; set; }
        protected UpdateExpression Update { get; set; }
        protected Expression Where { get; private set; }
        protected OrderByExpression OrderBy { get; private set; }
        protected List<JoinExpression> Join { get; private set; }

        protected LinqToSqlVisitor(Type entityType, TableHint tableHint = TableHint.None)
        {
            EntityType = entityType;
            TableHint = tableHint;
            MapType(EntityType);
        }

        public SqlExpression GetSqlExpression()
        {
            if (Where != null && !(Where is WhereExpression)) Where = new WhereExpression(Where, null);
            return GetSqlExpressionInner();
        }

        public string ToSqlString(Expression expression, ParameterizedQueryMode mode = ParameterizedQueryMode.ParameterizedQuery)
        {
            var visitor = new SqlCommandTextVisitor(mode);
            try
            {
                if (expression != null) Visit(expression);
                visitor.Visit(GetSqlExpression());
                if (visitor.Errors.Any()) throw new AggregateException("Incorrect SQL syntax: " + visitor.CommandText, visitor.Errors);
            }
            catch (Exception ex)
            {
                throw new InvalidExpressionException("Expression cannot be transformed to SQL: " + expression, ex);
            }
            return visitor.CommandText;
        }

        protected virtual SqlExpression GetSqlExpressionInner()
        {
            throw new NotSupportedException("Invalid target expression: " + typeof(TExpression));
        }

        private TableExpression GetFrom(Expression node)
        {
            var constExpr = node as ConstantExpression;
            if (constExpr != null)
            {
                var table = constExpr.Value as ITable;
                if (table != null) return MapType(table.ElementType);

                var queryable = constExpr.Value as IQueryable;
                if (queryable != null && queryable.Expression == node) throw new NotSupportedException("Enumerator data sources are not supported. Possible Enumerable to Queryable conversion occured.");
            }

            var methodCallExpr = node as MethodCallExpression;
            return methodCallExpr != null ? GetFrom(methodCallExpr.Arguments[0]) : null;
        }

        private TableExpression MapType(Type type)
        {
            if (!_typeTableMapping.ContainsKey(type))
            {
                var table = type.GetCustomAttribute<TableAttribute>() ?? new TableAttribute(type.Name);
                var expression = new TableExpression(type, table, $"t{_typeTableMapping.Count}")
                {
                    TableHint = TableHint
                };
                _typeTableMapping.Add(type, expression);
            }
            return _typeTableMapping[type];
        }

        public override Expression Visit(Expression node)
        {
            var nestedQueryExpresion = node as NestedQueryExpression;
            if (nestedQueryExpresion != null) return new NoOpExpression(nestedQueryExpresion.Type);

            return base.Visit(node);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if (typeof(TExpression) == typeof (DeleteExpression))
            {
                Where = BuildExpression(node);
            }
            return base.VisitLambda(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var from = GetFrom(node.Arguments[0]);

            var expression = node.Arguments.Count > 1 ? BuildExpression(node.Arguments[1]) : null;
            switch (node.Method.Name)
            {
                case MethodNames.Where:
                case MethodNames.StartsWith:
                case MethodNames.EndsWith:
                    AddWhere(expression);
                    break;
                case MethodNames.In:
                case MethodNames.NotIn:
                    var inExpression = new InExpression(expression, (NestedQueryExpression)node.Arguments[2], node.Method.Name == "NotIn");
                    AddWhere(inExpression);
                    break;
                case MethodNames.Select:
                    if (Select == null) Select = new SelectExpression { From = from };
                    Select.AddSelection(expression);
                    break;
                case MethodNames.Update:
                    if (Update == null) Update = new UpdateExpression();
                    Update.AddSet(expression);
                    break;
                case MethodNames.Count:
                    Select = new SelectExpression
                    {
                        Selection = new CountExpression(),
                        From = from
                    };
                    AddWhere(expression);
                    break;
                case MethodNames.Max:
                    Select = new SelectExpression
                    {
                        Selection = new MaxExpression(expression),
                        From = from
                    };
                    break;
                case MethodNames.Any:
                    Select = new SelectExpression
                    {
                        Selection = new CountExpression(),
                        From = from
                    };
                    AddWhere(expression);
                    break;
                case MethodNames.First:
                case MethodNames.FirstOrDefault:
                case MethodNames.Single:
                case MethodNames.SingleOrDefault:
                    if (Select == null) Select = new SelectExpression { From = from };
                    Select.Top = Expression.Constant(node.Method.Name.StartsWith(MethodNames.First) ? 1 : 2);
                    AddWhere(expression);
                    break;
                case MethodNames.Take:
                    if (Select == null) Select = new SelectExpression { From = from, Top = node.Arguments[1] };
                    break;
                case MethodNames.OrderBy:
                case MethodNames.ThenBy:
                    if (OrderBy == null) OrderBy = new OrderByExpression();
                    OrderBy.AddColumn(expression, ListSortDirection.Ascending);
                    break;
                case MethodNames.ThenByDescending:
                case MethodNames.OrderByDescending:
                    if (OrderBy == null) OrderBy = new OrderByExpression();
                    OrderBy.AddColumn(expression, ListSortDirection.Descending);
                    break;
                case MethodNames.Contains:
                    break;
                case MethodNames.Distinct:
                    if (Select == null) Select = new SelectExpression { From = from, Distinct = true };
                    break;
                case MethodNames.Skip:
                case MethodNames.AssignNewValue:
                    throw new NotImplementedException();
                case MethodNames.Join:
                    if (Select == null) Select = new SelectExpression { From = from };
                    if (Join == null) Join = new List<JoinExpression>();
                    var on = GetFrom(node.Arguments[1]);
                    var join = new JoinExpression(JoinType.Inner, on);
                    var left = BuildExpression(node.Arguments[2]);
                    var right = BuildExpression(node.Arguments[3]);
                    join.Condition = Expression.MakeBinary(ExpressionType.Equal, left, right);
                    var returnType = ((LambdaExpression)((UnaryExpression)node.Arguments[4]).Operand).ReturnType;
                    var selectionTable = _typeTableMapping[returnType];
                    Select.Selection = new AsteriskExpression(selectionTable);
                    Join.Add(join);
                    break;
                case MethodNames.OfType:
                    if (from.Table.MappingStrategy == MappingStrategy.TablePerHierarchy)
                    {
                        if (Select == null) Select = new SelectExpression { From = from };
                        var filterType = node.Arguments.Count > 1 &&
                                          node.Arguments[1].NodeType == ExpressionType.Constant &&
                                          typeof(Type).IsAssignableFrom(node.Arguments[1].Type)
                            ? (Type) ((ConstantExpression) node.Arguments[1]).Value
                            : node.Method.ReturnType.GetGenericArguments()[0];
                        var discriminatorColumn = new ColumnExpression(ColumnMapping.DiscriminatorColumnName, (TableExpression)Select.From, typeof(string));
                        var constValue = Expression.Constant(filterType.FullName);
                        //TODO: instead of exact match use CONTAINS(<all the derived types>)
                        AddWhere(Expression.MakeBinary(ExpressionType.Equal, discriminatorColumn, constValue));
                    }
                    break;
                default:
                    throw new NotSupportedException("Not supported method: " + node.Method.Name);
            }
            return base.VisitMethodCall(node);
        }

        protected void AddWhere(Expression expression)
        {
            Where = Where == null ? expression : new WhereExpression(Where, expression);
        }

        private Expression BuildExpression(Expression expression)
        {
            var binaryExpression = expression as BinaryExpression;
            if (binaryExpression != null)
            {
                var left = BuildExpression(binaryExpression.Left);
                var right = BuildExpression(binaryExpression.Right);
                if (binaryExpression.Type == typeof(bool) && (binaryExpression.NodeType == ExpressionType.And || binaryExpression.NodeType == ExpressionType.AndAlso))
                {
                    left = CheckForShortLogicalExpression(left);
                    right = CheckForShortLogicalExpression(right);
                }

                if ((binaryExpression.NodeType == ExpressionType.Equal 
                  || binaryExpression.NodeType == ExpressionType.NotEqual) 
                  && left.Type != right.Type)
                {
                    var constExpr = right as ConstantExpression;
                    if (constExpr != null)
                    {
                        right = Expression.Constant(Convert.ChangeType(constExpr.Value, left.Type));
                    }
                    else
                    {
                        constExpr = left as ConstantExpression;
                        if (constExpr != null)
                        {
                            left = Expression.Constant(Convert.ChangeType(constExpr.Value, right.Type));
                        }
                        else
                        {
                            Debugger.Break();
                        }
                    }
                    //if (left.Type.IsAssignableFrom(right.Type))
                    //{
                    //    var c = (ColumnExpression) left;
                    //    left = new ColumnExpression(c.ColumnName.Name, c.Table, left.Type.UnderlyingSystemType);
                    //}
                }
                return Expression.MakeBinary(binaryExpression.NodeType, left, right);
            }

            var unaryExpression = expression as UnaryExpression;
            if (unaryExpression != null)
            {
                switch (unaryExpression.NodeType)
                {
                    case ExpressionType.Not:
                        var operand = BuildExpression(unaryExpression.Operand);
                        var containsExpression = operand as ContainsExpression;
                        if (containsExpression != null)
                        {
                            containsExpression.IsNot = true;
                            return containsExpression;
                        }
                        return Expression.MakeBinary(ExpressionType.Equal, operand, Expression.Constant(false));
                    default:
                        return BuildExpression(unaryExpression.Operand);
                }
            }

            var lambdaExpression = expression as LambdaExpression;
            if (lambdaExpression != null) return BuildExpression(lambdaExpression.Body);

            var memberExpression = expression as MemberExpression;
            if (memberExpression != null)
            {
                var constValue = GetConstantValue(memberExpression);
                //TODO: what if const value is NULL?!
                if (constValue != null) return Expression.Constant(constValue, memberExpression.Type);
                switch (memberExpression.Member.MemberType)
                {
                    case MemberTypes.Field:
                        var fi = (FieldInfo)memberExpression.Member;
                        Debugger.Break();
                        throw new NotSupportedException("Fields are not supported for column mapping");
                    case MemberTypes.Property:
                        //TODO: support recurring member access expression, check if memberExpression.Expression is another member expression
                        var pi = (PropertyInfo)memberExpression.Member;
                        if (_typeTableMapping.ContainsKey(pi.DeclaringType))
                        {
                            var type = pi.PropertyType;
                            var columnName = pi.GetCustomAttribute<ColumnAttribute>()?.Name ?? pi.Name;
                            return new ColumnExpression(columnName, _typeTableMapping[pi.DeclaringType], type);
                        }
                        throw new NotSupportedException("Unknown entity type: " + pi.DeclaringType.FullName);
                    default:
                        throw new NotSupportedException("Not supported membertype: " + memberExpression.Member.MemberType);
                }
            }

            var constantExpression = expression as ConstantExpression;
            if (constantExpression != null) return constantExpression;

            var methodCallExpression = expression as MethodCallExpression;
            if (methodCallExpression != null)
            {
                switch (methodCallExpression.Method.Name)
                {
                    case "Contains":
                        if (methodCallExpression.Method.DeclaringType == typeof (string)) 
                            return BuildLikeExpression(methodCallExpression, true, true);

                        return BuildContainsExpression(methodCallExpression);
                    case "StartsWith":
                        return BuildLikeExpression(methodCallExpression, false, true);
                    case "EndsWith":
                        return BuildLikeExpression(methodCallExpression, true, false);
                    case "AssignNewValue":
                        return BuildSetExpression(methodCallExpression);
                    default:
                        throw new NotSupportedException("Not supported method call: " + methodCallExpression.Method.Name);
                }
            }

            var listingExpression = expression as ListingExpression;
            if (listingExpression != null) return expression;

            var sqlPartExpression = expression as SqlPartExpression;
            if (sqlPartExpression != null) return expression;

            throw new NotSupportedException("Not supported expression type: " + expression.GetType().Name);
        }

        private ContainsExpression BuildContainsExpression(MethodCallExpression methodCallExpression)
        {
            var constant = (ConstantExpression)BuildExpression(methodCallExpression.Arguments[0]);
            var enumerable = constant.Value as IEnumerable;
            if (enumerable == null) throw new NotSupportedException("Invalid type: " + constant.Value.GetType());

            var containsExpression = new ContainsExpression((ColumnExpression) BuildExpression(methodCallExpression.Arguments[1]), enumerable);
            return containsExpression;
        }

        private LikeExpression BuildLikeExpression(MethodCallExpression methodCallExpression, bool leftWildcard, bool rightWildcard)
        {
            var constant = (ConstantExpression)BuildExpression(methodCallExpression.Arguments[0]);
            var str = constant.Value as string;
            if (str == null) throw new NotSupportedException("Invalid type: " + constant.Value.GetType());

            var sb = new StringBuilder();
            if (leftWildcard) sb.Append("%");
            sb.Append(str);
            if (rightWildcard) sb.Append("%");

            return new LikeExpression
            {
                Column = (ColumnExpression)BuildExpression(methodCallExpression.Object),
                Value = Expression.Constant(sb.ToString())
            };
        }

        private SetExpression BuildSetExpression(MethodCallExpression methodCallExpression)
        {
            return new SetExpression(BuildExpression(methodCallExpression.Arguments[1]), BuildExpression(methodCallExpression.Arguments[2]));
        }

        private static object GetConstantValue(MemberExpression memberExpression)
        {
            object instance = null;
            var c = memberExpression.Expression as ConstantExpression;
            if (c != null) instance = c.Value;

            var m = memberExpression.Expression as MemberExpression;
            if (m != null) instance = GetConstantValue(m);

            if (instance == null) return null;

            var fieldInfo = memberExpression.Member as FieldInfo;
            if (fieldInfo != null) return fieldInfo.GetValue(instance);
            var propertyInfo = memberExpression.Member as PropertyInfo;
            if (propertyInfo != null) return propertyInfo.GetValue(instance, null);

            throw new NotSupportedException();
        }

        protected virtual Expression CheckForShortLogicalExpression(Expression node)
        {
            var columnExpression = node as ColumnExpression;
            return columnExpression != null
                ? Expression.MakeBinary(ExpressionType.Equal, columnExpression, Expression.Constant(true))
                : node;
        }

        protected ColumnMappingCollection GetColumnMappings()
        {
            return EntityType.GetColumnMappings();
        }
    }
}