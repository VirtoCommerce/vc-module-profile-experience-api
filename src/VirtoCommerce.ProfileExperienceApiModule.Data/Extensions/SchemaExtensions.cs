using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using MediatR;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;
using VirtoCommerce.ProfileExperienceApiModule.Data.Schemas;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Extensions;

public static class SchemaExtensions
{
    public static void AddMemberQuery<TAggregate, TType, TQuery>(this ISchema schema, string name, Func<IResolveFieldContext, object, Task> checkAuthAsync)
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
            Type = GraphTypeExtensionHelper.GetActualType<TType>(),
            Resolver = new FuncFieldResolver<object>(async context =>
            {
                var query = new TQuery { Id = context.GetArgument<string>("id") };
                var aggregate = await context.GetMediator().Send(query);

                await checkAuthAsync(context, aggregate);

                //store aggregate in the user context for future usage in the graph types resolvers
                context.UserContext.Add($"{name}Aggregate", aggregate);

                return aggregate;
            })
        });
    }

    [Obsolete("Use the overload without IMediator. The mediator is resolved from context.RequestServices per request.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public static void AddMemberQuery<TAggregate, TType, TQuery>(this ISchema schema, IMediator mediator, string name, Func<IResolveFieldContext, object, Task> checkAuthAsync)
        where TAggregate : MemberAggregateRootBase
        where TType : MemberBaseType<TAggregate>
        where TQuery : GetMemberByIdQueryBase<TAggregate>, new()
    {
        schema.AddMemberQuery<TAggregate, TType, TQuery>(name, checkAuthAsync);
    }
}
