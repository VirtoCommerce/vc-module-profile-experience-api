namespace VirtoCommerce.ProfileExperienceApiModule.Data.Configuration
{
    public class FrontendSecurityOptions
    {
        public string OrganizationMaintainerRole { get; set; }

        public InputValidationOptions InputValidation { get; set; } = new();
    }
}
