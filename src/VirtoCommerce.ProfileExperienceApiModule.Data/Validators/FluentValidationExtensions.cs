using System.Text.RegularExpressions;
using FluentValidation;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Validators
{
    public static class FluentValidationExtensions
    {
        private static readonly Regex _htmlTagPattern = new(
            @"<[^>]*>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _scriptInjectionPattern = new(
            @"<[^>]*>|javascript:|vbscript:|data:text/html",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Rejects strings containing HTML/XML-like tags.
        /// Returns valid for null/empty strings (chain with NotNull/NotEmpty separately).
        /// </summary>
        public static IRuleBuilderOptions<T, string> NoHtmlTags<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .Must(value => string.IsNullOrEmpty(value) || !_htmlTagPattern.IsMatch(value))
                .WithMessage("'{PropertyName}' must not contain HTML tags.");
        }

        /// <summary>
        /// Rejects strings containing HTML tags, javascript:, vbscript:, or data:text/html protocols.
        /// Returns valid for null/empty strings.
        /// </summary>
        public static IRuleBuilderOptions<T, string> NoScriptInjection<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .Must(value => string.IsNullOrEmpty(value) || !_scriptInjectionPattern.IsMatch(value))
                .WithMessage("'{PropertyName}' contains potentially unsafe content.");
        }

        /// <summary>
        /// Validates that the string matches the given allow-list regex pattern.
        /// Returns valid for null/empty strings (chain with NotNull/NotEmpty separately).
        /// </summary>
        public static IRuleBuilderOptions<T, string> MatchesNamePattern<T>(this IRuleBuilder<T, string> ruleBuilder, string pattern)
        {
            return ruleBuilder
                .Must(value => string.IsNullOrEmpty(value) || Regex.IsMatch(value, pattern))
                .WithMessage("'{PropertyName}' contains invalid characters.");
        }
    }
}
