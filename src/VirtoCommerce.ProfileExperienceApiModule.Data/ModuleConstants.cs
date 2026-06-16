namespace VirtoCommerce.ProfileExperienceApiModule.Data
{
    public static class ModuleConstants
    {
        public static class Security
        {
            public static class Permissions
            {
                public const string MyOrganizationEdit = "xapi:my_organization:edit";
                public const string MyOrganizationUserInvite = "xapi:my_organization:user:invite";
                public const string MyOrganizationOrderView = "xapi:my_organization:order:view";

                public static string[] AllPermissions { get; } = { MyOrganizationEdit, MyOrganizationUserInvite, MyOrganizationOrderView };
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
