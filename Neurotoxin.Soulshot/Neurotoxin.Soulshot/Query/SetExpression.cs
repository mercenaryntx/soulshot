using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class SetExpression : Expression
    {
        public Expression Left { get; private set; }
        public Expression Right { get; private set; }

        public SetExpression(Expression left, Expression right) : base(ExpressionType.Assign, null)
        {
            Left = left;
            Right = right;
        }
    }
}