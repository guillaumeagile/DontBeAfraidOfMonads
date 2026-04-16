using AwesomeAssertions;

namespace DontBeAfraidOfMonad;

public record User(int Id, string Name);
public record Post(int Id, int UserId, string Title);

public class TaskIsAMonadTests
{
    // ── TASK AS A MONAD ──────────────────────────────────────────────────
    //
    // A Monad is an endofunctor with two natural transformations:
    // 1. Return (or Pure): A → M<A>  — wraps a value in the monad
    // 2. Bind (or FlatMap): M<A> → (A → M<B>) → M<B>  — chains monadic operations
    //
    // Task<T> is a monad because:
    // - Return: Task.FromResult(value) wraps a value in Task<T>
    // - Bind: The await keyword chains Task operations
    //
    // The await keyword is syntactic sugar for the Bind operation!
    // When you write: var result = await task;
    // It's equivalent to: task.SelectMany(value => /* next operation */)

    // ── PART 1: Task as a Container (Functor) ──────────────────────────

    [Fact]
    public async Task Task_is_a_functor_with_Select()
    {
        // A functor is a container with a map operation
        // Task<T> has Select (map) which applies a function to the wrapped value

        var task = Task.FromResult(5);
        var mappedTask = task.Select(x => x * 2);

        var result = await mappedTask;

        result.Should().Be(10);
    }

    [Fact]
    public async Task Select_preserves_the_monadic_structure()
    {
        // Select (map) takes a function A → B and lifts it into Task<A> → Task<B>
        // The Task structure is preserved

        Func<int, string> intToString = x => $"Number: {x}";

        var task = Task.FromResult(42);
        var mappedTask = task.Select(intToString);

        var result = await mappedTask;

        result.Should().Be("Number: 42");
    }

    // ── PART 2: Task as a Monad (SelectMany / Bind) ──────────────────────

    [Fact]
    public async Task SelectMany_is_the_bind_operation()
    {
        // SelectMany (flatMap/bind) is the core monadic operation
        // It chains Task operations without nesting
        // Signature: Task<A> → (A → Task<B>) → Task<B>

        var task = Task.FromResult(5);
        int result = await task.SelectMany(x => Task.FromResult(x * 2));

        Task<int> justTheTask = task.SelectMany(x => Task.FromResult(x * 2));
        Task<Task<int>> whyYouNeedToBind = task.Select(x => Task.FromResult(x * 2));

        result.Should().Be(10);
    }

    [Fact]
    public async Task SelectMany_flattens_nested_tasks()
    {
        // Without SelectMany, we'd have Task<Task<int>>
        // SelectMany flattens it to Task<int>

        var task = Task.FromResult(3);

        // This would be Task<Task<int>> without SelectMany
        Task<Task<int>> nestedTask = task.ContinueWith(t => Task.FromResult(t.Result * 2));

        // SelectMany flattens it
        var flatTask = task.SelectMany(x => Task.FromResult(x * 2));

        var result = await flatTask;

        result.Should().Be(6);
    }

    // ── PART 3: Await is Syntactic Sugar for Bind ──────────────────────

    [Fact]
    public async Task Await_is_equivalent_to_SelectMany_bind()
    {
        // These two approaches are equivalent:
        // 1. Using await (syntactic sugar)
        // 2. Using SelectMany (explicit bind)

        var task1 = Task.FromResult(5);

        // Approach 1: Using await
        var result1 = await task1.SelectMany(async x =>
        {
            var intermediate = x * 2;
            return await Task.FromResult(intermediate + 3);
        });

        // Approach 2: Using explicit SelectMany chain
        var result2 = await task1
            .SelectMany(x => Task.FromResult(x * 2))
            .SelectMany(x => Task.FromResult(x + 3));

        result1.Should().Be(13);
        result2.Should().Be(13);
    }

    [Fact]
    public async Task Await_keyword_demonstrates_bind_operation()
    {
        // The await keyword is the bind operation in action
        // When you await a Task<A>, you extract the A and can use it in the next operation
        // This is exactly what bind does

        async Task<int> GetNumber() => 5;
        async Task<int> Double(int x) => x * 2;
        async Task<int> AddThree(int x) => x + 3;

        // Using await (bind operations chained)
        var result = await GetNumber();
        var doubled = await Double(result);
        var final = await AddThree(doubled);

        final.Should().Be(13);
    }

    // ── PART 4: Monadic Laws ────────────────────────────────────────────

