using System;
using Microsoft.AspNetCore.Mvc;

namespace Crispy.Application.Results
{
    public abstract class Result
    {
        public bool IsSuccess { get; protected init; }
        public string Message { get; protected init; } = string.Empty;

        public static Success Ok(string message = "Операція успішна") => new(message);
        public static Success<T> Ok<T>(T data, string message = "Операція успішна") => new(data, message);
        public static Failure Fail(string message = "Помилка при виконанні операції") => new(message);

        public static implicit operator bool(Result result) => result.IsSuccess;
        public static implicit operator string(Result result) => result.Message;

        public abstract IActionResult ToActionResult();

        public TResult Match<TResult>(
            Func<Success, TResult> onSuccess,
            Func<Failure, TResult> onFailure) =>
            ((object)this) switch 
            {
                Success s => onSuccess(s),
                Failure f => onFailure(f),
                _ => throw new InvalidOperationException("Unknown result type")
            };

        public void Match(
            Action<Success> onSuccess,
            Action<Failure> onFailure)
        {
            switch ((object)this) 
            {
                case Success s:
                    onSuccess(s);
                    break;
                case Failure f:
                    onFailure(f);
                    break;
            }
        }
    }

    public sealed class Success : Result
    {
        public Success(string message = "Операція успішна")
        {
            IsSuccess = true;
            Message = message;
        }

        public override IActionResult ToActionResult() =>
            throw new InvalidOperationException("Use Success<T> for IActionResult");
    }

    public sealed class Success<T> : Result
    {
        public T Data { get; }

        public Success(T data, string message = "Операція успішна")
        {
            Data = data;
            IsSuccess = true;
            Message = message;
        }

        public static implicit operator T(Success<T> result) => result.Data;

        // Реалізація методу замість оператора
        public override IActionResult ToActionResult() =>
            new OkObjectResult(new { success = true, message = Message, data = Data });
    }

    public sealed class Failure : Result
    {
        public string ErrorCode { get; }

        public Failure(string message, string errorCode = "ERROR")
        {
            IsSuccess = false;
            Message = message;
            ErrorCode = errorCode;
        }

        // Реалізація методу замість оператора
        public override IActionResult ToActionResult() =>
            new BadRequestObjectResult(new { success = false, message = Message, errorCode = ErrorCode });
    }
}