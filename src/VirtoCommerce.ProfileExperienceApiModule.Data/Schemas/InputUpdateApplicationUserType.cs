using GraphQL.Types;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputUpdateApplicationUserType : ExtendableInputObjectGraphType<ApplicationUser>
    {
        public InputUpdateApplicationUserType()
        {
            Field<IntGraphType>("accessFailedCount").Description("Failed login attempts for the current user");
            Field<NonNullGraphType<StringGraphType>>("email").Description("User Email");
            Field<NonNullGraphType<StringGraphType>>("id").Description("User ID");
            Field<BooleanGraphType>("lockoutEnabled").Description("Can user be locked out");
            Field<DateTimeGraphType>("LockoutEnd").Description("End date of lockout");
            Field<StringGraphType>("MemberId").Description("Id of the associated Member");
            Field<StringGraphType>("PhoneNumber").Description("User phone number");
            Field<BooleanGraphType>("PhoneNumberConfirmed").Description("Is user phone number confirmed");
            Field<StringGraphType>("PhotoUrl").Description("User photo URL");
            Field<ListGraphType<InputAssignRoleType>>(nameof(ApplicationUser.Roles)).Description("List of user roles");
            Field<StringGraphType>("StoreId").Description("Associated Store Id");
            Field<BooleanGraphType>("TwoFactorEnabled").Description("Is Two Factor Authentication enabled");
            Field<NonNullGraphType<StringGraphType>>("UserName").Description("User name");
            Field<NonNullGraphType<StringGraphType>>("UserType").Description("User type (Manager, Customer)"); // Manager, Customer
            Field<StringGraphType>("passwordHash").Description("Password Hash");
            Field<NonNullGraphType<StringGraphType>>("securityStamp").Description("SecurityStamp");
        }
    }
}