    [Fact]
    public async Task Left_identity_law_holds_for_Task()
    {
        // Left Identity: return a >>= f ≡ f a
        // In C#: Task.FromResult(a).SelectMany(f) ≡ f(a)

        var a = 5;
        Func<int, Task<int>> f = x => Task.FromResult(x * 2);

        var leftSide = await Task.FromResult(a).SelectMany(f);
        var rightSide = await f(a);

        leftSide.Should().Be(rightSide);
        leftSide.Should().Be(10);
    }

    [Fact]
    public async Task Right_identity_law_holds_for_Task()
    {
        // Right Identity: m >>= return ≡ m
        // In C#: task.SelectMany(Task.FromResult) ≡ task

        var task = Task.FromResult(42);

        var leftSide = await task.SelectMany(Task.FromResult);
        var rightSide = await task;

        leftSide.Should().Be(rightSide);
        leftSide.Should().Be(42);
    }

    [Fact]
    public async Task Associativity_law_holds_for_Task()
    {
        // Associativity: (m >>= f) >>= g ≡ m >>= (\x -> f x >>= g)
        // In C#: m.SelectMany(f).SelectMany(g) ≡ m.SelectMany(x => f(x).SelectMany(g))

        var m = Task.FromResult(5);
        Func<int, Task<int>> f = x => Task.FromResult(x * 2);
        Func<int, Task<int>> g = x => Task.FromResult(x + 3);

        var leftSide = await m.SelectMany(f).SelectMany(g);
        var rightSide = await m.SelectMany(x => f(x).SelectMany(g));

        leftSide.Should().Be(rightSide);
        leftSide.Should().Be(13);
    }

    // ── PART 5: Real-World Examples with Await ──────────────────────────

    [Fact]
    public async Task Await_chains_async_operations_like_bind()
    {
        // Real-world example: fetching and processing data
        // Each await is a bind operation

        async Task<int> FetchUserId() => 123;
        async Task<string> FetchUserName(int userId) => $"User_{userId}";
        async Task<int> CountUserPosts(string userName) => 42;

        // Using await (bind operations)
        var userId = await FetchUserId();
        var userName = await FetchUserName(userId);
        var postCount = await CountUserPosts(userName);

        postCount.Should().Be(42);
    }

    [Fact]
    public async Task Await_with_multiple_operations_shows_monadic_composition()
    {
        // Multiple awaits show how bind chains operations
        // Each await extracts the value from Task<T> and passes it to the next operation

        async Task<int> Step1() => 10;
        async Task<int> Step2(int x) => x + 5;
        async Task<int> Step3(int x) => x * 2;

        var result = await Step1();
        result = await Step2(result);
        result = await Step3(result);

        result.Should().Be(30);
    }

    [Fact]
    public async Task SelectMany_with_query_syntax_shows_monadic_composition()
    {
        // Query syntax (LINQ) is syntactic sugar for SelectMany
        // This demonstrates the monadic structure more explicitly

        var result = await (
            from x in Task.FromResult(5)
            from y in Task.FromResult(3)
            from z in Task.FromResult(2)
            select x + y + z
        );

        result.Should().Be(10);
    }

    // ── PART 6: Await vs SelectMany Equivalence ──────────────────────────

    [Fact]
    public async Task Await_and_SelectMany_are_equivalent_for_sequential_operations()
    {
        // Both approaches produce the same result
        // Await is just more readable syntactic sugar

        async Task<int> GetValue() => 5;
        async Task<int> Transform(int x) => x * 2;

        // Using await
        var awaitResult = await GetValue();
        var awaitFinal = await Transform(awaitResult);

        // Using SelectMany
        var selectManyFinal = await GetValue().SelectMany(Transform);

        awaitFinal.Should().Be(selectManyFinal);
        awaitFinal.Should().Be(10);
    }

    [Fact]
    public async Task Await_in_async_method_is_bind_operation()
    {
        // When you use await inside an async method, you're performing bind operations
        // The async/await syntax is the monad's interface

        async Task<int> ComputeValue()
        {
            var step1 = await Task.FromResult(5);
            var step2 = await Task.FromResult(step1 * 2);
            var step3 = await Task.FromResult(step2 + 3);
            return step3;
        }

        var result = await ComputeValue();

        result.Should().Be(13);
    }

    // ── PART 7: Error Handling in Monadic Context ──────────────────────

