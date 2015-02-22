using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Neurotoxin.Norm.Extensions
{
    public static class ILGeneratorExtensions
    {
        public static void SetFieldDefault(this ILGenerator il, FieldInfo field)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Newobj, field.FieldType.GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Stfld, field);

        }
    }
}
