using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Tests.Mappers
{
    public class GeographyAttribute : ColumnTypeAttribute
    {
        public GeographyAttribute() : base("geography")
        {
        }
    }
}