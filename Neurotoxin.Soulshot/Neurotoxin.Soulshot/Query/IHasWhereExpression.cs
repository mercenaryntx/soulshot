using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public interface IHasWhereExpression
    {
        Expression Where { get; set; }
    }
}