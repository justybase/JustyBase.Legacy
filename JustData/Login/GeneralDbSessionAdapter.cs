using AppBase.Common;
using AppBase.Data.Core.Interfaces;
using JustData.Application.Login;

namespace JustyBaseLegacy.UI.Login;

/// <summary>Phase 2 adapter; remove in Phase 4 when legacy database services no longer consume LoginDataDic.</summary>
public sealed class GeneralDbSessionAdapter(IGeneralDbService generalDbService)
{
    public void Apply(IApplicationSession session)
    {
        generalDbService.LoginDataDic.Clear();
        foreach (var profile in session.Profiles)
        {
            generalDbService.LoginDataDic[profile.Name] = LegacyConnectionProfileRepository.Map(profile);
        }
    }
}
