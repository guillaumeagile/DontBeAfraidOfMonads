using AwesomeAssertions;

namespace DontBeAfraidOfMonad;

public class EndoFoncteursTests
{
    //Un foncteur d'abord
    // Un foncteur, pour un développeur, c'est simplement un conteneur sur lequel on peut appliquer une fonction à ce qu'il contient, sans sortir du conteneur.
    // En pratique : c'est tout ce qui a un map.
    //
    // kotlin code:
    // listOf(1, 2, 3).map { it * 2 }  // List<Int> → List<Int>
    // Optional.of(42).map { it * 2 }  // Optional<Int> → Optional<Int>

    // map prend une fonction A → B et la fait "rentrer" dans le conteneur, sans l'ouvrir brutalement.

    // "Endo" = qui reste dans le même monde
    // Endo vient du grec : dedans, vers l'intérieur.
    // Un endofoncteur, c'est un foncteur qui reste dans la même catégorie — il transforme des types Kotlin en types Kotlin, des types TypeScript en types TypeScript. Il ne sort pas du langage.

    // En pratique, tous les foncteurs que tu utilises en programmation sont des endofoncteurs — List, Option, Result, IO... ils prennent tous un type de ton langage et retournent un autre type du même langage.
    // La distinction "endo" n'existe que parce qu'en théorie des catégories, un foncteur pourrait aller d'un monde vers un autre monde complètement différent. Dans notre contexte, ce n'est pas le cas.

    // Résumé
    //Foncteur : Un conteneur avec map
    //Endofoncteur : Un conteneur avec map qui reste dans le même type (= tous les tiens)
    //Monade : Un endofoncteur qui a en plus flatMap et pure, organisés comme un monoïde

    // ── PART 1: Func Delegate - The Foundation ──────────────────────────

    [Fact]
    public void Func_delegate_is_a_type_safe_function_reference()
    {
        // Func<TInput, TOutput> represents a function that takes TInput and returns TOutput
        // It's the basic building block for functional programming in C#

        Func<int, int> double_number = delegate(int x) { return x * 2; };
        Func<int, int> double_number_V2 = x => x * 2;
        // un cas interessant de l'inférence de type ici
        var double_number_V3 = (int x) => x * 2;
        var double_float_V3 = (float x) => x * 2; // Works in C# 9+


        int result1 = double_number(5);
        int result2 = double_number_V2(5);

        result1.Should().Be(10);
        result2.Should().Be(10);

        double_float_V3(3.3f).Should().Be(6.6f);
    }

    [Fact]
    public void Func_delegate_with_multiple_parameters()
    {
        // Func<T1, T2, ..., TN, TResult> - last type parameter is always the return type

        Func<int, int, int> add = (int a, int b) => a + b;

        int result = add(3, 7);

        result.Should().Be(10);
    }

    [Fact]
    public void Func_delegate_can_be_passed_as_parameter()
    {
        // Functions are first-class values - they can be passed around

        int ApplyOperation(int x, int y, Func<int, int, int> operation)
        {
            return operation(x, y);
        }

        Func<int, int, int> multiply = (int a, int b) => a * b;

        int result = ApplyOperation(4, 5, multiply);

        result.Should().Be(20);
    }

    // ── PART 2: Lambda Expressions - Syntactic Sugar ──────────────────


    [Fact]
    public void Lambda_with_multiple_parameters()
    {
        // Multiple parameters use parentheses

       var subtract = (double a, int b) => a - b;

        var result = subtract(10.2 , 3);

        result.Should().Be(7.2);
        result.Should().BeApproximately(7.2, 0.0001);
    }

    [Fact]
    public void Lambda_with_multiple_statements_uses_braces()
    {
        // For multiple statements, use braces and explicit return

        Func<int, int> increment_twice = x =>
        {
            int step1 = x + 1;
            int step2 = step1 + 1;
            return step2;
        };

        int result = increment_twice(5);

        result.Should().Be(7);
    }

