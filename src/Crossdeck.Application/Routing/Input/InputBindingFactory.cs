using System.Collections.Frozen;
using Crossdeck.Application.Expressions;
using Crossdeck.Core.Mapping.Expressions;
using Crossdeck.Core.Mapping.Input;

namespace Crossdeck.Application.Routing.Input;

internal sealed class InputBindingFactory(IExpressionCompiler compiler)
{
    public CompiledInputBinding Create(InputMapping mapping)
    {
        var guardScope = ExpressionScopes.InputGuard(mapping.Sources.Keys);
        var valueScope = ExpressionScopes.InputValue(mapping.Sources.Keys);

        var events = mapping.Actions.ToFrozenDictionary(
            e => e.Key,
            e => CreateEvent(e.Value, guardScope, valueScope));

        return new CompiledInputBinding(
            mapping.Sources.ToFrozenDictionary(),
            events);
    }

    private EventBinding CreateEvent(InputEventBinding binding, ExpressionScope guardScope, ExpressionScope valueScope) =>
        new(
            [.. binding.Actions.Select(a => CreateAction(a, valueScope))],
            binding.When is null ? null : compiler.Compile(binding.When, guardScope));

    private BoundAction CreateAction(InputAction action, ExpressionScope valueScope) => action switch
    {
        InputAction.Command c => new BoundAction.Command(c.Ref, c.Phase),
        InputAction.Set { Value: SetValue.Literal l } s => new BoundAction.SetLiteral(s.Ref, l.Value),
        InputAction.Set { Value: SetValue.Ref r } s => new BoundAction.SetRef(s.Ref, r.Source),
        InputAction.Set { Value: SetValue.Expr e } s =>
            new BoundAction.SetExpr(s.Ref, compiler.Compile(e.Spec, valueScope)),
    };
}
