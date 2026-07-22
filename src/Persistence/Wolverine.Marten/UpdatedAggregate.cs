using JasperFx.CodeGeneration.Frames;
using JasperFx.CodeGeneration.Model;
using JasperFx.Core.Reflection;
using Marten.Events;
using Wolverine.Configuration;

namespace Wolverine.Marten;

/// <summary>
/// Use this as a response from a message handler
/// or HTTP endpoint using the aggregate handler workflow
/// to response with the updated version of the aggregate being
/// altered *after* any new events have been applied
/// </summary>
public class UpdatedAggregate : IResponseAware {
    public static void ConfigureResponse(IChain chain)
    {
        if (AggregateHandling.TryLoad(
                chain,
                out var handling))
        {
            var idType = handling.AggregateId.VariableType;

            // Handle case where the aggregate is discovered via a natural key
            if (handling.IsNaturalKey)
            {
                var frame = typeof(FetchLatestByNaturalKey<,>).CloseAndBuildAs<MethodCall>(
                    handling.AggregateId,
                    handling.AggregateType,
                    idType);
                chain.UseForResponse(frame);
            }
            else
            {
                var simpleOpenType = idType == typeof(Guid) ? typeof(FetchLatestByGuid<>) :
                    idType == typeof(string) ? typeof(FetchLatestByString<>) : null;
                // If the idType is not a natural key, and it is also not a Guid or a String
                if (simpleOpenType is null)
                {
                    throw new ArgumentOutOfRangeException("Expected aggregate id type to be Guid or string. Did you forget to add a [NaturalKey] attribute? see: https://wolverinefx.net/guide/durability/marten/event-sourcing.html#natural-keys");
                }
                var frame = simpleOpenType.CloseAndBuildAs<MethodCall>(
                    handling.AggregateId,
                    handling.AggregateType);

                chain.UseForResponse(frame);
            }
        }
        else
        {
            throw new InvalidOperationException(
                $"UpdatedAggregate cannot be used because Chain {chain} is not marked as an aggregate handler. Are you missing an [AggregateHandler] or [Aggregate] attribute on the handler?");
        }

    }
}

/// <summary>
/// Use this as a response from a message handler
/// or HTTP endpoint using the aggregate handler workflow
/// to response with the updated version of the aggregate being
/// altered *after* any new events have been applied
/// </summary>
/// <typeparam name="T">The aggregate type. Use this version of UpdatedAggregate if you need to help Wolverine "know" which of multiple event streams should be the "updated aggregate"</typeparam>
public class UpdatedAggregate<T> : IResponseAware {
    public static void ConfigureResponse(IChain chain)
    {
        if (AggregateHandling.TryLoad<T>(
                chain,
                out var handling))
        {
            var idType = handling.AggregateId.VariableType;

            // Handle case where the aggregate is discovered via a natural key
            if (handling.IsNaturalKey)
            {
                var frame = typeof(FetchLatestByNaturalKey<,>).CloseAndBuildAs<MethodCall>(
                    handling.AggregateId,
                    handling.AggregateType,
                    idType);
                chain.UseForResponse(frame);
            }
            else
            {
                var simpleOpenType = idType == typeof(Guid) ? typeof(FetchLatestByGuid<>) :
                    idType == typeof(string) ? typeof(FetchLatestByString<>) : null;
                // If the idType is not a natural key, and it is also not a Guid or a String
                if (simpleOpenType is null)
                {
                    throw new ArgumentOutOfRangeException("Expected aggregate id type to be Guid or string. Did you forget to add a [NaturalKey] attribute? see: https://wolverinefx.net/guide/durability/marten/event-sourcing.html#natural-keys");
                }
                var frame = simpleOpenType.CloseAndBuildAs<MethodCall>(
                    handling.AggregateId,
                    handling.AggregateType);

                chain.UseForResponse(frame);
            }
        }
        else
        {
            throw new InvalidOperationException(
                $"UpdatedAggregate cannot be used because Chain {chain} is not marked as an aggregate handler. Are you missing an [AggregateHandler] or [Aggregate] attribute on the handler?");
        }

    }
}

internal class FetchLatestByGuid<T> : MethodCall
    where T : class {
    public FetchLatestByGuid(Variable id) : base(
        typeof(IEventStoreOperations),
        ReflectionHelper.GetMethod<IEventStoreOperations>(x => x.FetchLatest<T>(
            Guid.Empty,
            CancellationToken.None))!)
    {
        Arguments[0] = id;
    }
}

internal class FetchLatestByString<T> : MethodCall
    where T : class {
    public FetchLatestByString(Variable id) : base(
        typeof(IEventStoreOperations),
        ReflectionHelper.GetMethod<IEventStoreOperations>(x => x.FetchLatest<T>(
            "",
            CancellationToken.None))!)
    {
        Arguments[0] = id;
    }
}

internal class FetchLatestByNaturalKey<T, TId> : MethodCall
    where T : class
    where TId : notnull {
    public FetchLatestByNaturalKey(Variable id) : base(
        typeof(IEventStoreOperations),
        ReflectionHelper.GetMethod<IEventStoreOperations>(x => x.FetchLatest<T, TId>(
            // Note that libraries like Vogen won't allow using Activator on its generated value types. 
            // However, that is enforced via an analyzer; since this code is only used for reflection purposes
            // and is never actually executed, there should be no issue.
            Activator.CreateInstance<TId>(),
            CancellationToken.None))!)
    {
        Arguments[0] = id;
    }
}