    [Fact]
    public void Lambda_can_capture_variables_from_enclosing_scope()
    {
        // Lambdas can access variables from their surrounding context
        // They capture a REFERENCE to the variable, not its value

        int multiplier = 3;
        Func<int, int> multiply_by_multiplier = x => x * multiplier;
        multiplier = 4;  // Change the captured variable
        int result = multiply_by_multiplier(4);

        // The lambda sees the NEW value (4), not the old value (3)
        result.Should().Be(16);  // 4 * 4 = 16
    }

    [Fact]
    public void Closure_captures_value_by_creating_a_function_factory()
    {
        // A TRUE closure captures values at the time of function creation
        // This is achieved by creating a function factory (higher-order function)

        Func<int, Func<int, int>> CreateMultiplier = factor =>
        {
            // Each call to CreateMultiplier creates a NEW function
            // that captures its own 'factor' value
            return x => x * factor;
        };

        var multiply_by_2 = CreateMultiplier(2);
        var multiply_by_3 = CreateMultiplier(3);
        var multiply_by_5 = CreateMultiplier(5); // le multiplier est capturé 3 fois

        // Each closure has its own captured value
        multiply_by_2(10).Should().Be(20);
        multiply_by_3(10).Should().Be(30);
        multiply_by_5(10).Should().Be(50);

        // Even if we create new multipliers, the old ones keep their captured values
        var multiply_by_7 = CreateMultiplier(7);
        multiply_by_2(10).Should().Be(20);  // Still 20, not affected by multiply_by_7
    }

    [Fact]
    public void Closure_practical_example_creating_validators()
    {
        // Closures are useful for creating specialized functions with fixed parameters

        Func<int, Func<int, bool>> CreateRangeValidator = (min) =>
        {
            return x => x >= min;
        };

        var isPositive = CreateRangeValidator(0);
        var isGreaterThan10 = CreateRangeValidator(10);

        isPositive(-5).Should().BeFalse();
        isPositive(5).Should().BeTrue();

        isGreaterThan10(5).Should().BeFalse();
        isGreaterThan10(15).Should().BeTrue();
    }

    // ── PART 3: Functors - Containers with Map ──────────────────────

    [Fact]
    public void Functor_IEnumerable_with_Select_is_map()
    {
        // A functor is a container with a map operation
        // IEnumerable<T> is a functor - Select is its map

        IEnumerable<int> numbers = new[] { 1, 2, 3 };

        // map takes a function A → B and applies it inside the container
        // without breaking the container structure
        IEnumerable<int> doubled = numbers.Select(x => x * 2);

        doubled.Should().Equal(2, 4, 6);

        //other way: declare the function as a variable (delegate)
        int multiplier = 2;
        Func<int, int> multiply_by_multiplier = x => x * multiplier;
        // and use it in the Select (equivalent of apply/map)
        IEnumerable<int> doubledAgain = doubled.Select(multiply_by_multiplier);

        doubledAgain.Should().Equal(4, 8, 12);

        // Functor applied to Monoid: map over a combined structure
        // The functor (Select) preserves the monoid structure (Concat)
        IEnumerable<int> list1 = new[] { 1, 2 };
        IEnumerable<int> list2 = new[] { 3, 4 };

        // Monoid: a concatenated list
        IEnumerable<int> combined = list1.Concat(list2);
        combined.Should().Equal(1, 2, 3, 4);

        // Functor: apply a function to the combined structure
        multiplier = 10;
        IEnumerable<int> mapped = combined.Select(multiply_by_multiplier);
        mapped.Should().Equal(10, 20, 30, 40);


    }



    [Fact]
    public void Functor_preserves_container_structure()
    {
        // The key property: map doesn't "open" the container brutally
        // It keeps the structure intact

        IEnumerable<string> words = new[] { "hello", "world" };

        // We apply a function inside the container, but it stays a container
        IEnumerable<int> lengths = words.Select(w => w.Length);

        lengths.Should().Equal(5, 5);
        // The result is still enumerable - it's assignable to IEnumerable<int>
        // (the concrete type is an iterator, but that's an implementation detail)
        lengths.Should().BeAssignableTo<IEnumerable<int>>();

        // be aware when functor returns a monoid
        var what = words.Select(w => w.ToCharArray());
        //The nested structure: Select (keeps the monoid structure)
        what.Should().BeEquivalentTo([
            new[] { 'h', 'e', 'l', 'l', 'o' },
            new[] { 'w', 'o', 'r', 'l', 'd' }
        ]);

        // The flattened structure: SelectMany (flattens the monoid)
        var splitted = words.SelectMany(w => w.ToCharArray());
        splitted.Should().Equal('h', 'e', 'l', 'l', 'o', 'w', 'o', 'r', 'l', 'd');
    }

