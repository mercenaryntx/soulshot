using System.Linq.Expressions;
using Neurotoxin.Soulshot.Query;

namespace Neurotoxin.Soulshot.Extensions
{
    public static class HasColumnsExpressionExtension
    {
        public static void AddColumn(this IHasColumnsExpression thisExpression, Expression column)
        {
            thisExpression.Columns = thisExpression.Columns == null ? column : new ListingExpression(thisExpression.Columns, column);
        }
    }
}