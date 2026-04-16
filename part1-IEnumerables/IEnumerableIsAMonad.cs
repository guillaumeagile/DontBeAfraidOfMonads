using AwesomeAssertions;

namespace DontBeAfraidOfMonad;

// Two simple concepts you already use in C# every day:
//
// ── MONOID ──────────────────────────────────────────────────────────────
//
// Le nom vient des mathématiques, plus précisément de la théorie des catégories et de l'algèbre abstraite.
// "Mono" vient du grec monos (seul, unique) — il fait référence à l'élément neutre (ou identité), qui est unique dans la structure. C'est cet élément particulier qui distingue un monoïde d'une structure plus générale.
// Un monoïde est donc une structure avec :
//
// Une opération binaire associative (⊕)
// Un élément unique neutre e tel que e ⊕ x = x ⊕ e = x
//
// Le suffixe "-oïde" (du grec eidos, forme/type) est très courant en algèbre abstraite pour désigner des structures qui ressemblent à quelque chose mais en plus général :
//
// un groupeoïde = structure qui ressemble à un groupe mais sans toutes ses propriétés
// un monoïde = structure qui ressemble à un groupe, mais sans l'inverse (seulement l'élément neutre/unique)

// En pratique en programmation fonctionnelle, String avec + et "", ou List avec ++ et [], sont des monoïdes classiques
//      — l'élément neutre est bien unique dans chaque cas.

// A monoid is a type with:
//   1. An associative binary operation:  M<T> → M<T> → M<T>
//   2. An identity element:               M<T>
//
// For IEnumerable<T>:
//   • Concat  is the binary operation:  [1,2].Concat([3,4]) → [1,2,3,4]
//   • Empty   is the identity element:  [].Concat([1,2]) == [1,2]
//
// Laws:
//   Associativity:  a.Concat(b).Concat(c) == a.Concat(b.Concat(c))
//   Identity:       Empty.Concat(x) == x   and   x.Concat(Empty) == x
//
// A monoid is about COMBINING values of the same type.
// It's immutable — Concat returns a new sequence, never mutates.
//
// ── MONAD ───────────────────────────────────────────────────────────────
// Même famille étymologique, mais l'histoire est plus riche !
//
// "Monade" vient du grec monas (unité, ce qui est un), lui-même de monos. Le mot existait bien avant les mathématiques.
//
// Leibniz d'abord (XVIIe siècle)
// Leibniz appelait monades les unités fondamentales et indivisibles de la réalité dans sa métaphysique
//      — des atomes conceptuels, auto-suffisants, sans fenêtres sur l'extérieur. Chaque monade encapsule son propre monde.
// C'est une intuition curieusement proche de ce que fait une monade en FP : une valeur encapsulée dans un contexte opaque, qu'on ne peut manipuler qu'à travers des opérations définies.
//
// La théorie des catégories (XXe siècle)
// En catégorie, une monade est un endofoncteur T muni de deux transformations naturelles :
//
// η : Id → T (le return / pure)
// μ : T∘T → T (le join / aplatissement p)
//
// Le "mono" est toujours là : ces transformations doivent satisfaire des lois d'unité (l'élément neutre agit trivialement) et d'associativité — exactement comme un monoïde.
// En fait, il y a un théorème précis :
//
// ``` Une monade est un monoïde dans la catégorie des endofoncteurs. ```
//
// Ce n'est pas une métaphore — c'est littéralement vrai. bind/flatMap joue le rôle de ⊕,
// et pure/return joue le rôle de e.


