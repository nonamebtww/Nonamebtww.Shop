namespace Nonamebtww.Shop.Application.Common.Results;

/// <summary>Результат use case без значения.</summary>
public class Result {
  protected Result(bool isSuccess, Error? error) {
    IsSuccess = isSuccess;
    Error = error;
  }

  public bool IsSuccess { get; }
  public bool IsFailure => !IsSuccess;
  public Error? Error { get; }

  public static Result Success() => new(true, null);
  public static Result Failure(Error error) => new(false, error);
  public static Result<T> Success<T>(T value) => Result<T>.Success(value);
  public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);

  public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>Результат use case со значением.</summary>
public sealed class Result<T> : Result {
  private readonly T? _value;

  private Result(T? value, bool isSuccess, Error? error) : base(isSuccess, error) {
    _value = value;
  }

  public T Value => IsSuccess
    ? _value!
    : throw new InvalidOperationException($"Нельзя прочитать значение неуспешного результата: {Error?.Code}");

  public static Result<T> Success(T value) => new(value, true, null);
  public new static Result<T> Failure(Error error) => new(default, false, error);

  public static implicit operator Result<T>(T value) => Success(value);
  public static implicit operator Result<T>(Error error) => Failure(error);
}
