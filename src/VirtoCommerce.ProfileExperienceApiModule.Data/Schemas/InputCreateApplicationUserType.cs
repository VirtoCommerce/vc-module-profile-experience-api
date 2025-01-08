using GraphQL.Types;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputCreateApplicationUserType : InputObjectGraphType
    {
        public InputCreateApplicationUserType()
        {
            Field<StringGraphType>("createdBy").Description("Username of the creator");
            Field<DateTimeGraphType>("createdDate").Description("Date of user creation");
            Field<NonNullGraphType<StringGraphType>>("email").Description("User Email");
            Field<StringGraphType>("id").Description("User ID");
            Field<BooleanGraphType>("lockoutEnabled").Description("Can user be locked out");
            Field<DateTimeGraphType>("LockoutEnd").Description("End date of lockout");
            Field<ListGraphType<InputApplicationUserLoginType>>(nameof(ApplicationUser.Logins)).Description("External logins");
            Field<StringGraphType>("MemberId").Description("Id of the associated Member");
            Field<StringGraphType>("Password").Description("User password (nullable, for external logins)"); // nullable, for external logins
            Field<StringGraphType>("PhoneNumber").Description("User phone number");
            Field<BooleanGraphType>("PhoneNumberConfirmed").Description("Is user phone number confirmed");
            Field<StringGraphType>("PhotoUrl").Description("User photo URL");
            Field<ListGraphType<InputAssignRoleType>>(nameof(ApplicationUser.Roles)).Description("List of user roles");
            Field<StringGraphType>("StoreId").Description("Associated Store Id");
            Field<BooleanGraphType>("TwoFactorEnabled").Description("Is Two Factor Authentication enabled");
            Field<NonNullGraphType<StringGraphType>>("UserName").Description("User name");
            Field<NonNullGraphType<StringGraphType>>("UserType").Description("User type (Manager, Customer)"); // Manager, Customer
            Field<BooleanGraphType>("PasswordExpired").Description("Password expiration date");
        }
    }
}