// A monad is a wrapper type M<T> with:
//   1. Return  — wrap a value:            T → M<T>
//   2. Bind    — chain a computation:     M<T> → (T → M<U>) → M<U>
//
// For IEnumerable<T>:
//   • Return = new[] { x }         (wrap a single value)
//   • Bind   = SelectMany          (unwrap, apply, flatten)
//
// Laws:
//   Left identity:   Return(a).Bind(f) == f(a)
//   Right identity:  m.Bind(Return)    == m
//   Associativity:   m.Bind(f).Bind(g) == m.Bind(x => f(x).Bind(g))
//
// A monad is about CHAINING computations that produce wrapped values.
//
// ── HOW THEY RELATE ─────────────────────────────────────────────────────
//
// IEnumerable<T> has BOTH structures — it's a "monad plus":
//
//   • The monad (Return + Bind) lets you chain computations:
//       source.SelectMany(f).SelectMany(g)  — pipeline of transformations
//
//   • The monoid (Empty + Concat) lets you combine results:
//       Bind internally uses Concat to flatten M<M<T>> → M<T>
//
//   Bind = Map + Flatten = Map + Concat (the monoid)
//
// The monoid is the "how" behind Bind's flatten step.
// The monad is the "what" — the interface you use to chain.
//
// ── MAP / FILTER / REDUCE ──────────────────────────────────────────────
//
// The bread and butter of functional style is a three-step pipeline:
//
//   1. Map    (Select)     — transform each element
//   2. Filter (Where)      — keep only the ones you want
//   3. Reduce (Aggregate)  — collapse everything into one value
//
// In C# fluent dot-notation:
//   source.Select(f).Where(p).Aggregate(seed, combine)
//
// In LINQ query syntax:
//   from x in source
//   where p(x)
//   select f(x)          // no query keyword for Aggregate — use fluent
//
// This pattern is so common that every FP language has it built-in,
// and every C# developer uses it daily via LINQ — just with different names.
//
// ── FULL DICTIONARY ─────────────────────────────────────────────────────
//
//   FP concept          | LINQ / C#               | What it does
//   ────────────────────┼──────────────────────────┼──────────────────────────────
//   Return / pure       | new[] { x }              | Wrap a value into M<T>
//                       | Enumerable.Repeat(x, 1)  |
//   Bind / >>=          | SelectMany               | Chain M<T> → (T→M<U>) → M<U>
//   fmap / Map          | Select                   | Transform inside: M<T>→M<U>
//   Join / Flatten      | .SelectMany(x => x)      | Collapse M<M<T>> → M<T>
//   Guard / Filter      | Where                    | Keep only matching elements
//   mzero / Empty       | Enumerable.Empty<T>()    | The identity element
//   mplus / <>          | Concat                   | Combine two M<T> → M<T>
//   fold / reduce      | Aggregate                | Collapse M<M<T>> → M<T> via monoid
//   Kleisli >=>         | f.Then(g) via SelectMany | Compose T→M<U> and U→M<V>
//   Zip / <*>           | Zip                      | Applicative combine
//
// Let's prove it with tests.
public class IEnumerableIsAMonad
{
    // ── Return: wrapping a value ──────────────────────────────────────

    [Fact]
    public void Return_wraps_a_single_value_into_an_IEnumerable()
    {
        // "Return" lifts a plain value into the monadic world.
        // For IEnumerable<T>, the simplest Return is: new[] { value }

        IEnumerable<int> wrapped = new[] { 42 };

        wrapped.First().Should().Be(42);
        wrapped.Should().ContainSingle().Which.Should().Be(42);

        //how to lift a value with LINQ ?
        //LINQ's "select" is fmap (functor map), but combined with a single-element
        //source it gives us Return — the LINQ way to wrap a value:
        IEnumerable<int> lifted = from _ in new[] { 0 } select 42;

        //or written in the fluent syntax:
        IEnumerable<int> lifted2 = new[] { 0 }.Select(_ => 42);

        lifted.Should().Equal(wrapped);
        lifted2.Should().Equal(lifted);
    }

    // ── Other ways to put values into the monad ───────────────────────

    [Fact]
    public void Concat_is_the_monoid_append_not_Return()
    {
        // Return lifts ONE value:  T → M<T>
        // Concat combines TWO monadic values:  M<T> → M<T> → M<T>
        // That's the monoid structure (mplus / <>), not Return.
        // But it's still a way to get more values into the monadic context.

        IEnumerable<int> a = new[] { 1, 2 };
        IEnumerable<int> b = new[] { 3, 4 };

        IEnumerable<int> combined = a.Concat(b);

        combined.Should().Equal(1, 2, 3, 4);
    }


    [Fact]
    public void Add_is_the_imperative_mutable_version_of_lifting()
    {
        // List<T>.Add is the imperative mutation counterpart.
        // It mutates the existing monadic value instead of creating a new one.
        // Same idea, different paradigm.

        var list = new List<int> { 1, 2 };
        list.Add(3);  //returns nothing, but mutates the instance

        list.Should().Equal(1, 2, 3);

        // which is the same as:
        new[] { 1, 2 }.Concat(new[] { 3 }).Should().Equal(list);
    }

