using GraphQL.Types;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputUpdateApplicationUserType : ExtendableInputObjectGraphType<ApplicationUser>
    {
        private const string ManagedByPlatform = "Ignored: managed by the platform.";
        private const string NotChangeable = "Ignored: can't be changed with updateUser.";
        private const string ChangeEmailWithPersonalData = "Ignored: use updatePersonalData to change the email of the current user.";
        private const string ChangeRoleWithOrganizationContactRole = "Ignored: use changeOrganizationContactRole to change the role of an organization member.";

        public InputUpdateApplicationUserType()
        {
            Field<IntGraphType>("accessFailedCount").Description($"Failed login attempts for the current user. {ManagedByPlatform}").DeprecationReason(ManagedByPlatform);
            Field<StringGraphType>("email").Description($"User Email. {ChangeEmailWithPersonalData}").DeprecationReason(ChangeEmailWithPersonalData);
            Field<NonNullGraphType<StringGraphType>>("id").Description("User ID");
            Field<BooleanGraphType>("lockoutEnabled").Description($"Can user be locked out. {ManagedByPlatform}").DeprecationReason(ManagedByPlatform);
            Field<DateTimeGraphType>("LockoutEnd").Description($"End date of lockout. {ManagedByPlatform}").DeprecationReason(ManagedByPlatform);
            Field<StringGraphType>("MemberId").Description($"Id of the associated Member. {NotChangeable}").DeprecationReason(NotChangeable);
            Field<StringGraphType>("PhoneNumber").Description("User phone number");
            Field<BooleanGraphType>("PhoneNumberConfirmed").Description($"Is user phone number confirmed. {ManagedByPlatform}").DeprecationReason(ManagedByPlatform);
            Field<StringGraphType>("PhotoUrl").Description("User photo URL");
            Field<ListGraphType<InputAssignRoleType>>(nameof(ApplicationUser.Roles)).Description($"List of user roles. {ChangeRoleWithOrganizationContactRole}").DeprecationReason(ChangeRoleWithOrganizationContactRole);
            Field<StringGraphType>("StoreId").Description($"Associated Store Id. {NotChangeable}").DeprecationReason(NotChangeable);
            Field<BooleanGraphType>("TwoFactorEnabled").Description($"Is Two Factor Authentication enabled. {ManagedByPlatform}").DeprecationReason(ManagedByPlatform);
            Field<StringGraphType>("UserName").Description($"User name. {NotChangeable}").DeprecationReason(NotChangeable);
            Field<StringGraphType>("UserType").Description($"User type (Manager, Customer). {NotChangeable}").DeprecationReason(NotChangeable);
            Field<StringGraphType>("passwordHash").Description($"Password Hash. {ManagedByPlatform}").DeprecationReason(ManagedByPlatform);
            Field<StringGraphType>("securityStamp").Description($"SecurityStamp. {ManagedByPlatform}").DeprecationReason(ManagedByPlatform);
        }
    }
}
