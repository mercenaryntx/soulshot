using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public static class IHasFromExpressionExtensions
    {
        public static void AddFrom(this IHasFromExpression thisExpression, Expression from)
        {
            thisExpression.From = thisExpression.From == null ? from : new ListingExpression(thisExpression.From, from);
        }
    }
}