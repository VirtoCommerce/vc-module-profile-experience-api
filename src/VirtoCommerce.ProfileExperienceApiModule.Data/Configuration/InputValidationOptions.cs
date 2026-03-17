namespace VirtoCommerce.ProfileExperienceApiModule.Data.Configuration
{
    public class InputValidationOptions
    {
        public string NameValidationPattern { get; set; } = @"^[\p{L}\p{M}\s'\-\.]+$";

        public string OrganizationNameValidationPattern { get; set; } = @"^[\p{L}\p{M}\p{N}\s'\-\.&#/,()]+$";

        public bool EnableNoHtmlTagsValidation { get; set; } = true;

        public bool EnableScriptInjectionValidation { get; set; } = true;
    }
}
