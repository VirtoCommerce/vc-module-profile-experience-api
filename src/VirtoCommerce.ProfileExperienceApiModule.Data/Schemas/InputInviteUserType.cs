using GraphQL.Types;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputInviteUserType : ExtendableInputGraphType
    {
        public InputInviteUserType()
        {
            Field<NonNullGraphType<StringGraphType>>(nameof(InviteUserCommand.StoreId), "ID of store which will send invites");
            Field<StringGraphType>(nameof(InviteUserCommand.OrganizationId), "ID of organization where contact will be added for user");
            Field<StringGraphType>(nameof(InviteUserCommand.UrlSuffix), "Optional URL suffix: you may provide here relative URL to your page which handle registration by invite");
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>(nameof(InviteUserCommand.Emails), "Emails which will receive invites");
            Field<StringGraphType>(nameof(InviteUserCommand.Message), "Optional message to include into email with instructions which invites persons will see");
            Field<ListGraphType<NonNullGraphType<StringGraphType>>>(nameof(InviteUserCommand.RoleIds), "Role IDs or names to be assigned to the invited user");
            Field<StringGraphType>(nameof(InviteUserCommand.CustomerOrderId), "Customer order Id to be associated with this user.");
        }
    }
}
