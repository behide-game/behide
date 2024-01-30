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

    public string? ErrorValue => (this as Error)?.Failure;
}