    [Fact]
    public async Task Task_monad_handles_exceptions_in_chain()
    {
        // Task monad propagates exceptions through the chain
        // This is part of the monadic structure

        async Task<int> FailingOperation()
        {
            return await Task.FromException<int>(new InvalidOperationException("Error"));
        }

        var task = Task.FromResult(5)
            .SelectMany(x => Task.FromResult(x * 2))
            .SelectMany(x => FailingOperation());

        await task.Invoking(t => t).Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Await_propagates_exceptions_through_bind_chain()
    {
        // Exceptions in await chains propagate automatically
        // This is the monad's error handling mechanism

        async Task<int> Operation1() => 5;
        async Task<int> Operation2(int x) => throw new InvalidOperationException("Error");

        var task = async () =>
        {
            var result = await Operation1();
            return await Operation2(result);
        };

        await task.Invoking(t => t()).Should().ThrowAsync<InvalidOperationException>();
    }

    // ── PART 8: Comparison with Other Monads ────────────────────────────

    [Fact]
    public async Task Task_monad_follows_same_pattern_as_Optional()
    {
        // Task and Optional follow the same monadic pattern
        // Both have SelectMany (bind) and Select (map)

        var optionalValue = new Optional<int>(5);
        var optionalResult = optionalValue
            .SelectMany(x => new Optional<int>(x * 2))
            .SelectMany(x => new Optional<int>(x + 3));

        var taskValue = Task.FromResult(5);
        var taskResult = await taskValue
            .SelectMany(x => Task.FromResult(x * 2))
            .SelectMany(x => Task.FromResult(x + 3));

        optionalResult.Value.Should().Be(13);
        taskResult.Should().Be(13);
    }

    [Fact]
    public async Task Task_monad_follows_same_pattern_as_Result()
    {
        // Task and Result<T, E> follow the same monadic pattern
        // Both compose operations through SelectMany

        var resultValue = Result<int, Error>.Ok(5);
        var resultFinal = resultValue
            .SelectMany(x => Result<int, Error>.Ok(x * 2))
            .SelectMany(x => Result<int, Error>.Ok(x + 3));

        var taskValue = Task.FromResult(5);
        var taskFinal = await taskValue
            .SelectMany(x => Task.FromResult(x * 2))
            .SelectMany(x => Task.FromResult(x + 3));

        resultFinal.Value.Should().Be(13);
        taskFinal.Should().Be(13);
    }

    // ── PART 9: Practical Demonstration ──────────────────────────────────

    [Fact]
    public async Task Complete_example_showing_await_as_bind()
    {
        // Complete example: simulating an API call chain
        // Each await is a bind operation

        async Task<User> GetUser(int userId)
        {
            return await Task.FromResult(new User(userId, "Alice"));
        }

        async Task<Post> GetLatestPost(int userId)
        {
            return await Task.FromResult(new Post(1, userId, "Hello World"));
        }

        async Task<string> FormatResult(User user, Post post)
        {
            return await Task.FromResult($"{user.Name} wrote: {post.Title}");
        }

        // Using await (bind operations)
        var user = await GetUser(1);
        var post = await GetLatestPost(user.Id);
        var formatted = await FormatResult(user, post);

        formatted.Should().Be("Alice wrote: Hello World");
    }

    [Fact]
    public async Task Complete_example_using_SelectMany_explicitly()
    {
        // Same example using explicit SelectMany (bind)
        // Shows the underlying monadic structure

        async Task<User> GetUser(int userId)
        {
            return await Task.FromResult(new User(userId, "Bob"));
        }

        async Task<Post> GetLatestPost(int userId)
        {
            return await Task.FromResult(new Post(2, userId, "Functional Programming"));
        }

        async Task<string> FormatResult(User user, Post post)
        {
            return await Task.FromResult($"{user.Name} wrote: {post.Title}");
        }

        // Using SelectMany (explicit bind) - nested style
        var formatted1 = await GetUser(1)
            .SelectMany(user => GetLatestPost(user.Id)
                .SelectMany(post => FormatResult(user, post)));

        // Using LINQ query syntax - more fluent and concise
        var formatted2 = await (
            from user in GetUser(1)
            from post in GetLatestPost(user.Id)
            from result in FormatResult(user, post)
            select result
        );

        formatted1.Should().Be("Bob wrote: Functional Programming");
        formatted2.Should().Be("Bob wrote: Functional Programming");
    }
}