    [Fact]
    public void Functor_chaining_multiple_maps()
    {
        // You can chain multiple map operations

        IEnumerable<int> numbers = new[] { 1, 2, 3 };

        IEnumerable<int> result = numbers
            .Select(x => x * 2)      // double each
            .Select(x => x + 1)      // add 1 to each
            .Select(x => x * x);     // square each

        // (1*2+1)² = 3² = 9
        // (2*2+1)² = 5² = 25
        // (3*2+1)² = 7² = 49
        result.Should().Equal(9, 25, 49);
    }

    [Fact]
    public void Functor_with_custom_container_type()
    {
        // Any type with a Select method is a functor

        var optional = new Optional<int>(42);

        Optional<int> doubled = optional.Select(x => x * 2);

        doubled.Value.Should().Be(84);
    }


    // ── PART 4: Result Monad - Enterprise Web API Example ──────────────

    [Fact]
    public void Result_Monad_wraps_success_or_failure()
    {
        // A Result<T, E> monad represents either a success (T) or a failure (E)
        // It's useful for chaining operations that might fail
        // Using a typed Error record instead of plain strings

        Result<int, Error> success = Result<int, Error>.Ok(42);
        Result<int, Error> failure = Result<int, Error>.Err(Error.InvalidFormat("UserId"));

        success.IsOk.Should().BeTrue();
        failure.IsOk.Should().BeFalse();
    }

    [Fact]
    public void Result_Monad_chains_operations_with_Bind()
    {
        // Bind (SelectMany) chains operations that return Result
        // If any operation fails, the chain stops and returns the error

        Result<int, Error> ParseUserId(string input)
        {
            return int.TryParse(input, out var id)
                ? Result<int, Error>.Ok(id)
                : Result<int, Error>.Err(Error.InvalidFormat("UserId"));
        }

        Result<string, Error> FetchUserName(int userId)
        {
            return userId > 0
                ? Result<string, Error>.Ok($"User_{userId}")
                : Result<string, Error>.Err(Error.ValidationFailed("UserId", "must be positive"));
        }

        // Chain: parse ID, then fetch user name
        var result = ParseUserId("123")
            .SelectMany(FetchUserName);

        result.IsOk.Should().BeTrue();
        result.Value.Should().Be("User_123");
    }

    [Fact]
    public void Result_Monad_stops_on_first_error()
    {
        // When an operation fails, subsequent operations are skipped

        Result<int, Error> ParseUserId(string input)
        {
            return int.TryParse(input, out var id)
                ? Result<int, Error>.Ok(id)
                : Result<int, Error>.Err(Error.InvalidFormat("UserId"));
        }

        Result<string, Error> FetchUserName(int userId)
        {
            return userId > 0
                ? Result<string, Error>.Ok($"User_{userId}")
                : Result<string, Error>.Err(Error.ValidationFailed("UserId", "must be positive"));
        }

        // Chain with invalid input
        var result = ParseUserId("invalid")
            .SelectMany(FetchUserName);

        result.IsOk.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_FORMAT");
        result.Error.Message.Should().Contain("UserId");
    }

