using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public static class IHasWhereExpressionExtensions
    {
        public static void AddWhere(this IHasWhereExpression thisExpression, Expression where)
        {
            thisExpression.Where = thisExpression.Where == null ? where : new WhereExpression(thisExpression.Where, where);
        }
       
    }
}