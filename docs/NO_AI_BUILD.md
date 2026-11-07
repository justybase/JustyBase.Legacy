# No-AI build

JustyBase.Legacy ships its AI features as optional components. Set
`EnableAi=false` to build the WinForms application without the AI UI and
without restoring the heavy AI NuGet dependencies.

## What is excluded

The no-AI build removes the AI project references and source files:

- `JustyBase.Ai` and `JustyBase.Ai.Embedded`
- `GitHub.Copilot.SDK`, `Microsoft.Extensions.AI`, `OpenAI`, and embedded
  llama.cpp model management
- `Microsoft.Web.WebView2` and `Markdig` (used only by the AI chat panel)
- AI Chat, embedded FIM/chat, Codex/Copilot settings, and AI Chat tool windows
- AI-only files such as `JustData/Ai`, `JustData/Fim`,
  `BaseWindow.AiChat.cs`, and the AI preference panels

The dependency-free `JustyBase.Ai.Contracts` project is still referenced. It
contains only chat models, host ports, and the git commit-message contract; it
has no NuGet dependencies. Test files that exercise AI-only types are also
excluded from the test projects when `EnableAi=false`.

## Build

```powershell
# Whole solution (JustData.NoAi.slnx excludes the AI projects)
dotnet build JustData.NoAi.slnx -c Release -p:EnableAi=false

# Application only
dotnet build .\JustData\JustData.csproj -c Release -p:EnableAi=false

# Publish (example, Windows x64)
dotnet publish .\JustData\JustData.csproj -r win-x64 -c Release `
  -p:EnableAi=false --self-contained true
```

`EnableAi` defaults to `true`; omit the property for the standard AI-enabled
build. The `AI` preprocessor constant is defined only when `EnableAi=true`.

## Verification

After a clean no-AI build, confirm the output does not contain AI assemblies:

```powershell
Get-ChildItem .\JustData\bin\Release\net10.0-windows |
  Where-Object Name -Match 'JustyBase\.Ai(\.dll|\.Embedded)|Copilot|Microsoft\.Extensions\.AI|OpenAI|Markdig|WebView2'
```

Only `JustyBase.Ai.Contracts.dll` may appear. To inspect the restore graph:

```powershell
dotnet list .\JustData\JustData.csproj package --include-transitive |
  Select-String 'Copilot|Microsoft.Extensions.AI|OpenAI|JustyBase.Ai|Markdig|WebView2'
```

Run the check after a no-AI restore; the same command after a standard build
will show the AI packages.
