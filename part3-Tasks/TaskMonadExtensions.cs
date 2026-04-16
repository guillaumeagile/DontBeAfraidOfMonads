namespace DontBeAfraidOfMonad;

public static class TaskMonadExtensions
{
    public static async Task<U> Select<T, U>(this Task<T> task, Func<T, U> f)
    {
        var value = await task;
        return f(value);
    }

    public static async Task<U> SelectMany<T, U>(this Task<T> task, Func<T, Task<U>> f)
    {
        var value = await task;
        return await f(value);
    }

    public static async Task<V> SelectMany<T, U, V>(this Task<T> task, Func<T, Task<U>> f, Func<T, U, V> project)
    {
        var t = await task;
        var u = await f(t);
        return project(t, u);
    }
}
