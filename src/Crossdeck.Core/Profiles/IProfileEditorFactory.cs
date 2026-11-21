using Crossdeck.Core.Models;
using Crossdeck.Core.Profiles.Models;

namespace Crossdeck.Core.Profiles;

public interface IProfileEditorFactory
{
    IProfileEditor Open(ProfileDocument document);

    IProfileEditor Create(string name, Simulator simulator);
}