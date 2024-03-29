using GraphQL.Types;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputRegisterByInvitationType : InputObjectGraphType
    {
        public InputRegisterByInvitationType()
        {
            Field<NonNullGraphType<StringGraphType>>(nameof(RegisterByInvitationCommand.UserId), "ID of use created for invited email");
            Field<NonNullGraphType<StringGraphType>>(nameof(RegisterByInvitationCommand.Token), "Invitation token");
            Field<NonNullGraphType<StringGraphType>>(nameof(RegisterByInvitationCommand.FirstName), "First name of person");
            Field<NonNullGraphType<StringGraphType>>(nameof(RegisterByInvitationCommand.LastName), "Last name of person");
            Field<StringGraphType>(nameof(RegisterByInvitationCommand.Phone), "Phone");
            Field<NonNullGraphType<StringGraphType>>(nameof(RegisterByInvitationCommand.Username), "Username");
            Field<NonNullGraphType<StringGraphType>>(nameof(RegisterByInvitationCommand.Password), "Password");
            Field<StringGraphType>(nameof(RegisterByInvitationCommand.CustomerOrderId), "Customer order Id to be associated with this user.");
        }
    }
}
