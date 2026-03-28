using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace LicenseManager.API.Helpers
{
    public static class ApiExceptionResponseFactory
    {
        private const string GenericDatabaseErrorMessage = "Unable to process the request right now. Please try again later.";
        private const string GenericServerErrorMessage = "An unexpected error occurred while processing the request.";
        private const string GenericUnauthorizedMessage = "Unauthorized request.";

        public static ObjectResult Create(ControllerBase controller, Exception exception)
        {
            return Create(controller, exception, message => message);
        }

        public static ObjectResult Create<TBody>(
            ControllerBase controller,
            Exception exception,
            Func<string, TBody> bodyFactory)
        {
            var (statusCode, message) = Map(exception);
            return new ObjectResult(bodyFactory(message))
            {
                StatusCode = statusCode
            };
        }

        private static (int StatusCode, string Message) Map(Exception exception)
        {
            if (exception is InvalidOperationException || exception is ArgumentException)
            {
                return (StatusCodes.Status400BadRequest, exception.Message);
            }

            if (exception is UnauthorizedAccessException)
            {
                return (StatusCodes.Status401Unauthorized, GenericUnauthorizedMessage);
            }

            if (IsDatabaseException(exception))
            {
                return (StatusCodes.Status500InternalServerError, GenericDatabaseErrorMessage);
            }

            return (StatusCodes.Status500InternalServerError, GenericServerErrorMessage);
        }

        private static bool IsDatabaseException(Exception? exception)
        {
            while (exception != null)
            {
                if (exception is PostgresException || exception is NpgsqlException)
                {
                    return true;
                }

                exception = exception.InnerException;
            }

            return false;
        }
    }
}