    [Fact]
    public void Result_Monad_practical_web_api_validation()
    {
        // Practical example: validating and processing a user registration request

        Result<string, Error> ValidateEmail(string email)
        {
            return email.Contains("@")
                ? Result<string, Error>.Ok(email)
                : Result<string, Error>.Err(Error.InvalidFormat("Email"));
        }

        Result<int, Error> ValidateAge(int age)
        {
            return age >= 18
                ? Result<int, Error>.Ok(age)
                : Result<int, Error>.Err(Error.ValidationFailed("Age", "must be at least 18 years old"));
        }

        Result<(string email, int age), Error> RegisterUser(string email, int age)
        {
            return ValidateEmail(email)
                .SelectMany(validEmail =>
                    ValidateAge(age)
                        .SelectMany(validAge =>
                            Result<(string, int), Error>.Ok((validEmail, validAge))
                        )
                );
        }

        // Success case
        var success = RegisterUser("user@example.com", 25);
        success.IsOk.Should().BeTrue();
        success.Value.Should().Be(("user@example.com", 25));

        // Failure case: invalid email
        var failEmail = RegisterUser("invalid-email", 25);
        failEmail.IsOk.Should().BeFalse();
        failEmail.Error.Code.Should().Be("INVALID_FORMAT");

        // Failure case: underage
        var failAge = RegisterUser("user@example.com", 16);
        failAge.IsOk.Should().BeFalse();
        failAge.Error.Code.Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public void Result_Monad_LINQ_query_syntax()
    {
        // Result monad works with LINQ query syntax too

        Result<int, Error> ParseUserId(string input)
        {
            return int.TryParse(input, out var id)
                ? Result<int, Error>.Ok(id)
                : Result<int, Error>.Err(Error.InvalidFormat("UserId"));
        }

        Result<string, Error> FetchUserName(int userId)
        {
            return userId > 0
                ? Result<string, Error>.Ok($"User_{userId}")
                : Result<string, Error>.Err(Error.ValidationFailed("UserId", "must be positive"));
        }

        // Fluent syntax
        var fluentResult = ParseUserId("456")
            .SelectMany(FetchUserName);

        // LINQ query syntax (desugars to SelectMany)
        var queryResult = from userId in ParseUserId("456")
                          from userName in FetchUserName(userId)
                          select userName;

        fluentResult.Value.Should().Be(queryResult.Value);
    }

    [Fact]
    public void Result_Monad_Match_for_fluent_result_handling()
    {
        // Manual Match is the catamorphism for Result - it handles both success and failure
        // in a clean, fluent way without if/else, reducing cyclomatic complexity

        // the bad way :
        Result<int, Error> ParseUserId(string input)
        {
            return int.TryParse(input, out var id)
                ? Result<int, Error>.Ok(id)   // ? : is equivalent to if/else
                : Result<int, Error>.Err(Error.InvalidFormat("UserId")); // it increases cyclomatic complexity
        }

        // Pattern 1: Match returning a value (like a ternary operator)
        var result1 = ParseUserId("42");
        string message1 = result1.Match(
            onOk: id => $"Successfully parsed user ID: {id}",
            onErr: err => $"Error: {err.Code} - {err.Message}"
        );

        message1.Should().Be("Successfully parsed user ID: 42");

        // Pattern 2: Match with side effects (like if/else)
        var result2 = ParseUserId("invalid");
        var sideEffectLog = new List<string>();

        result2.Match(
            onOk: id => sideEffectLog.Add($"User ID: {id}"),
            onErr: err => sideEffectLog.Add($"Error: {err.Code}")
        );

        sideEffectLog.Should().ContainSingle().Which.Should().Be("Error: INVALID_FORMAT");
    }

    [Fact]
    public void Result_Monad_Match_in_web_api_response()
    {
        // Practical example: converting Result to HTTP response in a clean, fluent way

        Result<string, Error> FetchUser(int userId)
        {
            return userId > 0
                ? Result<string, Error>.Ok($"User_{userId}")
                : Result<string, Error>.Err(Error.NotFound("User"));
        }

        // Clean, fluent API response handling
        var apiResponse = FetchUser(123).Match(
            onOk: userName => new { success = true, data = userName, error = (string)null },
            onErr: err => new { success = false, data = (string)null, error = err.Code }
        );

        apiResponse.success.Should().BeTrue();
        apiResponse.data.Should().Be("User_123");

        // Error case
        var errorResponse = FetchUser(-1).Match(
            onOk: userName => new { success = true, data = userName, error = (string)null },
            onErr: err => new { success = false, data = (string)null, error = err.Code }
        );

        errorResponse.success.Should().BeFalse();
        errorResponse.error.Should().Be("NOT_FOUND");
    }
}
