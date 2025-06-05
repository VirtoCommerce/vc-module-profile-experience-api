using GraphQL.Types;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public abstract class InputContactBaseType : InputMemberBaseType
    {
        protected InputContactBaseType()
        {
            Field<StringGraphType>(nameof(ContactCommand.FullName));
            Field<NonNullGraphType<StringGraphType>>(nameof(ContactCommand.FirstName));
            Field<NonNullGraphType<StringGraphType>>(nameof(ContactCommand.LastName));
            Field<StringGraphType>(nameof(ContactCommand.MiddleName));
            Field<StringGraphType>(nameof(ContactCommand.Salutation));
            Field<StringGraphType>(nameof(ContactCommand.PhotoUrl));
            Field<StringGraphType>(nameof(ContactCommand.TimeZone));
            Field<StringGraphType>(nameof(ContactCommand.DefaultLanguage));
            Field<StringGraphType>(nameof(ContactCommand.CurrencyCode));
            Field<StringGraphType>(nameof(ContactCommand.About));
            Field<StringGraphType>(nameof(ContactCommand.SelectedAddressId));
            Field<ListGraphType<StringGraphType>>(nameof(ContactCommand.Organizations));
        }
    }
}
