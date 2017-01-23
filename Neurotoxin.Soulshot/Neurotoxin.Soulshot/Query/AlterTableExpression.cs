using System;
using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class AlterTableExpression : CreateTableExpression
    {
        public AlterTableExpression(TableExpression table) : base(table)
        {
        }

        public void DropConstraint(string name)
        {
            ConstraintExpression constraint;
            if (Constraints == null)
            {
                constraint = new ConstraintExpression(name, ExpressionType.Subtract);
                Constraints = constraint;
            }
            else
            {
                constraint = Constraints as ConstraintExpression;
                if (constraint == null) throw new NotSupportedException();
                if (constraint.NodeType != ExpressionType.Subtract) throw new NotSupportedException();
                constraint.AddName(new ObjectNameExpression(name));
            }
        }
    }
}