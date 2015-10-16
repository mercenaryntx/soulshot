using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class WhereExpression : Expression
    {
        public Expression Left { get; private set; }
        public Expression Right { get; private set; }

        public WhereExpression(Expression left, Expression right) : base(ExpressionType.And, null)
        {
            Left = CheckForShortLogicalExpression(left);
            Right = CheckForShortLogicalExpression(right);
        }

        private Expression CheckForShortLogicalExpression(Expression node)
        {
            var columnExpression = node as ColumnExpression;
            return columnExpression != null ? MakeBinary(ExpressionType.Equal, columnExpression, Constant(true)) : node;
        }
    }
}