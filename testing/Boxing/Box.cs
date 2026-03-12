namespace Ocelot.Testing.Boxing;

public class Box
{
    public static TResult In<TResult, TBoxee>(TBoxee instance)
        where TBoxee : class
        where TResult : Box<TBoxee>
        => (TResult)Activator.CreateInstance(typeof(TResult), instance)!;
    public static TResult With<TResult, TBoxee>(TBoxee instance)
        where TBoxee : class
        where TResult : Box<TBoxee>
        => In<TResult, TBoxee>(instance);
}

public class Box<T> : Box
{
    protected readonly T Instance;
    protected readonly Type Me;
    public Box(T instance, string? typeName = null)
    {
        if (instance?.GetType().FullName != typeName)
            throw new ArgumentException($"Is not type of {typeName ?? "?"}", nameof(instance));
        Instance = instance;
        Me = typeof(T);
    }

    public T Out() => Instance;
    public T Unbox() => Instance;
}
