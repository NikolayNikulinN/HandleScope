# HandleScope Installer

HandleScope uses a WiX Toolset SDK-style project to build an MSI installer.
The project currently targets `WixToolset.Sdk/6.0.2`.

## Build

```powershell
dotnet build .\HandleScope\Installer\HandleScope.Installer.wixproj -c Release
```

The MSI is written to:

```text
HandleScope\Installer\bin\x64\Release\HandleScopeSetup.msi
```

## Runtime prerequisite

The installer is framework-dependent. Target machines must have the .NET 9 Desktop Runtime installed.

## Installed layout

The MSI installs application files to:

```text
Program Files\HandleScope
```

It also creates a Start Menu shortcut named `HandleScope`.
