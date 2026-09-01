using System.Linq.Expressions;

namespace Ocelot.Acceptance;

public static class IFluentStepBuilderExtensions
{
    public static IFluentStepBuilder<TScenario> AndIf<TScenario>(this IFluentStepBuilder<TScenario> scenario, bool when, Expression<Action<TScenario>> step)
        where TScenario : class
        => when ? scenario.And(step) : scenario;
    public static IFluentStepBuilder<TScenario> AndIfNot<TScenario>(this IFluentStepBuilder<TScenario> scenario, bool when, Expression<Action<TScenario>> step)
        where TScenario : class
        => !when ? scenario.And(step) : scenario;

    public static IFluentStepBuilder<TScenario> ThenIf<TScenario>(this IFluentStepBuilder<TScenario> scenario, bool when, Expression<Action<TScenario>> step)
        where TScenario : class
        => when ? scenario.Then(step) : scenario;
    public static IFluentStepBuilder<TScenario> ThenIfNot<TScenario>(this IFluentStepBuilder<TScenario> scenario, bool when, Expression<Action<TScenario>> step)
        where TScenario : class
        => !when ? scenario.Then(step) : scenario;
}
