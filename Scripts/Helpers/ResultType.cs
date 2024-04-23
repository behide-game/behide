namespace Behide.Types;

using System;
using Microsoft.FSharp.Core;

public readonly struct Result<T, E>
{
    private readonly bool _success;
    public readonly T Value;
    public readonly E Error;

    private Result(T v, E e, bool success)
    {
        Value = v;
        Error = e;
        _success = success;
    }

    public bool IsOk => _success;

    #nullable disable
    public static Result<T, E> Ok(T v) => new(v, default, true);
    public static Result<T, E> Err(E e) => new(default, e, false);

    public static implicit operator Result<T, E>(T v) => new(v, default, true);
    public static implicit operator Result<T, E>(E e) => new(default, e, false);
    #nullable enable

    public R Match<R>(
            Func<T, R> success,
            Func<E, R> failure) =>
        _success ? success(Value) : failure(Error);

    public void Match(
        Action<T> success,
        Action<E> failure)
    {
        if (_success)
            success(Value);
        else
            failure(Error);
    }

    public bool HasValue(out T value)
    {
        value = Value;
        return _success;
    }

    public bool HasError(out E error)
    {
        error = Error;
        return !_success;
    }

    public Result<T2, E> Map<T2>(Func<T, T2> map) => _success ? map(Value) : Error;
    public Result<T, E2> MapError<E2>(Func<E, E2> map) => _success ? Value : map(Error);
    public Option<T> ToOption() => _success ? Value : Option<T>.None;
    public Option<E> ToOptionError() => _success ? Option<E>.None : Error;
}

public class Result
{
    public static Result<T, E> Ok<T, E>(T value) => Result<T, E>.Ok(value);
    public static Result<T, E> Err<T, E>(E error) => Result<T, E>.Err(error);
}

public static class CustomFSharpResultExtensions
{
    public static FSharpResult<TResult, NewError> MapError<BaseError, NewError, TResult>(
        this FSharpResult<TResult, BaseError> result,
        Func<BaseError, NewError> func)
    {
        return result.IsError
            ? FSharpResult<TResult, NewError>.NewError(func(result.ErrorValue))
            : FSharpResult<TResult, NewError>.NewOk(result.ResultValue);
    }

    public static Result<TR, TE> ToResult<TR, TE>(this FSharpResult<TR, TE> result)
    {
        return result.IsError
            ? Result<TR, TE>.Err(result.ErrorValue)
            : Result<TR, TE>.Ok(result.ResultValue);
    }

    public static bool HasValue<T, E>(this FSharpResult<T, E> result, out T value)
    {
        // if (result.IsOk)
        //     value = result.ResultValue;
        // else
        //     value = default;
        // TODO: Check if it works
        value = result.ResultValue;
        return result.IsOk;
    }

    public static bool HasError<T, E>(this FSharpResult<T, E> result, out E error)
    {
        // if (result.IsError)
        //     error = result.ErrorValue;
        // else
        //     error = default;
        error = result.ErrorValue;
        return result.IsError;
    }
}
