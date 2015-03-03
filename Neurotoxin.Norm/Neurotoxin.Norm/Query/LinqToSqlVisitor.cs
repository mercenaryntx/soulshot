using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Query
{
    public class LinqToSqlVisitor : ExpressionVisitor
    {
        private SelectExpression _select;
        private Expression _where;

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

            if (_targetExpression == typeof(SelectExpression))
            {
                var select = _select ?? new SelectExpression(from);
                select.Selection = new AsteriskExpression();
                select.Where = _where;
                return select;
            }
            if (_targetExpression == typeof(DeleteExpression))
            {
                if (from == null) throw new NotSupportedException("From cannot be null");
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
            if (node.Method.Name == "Where")
            {
                var expression = BuildExpression(node.Arguments[1]);
                _where = _where == null ? expression : new WhereExpression(_where, expression);
            }

            if (node.Method.Name == "Select")
            {
                if (_select == null) _select = new SelectExpression();
                var expression = BuildExpression(node.Arguments[1]);
                _select.Selection = _select.Selection == null ? expression : new ListingExpression(_select.Selection, expression);
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
                var column = _columnMapping.SingleOrDefault(c => c.DeclaringTypes.Contains(declaringType) && c.PropertyName == mi.Name);
                if (column != null)
                {
                    var alias = GetAlias(declaringType);
                    return column.ToColumnExpression(alias);
                }
                else
                {
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
                        else
                        {
                            Debugger.Break();
                        }
                    }
                }
            }

            var constantExpression = expression as ConstantExpression;
            if (constantExpression != null) return constantExpression;

            Debugger.Break();
            return null;
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