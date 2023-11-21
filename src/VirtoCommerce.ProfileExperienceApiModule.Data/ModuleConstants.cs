namespace VirtoCommerce.ProfileExperienceApiModule.Data
{
    public static class ModuleConstants
    {
        public static class Security
        {
            public static class Permissions
            {
                public const string MyOrganizationEdit = "xapi:my_organization:edit";

                public static string[] AllPermissions { get; } = { MyOrganizationEdit };
            }
        }

        public static class ContactStatuses
        {
            public const string Locked = "Locked";
            public const string Invited = "Invited";
            public const string Approved = "Approved";
        }

        public static class RegistrationFlows
        {
            public const string NoEmailVerification = "NoEmailVerification";
            public const string EmailVerificationOptional = "EmailVerificationOptional";
            public const string EmailVerificationRequired = "EmailVerificationRequired";
        }
    }
}
