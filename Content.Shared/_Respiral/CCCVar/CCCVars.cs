using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._Respiral.CCCVar;

[CVarDefs]
public sealed partial class CCCVars : CVars
{
    public static readonly CVarDef<bool> MedievalPrefixFilterEnabled =
        CVarDef.Create("respiral.ui.medieval_prefix_filter", true, CVar.CLIENTONLY | CVar.ARCHIVE);
}
