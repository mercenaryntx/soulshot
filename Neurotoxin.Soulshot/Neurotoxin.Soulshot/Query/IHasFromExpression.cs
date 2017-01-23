using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public interface IHasFromExpression
    {
        Expression From { get; set; }
    }
}