    // Monoids are immutable: Concat returns a NEW sequence, it never
    // mutates the originals. This is essential for composability:
    //
    //   • You can share a sequence without fearing someone will change it.
    //   • You can chain: a.Concat(b).Concat(c) — each step produces a
    //     fresh value, so the order and grouping are predictable.
    //   • Contrast with List.Add: it mutates in place, so sharing the
    //     same list in two places means both see the mutation.
    //
    // Immutability is what makes the monoid laws (associativity, identity)
    // hold reliably — there are no side-effects to surprise you.

    [Fact]
    public void Concat_is_the_monoid_append_chainable()
    {
        // Return lifts ONE value:  T → M<T>
        // Concat combines TWO monadic values:  M<T> → M<T> → M<T>
        // That's the monoid structure (mplus / <>), not Return.
        // But it's still a way to get more values into the monadic context.

        IEnumerable<int> a = new[] { 1, 2 };
        IEnumerable<int> b = new[] { 3, 4 };
        IEnumerable<int> c = new[] { 5, 6 };

        IEnumerable<int> combined = a.Concat(b).Concat(c);

        combined.Should().Equal(1, 2, 3, 4, 5, 6);
    }

    [Fact]
    public void Concat_associativity_grouping_does_not_matter()
    {
        // A monoid must be associative: (a + b) + c == a + (b + c)
        // For Concat, this means grouping doesn't change the result.
        // This is what makes chaining safe and predictable.

        IEnumerable<int> a = new[] { 1, 2 };
        IEnumerable<int> b = new[] { 3, 4 };
        IEnumerable<int> c = new[] { 5, 6 };

        // Group left:  (a + b) + c
        IEnumerable<int> leftGrouped  = a.Concat(b).Concat(c);
        // Group right:  a + (b + c)
        IEnumerable<int> rightGrouped = a.Concat(b.Concat(c));

        leftGrouped.Should().Equal(rightGrouped);
    }


    [Fact]
    public void Concat_associativity_practical_partitioning()
    {
        // Another practical angle: you partition data, process partitions
        // independently, then recombine. Associativity guarantees the
        // partition boundaries don't affect the final result.

        IEnumerable<int> data = Enumerable.Range(1, 100);

        // Split into 3 partitions
        IEnumerable<int> partition1 = data.Take(33);
        IEnumerable<int> partition2 = data.Skip(33).Take(34);
        IEnumerable<int> partition3 = data.Skip(67);

        // Recombine — any grouping gives the same result
        IEnumerable<int> recombined = partition1.Concat(partition2).Concat(partition3);

        recombined.Should().Equal(data);
    }

    // ====================================================================
    // MONADS: MANIPULATIONS IN THE ELEVATED WORLD

    // ── Bind: chaining computations ───────────────────────────────────

    [Fact]
    public void Bind_chains_a_simple_function_that_returns_IEnumerable()
    {
        // "Bind" (aka >>= or SelectMany) takes an M<T> and a function T → M<U>,
        // and returns M<U>. It handles the unwrapping and re-wrapping for you.
        //
        // Let's start simple: a function that takes an int and returns
        // a single-element IEnumerable (i.e. T → M<T>).

        IEnumerable<int> source = new[] { 1, 2, 3 };

        // Each element gets wrapped in a new IEnumerable after being incremented
        IEnumerable<int> Increment(int x) => new[] { x + 1 };

        // Bind = SelectMany (fluent dot-notation)
        IEnumerable<int> result = source.SelectMany(Increment);

        // Same with LINQ query syntax (second "from" = Bind):
        IEnumerable<int> query = from x in source
                                 from i in Increment(x)
                                 select i;

        result.Should().Equal(2, 3, 4);
        query.Should().Equal(result);

        // Note: when the function always returns a single element,
        // Bind behaves like Select (fmap). It's the same result:
        source.Select(x => x + 1).Should().Equal(result);
    }

