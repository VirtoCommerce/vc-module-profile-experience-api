using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using GraphQL;
using GraphQL.Execution;
using GraphQLParser;
using GraphQLParser.AST;
using VirtoCommerce.ProfileExperienceApiModule.Data.Extensions;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Extensions
{
    public class ValidationExceptionExtensionsTests
    {
        private const string Query = "mutation {\n  updateContact(command: { id: \"contact-1\" }) { id }\n}";

        // Serializes errors the way a client receives them: x-api's provider outside Development
        // (ExposeExtensions on, no exception details, no Exception.Data)
        private readonly ErrorInfoProvider _errorInfoProvider = new(options => options.ExposeExtensions = true);

        [Fact]
        public void ToExecutionErrors_TwoFailures_TwoErrorsWithMessageFieldAndRule()
        {
            // Every failure becomes an error of its own, so a client can put each message on its own input.
            // The code stays VALIDATION as before; the value the user typed is not sent back.
            var exception = new ValidationException(
            [
                new ValidationFailure("LastName", "'Last Name' contains invalid characters.", "Tester2") { ErrorCode = "InvalidCharacters" },
                new ValidationFailure("Phones[0]", "The length of 'Phones' must be 64 characters or fewer.") { ErrorCode = "MaximumLengthValidator" },
            ]);

            var errors = exception.ToExecutionErrors(CreateContext());

            var infos = errors.Select(_errorInfoProvider.GetInfo).ToList();
            infos.Select(x => x.Message).Should().Equal(
                "'Last Name' contains invalid characters.",
                "The length of 'Phones' must be 64 characters or fewer.");
            infos[0].Extensions.Should().BeEquivalentTo(new Dictionary<string, object>
            {
                ["code"] = "VALIDATION",
                ["codes"] = new[] { "VALIDATION" },
                ["field"] = "lastName",
                ["errorCode"] = "InvalidCharacters",
            });
            infos[1].Extensions["field"].Should().Be("phones[0]");
            infos[1].Extensions["errorCode"].Should().Be("MaximumLengthValidator");
        }

        [Theory]
        [InlineData("LastName", "lastName")]
        [InlineData("Phones[0]", "phones[0]")]
        [InlineData("Addresses[1].FirstName", "addresses[1].firstName")]
        [InlineData("PersonalData.FirstName", "personalData.firstName")]
        public void ToExecutionErrors_PropertyName_BecomesTheInputFieldPath(string propertyName, string field)
        {
            // The field is the path of the input field in the GraphQL command (camelCase segments),
            // not the C# property path FluentValidation reports
            var exception = new ValidationException([new ValidationFailure(propertyName, "Invalid.")]);

            var error = exception.ToExecutionErrors(CreateContext()).Single();

            error.Extensions["field"].Should().Be(field);
        }

        [Fact]
        public void ToExecutionErrors_Failure_PointsAtTheMutationField()
        {
            // Errors added to the context directly don't get the path and location GraphQL.NET sets on a thrown error,
            // so the conversion sets them: the client sees the same path and location as before
            var exception = new ValidationException([new ValidationFailure("LastName", "Invalid.")]);

            var error = exception.ToExecutionErrors(CreateContext()).Single();

            error.Path.Should().Equal("updateContact");
            var location = error.Locations.Should().ContainSingle().Subject;
            location.Line.Should().Be(2);
            location.Column.Should().Be(3);
        }

        private static ResolveFieldContext CreateContext()
        {
            var document = Parser.Parse(Query);
            var operation = (GraphQLOperationDefinition)document.Definitions[0];

            return new ResolveFieldContext
            {
                Document = document,
                FieldAst = (GraphQLField)operation.SelectionSet.Selections[0],
                ResponsePath = ["updateContact"],
            };
        }
    }
}
