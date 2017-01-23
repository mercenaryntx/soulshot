using System;
using System.Linq.Expressions;
using Neurotoxin.Soulshot.Extensions;

namespace Neurotoxin.Soulshot.Query
{
    public class SelectQuery<TEntity> : SelectQuery
    {
        public SelectQuery(TableHint tableHint = TableHint.None) : base(typeof(TEntity), tableHint)
        {
        }
    }

    public class SelectQuery : LinqToSqlVisitor<SelectExpression>
    {
        private SelectExpression Union { get; set; }

        public SelectQuery(Type entityType, TableHint tableHint = TableHint.None) : base(entityType, tableHint)
        {
            //Select = new SelectExpression();
        }

        protected override SqlExpression GetSqlExpressionInner()
        {
            var select = Select ?? new SelectExpression();
            if (select.From == null) select.From = From;
            if (select.Joins == null) select.Joins = Join;
            //if (select.Selection == null) select.AddSelection(new AsteriskExpression());
            if (select.Selection == null)
            {
                select.SelectAllFrom(select.From as TableExpression);
                if (select.Joins != null)
                {
                    foreach (var joinExpression in select.Joins)
                    {
                        select.SelectAllFrom(joinExpression.Table);
                    }
                }
            }
            select.AddWhere(Where);
            select.OrderBy = OrderBy;
            select.Union = Union;
            return select;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == "Union")
            {
                var unionVisitor = new SelectQuery(EntityType, TableHint)
                {
                    Select = new SelectExpression {Selection = Select?.Selection}
                };
                unionVisitor.Visit(node.Arguments[1]);
                Union = unionVisitor.GetSqlExpression() as SelectExpression;
                return Visit(node.Arguments[0]);
            }
            return base.VisitMethodCall(node);
        }
    }
}