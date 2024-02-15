using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using MediatR;
using VirtoCommerce.ExperienceApiModule.Core.Helpers;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;
using VirtoCommerce.ProfileExperienceApiModule.Data.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Extensions;

public static class SchemaExtensions
{
    public static void AddMemberQuery<TAggregate, TType, TQuery>(this ISchema schema, IMediator mediator, string name, Func<IResolveFieldContext, object, Task> checkAuthAsync)
        where TAggregate : MemberAggregateRootBase
        where TType : MemberBaseType<TAggregate>
        where TQuery : GetMemberByIdQueryBase<TAggregate>, new()
    {
        schema.Query.AddField(new FieldType
        {
            Name = name,
            Arguments = new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" },
                new QueryArgument<StringGraphType> { Name = "userId" }
            ),
            Type = GraphTypeExtenstionHelper.GetActualType<TType>(),
            Resolver = new AsyncFieldResolver<object>(async context =>
            {
                var query = new TQuery { Id = context.GetArgument<string>("id") };
                var aggregate = await mediator.Send(query);

                await checkAuthAsync(context, aggregate);

                //store aggregate in the user context for future usage in the graph types resolvers
                context.UserContext.Add($"{name}Aggregate", aggregate);

                return aggregate;
            })
        });
    }
}
