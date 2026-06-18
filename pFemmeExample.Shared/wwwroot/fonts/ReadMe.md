Neue Schriftart hinzufügen:
---------------------------
1. Erstellen Sie unter [APPLICATIONNAME.Shared].wwwroot.fonts.[FONTVERZEICHNIS] einen neuen Verzeichnis (am besten nutzen Sie den Fontname dazu)

2. Holen Sie sich die Schriftart (z.B. von Google-Fonts Webseite)

3. Falls heruntergeladene und entpackte Fonts im Format 'ttf' sind, dann sollen die in woff/woff2 umgewandelt werden
   Die Umwandlung erfolgt am besten über die Webseite 'https://transfonter.org/':
	- Laden SieIhre font hoch über 'Add fonts'
	- Bei der letzten Option 'Fonts directory' könenn Sie das Verzeichnis eintragen (z.B '/_content/APPLICATIONNAME.Shared/fonts/[FONT-VERZEICHNISNAME]/') 
	- Wenn Font hochgeladen ist dann auf Button 'Convert' klicken
	- Anschliessend konvertierten Font über 'Download' herunterladen
                 
4. Im heruntergeladenen Datei erhalten Sie unter anderem auch eine CSS Datei Namens 'stylesheet.css'. 
   Hierraus können Sie die CSS-Fontface Properties direkt entnehmen und in Ihre font.css kopieren
   z.B.:
   @font-face {
       font-family: 'Amiri Quran';
       src: url('_content/APPLICATIONNAME.Shared/fonts/AmiriQuran/AmiriQuran-Regular.woff2') format('woff2'),
           url('_content/APPLICATIONNAME.Shared/fonts/AmiriQuran/AmiriQuran-Regular.woff') format('woff');
       font-weight: normal;
       font-style: normal;
       font-display: swap;
   }
   Bemerkung: Vergessen Sie hier bitte nicht noch ein Schreckstrich vor der Verzeichnisangabe zu platzieren:
            FALSCH  => '_content/APPLICATIONNAME.Shared/fonts/AmiriQuran/AmiriQuran-Regular.woff2'
            RICHTIG => '/_content/APPLICATIONNAME.Shared/fonts/AmiriQuran/AmiriQuran-Regular.woff2'
   Achtung: Bei font-weight die Konstanten (z.B. 'normal') durch Zahlen ersetzen (z.B. font-weight: 800;)!

5. Anschliessend übernehmen Sie auch die konvertierte Fonts und platzieren diese in das [APPLICATIONNAME.Shared].wwwroot.fonts.[FONTVERZEICHNIS]

6. Damit die neue Font in Blazor-Cross-Host verwendet werdne kann, müssen wir sie in die static Liste in Shared.Global.StateInit.cs einfügen:
   public static List<FontFamilyModel> Fonts = new()
   {
      new FontFamilyModel { Font = "Default font", CssFontFamily = "Cinzel" },
      new FontFamilyModel { Font = "Dyslexic font", CssFontFamily = "OpenDyslexicAlta" },
      new FontFamilyModel { Font = "Atkinson font", CssFontFamily = "Atkinson Hyperlegible" },
      new FontFamilyModel { Font = "Lexend font", CssFontFamily = "Lexend" },
      new FontFamilyModel { Font = "Amiri Quran", CssFontFamily = "Amiri Quran" },
   };  

7. Am Schluss der fonts.css Datei noch Default-Font setzen mit:
   /* DEFAUL */
   :root {
       --bs-body-font-family: 'Comfortaa', serif !important;
       --bs-headings-font-family: 'Comfortaa', serif !important;
   }

8. Browser-Cash löschen und prüfen, ob Font angezeigt wird