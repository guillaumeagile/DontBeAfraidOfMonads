using AwesomeAssertions;

namespace DontBeAfraidOfMonad;

public class TaskMonadEssentialsTests
{
    // Task<T> is a Monad with two operations:
    // 1. Return: Task.FromResult(value) — wraps a value
    // 2. Bind: SelectMany(func) — chains operations
    //
    // Await is syntactic sugar for bind!

    [Fact]
    public async Task Task_has_return_operation()
    {
        var wrapped = Task.FromResult(42);
        var result = await wrapped;
        result.Should().Be(42);
    }

    [Fact]
    public async Task Task_has_bind_operation_with_SelectMany()
    {
        var result = await Task.FromResult(5)
            .SelectMany(x => Task.FromResult(x * 2));

        result.Should().Be(10);
    }

    [Fact]
    public async Task Chaining_binds_without_nesting()
    {
        // Without bind (SelectMany), you'd have Task<Task<Task<int>>>
        // With bind, it's flat: Task<int>

        var result = await Task.FromResult(5)
            .SelectMany(x => Task.FromResult(x * 2))
            .SelectMany(x => Task.FromResult(x + 3));

        result.Should().Be(13);

        Task<int> task = Task.FromResult(5);
        Task<int> justTheTask = task.SelectMany(x => Task.FromResult(x * 2));
        Task<Task<int>> whyYouNeedToBind = task.Select(x => Task.FromResult(x * 2));

    }

    [Fact]
    public async Task LINQ_query_syntax_is_fluent_bind()
    {
        // LINQ query syntax is syntactic sugar for SelectMany
        var result = await (
            from x in Task.FromResult(5)
            from y in Task.FromResult(x * 2)
            from z in Task.FromResult(y + 3)
            select z
        );

        result.Should().Be(13);
    }


    [Fact]
    public async Task Real_world_async_composition()
    {
        async Task<int> FetchUserId() => 123;
        async Task<string> FetchUserName(int id) => $"User_{id}";
        async Task<int> CountPosts(string name) => 42;

        // Using await
        var userId = await FetchUserId();
        var userName = await FetchUserName(userId);
        var postCount = await CountPosts(userName);

        postCount.Should().Be(42);
    }

    [Fact]
    public async Task Same_composition_with_SelectMany()
    {
        async Task<int> FetchUserId() => 123;
        async Task<string> FetchUserName(int id) => $"User_{id}";
        async Task<int> CountPosts(string name) => 42;

        // Using SelectMany (bind)
        var postCount = await FetchUserId()
            .SelectMany(FetchUserName)
            .SelectMany(CountPosts);
        // the interest of Bind here is that we can chain multiple operations without nesting tasks
        // means we don't need to use await for each operation !!!!
        postCount.Should().Be(42);
    }

    [Fact]
    public async Task Same_composition_with_LINQ_query()
    {
        async Task<int> FetchUserId() => 123;
        async Task<string> FetchUserName(int id) => $"User_{id}";
        async Task<int> CountPosts(string name) => 42;

        // Using LINQ query syntax (most fluent)
        var postCount = await (
            from userId in FetchUserId()
            from userName in FetchUserName(userId)
            from count in CountPosts(userName)
            select count
        );

        postCount.Should().Be(42);
    }

