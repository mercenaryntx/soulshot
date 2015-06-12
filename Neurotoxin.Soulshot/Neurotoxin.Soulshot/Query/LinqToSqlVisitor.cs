using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Query
{
    public class LinqToSqlVisitor : ExpressionVisitor
    {
        private SelectExpression _select;
        private UpdateExpression _update;
        private Expression _from;
        private Expression _where;
        private OrderByExpression _orderBy;

        private readonly IDbSet _dbSet;
        private readonly Type _targetExpression;

//        private readonly Dictionary<KeyValuePair<string, IDbSet>, TableExpression> _from = new Dictionary<KeyValuePair<string, IDbSet>, TableExpression>();

        public LinqToSqlVisitor(IDbSet dbSet, Type targetExpression)
        {
            _dbSet = dbSet;
            _targetExpression = targetExpression;
            //GetTable(dbSet, null);
        }

        public SqlExpression GetResult()
        {
            if (_targetExpression == typeof(SelectExpression))
            {
                var select = _select ?? new SelectExpression();
                if (select.Selection == null)
                {
                    SelectAll(_dbSet, null, select, string.Empty);
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
                //if (_update == null || _update.Set == null) throw new Exception("Update statement is missing.");
                if (_update == null) _update = new UpdateExpression();
                if (_update.Target == null) _update.Target = GetFrom();
                _update.AddWhere(_where);
                return _update;
            }

            throw new NotSupportedException("Invalid target expression: " + _targetExpression);
        }

        //TODO: detect loops
        //TODO: identify already added froms
        private void SelectAll(IDbSet dbSet, string memberName, SelectExpression select, string asPrefix)
        {
            //var table = GetTable(dbSet, memberName);
            var table = new TableExpression(dbSet.Table);
            foreach (var column in dbSet.Columns)
            {
                var expression = column.ToColumnExpression(table);
                expression.As = asPrefix + column.ColumnName;
                select.AddSelection(expression);
                //if (column.ReferenceTable != null)
                //{
                //    var target = _dbSet.Context.GetDbSet(column.ReferenceTable.BaseType);
                //    var targetTable = GetTable(target, column.PropertyName);
                //    var fk = target.PrimaryKey.ToColumnExpression(targetTable.Alias);
                //    select.AddWhere(Expression.MakeBinary(ExpressionType.Equal, column.ToColumnExpression(table.Alias, fk.Type), fk));
                //    SelectAll(target, column.PropertyName, select, string.Format("{0}{1}.", asPrefix, column.ColumnName));
                //}
            }
        }

        private Expression GetFrom()
        {
            Expression from = null;
            //foreach (Expression expression in _from.Values)
            //{                
            //    from = from == null ? expression : new ListingExpression(from, expression);
            //}
            return from ?? new TableExpression(_dbSet.Table, "t0");
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
                    _select.AddSelection(expression);
                    break;
                case "Update":
                    Debugger.Break();
                    if (_update == null) _update = new UpdateExpression();
                    _update.AddSet(expression);
                    break;
                case "Count":
                    _select = new SelectExpression {Selection = new CountExpression()};
                    _where = _where == null ? expression : new WhereExpression(_where, expression);
                    break;
                case "Max":
                    _select = new SelectExpression { Selection = new MaxExpression(expression) };
                    break;
                case "Any":
                    _select = new SelectExpression { Selection = new CountExpression() };
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
                case "AssignNewValue":
                    break;
                default:
                    throw new NotSupportedException("Not supported method: " + node.Method.Name);
            }
            return base.VisitMethodCall(node);
        }

        private Expression BuildExpression(Expression expression, Type desiredType = null)
        {
            var binaryExpression = expression as BinaryExpression;
            if (binaryExpression != null)
            {
                var left = BuildExpression(binaryExpression.Left);
                var right = BuildExpression(binaryExpression.Right);

                if (binaryExpression.NodeType == ExpressionType.Equal && left.Type != right.Type)
                {
                    //HACK: sometimes the enums become int on the right side (no clue why :S)
                    if (left.Type.IsEnum && right.Type == typeof(int))
                    {
                        var c = (ColumnExpression) left;
                        left = new ColumnExpression(c.ColumnName.Name, c.Table, typeof(int));
                    }
                    else
                    {
                        Debugger.Break();
                    }
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
                if (constValue != null) return Expression.Constant(constValue);

                var e = memberExpression;

                if (e.Member.Name == "TimeOfDay" && e.Member.DeclaringType == typeof(DateTime))
                {
                    e = e.Expression as MemberExpression;
                    var column = _dbSet.Columns.Single(c => c.DescribesProperty(e.Member));
                    var pi = e.Member as PropertyInfo;
                    return new ConvertExpression(new TimeAttribute(), column.ToColumnExpression(new TableExpression(_dbSet.Table), pi.PropertyType), typeof(TimeSpan));
                }
                else
                {
                    var column = _dbSet.Columns.Single(c => c.DescribesProperty(e.Member));
                    var pi = e.Member as PropertyInfo;
                    var type = pi.PropertyType;
                    return column.ToColumnExpression(new TableExpression(_dbSet.Table), type);
                }

                //var stack = new Stack<IDbSet>();
                //var p = new Stack<string>();
                //do
                //{
                //    if (stack.Count > 0) p.Push(e.Member.Name);
                //    stack.Push(_dbSet.Context.GetDbSet(e.Member.DeclaringType));
                //    e = e.Expression as MemberExpression;
                //} 
                //while (e != null);

                //var a = new StringBuilder();
                //Expression from = null;
                //IDbSet dbSet = null;
                //IDbSet prev = null;
                //while (stack.Count > 0)
                //{
                //    dbSet = stack.Pop();
                //    var t = new TableExpression(dbSet.Table);
                //    if (from == null)
                //    {
                //        from = t;
                //    }
                //    else
                //    {
                //        var join = new JoinExpression(JoinType.Inner, t);
                //        join.Condition = Expression.MakeBinary(ExpressionType.Equal, prev.Columns.Single(c => c.PropertyName == p.Peek()).ToColumnExpression(), dbSet.PrimaryKey.ToColumnExpression(t));
                //        from = Expression.MakeBinary(ExpressionType.Default, from, join);
                //    }
                //    prev = dbSet;

                //    //t = GetTable(dbSet, a.Length == 0 ? null : a.ToString());
                //    if (stack.Count == 0) continue;
                //    if (a.Length != 0) a.Append('.');
                //    var propertyName = p.Pop();
                //    a.Append(propertyName);

                //}
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
                    case "AssignNewValue":
                        return BuildSetExpression(methodCallExpression);
                    default:
                        throw new NotSupportedException(methodCallExpression.Method.Name);
                }
            }

            var listingExpression = expression as ListingExpression;
            if (listingExpression != null) return expression;

            var sqlPartExpression = expression as SqlPartExpression;
            if (sqlPartExpression != null) return expression;

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


        //private TableExpression GetTable(IDbSet dbSet, string memberName)
        //{
        //    var key = new KeyValuePair<string, IDbSet>(memberName, dbSet);
        //    if (!_from.ContainsKey(key))
        //    {
        //        var a = "t" + _from.Count;
        //        _from.Add(key, new TableExpression(dbSet.Table, a));
        //    }

        //    return _from[key];
        //}
    }
}