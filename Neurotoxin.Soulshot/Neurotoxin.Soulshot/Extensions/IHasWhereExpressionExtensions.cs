using System.Linq.Expressions;
using Neurotoxin.Soulshot.Query;

namespace Neurotoxin.Soulshot.Extensions
{
    public static class IHasWhereExpressionExtensions
    {
        public static void AddWhere(this IHasWhereExpression thisExpression, Expression where)
        {
            thisExpression.Where = thisExpression.Where == null ? where : new WhereExpression(thisExpression.Where, where);
        }
       
    }
}