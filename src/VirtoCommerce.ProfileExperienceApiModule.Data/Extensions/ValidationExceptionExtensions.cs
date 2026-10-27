using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using GraphQL;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Extensions
{
    public static class ValidationExceptionExtensions
    {
        public static IList<ExecutionError> ToExecutionErrors(this ValidationException exception, IResolveFieldContext context)
        {
            return exception.Errors
                .Select(failure => new ExecutionError(failure.ErrorMessage, exception) { Path = context.ResponsePath }
                    .AddLocation(context.FieldAst, context.Document)
                    .AddExtension("field", ToFieldPath(failure.PropertyName))
                    .AddExtension("errorCode", failure.ErrorCode))
                .ToList();
        }

        // "Addresses[1].FirstName" -> "addresses[1].firstName", the path of the input field in the GraphQL command
        private static string ToFieldPath(string propertyName)
        {
            return string.Join('.', propertyName.Split('.').Select(x => x.ToCamelCase()));
        }
    }
}
