using System.Linq.Expressions;

namespace Neurotoxin.Norm.Query
{
    public class ListingExpression : Expression
    {
        public Expression Left { get; private set; }
        public Expression Right { get; private set; }

        public ListingExpression(Expression left, Expression right)
        {
            Left = left;
            Right = right;
        }
    }
}