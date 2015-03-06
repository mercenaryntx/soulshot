using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Neurotoxin.Soulshot.Query
{
    public class LinqToSqlVisitor : ExpressionVisitor
    {
        private SelectExpression _select;
        private Expression _where;
        private OrderByExpression _orderBy;

        private readonly IDbSet _dbSet;
        private readonly Type _targetExpression;

        private readonly Dictionary<object, string> _aliases = new Dictionary<object, string>();

        public LinqToSqlVisitor(IDbSet dbSet, Type targetExpression)
        {
            _dbSet = dbSet;
            _targetExpression = targetExpression;
        }

        public SqlExpression GetResult()
        {
            if (_targetExpression == typeof(SelectExpression))
            {
                var select = _select ?? new SelectExpression();
                if (select.Selection == null)
                {
                    if (!_aliases.Any()) GetAlias(_dbSet.EntityType);
                    SelectAll(_aliases.ElementAt(0).Value, _dbSet, select, string.Empty);
                }
                if (select.From == null) select.From = GetFrom();
                select.AddWhere(_where);
                select.OrderBy = _orderBy;
                return select;
            }
            if (_targetExpression == typeof(DeleteExpression))
            {
                var from = GetFrom();
                if (from is ListingExpression) throw new NotSupportedException("Multiple from is not supported in case of Delete");
                return new DeleteExpression(from) { Where = _where };
            }
            
            if (_targetExpression == typeof(UpdateExpression))
            {
                //TODO
                return new UpdateExpression(GetFrom());
            }

            throw new NotSupportedException("Invalid target expression: " + _targetExpression);
        }

        private void SelectAll(string alias, IDbSet dbSet, SelectExpression select, string asPrefix)
        {
            foreach (var column in dbSet.Columns)
            {
                var expression = column.ToColumnExpression(alias);
                expression.As = asPrefix + column.ColumnName;
                select.AddSelection(expression);
                if (column.ReferenceTable != null)
                {
                    var a = GetAlias(column);
                    var target = _dbSet.Context.GetDbSet(column.ReferenceTable.BaseType);
                    var fk = target.PrimaryKey.ToColumnExpression(a);
                    select.AddWhere(Expression.MakeBinary(ExpressionType.Equal, column.ToColumnExpression(alias, fk.Type), fk));
                    SelectAll(a, target, select, string.Format("{0}{1}.", asPrefix, column.ColumnName));
                }
            }
        }

        private Expression GetFrom()
        {
            Expression from = null;
            foreach (var a in _aliases)
            {
                var type = a.Key is Type ? (Type)a.Key : a.Key.GetType();
                var columnInfo = a.Key as ColumnInfo;
                if (columnInfo != null) type = columnInfo.ReferenceTable.BaseType;
                var columnInfoCollection = a.Key as ColumnInfoCollection;
                if (columnInfoCollection != null) type = columnInfoCollection.BaseType;
                
                Expression expression = new TableExpression(_dbSet.Context.GetDbSet(type).Table, a.Value);
                from = from == null ? expression : new ListingExpression(from, expression);
            }
            return from ?? new TableExpression(_dbSet.Table);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if (_targetExpression == typeof (DeleteExpression))
            {
                _where = BuildExpression(node);
            }
            return base.VisitLambda(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var expression = node.Arguments.Count > 1 ? BuildExpression(node.Arguments[1]) : null;
            switch (node.Method.Name)
            {
                case "Where":
                case "Single":
                case "SingleOrDefault":
                    _where = _where == null ? expression : new WhereExpression(_where, expression);
                    break;
                case "Select":
                    if (_select == null) _select = new SelectExpression();
                    _select.Selection = _select.Selection == null ? expression : new ListingExpression(_select.Selection, expression);
                    break;
                case "Count":
                    _select = new SelectExpression {Selection = new CountExpression()};
                    _where = _where == null ? expression : new WhereExpression(_where, expression);
                    break;
                case "First":
                case "FirstOrDefault":
                    if (_select == null) _select = new SelectExpression();
                    _select.Top = Expression.Constant(1);
                    _where = _where == null ? expression : new WhereExpression(_where, expression);
                    break;
                case "Take":
                    if (_select == null) _select = new SelectExpression();
                    _select.Top = node.Arguments[1];
                    break;
                case "Skip":
                    throw new NotImplementedException();
                case "OrderBy":
                case "ThenBy":
                    if (_orderBy == null) _orderBy = new OrderByExpression();
                    _orderBy.AddColumn(expression, ListSortDirection.Ascending);
                    break;
                case "ThenByDescending":
                case "OrderByDescending":
                    if (_orderBy == null) _orderBy = new OrderByExpression();
                    _orderBy.AddColumn(expression, ListSortDirection.Descending);
                    break;
                case "Contains":
                case "StartsWith":
                case "EndsWith":
                    break;
                default:
                    throw new NotSupportedException("Not supported method: " + node.Method.Name);
            }
            return base.VisitMethodCall(node);
        }

        private Expression BuildExpression(Expression expression)
        {
            var binaryExpression = expression as BinaryExpression;
            if (binaryExpression != null)
            {
                var left = BuildExpression(binaryExpression.Left);
                var right = BuildExpression(binaryExpression.Right);
                return Expression.MakeBinary(binaryExpression.NodeType, left, right);
            }

            var unaryExpression = expression as UnaryExpression;
            if (unaryExpression != null)
            {
                switch (unaryExpression.NodeType)
                {
                    case ExpressionType.Not:
                        var operand = BuildExpression(unaryExpression.Operand);
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
                var mi = memberExpression.Member;
                var declaringType = mi.DeclaringType;
                var column = _dbSet.Columns.SingleOrDefault(c => c.DescribesProperty(mi));
                if (column != null)
                {
                    var alias = GetAlias(declaringType);
                    return column.ToColumnExpression(alias);
                }

                var constExpression = memberExpression.Expression as ConstantExpression;
                if (constExpression != null)
                {
                    var container = constExpression.Value;
                    var fieldInfo = mi as FieldInfo;
                    if (fieldInfo != null) return Expression.Constant(fieldInfo.GetValue(container));
                    var propertyInfo = mi as PropertyInfo;
                    if (propertyInfo != null) return Expression.Constant(propertyInfo.GetValue(container, null));
                }
                else
                {
                    constExpression = BuildExpression(memberExpression.Expression) as ConstantExpression;
                    if (constExpression != null)
                    {
                        object value = null;
                        var fieldInfo = mi as FieldInfo;
                        if (fieldInfo != null) value = fieldInfo.GetValue(constExpression.Value);
                        var propertyInfo = mi as PropertyInfo;
                        if (propertyInfo != null) value = propertyInfo.GetValue(constExpression.Value, null);

                        return Expression.Constant(value);
                    }

                    Debugger.Break();
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
                        return BuildLikeExpression(methodCallExpression, true, false);
                    case "EndsWith":
                        return BuildLikeExpression(methodCallExpression, false, true);
                    default:
                        throw new NotSupportedException(methodCallExpression.Method.Name);
                }
            }

            throw new NotSupportedException(expression.GetType().Name);
        }

        private ContainsExpression BuildContainsExpression(MethodCallExpression methodCallExpression)
        {
            var containsExpression = new ContainsExpression
            {
                Column = (ColumnExpression)BuildExpression(methodCallExpression.Arguments[1])
            };

            var constant = (ConstantExpression)BuildExpression(methodCallExpression.Arguments[0]);
            var enumerable = constant.Value as IEnumerable;
            if (enumerable == null) throw new NotSupportedException("Invalid type: " + constant.Value.GetType());

            foreach (var value in enumerable)
            {
                containsExpression.AddContent(Expression.Constant(value));
            }
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

        private string GetAlias(object obj)
        {
            string alias;
            if (_aliases.ContainsKey(obj))
            {
                alias = _aliases[obj];
            }
            else
            {
                alias = "t" + _aliases.Count;
                _aliases.Add(obj, alias);
            }
            return alias;
        }
    }
}