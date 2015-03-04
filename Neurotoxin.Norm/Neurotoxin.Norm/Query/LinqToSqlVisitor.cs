using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Query
{
    public class LinqToSqlVisitor : ExpressionVisitor
    {
        private SelectExpression _select;
        private Expression _where;
        private OrderByExpression _orderBy;

        private readonly TableAttribute _table;
        private readonly List<ColumnInfo> _columnMapping;
        private readonly Type _targetExpression;

        private readonly Dictionary<Type, string> _aliases = new Dictionary<Type, string>();

        public LinqToSqlVisitor(TableAttribute table, List<ColumnInfo> columnMapping, Type targetExpression)
        {
            _table = table;
            _columnMapping = columnMapping;
            _targetExpression = targetExpression;
        }

        public SqlExpression GetResult()
        {
            Expression from = null;
            foreach (var a in _aliases)
            {
                //TODO: set join type, map tables property
                Expression expression = new TableExpression(_table, a.Value);
                from = from == null ? expression : new ListingExpression(from, expression);
            }
            if (from == null) from = new TableExpression(_table);

            if (_targetExpression == typeof(SelectExpression))
            {
                var select = _select ?? new SelectExpression();
                if (select.Selection == null) select.Selection = new AsteriskExpression();
                if (select.From == null) select.From = from;
                select.Where = _where;
                select.OrderBy = _orderBy;
                return select;
            }
            if (_targetExpression == typeof(DeleteExpression))
            {
                if (from is ListingExpression) throw new NotSupportedException("Multiple from is not supported in case of Delete");
                return new DeleteExpression(from) { Where = _where };
            }
            
            if (_targetExpression == typeof(UpdateExpression))
            {
                //TODO
                return new UpdateExpression(from);
            }

            throw new NotSupportedException("Invalid target expression: " + _targetExpression);
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
            var expression = BuildExpression(node.Arguments[1]);
            switch (node.Method.Name)
            {
                case "Where":
                    _where = _where == null ? expression : new WhereExpression(_where, expression);
                    break;
                case "Select":
                    if (_select == null) _select = new SelectExpression();
                    _select.Selection = _select.Selection == null ? expression : new ListingExpression(_select.Selection, expression);
                    break;
                case "Count":
                    _select = new SelectExpression {Selection = new CountExpression()};
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
                var column = _columnMapping.SingleOrDefault(c => c.DescribesProperty(mi));
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

        private string GetAlias(Type type)
        {
            string alias;
            if (_aliases.ContainsKey(type))
            {
                alias = _aliases[type];
            }
            else
            {
                alias = "t" + _aliases.Count;
                _aliases.Add(type, alias);
            }
            return alias;
        }
    }
}