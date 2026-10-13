# HandleScope

Windows File Lock Detection Utility

![HandleScope main window](HandleScope.Presentation/Images/main-window.png)

HandleScope is a Windows desktop utility that identifies processes locking files and folders, helping users quickly discover why resources cannot be deleted, moved, renamed, or modified.

Built with WPF on .NET 9 using the MVVM pattern and powered by the Windows Restart Manager API.

## Features

* Find processes locking a selected file.
* Search for locks inside a folder, including child paths.
* View process details, including:

  * Process name
  * Process ID (PID)
  * User
  * Executable path
  * Locked path
* Refresh results without selecting the target again.
* Cancel long-running searches.
* End a selected process with confirmation.
* Log errors and diagnostic information.

## Technology Stack

* .NET 9
* WPF
* MVVM
* CommunityToolkit.Mvvm
* Windows Restart Manager API
* Serilog
* WiX Toolset

## How It Works

HandleScope uses the Windows Restart Manager API (`rstrtmgr.dll`) to identify processes associated with files currently in use.

For folder searches, the application registers files located inside the selected directory and queries Restart Manager for associated processes.

To keep searches predictable and responsive, folder scanning is limited to the first 512 files.

Because Restart Manager works with file resources, pure directory handles are not detected unless files inside the directory are involved.

## Requirements

### Development

* Windows 10 / Windows 11
* .NET 9 SDK
* Visual Studio 2022

### Runtime

* Windows operating system
* .NET 9 Desktop Runtime (for framework-dependent deployments)

## Build and Run

Build the solution:

```powershell
dotnet build .\HandleScope.sln
```

Run the application:

```powershell
dotnet run --project .\HandleScope\HandleScope.csproj
```

## MSI Installer

The installer project is located in:

```text
HandleScope.Installer
```

Build the MSI installer:

```powershell
dotnet build .\HandleScope.sln -c Debug -p:Platform=x86
dotnet build .\HandleScope.sln -c Debug -p:Platform=x64
dotnet build .\HandleScope.sln -c Release -p:Platform=x86
dotnet build .\HandleScope.sln -c Release -p:Platform=x64
```

The generated MSI package is created in:

```text
HandleScope.Installer\bin\Debug\x86\HandleScopeSetup.msi
HandleScope.Installer\bin\Debug\x64\HandleScopeSetup.msi
HandleScope.Installer\bin\Release\x86\HandleScopeSetup.msi
HandleScope.Installer\bin\Release\x64\HandleScopeSetup.msi
```

The installer:

* Installs the application into `Program Files\HandleScope`
* Creates Start Menu shortcuts
* Creates a Desktop shortcut
* Registers the application in Windows Programs and Features

## Project Structure

```text
HandleScope/                 WPF application and user interface
HandleScope.Services/        Service contracts, models, and Windows process services
HandleScope.Presentation/    Shared WPF resources and controls
HandleScope.Logger/          Logging configuration and constants
HandleScope.Installer/       WiX MSI installer project
```

## Logging

When run from the repository, application logs are stored in:

```text
<solution root>\Logs
```

When installed, the app uses `Logs` next to `HandleScope.exe`. If that directory is not writable, it uses `%LOCALAPPDATA%\HandleScope\Logs`.

## Note

Terminating processes may result in unsaved work being lost.

Always review process information carefully before ending a process.

## GitHub Description

Find and manage processes locking files and folders. Built with WPF, .NET 9, and Windows Restart Manager.
