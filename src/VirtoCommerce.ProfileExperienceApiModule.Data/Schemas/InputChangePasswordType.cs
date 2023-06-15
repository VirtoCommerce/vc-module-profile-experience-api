﻿using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputChangePasswordType : InputObjectGraphType
    {
        public InputChangePasswordType()
        {
            Field<NonNullGraphType<StringGraphType>>("userId", "User identifier");
            Field<NonNullGraphType<StringGraphType>>("oldPassword", "Old user password");
            Field<NonNullGraphType<StringGraphType>>("newPassword", "New password according with system security policy");
        }
    }
}
