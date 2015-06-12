using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class WhereExpression : Expression
    {
        public Expression Left { get; private set; }
        public Expression Right { get; private set; }

        public WhereExpression(Expression left, Expression right) : base(ExpressionType.And, null)
        {
            Left = left;
            Right = right;
        }
    }
}