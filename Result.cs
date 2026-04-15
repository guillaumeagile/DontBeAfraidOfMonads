namespace DontBeAfraidOfMonad;

// ── Error Record for Type-Safe Error Handling ──────────────────────

public record Error(string Code, string Message)
{
    public static Error InvalidFormat(string fieldName) =>
        new("INVALID_FORMAT", $"Invalid {fieldName} format");

    public static Error ValidationFailed(string fieldName, string reason) =>
        new("VALIDATION_FAILED", $"{fieldName} validation failed: {reason}");

    public static Error NotFound(string resource) =>
        new("NOT_FOUND", $"{resource} not found");
}

// ── Result<T, E> Monad Implementation for Web API Examples ──────────
public abstract class Result<T, E>
{
    public bool IsOk { get; protected set; }
    public T Value { get; protected set; }
    public E Error { get; protected set; }

    public static Result<T, E> Ok(T value) => new OkResult(value);
    public static Result<T, E> Err(E error) => new ErrResult(error);

    public Result<U, E> Select<U>(Func<T, U> f)
    {
        return this switch
        {
            OkResult ok => Result<U, E>.Ok(f(ok.Value)),
            ErrResult err => Result<U, E>.Err(err.Error),
            _ => throw new InvalidOperationException()
        };
    }

    public Result<U, E> SelectMany<U>(Func<T, Result<U, E>> f)
    {
        return this switch
        {
            OkResult ok => f(ok.Value),
            ErrResult err => Result<U, E>.Err(err.Error),
            _ => throw new InvalidOperationException()
        };
    }

    public Result<V, E> SelectMany<U, V>(Func<T, Result<U, E>> f, Func<T, U, V> project)
    {
        return this switch
        {
            OkResult ok => f(ok.Value).Select(u => project(ok.Value, u)),
            ErrResult err => Result<V, E>.Err(err.Error),
            _ => throw new InvalidOperationException()
        };
    }

    public TResult Match<TResult>(Func<T, TResult> onOk, Func<E, TResult> onErr)
    {
        return this switch
        {
            OkResult ok => onOk(ok.Value),
            ErrResult err => onErr(err.Error),
            _ => throw new InvalidOperationException()
        };
    }

    public void Match(Action<T> onOk, Action<E> onErr)
    {
        switch (this)
        {
            case OkResult ok:
                onOk(ok.Value);
                break;
            case ErrResult err:
                onErr(err.Error);
                break;
        }
    }

    private sealed class OkResult : Result<T, E>
    {
        public OkResult(T value)
        {
            IsOk = true;
            Value = value;
        }
    }

    private sealed class ErrResult : Result<T, E>
    {
        public ErrResult(E error)
        {
            IsOk = false;
            Error = error;
        }
    }


}
