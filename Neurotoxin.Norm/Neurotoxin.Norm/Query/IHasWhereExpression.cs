using System.Linq.Expressions;

namespace Neurotoxin.Norm.Query
{
    public interface IHasWhereExpression
    {
        Expression Where { get; set; }
    }
}