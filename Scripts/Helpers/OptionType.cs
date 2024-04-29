namespace Behide.Types;

using System;
using Microsoft.FSharp.Core;

#nullable disable

public readonly struct Option<T>
{
    private readonly bool _hasValue;
    private readonly T _value;

    private Option(T value, bool hasValue)
    {
        _value = value;
        _hasValue = hasValue;
    }

    // public bool IsSome => _hasValue;
    // public bool IsNone => !_hasValue;

    public bool HasValue(out T value)
    {
        value = _value;
        return _hasValue;
    }

    public T Value => _hasValue ? _value : throw new InvalidOperationException("Option is None");

    public static Option<T> Some(T value) => new(value, true);
    public static Option<T> None => new(default, false);

    public static implicit operator Option<T>(T value) => new(value, true);
    public static implicit operator FSharpOption<T>(Option<T> opt) => opt._hasValue ? new FSharpOption<T>(opt.Value) : FSharpOption<T>.None;
    public static implicit operator Option<T>(FSharpOption<T> opt) => FSharpOption<T>.get_IsSome(opt) ? Some(opt.Value) : None;
}

public class Option
{
    public static Option<T> Some<T>(T value) => Option<T>.Some(value);
    public static Option<T> None<T>() => Option<T>.None;
    // public static Option<T> FromResult<T, E>(Result<T, E> res) => res.HasValue(out var value) ? Some(value) : None<T>();
    // public static Option<E> FromResultErr<T, E>(Result<T, E> res) => res.HasError(out var error) ? Some(error) : None<E>();
}

#nullable enable

public static class FSharpOptionExtension
{
    // static public FSharpOption<T> FromNullable(T? nullable) =>
    //     nullable switch
    //     {
    //         null => FSharpOption<T>.None,
    //         _ => FSharpOption<T>.Some(nullable)
    //     };

    // static public T? ToNullable(FSharpOption<T> opt) =>
    //     FSharpOption<T>.GetTag(opt) switch
    //     {
    //         FSharpOption<T>.Tags.Some => opt.Value,
    //         _ => default
    //     };

    static public bool IsSome<T>(this FSharpOption<T> opt) => FSharpOption<T>.get_IsSome(opt);
    static public bool IsNone<T>(this FSharpOption<T> opt) => FSharpOption<T>.get_IsNone(opt);

    static public bool HasValue<T>(this FSharpOption<T> opt, out T value)
    {
        if (opt.IsSome())
        {
            value = opt.Value;
            return true;
        }
        else
        {
            value = default!;
            return false;
        }
    }
}
