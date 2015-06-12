namespace Neurotoxin.Soulshot.Extensions
{
    public static class EntityProxyExtensions
    {
        public static void ClearDirty(this IEntityProxy proxy)
        {
            proxy.DirtyProperties.Clear();
        }
    }
}