    [Fact]
    public void Bind_chains_a_function_that_expands_each_element()
    {
        // Now a function that returns MORE than one element per input.
        // This is where Bind differs from Select: it flattens.

        IEnumerable<int> source = new[] { 1, 2, 3 };

        // Each int becomes a collection of its neighbours
        IEnumerable<int> Neighbours(int x) => new[] { x - 1, x + 1 };

        // Bind = SelectMany (fluent dot-notation)
        IEnumerable<int> result = source.SelectMany(Neighbours);

        //var z = source.Select(Neighbours);

        // Same with LINQ query syntax (second "from" = Bind):
        IEnumerable<int> query = from x in source
                                 from n in Neighbours(x)
                                 select n;
        //                                     1     2     3
        //                                    /\    /\    /\
        result.Should().Equal(0, 2, 1, 3, 2, 4);
        query.Should().Equal(result);
    }

    [Fact]
    public void Bind_does_NOT_deduplicate_overlapping_expansions_are_kept()
    {
        // Bind just flattens via Concat — it does NOT remove duplicates.
        // When two different source elements produce the same output,
        // both copies appear in the result.
        //
        //   Neighbours(1) = [0, 2]     ← 2 is produced here
        //   Neighbours(2) = [1, 3]
        //   Neighbours(3) = [2, 4]     ← 2 is produced again
        //
        //   Result = [0, 2, 1, 3, 2, 4]  ← 2 appears TWICE

        IEnumerable<int> source = new[] { 1, 2, 3 };
        IEnumerable<int> Neighbours(int x) => new[] { x - 1, x + 1 };

        IEnumerable<int> result = source.SelectMany(Neighbours);

        // 2 appears twice — Bind does not deduplicate
        result.Should().Equal(0, 2, 1, 3, 2, 4);
        result.Count(x => x == 2).Should().Be(2);

        // If you need uniqueness, that's a separate concern — use Distinct:
        IEnumerable<int> unique = result.Distinct();

        unique.Should().Equal(0, 2, 1, 3, 4);
        unique.Count(x => x == 2).Should().Be(1);

        // Fluent dot-notation:
        IEnumerable<int> distinctFluent = source
            .SelectMany(Neighbours)
            .Distinct();

        // LINQ query syntax (Distinct has no keyword, must use fluent):
        IEnumerable<int> distinctQuery = (from x in source
                                          from n in Neighbours(x)
                                          select n).Distinct();

        distinctFluent.Should().Equal(distinctQuery);
    }

    // ── Bind uses the monoid internally ────────────────────────────────

    [Fact]
    public void Bind_is_map_then_flatten_and_flatten_uses_Concat()
    {
        // Bind can be decomposed into two steps:
        //   1. Map:   M<T> → (T → M<U>) → M<M<U>>     (Select)
        //   2. Flatten: M<M<U>> → M<U>                  (Concat all inner sequences)
        //
        // The flatten step IS the monoid in action: it Concat's all the inner
        // sequences together. Without the monoid structure, you couldn't flatten.

        IEnumerable<int> source = new[] { 1, 3, 5 };
        IEnumerable<int> Neighbours(int x) => new[] { x - 1, x + 1 };


        // Step 1: Map gives us M<M<U>> — a sequence of sequences
        IEnumerable<IEnumerable<int>> mapped = source.Select(Neighbours);
        mapped.Count().Should().Be(3);
        mapped.Should().Equal(source.SelectMany(Neighbours)); //this must fail

        // Step 2: Flatten using the monoid (Concat)
        IEnumerable<int> flattened = mapped.Aggregate(
            Enumerable.Empty<int>(),
            (acc, seq) => acc.Concat(seq));

        // This is equivalent to Bind:
        flattened.Should().Equal(source.SelectMany(Neighbours));
    }

