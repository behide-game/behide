namespace Behide;

public record Result {
    public record Ok() : Result;
    public record Error(string Failure) : Result;

    private Result() {}
}

public record Result<T> {
    public record Ok(T Value) : Result<T>;
    public record Error(string Failure) : Result<T>;

    private Result() {}
}

// public class Result
// {
//     protected Result(bool success, string error)
//     {
//         if (success && error != string.Empty)
//             throw new InvalidOperationException();
//         if (!success && error == string.Empty)
//             throw new InvalidOperationException();
//         Success = success;
//         Error = error;
//     }

//     public bool Success { get; }
//     public string Error { get; }
//     public bool IsFailure => !Success;

//     public static Result Fail(string message) => new Result(false, message);
//     public static Result Ok() => new Result(true, string.Empty);

//     public static Result<T> Fail<T>(string message) => new(default, false, message);
//     public static Result<T> Ok<T>(T value) => new(value, true, string.Empty);
//     static public Result<T> OfOption<T>(FSharpOption<T> opt, string error) =>
//         FSharpOption<T>.get_IsSome(opt) ? Ok(opt.Value) : Fail<T>(error);
// }

// public class Result<T> : Result
// {
//     protected internal Result(T value, bool success, string error)
//         : base(success, error)
//     {
//         Value = value;
//     }

//     public T Value { get; set; }
// }

// record Result<T, U> {
//     public record Ok(T Value) : Result<T, U>();
//     public record Error(U Failure) : Result<T, U>();

//     private Result() {}
// }
