namespace Crossdeck.Core.Simulators.Models;

public closed record SimRef
{
    public string Key => this switch
    {
        Plain p => p.Name,
        Variable v => $"var:{v.Name}",
        Output o => $"out:{o.Id}",
    };

    public static SimRef Parse(string text) => text switch
    {
        _ when text.StartsWith("var:") => new Variable(text[4..]),
        _ when text.StartsWith("out:") => new Output(text[4..]),
        _ => new Plain(text)
    };

    public sealed record Plain(string Name) : SimRef;

    public sealed record Variable(string Name) : SimRef;

    public sealed record Output(string Id) : SimRef;
}