using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Services;
using CommunityToolkit.Mvvm.Messaging;
using DatabaseDataGridView.WinForms;
using JustData.Application.Login;

namespace JustyBaseLegacy.UI;

/// <summary>Creates a fresh login dialog for process startup and re-login flows.</summary>
public sealed class LoginFormFactory(
    IApplicationSettingsContext applicationSettingsContext,
    IUiHelperService uiHelperService,
    ICredentialStore credentialStore,
    IApplicationSession applicationSession,
    IMessenger messenger,
    ILoginDataValidator loginDataValidator)
{
    public LoginForm Create() => new(
        applicationSettingsContext,
        uiHelperService,
        credentialStore,
        applicationSession,
        messenger,
        loginDataValidator);
}
