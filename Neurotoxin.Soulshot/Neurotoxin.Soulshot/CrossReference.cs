using System;
using System.Linq;

namespace Neurotoxin.Soulshot
{
    public class CrossReference
    {
        public bool IsInitiated { get; private set; }
        public Type TargetType { get; private set; }

        public CrossReference(Type targetType)
        {
            TargetType = targetType;
        }

        public void Init(ColumnMapper columnMapper, Type baseType)
        {
            var otherColumns = columnMapper.Map(TargetType);
            var reference = otherColumns.FirstOrDefault(c => c.ReferenceTable != null && c.ReferenceTable.BaseType == baseType);
            if (reference != null)
            {
                //1-to-many
            }
            else
            {
                //many-to-many, auto cross reference table needed
                //collection.AddCrossTable();
            }

        }
    }
}