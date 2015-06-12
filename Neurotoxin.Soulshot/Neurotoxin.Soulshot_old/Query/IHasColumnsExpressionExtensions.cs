using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public static class IHasColumnsExpressionExtensions
    {
        public static void AddColumn(this IHasColumnsExpression thisExpression, Expression column)
        {
            thisExpression.Columns = thisExpression.Columns == null ? column : new ListingExpression(thisExpression.Columns, column);
        }
    }
}