    [Fact]
    public void Aggregate_is_the_monoid_fold_with_accumulator()
    {
        // Aggregate folds a sequence using the monoid operation.
        // It starts from the identity element (Empty) and Concat's every
        // element onto the accumulator — exactly what Flatten does.
        //
        //   Aggregate(seed, func) = seed.Concat(x1).Concat(x2).Concat(x3)...

        // Practical example: collecting tags from multiple articles
        IEnumerable<string> article1Tags = new[] { "c#", "monad" };
        IEnumerable<string> article2Tags = new[] { "linq", "monad" };
        IEnumerable<string> article3Tags = new[] { "c#", "linq", "fun" };

        IEnumerable<IEnumerable<string>> allTags = new[] { article1Tags, article2Tags, article3Tags };

        // Fluent dot-notation: Aggregate with monoid seed + Concat
        IEnumerable<string> collected = allTags.Aggregate(
            Enumerable.Empty<string>(),
            (acc, tags) => acc.Concat(tags));

        collected.Should().Equal("c#", "monad", "linq", "monad", "c#", "linq", "fun");

        // This is the same as chaining Concat manually:
        IEnumerable<string> chained = Enumerable.Empty<string>()
            .Concat(article1Tags)
            .Concat(article2Tags)
            .Concat(article3Tags);

        collected.Should().Equal(chained);

        // And the same as Flatten / Join:
        allTags.SelectMany(x => x)
            .Should().Equal(collected);
    }

    [Fact]
    public void Map_Filter_Reduce_is_the_functional_pipeline()
    {
        // The classic functional pipeline: Map → Filter → Reduce
        // In C#: Select → Where → Aggregate

        IEnumerable<int> source = Enumerable.Range(1, 20);

        // Fluent dot-notation:
        int result = source
            .Select(x => x * 3)              // Map:    multiply each by 3
            .Where(x => x % 2 == 0)          // Filter: keep even numbers
            .Aggregate(0, (acc, x) => acc + x); // Reduce: sum them all

        // LINQ query syntax (Map + Filter):
        var query = from x in source
                    let tripled = x * 3
                    where tripled % 2 == 0
                    select tripled;
        // Reduce has no query keyword — switch to fluent:
        int resultQuery = query.Aggregate(0, (acc, x) => acc + x);

        // Verify: 1..20 ×3 = 3,6,9,...,60 → even = 6,12,18,...,60 → sum
        result.Should().Be(330);
        resultQuery.Should().Be(result);

        // Same result with the built-in Sum (a specialized reduce):
        source.Select(x => x * 3).Where(x => x % 2 == 0).Sum()
            .Should().Be(result);
    }

    // ── Partial conclusion ─────────────────────────────────────────────
    //
    // So: Bind is the primary monadic chaining mechanism.
    // But Bind relies on the monoid (Concat) for its flatten step.
    //
    //   Bind = Map + Flatten
    //        = Map + Concat (the monoid)
    //
    // This is why IEnumerable<T> is sometimes called a "monad plus":
    // it has both the monad structure (Return + Bind) AND the monoid
    // structure (Concat + Empty), and Bind uses the monoid internally.
    //
    // In monadic style, you chain with Bind (SelectMany / LINQ).
    // The monoid is what makes the chaining *combine* results rather
    // than discarding or failing — it's the "how" behind the scenes.

    // ── Monad Law 1: Left Identity ────────────────────────────────────

    [Fact]
    public void LeftIdentity_Return_then_Bind_equals_applying_the_function_directly()
    {
        // Law: Return(a).Bind(f) == f(a)
        //
        // If you wrap a value and immediately bind a function,
        // that's the same as just calling the function on the value.

        const int value = 5;
        IEnumerable<string> f(int x) => new[] { x.ToString(), (x * 2).ToString() };

        // Fluent dot-notation:
        IEnumerable<string> leftSide  = new[] { value }.SelectMany(f); // Return then Bind
        // LINQ query syntax:
        IEnumerable<string> leftSideQuery = from v in new[] { value }
                                            from r in f(v)
                                            select r;
        IEnumerable<string> rightSide = f(value);                       // just the function

        leftSide.Should().Equal(rightSide);
        leftSideQuery.Should().Equal(rightSide);
    }

    // ── Monad Law 2: Right Identity ───────────────────────────────────

    [Fact]
    public void RightIdentity_Bind_then_Return_equals_the_original()
    {
        // Law: m.Bind(Return) == m
        //
        // If you bind the Return function itself, nothing changes —
        // you just re-wrap every element, which flattens back to the original.

        IEnumerable<int> source = new[] { 10, 20, 30 };

        IEnumerable<int> Return(int x) => new[] { x };

        // Fluent dot-notation:
        IEnumerable<int> result = source.SelectMany(Return);
        // LINQ query syntax:
        IEnumerable<int> resultQuery = from x in source
                                       from r in Return(x)
                                       select r;

        result.Should().Equal(source);
        resultQuery.Should().Equal(source);
    }

