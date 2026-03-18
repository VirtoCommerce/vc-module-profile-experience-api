using FluentValidation;
using VirtoCommerce.ProfileExperienceApiModule.Data.Validators;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Validators
{
    public class FluentValidationExtensionsTests
    {
        private readonly InlineValidator<TestModel> _validator = new();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("John Doe")]
        [InlineData("O'Brien")]
        [InlineData("Jean-Pierre")]
        [InlineData("Müller")]
        [InlineData("3 < 5")] // isolated < without closing > is NOT a tag
        public void NoHtmlTags_ValidInput_Passes(string value)
        {
            _validator.RuleFor(x => x.Value).NoHtmlTags();
            var result = _validator.Validate(new TestModel { Value = value });
            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData("<script>alert(1)</script>")]
        [InlineData("Paul<script src=//ooo.mn/></script>")]
        [InlineData("<img onerror=alert(1)>")]
        [InlineData("test</div>")]
        [InlineData("<a href='evil'>click</a>")]
        [InlineData("Paul Bok<script src=//ooo.mn/></script>")]
        public void NoHtmlTags_HtmlInput_Fails(string value)
        {
            _validator.RuleFor(x => x.Value).NoHtmlTags();
            var result = _validator.Validate(new TestModel { Value = value });
            Assert.False(result.IsValid);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("A simple description")]
        [InlineData("Product #123 - great value")]
        public void NoScriptInjection_ValidInput_Passes(string value)
        {
            _validator.RuleFor(x => x.Value).NoScriptInjection();
            var result = _validator.Validate(new TestModel { Value = value });
            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData("<script>alert(1)</script>")]
        [InlineData("javascript:alert(1)")]
        [InlineData("JAVASCRIPT:alert(1)")]
        [InlineData("vbscript:msgbox")]
        [InlineData("data:text/html,<script>alert(1)</script>")]
        [InlineData("<img src=x onerror=alert(1)>")]
        public void NoScriptInjection_MaliciousInput_Fails(string value)
        {
            _validator.RuleFor(x => x.Value).NoScriptInjection();
            var result = _validator.Validate(new TestModel { Value = value });
            Assert.False(result.IsValid);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("John")]
        [InlineData("O'Brien")]
        [InlineData("Jean-Pierre")]
        [InlineData("Müller")]
        [InlineData("María José")]
        public void MatchesNamePattern_ValidNames_Passes(string value)
        {
            _validator.RuleFor(x => x.Value).MatchesNamePattern(@"^[\p{L}\p{M}\s'\-\.]+$");
            var result = _validator.Validate(new TestModel { Value = value });
            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData("Paul123")]
        [InlineData("user@domain")]
        [InlineData("Paul<script>")]
        [InlineData("name;DROP TABLE")]
        public void MatchesNamePattern_InvalidNames_Fails(string value)
        {
            _validator.RuleFor(x => x.Value).MatchesNamePattern(@"^[\p{L}\p{M}\s'\-\.]+$");
            var result = _validator.Validate(new TestModel { Value = value });
            Assert.False(result.IsValid);
        }

        private class TestModel
        {
            public string Value { get; set; }
        }
    }
}
