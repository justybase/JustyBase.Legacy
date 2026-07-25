# winget - JustyBase.JustyBaseLegacy

## Manual submission process for winget-pkgs

### Prerequisites
- GitHub account
- Fork of [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs)

### Steps

#### 1. After creating a GitHub Release (tag `vX.Y.Z.W`)

Download installer files from release assets:
- `JustyBaseLegacy-X.Y.Z.W.exe` (Inno Setup installer)
- `JustyBaseLegacy-X.Y.Z.W.zip` (ZIP package)

#### 2. Calculate SHA256 hashes

```powershell
Get-FileHash -Algorithm SHA256 .\JustyBaseLegacy-X.Y.Z.W.exe
Get-FileHash -Algorithm SHA256 .\JustyBaseLegacy-X.Y.Z.W.zip
```

#### 3. Prepare the manifest

Fill in `JustyBase.JustyBaseLegacy.yaml.template`:

| Placeholder | Value |
|-------------|-------|
| `{VERSION}` | `X.Y.Z.W` (e.g. `1.0.0.0`) |
| `{SHA256_EXE}` | SHA256 hash of EXE file (uppercase) |
| `{SHA256_ZIP}` | SHA256 hash of ZIP file (uppercase) |

Save as `JustyBase.JustyBaseLegacy.yaml`.

#### 4. Create the directory structure in your winget-pkgs fork

```
manifests/j/JustyBase/JustyBaseLegacy/X.Y.Z.W/
  └── JustyBase.JustyBaseLegacy.yaml
```

#### 5. Validate the manifest

```powershell
# From winget-pkgs root
.\Tools\Validate-Schema.ps1 .\manifests\j\JustyBase\JustyBaseLegacy\X.Y.Z.W\JustyBase.JustyBaseLegacy.yaml
.\Tools\Validate-Manifest.ps1 .\manifests\j\JustyBase\JustyBaseLegacy\X.Y.Z.W\JustyBase.JustyBaseLegacy.yaml
```

#### 6. Submit a Pull Request

- Title: `New version: JustyBase.JustyBaseLegacy version X.Y.Z.W`
- Description: Include release notes summary
- After the PR is approved, winget will update automatically (usually within 1-3 days)

### Install via winget

```powershell
winget install JustyBase.JustyBaseLegacy
# or
winget install --id JustyBase.JustyBaseLegacy -e
```
