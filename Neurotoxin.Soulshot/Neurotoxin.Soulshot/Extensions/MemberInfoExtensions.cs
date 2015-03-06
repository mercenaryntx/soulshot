using System;
using System.Linq;
using System.Reflection;

namespace Neurotoxin.Soulshot.Extensions
{
    /// <summary>
    /// Extension methods to the System.Reflection.MemberInfo class
    /// </summary>
    public static class MemberInfoExtensions
    {
        public static bool HasAttribute<T>(this MemberInfo memberInfo, bool inherit = true) where T : Attribute
        {
            return memberInfo.GetAttribute<T>(inherit) != null;
        }

        /// <summary>
        /// Gets an attribute with the given type
        /// </summary>
        /// <typeparam name="T">The attribute type</typeparam>
        /// <param name="memberInfo">The member info</param>
        /// <param name="inherit"></param>
        /// <returns>The desired attribute or null</returns>
        public static T GetAttribute<T>(this MemberInfo memberInfo, bool inherit = true) where T : Attribute
        {
            return memberInfo.GetCustomAttributes(inherit).FirstOrDefault(a => a is T) as T;
        }

        /// <summary>
        /// Gets an attribute collection with the given type
        /// </summary>
        /// <typeparam name="T">The attribute type</typeparam>
        /// <param name="memberInfo">The member info</param>
        /// <param name="inherit"></param>
        /// <returns>A collection of the desired attributes</returns>
        public static T[] GetAttributes<T>(this MemberInfo memberInfo, bool inherit = true) where T : Attribute
        {
            return memberInfo.GetCustomAttributes(inherit).Where(a => a is T).Cast<T>().ToArray();
        }
    }
}