    // ── Monad Law 3: Associativity ────────────────────────────────────

    [Fact]
    public void Associativity_nesting_Bind_does_not_change_the_result()
    {
        // Law: m.Bind(f).Bind(g) == m.Bind(x => f(x).Bind(g))
        //
        // You can re-associate the binds without changing the outcome.
        // This is what makes chaining (pipelines) predictable.

        IEnumerable<int> source = new[] { 1, 2 };

        IEnumerable<int> f(int x) => new[] { x, x * 10 };
        IEnumerable<string> g(int x) => new[] { x.ToString(), "!" + x };

        // Left-associative (fluent): (m >>= f) >>= g
        IEnumerable<string> leftAssoc = source
            .SelectMany(f)
            .SelectMany(g);
        // Left-associative (query syntax):
        IEnumerable<string> leftAssocQuery = from x in source
                                              from fx in f(x)
                                              from gx in g(fx)
                                              select gx;

        // Right-associative (fluent): m >>= (x => f(x) >>= g)
        IEnumerable<string> rightAssoc = source.SelectMany(x => f(x).SelectMany(g));
        // Right-associative (query syntax):
        IEnumerable<string> rightAssocQuery = from x in source
                                              from fx in f(x)
                                              from gx in g(fx)
                                              select gx;

        leftAssoc.Should().Equal(rightAssoc);
        leftAssocQuery.Should().Equal(rightAssocQuery);
    }

    // ── LINQ is just syntactic sugar ──────────────────────────────────
    //
    // LINQ query syntax is NOT a different language feature.
    // The C# compiler simply rewrites every query keyword into
    // a method call. Nothing more.
    //
    //   Query keyword    →    Method call
    //   ─────────────────────────────────
    //   select x         →    .Select(x => x)
    //   where p          →    .Where(x => p)
    //   from x in src    →    src (first) / .SelectMany(...) (second+)
    //   orderby x        →    .OrderBy(x => x)
    //   group x by k     →    .GroupBy(k => k)
    //   join x in y on k  →    .Join(y, k1, k2, ...)
    //
    // Any type with these methods works with LINQ — it doesn't
    // need to implement any interface. That's why LINQ is just
    // monad syntax: it's pattern-based, like >>= in Haskell.

    [Fact]
    public void LINQ_select_many_is_bind_and_you_use_it_every_day()
    {
        // Every LINQ query with multiple "from" clauses is a chain of Binds.
        // The compiler translates this:

        var query = from x in new[] { 1, 2, 3 }
                    from y in new[] { x, x * 10 }
                    select $"{x}:{y}";

        // ...into exactly this:

        IEnumerable<string> desugared = new[] { 1, 2, 3 }
            .SelectMany(x => new[] { x, x * 10 }, (x, y) => $"{x}:{y}");

        query.Should().Equal(desugared);
    }

    [Fact]
    public void LINQ_where_is_sugar_for_Where()
    {
        // "where" in query syntax → .Where() in fluent syntax

        var query = from x in new[] { 1, 2, 3, 4, 5 }
                    where x > 2
                    select x;

        IEnumerable<int> desugared = new[] { 1, 2, 3, 4, 5 }
            .Where(x => x > 2);

        query.Should().Equal(desugared);
    }

    [Fact]
    public void LINQ_orderby_is_sugar_for_OrderBy()
    {
        // "orderby" in query syntax → .OrderBy() / .OrderByDescending()

        var query = from x in new[] { 3, 1, 4, 1, 5 }
                    orderby x
                    select x;

        IEnumerable<int> desugared = new[] { 3, 1, 4, 1, 5 }
            .OrderBy(x => x);

        query.Should().Equal(desugared);
    }

    [Fact]
    public void LINQ_group_by_is_sugar_for_GroupBy()
    {
        // "group ... by ..." in query syntax → .GroupBy()

        var query = from x in new[] { 1, 2, 3, 4, 5, 6 }
                    group x by x % 2 into g
                    select g.Key;

        IEnumerable<int> desugared = new[] { 1, 2, 3, 4, 5, 6 }
            .GroupBy(x => x % 2)
            .Select(g => g.Key);

        query.Should().Equal(desugared);
    }