    [Fact]
    public async Task Exception_propagates_through_bind_chain()
    {
        async Task<int> FailingOperation(int x) =>
            throw new InvalidOperationException("Error");

        var task = Task.FromResult(5)
            .SelectMany(FailingOperation)
            .SelectMany(x => Task.FromResult(x + 3));

        await task.Invoking(t => t).Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Task_monad_pattern_matches_other_monads()
    {
        // Task<T>
        var taskResult = await Task.FromResult(5)
            .SelectMany(x => Task.FromResult(x * 2));

        // Optional<T>
        var optionalResult = new Optional<int>(5)
            .SelectMany(x => new Optional<int>(x * 2));

        // Result<T, E>
        var resultResult = Result<int, Error>.Ok(5)
            .SelectMany(x => Result<int, Error>.Ok(x * 2));

        taskResult.Should().Be(10);
        optionalResult.Value.Should().Be(10);
        resultResult.Value.Should().Be(10);
    }

    [Fact]
    public async Task IEnumerable_with_async_operations_is_practical()
    {
        // Real-world scenario: async operations on collections
        // Each operation is async and returns a collection

        async Task<IEnumerable<int>> FetchUserIds() =>
            new[] { 1, 2, 3 };

        async Task<IEnumerable<string>> FetchUserPosts(int userId) =>
            userId switch
            {
                1 => new[] { "Post A", "Post B" },
                2 => new[] { "Post C" },
                3 => new[] { "Post D", "Post E", "Post F" },
                _ => new[] { "Unknown" }
            };

        async Task<int> CountWords(string post) =>
            post.Split(' ').Length;

        // Composing async operations on collections
        var userIds = await FetchUserIds();
        var allPosts = new List<string>();
        foreach (var userId in userIds)
        {
            var posts = await FetchUserPosts(userId);
            allPosts.AddRange(posts);
        }

        var wordCounts = new List<int>();
        foreach (var post in allPosts)
        {
            var count = await CountWords(post);
            wordCounts.Add(count);
        }

        // Result: word count for each post (6 posts total: 2 + 1 + 3)
        wordCounts.Should().Equal(2, 2, 2, 2, 2, 2);
    }

    [Fact]
    public void IEnumerable_is_a_monad_like_Task()
    {
        // IEnumerable<T> is also a monad!
        // SelectMany chains operations without nesting collections

        // Functions that return collections
        IEnumerable<int> GetNumbers() => new[] { 1, 2, 3 };
        IEnumerable<int> Double(int x) => new[] { x * 2 };
        IEnumerable<int> AddThree(int x) => new[] { x + 3 };

        // Using SelectMany (bind) - same pattern as Task!
        var result = GetNumbers()
            .SelectMany(Double)
            .SelectMany(AddThree);

        // Each number flows through the chain: 1→2→5, 2→4→7, 3→6→9
        result.Should().Equal(5, 7, 9);
    }



    [Fact]
    public void IEnumerable_monad_with_multiple_results()
    {
        // IEnumerable shines when functions return multiple values
        // This shows the power of monadic composition

        IEnumerable<int> GetUserIds() => new[] { 1, 2 };
        IEnumerable<string> GetUserPosts(int userId) => userId == 1
            ? new[] { "Post A", "Post B" }
            : new[] { "Post C" };

        // Using SelectMany: each user flows through GetUserPosts
        // Result: all posts from all users
        var allPosts = GetUserIds()
            .SelectMany(GetUserPosts);

        allPosts.Should().Equal("Post A", "Post B", "Post C");
    }

    [Fact]
    public void IEnumerable_monad_with_LINQ_query_syntax()
    {
        // LINQ query syntax is the fluent way to express monadic composition
        // Each 'from' is a bind operation

        IEnumerable<int> GetNumbers() => new[] { 1, 2 };
        IEnumerable<int> GetMultiples(int x) => new[] { x, x * 2, x * 3 };

        // Using LINQ query syntax (most readable)
        var result = from num in GetNumbers()
                     from multiple in GetMultiples(num)
                     select multiple;

        // 1→[1,2,3], 2→[2,4,6]
        result.Should().Equal(1, 2, 3, 2, 4, 6);
    }

    [Fact]
    public void Same_monadic_pattern_Task_vs_IEnumerable()
    {
        // Task<T> and IEnumerable<T> follow the SAME monadic pattern
        // The only difference is the container semantics

        // With Task: one async value
        async Task<int> TaskGetValue() => 5;
        async Task<int> TaskDouble(int x) => x * 2;

        // With IEnumerable: multiple values
        IEnumerable<int> EnumerableGetValues() => new[] { 5 };
        IEnumerable<int> EnumerableDouble(int x) => new[] { x * 2 };

        // Task composition (bind)
        var taskChain = TaskGetValue()
            .SelectMany(TaskDouble);

        // IEnumerable composition (bind) - same structure!
        var enumerableChain = EnumerableGetValues()
            .SelectMany(EnumerableDouble);

        // Both produce 10, just in different containers
        taskChain.Result.Should().Be(10);
        enumerableChain.Should().Equal(10);
    }

    // --------- complementary concepts

    [Fact]
    public async Task Left_identity_law()
    {
        // return a >>= f ≡ f a
        var a = 5;
        Func<int, Task<int>> f = x => Task.FromResult(x * 2);

        var leftSide = await Task.FromResult(a).SelectMany(f);
        var rightSide = await f(a);

        leftSide.Should().Be(rightSide);
    }

    [Fact]
    public async Task Right_identity_law()
    {
        // m >>= return ≡ m
        var task = Task.FromResult(42);

        var leftSide = await task.SelectMany(Task.FromResult);
        var rightSide = await task;

        leftSide.Should().Be(rightSide);
    }

    [Fact]
    public async Task Associativity_law()
    {
        // (m >>= f) >>= g ≡ m >>= (\x -> f x >>= g)
        var m = Task.FromResult(5);
        Func<int, Task<int>> f = x => Task.FromResult(x * 2);
        Func<int, Task<int>> g = x => Task.FromResult(x + 3);

        var leftSide = await m.SelectMany(f).SelectMany(g);
        var rightSide = await m.SelectMany(x => f(x).SelectMany(g));

        leftSide.Should().Be(rightSide);
    }

}
