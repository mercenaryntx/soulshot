using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Neurotoxin.Soulshot.Extensions
{
    public static class TypeBuilderExtensions
    {
        public static FieldBuilder CreateField<T>(this TypeBuilder typeBuilder, string name)
        {
            return CreateField(typeBuilder, typeof(T), name);
        }

        public static FieldBuilder CreateField(this TypeBuilder typeBuilder, Type fieldType, string name)
        {
            return typeBuilder.DefineField(name, fieldType, FieldAttributes.Private);
        }

        public static PropertyFieldPair CreateProperty<T>(this TypeBuilder typeBuilder, string name, Func<FieldBuilder, MethodBuilder> getter = null, Func<FieldBuilder, MethodBuilder> setter = null, bool createBackingField = true)
        {
            return CreateProperty(typeBuilder, typeof(T), name, getter, setter, createBackingField);
        }

        public static PropertyFieldPair CreateProperty(this TypeBuilder typeBuilder, Type propertyType, string name, Func<FieldBuilder, MethodBuilder> getter = null, Func<FieldBuilder, MethodBuilder> setter = null, bool createBackingField = true)
        {
            FieldBuilder backingField = null;
            if (createBackingField)
            {
                var fieldName = new StringBuilder("_");
                fieldName.Append(name.Substring(0, 1).ToLower());
                fieldName.Append(name.Substring(1));
                backingField = CreateField(typeBuilder, propertyType, fieldName.ToString());
            }
            var propertyBuilder = typeBuilder.DefineProperty(name, PropertyAttributes.None, CallingConventions.Any, propertyType, Type.EmptyTypes);

            if (getter != null)
            {
                var getMethod = getter.Invoke(backingField);
                if (getMethod != null) propertyBuilder.SetGetMethod(getMethod);
            }

            if (setter != null)
            {
                var setMethod = setter.Invoke(backingField);
                if (setMethod != null) propertyBuilder.SetSetMethod(setMethod);
            }
            return new PropertyFieldPair(propertyBuilder, backingField);
        }

        public static ConstructorBuilder CreateDefaultConstructor(this TypeBuilder typeBuilder, Action<ILGenerator> setConstructorBody)
        {
            var constructor = typeBuilder.BaseType.GetConstructor(Type.EmptyTypes);
            if (constructor == null) throw new Exception("Parameterless constructor is a must");
            //var parameterTypes = constructor.GetParameters().Select(p => p.ParameterType).ToArray();
            var constructorBuilder = typeBuilder.DefineConstructor
                (
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig, 
                    CallingConventions.Standard, 
                    Type.EmptyTypes
                );
            var il = constructorBuilder.GetILGenerator();
            setConstructorBody.Invoke(il);
            return constructorBuilder;
        }

        public static PropertyFieldPair ImplementInterfaceProperty<TInterface>(this TypeBuilder typeBuilder, string propertyName, Func<TypeBuilder, FieldBuilder, PropertyInfo, Type, MethodBuilder> getter = null, Func<TypeBuilder, FieldBuilder, PropertyInfo, Type, MethodBuilder> setter = null)
        {
            var interfaceType = typeof (TInterface);
            var pi = interfaceType.GetProperty(propertyName);
            if (getter == null) getter = DefaultInterfacePropertyGetter;
            if (setter == null) setter = DefaultInterfacePropertySetter;
            return CreateProperty(typeBuilder, pi.PropertyType, pi.Name, f => getter(typeBuilder, f, pi, interfaceType), f => setter(typeBuilder, f, pi, interfaceType));
        }

        private static MethodBuilder DefaultInterfacePropertyGetter(TypeBuilder typeBuilder, FieldBuilder field, PropertyInfo pi, Type interfaceType)
        {
            var name = "get_" + pi.Name;
            var interfaceGetter = interfaceType.GetMethod(name);
            if (interfaceGetter == null) return null;

            var setter = typeBuilder.DefineMethod(name, MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual, pi.PropertyType, Type.EmptyTypes);
            typeBuilder.DefineMethodOverride(setter, interfaceGetter);
            var il = setter.GetILGenerator();

            //Generates the code of return this._backingField;
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);

            return setter;
        }

        private static MethodBuilder DefaultInterfacePropertySetter(TypeBuilder typeBuilder, FieldBuilder field, PropertyInfo pi, Type interfaceType)
        {
            var name = "set_" + pi.Name;
            var interfaceSetter = interfaceType.GetMethod(name);
            if (interfaceSetter == null) return null;

            var setter = typeBuilder.DefineMethod(name, MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual, typeof(void), new[] { pi.PropertyType });
            typeBuilder.DefineMethodOverride(setter, interfaceSetter);
            var il = setter.GetILGenerator();

            //Generates the code of this._backingField = value;
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);

            return setter;
        }
    }
}