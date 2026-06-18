
Use this when the template is not yet installed.
------------------------------------------------
PS C:\Users\perfe> cd C:\Users\perfe\source\repos\pFemmeExample
PS C:\Users\perfe\source\repos\pFemmeExample> dotnet new install .
PS C:\Users\perfe\source\repos\pFemmeExample> dotnet new list
PS C:\Users\perfe\source\repos\pFemmeExample> cd ..
PS C:\Users\perfe\source\repos> dotnet new b-c-h -n MyNewApp

Use this when the pFemmeExample template has been modified (e.g. after a git pull or local changes).
------------------------------------------------------------------------------------------------------
PS C:\Users\perfe> cd C:\Users\perfe\source\repos\pFemmeExample
PS C:\Users\perfe\source\repos\pFemmeExample> dotnet new uninstall C:\Users\perfe\source\repos\pFemmeExample
PS C:\Users\perfe\source\repos\pFemmeExample> dotnet new install .
PS C:\Users\perfe\source\repos> cd ..
PS C:\Users\perfe\source\repos> dotnet new b-c-h -n MyNewApp

Important Notes:
- Always run install from the actual template folder (the one containing .template.config/template.json)
- Do NOT run dotnet new install . from a parent folder like C:\Users\perfe\source\repos
- After template changes, always:
  uninstall
  then install again
- dotnet new list shows all available templates after installation



OLD:
----
The following steps are necessary to use Blazor Cross-Host as a template for new projects:
In PowerShell
-> PS C:\Users\perfe> cd C:\Users\perfe\source\repos\pFemmeExample
-> PS C:\Users\perfe\source\repos> dotnet new install .
-> PS C:\Users\perfe\source\repos> dotnet new list
-> PS C:\Users\perfe\source\repos> dotnet new b-c-h -n MyNewApp
---
When a new version of pFemmeExample is pulled from GitHub, the pFemmeExample template should be uninstalled and reinstalled:
-> PS C:\Users\perfe> cd C:\Users\perfe\source\repos\pFemmeExample
-> PS C:\Users\perfe\source\repos> dotnet new uninstall .
-> PS C:\Users\perfe\source\repos> dotnet new install .
---