    [Fact]
    public void LINQ_join_is_sugar_for_Join()
    {
        // "join ... on ... equals ..." → .Join()

        var customers = new[] { (id: 1, name: "Alice"), (id: 2, name: "Bob") };
        var orders    = new[] { (custId: 1, item: "Book"), (custId: 2, item: "Pen"), (custId: 1, item: "Cup") };

        var query = from c in customers
                    join o in orders on c.id equals o.custId
                    select $"{c.name} bought {o.item}";

        var desugared = customers.Join(
            orders,
            c => c.id,
            o => o.custId,
            (c, o) => $"{c.name} bought {o.item}");

        query.Should().Equal(desugared);
    }

    [Fact]
    public void LINQ_full_pipeline_every_keyword_is_a_method_call()
    {
        // A complex query with multiple keywords — all just method calls:

        var query = from x in new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
                    where x % 2 == 0
                    orderby x descending
                    select x * 10;

        // The compiler desugars the above into:
        IEnumerable<int> desugared = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
            .Where(x => x % 2 == 0)
            .OrderByDescending(x => x)
            .Select(x => x * 10);

        query.Should().Equal(desugared);
    }

    // ── foreach is also monadic ───────────────────────────────────────

    [Fact]
    public void ForEach_is_monadic_binding_over_side_effects()
    {
        // foreach unwraps each element (like Bind) and runs a side-effect.
        // It's the "imperative" face of the same monadic structure.

        var collected = new List<int>();

        foreach (var x in new[] { 1, 2, 3 }.SelectMany(x => new[] { x, x * 10 }))
        {
            collected.Add(x);
        }

        collected.Should().Equal(1, 10, 2, 20, 3, 30);
    }

    // ── fmap / Map = Select ────────────────────────────────────────────

    [Fact]
    public void fmap_is_Select_transform_inside_the_monad()
    {
        // fmap (functor map) transforms the value inside the monadic context
        // without changing the structure. In LINQ, that's Select.
        //
        //   fmap : (T → U) → M<T> → M<U>
        //   Select: (T → U) → IEnumerable<T> → IEnumerable<U>

        IEnumerable<int> source = new[] { 1, 2, 3 };

        // fmap via Select (fluent)
        IEnumerable<string> result = source.Select(x => x.ToString());

        // fmap via LINQ query syntax
        IEnumerable<string> query = from x in source select x.ToString();

        result.Should().Equal("1", "2", "3");
        query.Should().Equal(result);
    }

    // ── Join / Flatten = SelectMany(identity) ───────────────────────────

    [Fact]
    public void Join_flattens_nested_monadic_layers()
    {
        // Join (aka flatten) collapses M<M<T>> → M<T>.
        // In FP:  join : M<M<T>> → M<T>
        // In LINQ: .SelectMany(x => x)   (bind with the identity function)

        IEnumerable<IEnumerable<int>> nested = new[]
        {
            new[] { 1, 2 },
            new[] { 3 },
            new[] { 4, 5, 6 },
        };

        // Fluent dot-notation:
        IEnumerable<int> flattened = nested.SelectMany(x => x);
        // LINQ query syntax:
        IEnumerable<int> flattenedQuery = from inner in nested
                                          from x in inner
                                          select x;

        flattened.Should().Equal(1, 2, 3, 4, 5, 6);
        flattenedQuery.Should().Equal(flattened);

        // And Bind = fmap then Join:
        //   SelectMany(f) = Join(Select(f))
        IEnumerable<int> Neighbours(int x) => new[] { x - 1, x + 1 };
        IEnumerable<int> source = new[] { 1, 2, 3 };

        source.SelectMany(Neighbours)
            .Should().Equal(source.Select(Neighbours).SelectMany(x => x));
    }

    // ── Guard / Filter = Where ──────────────────────────────────────────

    [Fact]
    public void Guard_is_Where_monadic_filtering()
    {
        // In the list monad, Guard (or mfilter / guard) filters elements.
        // It's the monadic way to add conditional logic inside a Bind chain.
        // In LINQ: Where
        //
        //   guard : (T → bool) → M<T> → M<T>
        //   Where : (T → bool) → IEnumerable<T> → IEnumerable<T>

        IEnumerable<int> source = Enumerable.Range(1, 10);

        // Guard via Where
        IEnumerable<int> evens = source.Where(x => x % 2 == 0);

        evens.Should().Equal(2, 4, 6, 8, 10);

        // In LINQ query syntax, "where" is guard:
        var evensQuery = from x in source
                         where x % 2 == 0
                         select x;

        evensQuery.Should().Equal(evens);
    }

