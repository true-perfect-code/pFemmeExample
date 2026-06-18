
Im Shared-Verzeichnis befinden sich:
- _Imports.razor => hier sollte man: '@using p11.UI' und '@using p11.UI.Models' hinzufügen
- Routes.razor => ist entscheidend für die App-Funktionalität und sollte genau so übenrommen werden mit einer sehr wichtigen Anpassung:
	=> bei der Zeile '<BlazorCore.Pages.AppStartup Assembly="typeof(APPLICATIONNAME.Shared.Routes).Assembly" />'
		soll 'APPLICATIONNAME' durch App-name (z.B. pmunus, pTonso...) ersetzt werden !!!
- Die hier gezeigte csproj-Datei soll kontrolliert bzw. verlgichen werden mit der aktuellen, die im Projekt erstellt wurde