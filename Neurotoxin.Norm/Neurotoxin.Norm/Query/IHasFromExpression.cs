using System.Linq.Expressions;

namespace Neurotoxin.Norm.Query
{
    public interface IHasFromExpression
    {
        Expression From { get; set; }
    }
}