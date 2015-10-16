using System.Linq.Expressions;
using Neurotoxin.Soulshot.Query;

namespace Neurotoxin.Soulshot.Extensions
{
    public static class IHasFromExpressionExtensions
    {
        public static void AddFrom(this IHasFromExpression thisExpression, Expression from)
        {
            thisExpression.From = thisExpression.From == null ? from : new ListingExpression(thisExpression.From, from);
        }
    }
}