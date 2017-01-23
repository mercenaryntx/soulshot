using System;
using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class InExpression : Expression
    {
        public bool IsNot { get; set; }

        public Expression Left { get; private set; }
        public Expression Right { get; private set; }
        public override ExpressionType NodeType => ExpressionType.Default;
        public override Type Type => typeof(bool);

        public InExpression(Expression left, NestedQueryExpression right, bool isNot = false)
        {
            Left = left;
            var nestedQuery = new SelectQuery(left.Type);
            nestedQuery.Visit(right.Select);
            Right = nestedQuery.GetSqlExpression();
            IsNot = isNot;
        }

        public override string ToString()
        {
            var not = IsNot ? "Not" : string.Empty;
            return $"[{Left}] {not} In [{Right}]";
        }
    }
}