using System;

public class Result
{
    protected Result(bool success, string error)
    {
        if (success && error != string.Empty)
            throw new InvalidOperationException();
        if (!success && error == string.Empty)
            throw new InvalidOperationException();
        Success = success;
        Error = error;
    }

    public bool Success { get; }
    public string Error { get; }
    public bool IsFailure => !Success;

    public static Result Fail(string message) => new Result(false, message);
    public static Result Ok() => new Result(true, string.Empty);

    public static Result<T> Fail<T>(string message) => new Result<T>(default, false, message);
    public static Result<T> Ok<T>(T value) => new Result<T>(value, true, string.Empty);
}

public class Result<T> : Result
{
    protected internal Result(T value, bool success, string error)
        : base(success, error)
    {
        Value = value;
    }

    public T Value { get; set; }
}