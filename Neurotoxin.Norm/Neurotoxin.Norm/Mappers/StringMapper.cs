using System;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Mappers
{
    public class StringMapper : MapperBase
    {
        public StringMapper() : base(typeof(String), new NVarcharAttribute(true))
        {
        }
    }
}