    [Fact]
    public void Guard_inside_Bind_acts_like_monadic_do_notation()
    {
        // In Haskell's do-notation, you'd write:
        //   do x <- [1..10]
        //      guard (even x)
        //      return (x * x)
        //
        // In LINQ, "where" between "from" clauses does exactly this:

        // LINQ query syntax:
        var result = from x in Enumerable.Range(1, 10)
                     where x % 2 == 0
                     select x * x;
        // Fluent dot-notation:
        IEnumerable<int> resultFluent = Enumerable.Range(1, 10)
            .Where(x => x % 2 == 0)
            .Select(x => x * x);

        result.Should().Equal(4, 16, 36, 64, 100);
        resultFluent.Should().Equal(result);
    }

    // ── mzero / Empty = Enumerable.Empty ────────────────────────────────

    [Fact]
    public void mzero_is_Empty_the_identity_element()
    {
        // mzero (or mempty / mzero) is the identity of the monoid:
        //   Empty.Concat(x) == x   and   x.Concat(Empty) == x
        //
        // In the monad-plus world, it also represents "failure" or "no result".

        IEnumerable<int> empty = Enumerable.Empty<int>();
        IEnumerable<int> values = new[] { 1, 2, 3 };

        empty.Concat(values).Should().Equal(values);
        values.Concat(empty).Should().Equal(values);

        // Guard that rejects everything produces mzero:
        Enumerable.Range(1, 10).Where(_ => false)
            .Should().Equal(Enumerable.Empty<int>());
    }

    // ── Kleisli composition (>=>) ───────────────────────────────────────

    [Fact]
    public void Kleisli_composition_composes_two_Bind_functions()
    {
        // Kleisli composition (>=>) takes two functions:
        //   f : T → M<U>    and    g : U → M<V>
        // and produces:    T → M<V>
        //
        //   f >=> g  =  x => f(x).Bind(g)
        //
        // In C#, there's no built-in operator, but it's just:
        //   x => f(x).SelectMany(g)

        IEnumerable<int> Half(int x) => x % 2 == 0 ? new[] { x / 2 } : Enumerable.Empty<int>();
        IEnumerable<int> Double(int x) => new[] { x * 2 };

        // Kleisli composition: Half >=> Double
        IEnumerable<int> HalfThenDouble(int x) => Half(x).SelectMany(Double);

        // Half of 8 is 4, double of 4 is 8 → [8]
        HalfThenDouble(8).Should().Equal(8);

        // Half of 7 fails (mzero), so result is empty
        HalfThenDouble(7).Should().Equal(Enumerable.Empty<int>());

        // This is exactly what chained "from" clauses do:
        var query = from h in Half(8)
                    from d in Double(h)
                    select d;

        query.Should().Equal(HalfThenDouble(8));
    }

    // ── Zip / Applicative <*> ───────────────────────────────────────────

    [Fact]
    public void Zip_is_the_applicative_combine()
    {
        // Zip pairs elements positionally — it's the applicative functor's
        // <*> (ap) operation for the list monad.
        //
        //   <*> : M<(T → U)> → M<T> → M<U>
        //   Zip : IEnumerable<T> → IEnumerable<U> → IEnumerable<(T,U)>

        IEnumerable<int>   nums   = new[] { 1, 2, 3 };
        IEnumerable<string> words = new[] { "one", "two", "three" };

        // Fluent dot-notation:
        var paired = nums.Zip(words);

        paired.Should().Equal((1, "one"), (2, "two"), (3, "three"));

        // With a combiner function, it's even closer to <*>:
        IEnumerable<string> applied = nums.Zip(words, (n, w) => $"{n}={w}");

        applied.Should().Equal("1=one", "2=two", "3=three");

        // LINQ has no direct query syntax for Zip, but you can emulate
        // the positional pairing with Select + index:
        var withIndex = nums.Select((n, i) => (n, i))
            .Select(t => $"{t.n}={words.ElementAt(t.i)}");

        withIndex.Should().Equal(applied);
    }
}
