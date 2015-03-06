namespace Neurotoxin.Norm
{
    public interface ILazyProxy<T> : ILazyProxy
    {
        DbSet<T> DbSet { get; set; }
    }

    public interface ILazyProxy
    {
        
    }
}