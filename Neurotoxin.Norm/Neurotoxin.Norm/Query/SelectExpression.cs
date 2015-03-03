using System.Linq.Expressions;

namespace Neurotoxin.Norm.Query
{
    public class SelectExpression : SqlExpression, IHasFromExpression, IHasWhereExpression
    {
        public Expression Selection { get; set; }
        public Expression From { get; set; }
        public Expression Where { get; set; }

        public SelectExpression(Expression from = null)
        {
            From = from;
        }

        public void AddSelection(Expression selection)
        {
            Selection = Selection == null ? selection : new ListingExpression(Selection, selection);
        }
    }
}