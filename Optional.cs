namespace DontBeAfraidOfMonad;

public class Optional<T>
{// ── Simple Optional<T> Implementation for Examples ──────────────────
    public T Value { get; }
    public bool HasValue { get; }

    public Optional(T value)
    {
        Value = value;
        HasValue = true;
    }

    public Optional()
    {
        HasValue = false;
    }

    public Optional<U> Select<U>(Func<T, U> f)
    {
        return HasValue ? new Optional<U>(f(Value)) : new Optional<U>();
    }

    public Optional<U> SelectMany<U>(Func<T, Optional<U>> f)
    {
        return HasValue ? f(Value) : new Optional<U>();
    }
}