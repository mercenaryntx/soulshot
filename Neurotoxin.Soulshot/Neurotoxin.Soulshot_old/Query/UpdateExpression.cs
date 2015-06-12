using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class UpdateExpression : SqlExpression, IHasWhereExpression
    {
        public Expression Target { get; set; }
        public Expression Set { get; set; }
        public Expression Where { get; set; }

        public UpdateExpression(Expression target = null)
        {
            Target = target;
        }

        public void AddSet(Expression set)
        {
            Set = Set == null ? set : new ListingExpression(Set, set);
        }

    }
}