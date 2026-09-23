# Exetera.DeployTools

A .NET Global Tool (command: `xtr`) with a collection of CLI utilities to prep local web project folders for deployment/publishing.

## Install / update

```
dotnet tool install --global Exetera.DeployTools
dotnet tool update --global Exetera.DeployTools
```

## Commands

| Command        | Description                                                                                     |
|----------------|---------------------------------------------------------------------------------------------------|
| `xtr clean`        | Deletes all files/folders in the current directory except `web.config`, `appsettings*.json`, and the platform's content folder (`assets` for Kentico, `wwwroot`/`media` + `umbraco`/`Licenses` for Umbraco). |
| `xtr clean-config` | Recursively deletes `web.config` and any `appsettings*.json` file under the current directory.  |
| `xtr sanitize`     | Rewrites `*.html` files recursively for static hosting (strips `/inc/` prefixes, `?id=...` query strings, rewrites root-relative links). |
| `xtr days`         | Copies the number of days since 2000-01-01 to the clipboard.                                    |

Each destructive command asks for `Y` confirmation before deleting anything.
