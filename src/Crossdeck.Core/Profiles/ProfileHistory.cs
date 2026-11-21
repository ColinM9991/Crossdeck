using Crossdeck.Core.Profiles.Models;

namespace Crossdeck.Core.Profiles;

/// <summary>
/// Provides a historical timeline of profile changes.
/// </summary>
/// <param name="capacity">The maximum capacity of the timeline.</param>
public sealed class ProfileHistory(int capacity = 100)
{
    private readonly LinkedList<Profile> _undo = new();
    private readonly Stack<Profile> _redo = new();

    /// <summary>
    /// Appends a profile change to the timeline.
    /// </summary>
    /// <param name="profile">The <see cref="Profile"/> to add to the timeline.</param>
    public void Push(Profile profile)
    {
        _undo.AddLast(profile);
        _redo.Clear();

        if (_undo.Count > capacity) _undo.RemoveFirst();
    }

    /// <summary>
    /// Undoes a profile change.
    /// </summary>
    /// <param name="current">The current <see cref="Profile"/> which will be undone.</param>
    /// <returns>The previous <see cref="Profile"/> in the timeline.</returns>
    public Profile Undo(Profile current)
    {
        var previous = _undo.Last!.Value;

        _redo.Push(current);
        _undo.RemoveLast();

        return previous;
    }

    /// <summary>
    /// Redoes a profile change.
    /// </summary>
    /// <param name="current">The current <see cref="Profile"/>..</param>
    /// <returns>The next <see cref="Profile"/> in the timeline.</returns>
    public Profile Redo(Profile current)
    {
        _undo.AddLast(current);

        return _redo.Pop();
    }
}