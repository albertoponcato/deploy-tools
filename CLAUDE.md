# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

DeployTools is a small .NET 8 console CLI (built assembly name: `xtr`) used to prep local web project folders for deployment/publishing. It is a personal utility tool, not a library or service — all logic lives in a single `Program.cs`, and there are no tests. It is distributed as a .NET Global Tool (`PackageId`: `Exetera.DeployTools`, command: `xtr`) via a private NuGet feed on `gitlab.exetera.com` (the same one used for `Umbraco.Boilerplate`), configured in the committed `nuget.config`. Package naming/versioning conventions (the `Exetera.` prefix, and a `PackageVersion` in `Directory.Build.props` kept independent of the internal build `Version`) follow the `Umbraco.Boilerplate` repo.

## Commands

```
dotnet build                          # build
dotnet run -- <command>               # run a command from source, e.g. dotnet run -- clean
dotnet publish -p:PublishProfile=FolderProfile   # publish using the folder profile (bin\Release\net8.0\publish\)

dotnet pack -c Release                # produce the tool .nupkg (bin\Release\Exetera.DeployTools.<PackageVersion>.nupkg)
.\scripts\publish-tool.ps1             # pack + push to the NuGet feed (gitlab.exetera.com) in one step
```

Colleagues install/update the tool with (assumes they already have `gitlab.exetera.com` added as a NuGet source with credentials, same as for `Umbraco.Boilerplate`):

```
dotnet tool install --global Exetera.DeployTools
dotnet tool update --global Exetera.DeployTools
```

`scripts/publish-tool.ps1` reads the version to push from `Directory.Build.props`'s `PackageVersion` rather than matching `*.nupkg`, so a stale package from a previous version can never get pushed by mistake (mirrors `Umbraco.Boilerplate/scripts/publish-template.ps1`).

There is no test suite and no lint/CI configuration in this repo.

## Architecture

- Built with [Cocona](https://github.com/mayuki/Cocona) — commands are registered as simple lambdas via `app.AddCommand("name", () => Method())` in `Main`, each with a `.WithDescription(...)`. To add a new command, add another `AddCommand` call and a corresponding private static method.
- Every destructive command operates on `Environment.CurrentDirectory` (i.e. wherever the tool is invoked from) and prompts for `Y` confirmation via `Console.ReadLine()` before deleting anything — follow this same confirm-before-delete pattern for new destructive commands.
- Commands implemented in `Program.cs`:
  - `clean` (`PrePublishCleaning`): deletes everything in the current directory except a fixed allowlist. Always keeps `web.config` and any `appsettings*.json` file at the root (matched via the shared `IsAppSettingsFileName` helper, also used by `clean-config`). First calls `DetectPlatform` to classify the folder as `SitePlatform.Kentico`, `.Umbraco`, or `.Other`, by looking for platform-specific vendor DLLs at the root (`CMS.*.dll` / `Kentico.Xperience.*.dll` for Kentico, `Umbraco.Cms.*.dll` for Umbraco) with a folder-name fallback (`assets/` for Kentico, `umbraco/` for Umbraco) for older/atypical publish outputs. Behavior per platform:
    - **Kentico**: keeps `assets/` whole (it holds Kentico's media library); everything else at the root not in the allowlist is deleted, including `wwwroot/` (just compiled static output).
    - **Umbraco**: keeps `wwwroot/` but prunes its subfolders down to `media/` only; keeps the `umbraco/` folder but prunes it down to `Licenses/` only.
    - **Other** (assumed plain ASP.NET): no folder exclusion at all — `wwwroot/` is deleted like everything else, since it's just rebuilt on publish.
  - `clean-config`: recursively deletes `web.config` and any `appsettings*.json` file under the current directory (used to strip environment-specific config before packaging a template).
  - `sanitize` (`SanitizeTemplate`): rewrites `*.html` files recursively for static hosting — strips leading `/inc/` and querystring `?id=...` tokens, rewrites `href="/"` to `index.html`, and (only in files named `index.html`) turns absolute root-relative `.html` links into relative ones. Uses `HtmlAgilityPack` to parse/save HTML.
  - `days`: copies the number of days since 2000-01-01 to the Windows clipboard via `clip.exe` (Windows-only; relies on `ProcessStartInfo("clip")`).
- Versioning: assembly `Version`/`AssemblyVersion`/`FileVersion` are all driven from a single `<Version>` in `Directory.Build.props` (internal build-number scheme, bumped for any change). The NuGet package version is a separate `<PackageVersion>` (plain SemVer) in the same file — bump `patch` for fixes, `minor` for new commands/features, `major` for breaking changes, and don't reuse the internal `Version` numbers here.
