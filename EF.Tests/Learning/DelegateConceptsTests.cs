using EmployeeManagement.Models;
using EmployeeManagement.Repositories.AdoNet;

namespace EF.Tests.Learning;

/// <summary>
/// Interview-practice notes on delegates, as runnable tests. Each test is one idea; set a breakpoint
/// and step through it (see the README on debugging). The real use in this project is
/// <see cref="RowMapper{T}"/> and the private QueryAsync helper in EmployeeAdoRepository.
/// </summary>
public class DelegateConceptsTests
{
    // 1. A delegate is a TYPE for "a method with this signature". Any matching method can be stored in it.
    private delegate int Operation(int a, int b);
    private static int Add(int a, int b) => a + b;
    private static int Multiply(int a, int b) => a * b;

    [Fact]
    public void A_delegate_variable_holds_any_method_with_a_matching_signature()
    {
        Operation op = Add;                 // method group -> delegate
        Assert.Equal(7, op(3, 4));          // calling it looks like calling a method

        op = Multiply;                      // same variable, different behaviour
        Assert.Equal(12, op(3, 4));

        op = (a, b) => a - b;               // a lambda works too
        Assert.Equal(-1, op(3, 4));
    }

    // 2. Func<...> / Action<...> / Predicate<T> are ready-made generic delegate types.
    [Fact]
    public void Func_Action_and_Predicate_are_the_built_in_delegates()
    {
        Func<int, int, int> func = Add;            // returns a value; the LAST type is the return type
        var log = new List<string>();
        Action<string> action = log.Add;           // returns nothing (void)
        Predicate<int> isEven = n => n % 2 == 0;   // returns bool

        action($"sum={func(2, 3)}");

        Assert.Equal(new[] { "sum=5" }, log);
        Assert.True(isEven(4));
        Assert.False(isEven(5));
    }

    // 3. RowMapper<T> is the same as Func<NpgsqlDataReader, T>: a custom delegate is just a named signature.
    [Fact]
    public void A_custom_delegate_and_the_equivalent_Func_are_different_types_but_convert_by_method_group()
    {
        RowMapper<Employee> custom = reader => new Employee { Id = 1 };
        Func<Npgsql.NpgsqlDataReader, Employee> builtIn = reader => new Employee { Id = 1 };

        Assert.NotEqual(custom.GetType(), builtIn.GetType());
        Func<Npgsql.NpgsqlDataReader, Employee> viaMethod = custom.Invoke; // wrap one in the other
        Assert.Equal(1, viaMethod(null!).Id);
    }

    // 4. Lambdas CLOSE OVER local variables: they capture the variable, not a copy of its value.
    [Fact]
    public void A_lambda_captures_the_variable_not_its_value()
    {
        var id = 5;
        Func<int> readId = () => id;     // like the "parameters => AddWithValue("id", id)" lambda in the repository

        id = 6;                           // changed AFTER the lambda was created

        Assert.Equal(6, readId());        // the lambda sees the new value
    }

    // 5. Delegates are MULTICAST: += chains methods, and one call runs them all in order.
    [Fact]
    public void Delegates_can_be_combined_and_run_in_order()
    {
        var calls = new List<string>();
        Action<string> notify = s => calls.Add($"first:{s}");
        notify += s => calls.Add($"second:{s}");

        notify("saved");

        Assert.Equal(new[] { "first:saved", "second:saved" }, calls);

        // For delegates with a return value only the LAST result is returned.
        Func<int> many = () => 1;
        many += () => 2;
        Assert.Equal(2, many());
    }

    // 6. An unset delegate is null: guard with ?.Invoke (QueryAsync does this for addParameters).
    [Fact]
    public void A_null_delegate_is_skipped_with_the_null_conditional_Invoke()
    {
        Action<List<int>>? optional = null;
        var list = new List<int>();

        optional?.Invoke(list);           // does nothing; calling optional(list) would throw

        Assert.Empty(list);
        Assert.Throws<NullReferenceException>(() => optional!(list));
    }

    // 7. Variance: "out T" lets RowMapper<Employee> be used where RowMapper<object> is expected
    //    (a mapper that makes Employees is also a mapper that makes objects).
    [Fact]
    public void An_out_type_parameter_makes_the_delegate_covariant()
    {
        RowMapper<Employee> employees = reader => new Employee { Name = "Asha" };

        RowMapper<object> objects = employees;       // compiles only because of "out T"

        Assert.Equal("Asha", ((Employee)objects(null!)).Name);
    }

    // 8. A delegate also remembers the TARGET object, so instance methods keep their state.
    private sealed class Counter { public int Count; public void Increment() => Count++; }

    [Fact]
    public void A_delegate_to_an_instance_method_remembers_the_object()
    {
        var counter = new Counter();
        Action tick = counter.Increment;

        tick(); tick(); tick();

        Assert.Equal(3, counter.Count);
        Assert.Same(counter, tick.Target);
    }

    // 9. Passing behaviour in as a parameter: the idea behind QueryAsync(sql, map, addParameters), LINQ's
    //    Where/Select, and Expression<Func<>> projections used with EF Core elsewhere in this project.
    private static List<TOut> MapAll<TIn, TOut>(IEnumerable<TIn> items, Func<TIn, TOut> map)
    {
        var result = new List<TOut>();
        foreach (var item in items) result.Add(map(item));
        return result;
    }

    [Fact]
    public void Behaviour_can_be_passed_in_as_a_parameter()
    {
        var names = MapAll(new[] { 1, 2, 3 }, n => $"emp{n}");

        Assert.Equal(new[] { "emp1", "emp2", "emp3" }, names);
        Assert.Equal(new[] { 2, 4, 6 }, MapAll(new[] { 1, 2, 3 }, n => n * 2));
    }
}
