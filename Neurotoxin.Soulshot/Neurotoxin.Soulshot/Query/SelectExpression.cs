using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Neurotoxin.Soulshot.Annotations;
using Neurotoxin.Soulshot.Extensions;

namespace Neurotoxin.Soulshot.Query
{
    public class SelectExpression : SqlExpression, IHasFromExpression, IHasWhereExpression
    {
        public Expression Selection { get; set; }
        public Expression From { get; set; }
        public List<JoinExpression> Joins { get; set; }
        public Expression Where { get; set; }
        public Expression Top { get; set; }
        public OrderByExpression OrderBy { get; set; }
        public SelectExpression Union { get; set; }
        public bool Distinct { get; set; }
        public bool WithNoLock { get; set; }
        public override ExpressionType NodeType => ExpressionType.Default;
        public override Type Type => typeof(object);

        public SelectExpression(Expression from = null)
        {
            From = from;
        }

        public void AddSelection(Expression selection)
        {
            Selection = Selection == null ? selection : new ListingExpression(Selection, selection);
        }

        public void SelectAllFrom(TableExpression table)
        {
            if (table == null) throw new NotSupportedException();
            if (table.Table.MappingStrategy == MappingStrategy.TablePerHierarchy)
            {
                //TODO: more appropriate selection
                AddSelection(new AsteriskExpression());
                return;
            }
            foreach (var cm in table.EntityType.GetColumnMappings().Columns)
            {
                AddSelection(new ColumnExpression(cm.ColumnName, table, cm.Property.PropertyType));
            }
        }

        public override string ToString()
        {
            var top = Top != null ? $" Top [{Top}]" : string.Empty;
            var where = Where != null ? $" Where [{Where}]" : string.Empty;
            var orderBy = OrderBy != null ? $" Order by [{OrderBy}]" : string.Empty;
            return $"Select{top} [{Selection}] From [{From}]{where}{orderBy}";
        }
    }
}