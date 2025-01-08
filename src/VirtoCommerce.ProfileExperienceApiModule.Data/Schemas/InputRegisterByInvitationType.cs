using GraphQL.Types;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputRegisterByInvitationType : InputObjectGraphType
    {
        public InputRegisterByInvitationType()
        {
            Field<NonNullGraphType<StringGraphType>>(nameof(RegisterByInvitationCommand.UserId)).Description("ID of use created for invited email");
            Field<NonNullGraphType<StringGraphType>>(nameof(RegisterByInvitationCommand.Token)).Description("Invitation token");
            Field<NonNullGraphType<StringGraphType>>(nameof(RegisterByInvitationCommand.FirstName)).Description("First name of person");
            Field<NonNullGraphType<StringGraphType>>(nameof(RegisterByInvitationCommand.LastName)).Description("Last name of person");
            Field<StringGraphType>(nameof(RegisterByInvitationCommand.Phone)).Description("Phone");
            Field<NonNullGraphType<StringGraphType>>(nameof(RegisterByInvitationCommand.Username)).Description("Username");
            Field<NonNullGraphType<StringGraphType>>(nameof(RegisterByInvitationCommand.Password)).Description("Password");
            Field<StringGraphType>(nameof(RegisterByInvitationCommand.CustomerOrderId)).Description("Customer order Id to be associated with this user.");
        }
    }
}
