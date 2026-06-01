using System.Reflection;
using FluentAssertions;
using Wolverine;
using Wolverine.Attributes;
using Wolverine.Persistence.Sagas;
using XBOL.Ticketing.Services;
using XBOL.Ticketing.Services.Bundle;
using XBOL.Ticketing.Services.Event;
using XBOL.Ticketing.Services.Extensions;
using XBOL.Ticketing.Services.Messages;

namespace XBOL.Ticketing.Tests.Infrastructure;

public class WolverineConfigurationTests
{
    [Fact]
    public void ConfigureTicketingLifecycle_IncludesServicesAssemblyForHandlerDiscovery()
    {
        var options = new WolverineOptions();

        TicketingWolverineConfiguration.ConfigureTicketingLifecycle(options);

        options.Assemblies.Should().Contain(typeof(CreateSeatsIoEventCommand).Assembly);
    }

    [Theory]
    [MemberData(nameof(LifecycleHandlerMethods))]
    public void TicketingLifecycleHandlers_AreTransactional(MethodInfo handleMethod)
    {
        handleMethod.GetCustomAttribute<TransactionalAttribute>()
            .Should()
            .NotBeNull();
    }

    [Fact]
    public void TicketingServicesAssembly_DoesNotDefineWolverineSagaState()
    {
        var sagaTypes = typeof(CreateSeatsIoEventCommand).Assembly
            .GetTypes()
            .Where(type => type.IsAssignableTo(typeof(Saga)))
            .ToList();

        sagaTypes.Should().BeEmpty("current lifecycle handlers are stateless command handlers");
    }

    public static IEnumerable<object[]> LifecycleHandlerMethods()
    {
        yield return [HandleMethod(typeof(CreateSeatsIoEventHandler), typeof(CreateSeatsIoEventCommand))];
        yield return [HandleMethod(typeof(UpdateSeatsIoEventHandler), typeof(UpdateSeatsIoEventCommand))];
        yield return [HandleMethod(typeof(DeleteSeatsIoEventHandler), typeof(DeleteSeatsIoEventCommand))];
        yield return [HandleMethod(typeof(CreateSeatsIoSeasonHandler), typeof(CreateSeatsIoSeasonCommand))];
        yield return [HandleMethod(typeof(AddEventsToSeasonHandler), typeof(AddEventsToSeasonCommand))];
        yield return [HandleMethod(typeof(DeleteSeatsIoSeasonHandler), typeof(DeleteSeatsIoSeasonCommand))];
        yield return [HandleMethod(typeof(UpdateSeatsIoSeasonHandler), typeof(UpdateSeatsIoSeasonCommand))];
    }

    private static MethodInfo HandleMethod(Type handlerType, Type commandType)
    {
        return handlerType
            .GetMethod("Handle", BindingFlags.Instance | BindingFlags.Public, [commandType])
            ?? throw new InvalidOperationException(
                $"{handlerType.Name} must expose a public Handle({commandType.Name}) method.");
    }
}
