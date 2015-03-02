using System.Linq.Expressions;

namespace Neurotoxin.Norm.Query
{
    public static class IHasWhereExpressionExtensions
    {
        public static void AddWhere(this IHasWhereExpression thisExpression, Expression where)
        {
            thisExpression.Where = thisExpression.Where == null ? where : new WhereExpression(thisExpression.Where, where);
        }
       
    }
}