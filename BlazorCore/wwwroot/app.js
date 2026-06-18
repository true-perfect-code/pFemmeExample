
window.pE_Common = {
    debugEnabled: false,

    // Neue Setter-Funktion für C#
    setDebugMode: function (isEnabled) {
        this.debugEnabled = isEnabled;
        console.log(`[pE_Common] Debug Mode ${isEnabled ? 'ENABLED' : 'DISABLED'} by C#`);
    },

    /**
     * Zentrales Enum für Datenbank-Status und Fehlercodes.
     * Muss synchron mit dem C# DB_STATUS Enum gehalten werden.
     */
    DB_STATUS: Object.freeze({
        ERROR: 0,
        INITIALIZING: 1,
        READY: 2,
        NEW: 3,
        NOT_CONNECTED: 4
    }),

    // Zentraler Logger
    log: function (module, msg, type = 'log', data = null) {
        if (!this.debugEnabled && type !== 'error' && type !== 'warn') return;

        const prefix = `[${module}] ${msg}`;

        // SICHERHEITS-CHECK: Existiert die Methode auf console? (z.B. console.info, console.error)
        // Falls 'type' z.B. 'info' ist und der Browser das nicht kennt, Fallback auf 'log'
        const method = (typeof console[type] === 'function') ? type : 'log';

        if (data) {
            console[method](prefix, data);
        } else {
            console[method](prefix);
        }
    },

    /**
     * ZENTRALE KONVERTIERUNG (ScalarModel)
     * Verarbeitet Nutzdaten UND Status-Codes für die Blazor-Gegenstelle.
     * * @param {any} val - Der Rückgabewert der DB (bei Erfolg).
     * @param {boolean} success - Technischer Erfolg des Aufrufs.
     * @param {string|number} err - Fehlertext ODER Code aus DB_CODE.
     * @param {Uint8Array} bytes - Optionale Binärdaten.
     */
    toScalar: function (val, success, err, bytes = null) {

        let errorMessage = "";

        // 1. Smarte Fehler/Status-Auswertung
        if (err !== undefined && err !== null && err !== "") {
            if (typeof err === 'number') {
                // Falls err eine Zahl ist, suchen wir den Namen im DB_CODE Enum
                // Wir mappen z.B. 4 -> "NOT_CONNECTED"
                const codeName = Object.keys(this.DB_CODE).find(key => this.DB_CODE[key] === err);
                errorMessage = codeName ? codeName : "UNKNOWN_CODE_" + err;
            } else {
                // Es ist bereits ein String-Fehler (z.B. "Table not found")
                errorMessage = String(err);
            }
        }

        // 2. Fehlerfall-Struktur: 
        // Wir lösen aus, wenn success explizit false ist ODER eine errorMessage vorliegt.
        if (success === false || errorMessage !== "") {
            return {
                out_err: errorMessage !== "" ? errorMessage : "Unknown Error",
                out_value_str: "",
                out_value_bool: false,
                out_value_int: 0,
                out_value_long: 0,
                out_value_dbl: 0,
                out_bytes: null,
                in_sql: "",
                out_mssql: "",
                out_sqlite: ""
            };
        }

        // 3. Erfolgsfall: Werte-Normalisierung
        const valStr = (val !== undefined && val !== null) ? String(val) : "";

        // PERFORMANCE-BOOST: Nur kurze Strings auf Zahlen prüfen
        const isNum = valStr.length > 0 && valStr.length <= 15 && !isNaN(valStr);
        const numVal = isNum ? Number(val) : 0;

        // Boolean-Check: Erkennt 1, true, "1", "true"
        const boolVal = (val === 1 || val === true || valStr === "1" || valStr.toLowerCase() === "true");

        // 4. Vollständiges Objekt-Mapping für das C# ScalarModel
        return {
            out_value_str: valStr,
            out_value_int: isNum ? Math.floor(numVal) : 0,
            out_value_long: isNum ? Math.floor(numVal) : 0,
            out_value_dbl: numVal,
            out_value_bool: boolVal,
            out_bytes: bytes,
            out_err: "",
            in_sql: "",
            out_mssql: "",
            out_sqlite: ""
        };
    },

    //base64ToBytes: function (base64) {
    //    const binaryString = window.atob(base64);
    //    const len = binaryString.length;
    //    const bytes = new Uint8Array(len);
    //    for (let i = 0; i < len; i++) {
    //        bytes[i] = binaryString.charCodeAt(i);
    //    }
    //    return bytes;
    //},
    base64ToBytes: function (base64) {
        if (!base64 || typeof base64 !== 'string') return new Uint8Array(0);

        // 1. Bereinigung: Leerzeichen/Zeilenumbrüche entfernen
        let cleaned = base64.trim();

        // 2. URL-Safe Base64 zu Standard Base64 konvertieren
        // Ersetzt '-' durch '+' und '_' durch '/'
        cleaned = cleaned.replace(/-/g, '+').replace(/_/g, '/');

        // 3. Padding korrigieren: Ein gültiger Base64 String muss durch 4 teilbar sein
        while (cleaned.length % 4 !== 0) {
            cleaned += '=';
        }

        try {
            const binaryString = window.atob(cleaned);
            const len = binaryString.length;
            const bytes = new Uint8Array(len);
            for (let i = 0; i < len; i++) {
                bytes[i] = binaryString.charCodeAt(i);
            }
            return bytes;
        } catch (e) {
            console.error("[pE_Common] base64ToBytes failed for string:", cleaned, e);
            return new Uint8Array(0);
        }
    },
};


/**
 * Web-Pendant zur pE_Capacitor (app.js)
 * Beinhaltet die vollständige Browser-Logik für Bildoptimierung und Fallbacks.
 */
window.pE_Web = {
    /**
     * Kopiert einen Text in die Zwischenablage des Betriebssystems.
     * @param {string} text - Der zu kopierende Text.
     */
    copyClipboard: async function (text) {
        pE_Common.log("Common", "copyClipboard aufgerufen", "log", { textLength: text?.length });

        if (!navigator.clipboard) {
            pE_Common.log("Common", "Clipboard API nicht verfügbar", "error");
            return pE_Common.toScalar(false, false, "Clipboard API not supported");
        }

        try {
            await navigator.clipboard.writeText(text);
            pE_Common.log("Common", "Text erfolgreich in die Zwischenablage kopiert.");
            return pE_Common.toScalar(true, true, null);
        } catch (err) {
            pE_Common.log("Common", "Fehler beim Kopieren in die Zwischenablage", "error", err);
            return pE_Common.toScalar(false, false, err.message);
        }
    },

    /**
     * Holt einen Wert zuerst aus dem Cookie, mit Fallback auf localStorage.
     * @param {string} name - Der Schlüsselname des Wertes.
     */
    getValue: function (name) {
        pE_Common.log("Common", `getValue aufgerufen für: ${name}`);

        // 1. Erst aus Cookie lesen
        // Hinweis: Wir gehen davon aus, dass getCookie ebenfalls in pE_Common liegt 
        // oder global verfügbar ist.
        let value = (typeof this.getCookie === "function") ? this.getCookie(name) : getCookie(name);

        // 2. Fallback: localStorage (nur wenn Cookie nicht existiert/blockiert ist)
        if (value === null || value === "") {
            try {
                value = localStorage.getItem(name);

                if (value !== null) {
                    pE_Common.log("Common", `Wert für "${name}" aus localStorage (Cookie leer/blockiert)`, "warn");
                } else {
                    pE_Common.log("Common", `Kein Wert für "${name}" in Cookies oder localStorage gefunden.`);
                }
            } catch (e) {
                pE_Common.log("Common", `localStorage blockiert für "${name}"`, "error", e);
            }
        } else {
            pE_Common.log("Common", `Wert für "${name}" erfolgreich aus Cookie geladen.`);
        }

        // 3. Rückgabe (kann null sein, falls nichts gefunden wurde)
        // Wir geben hier den Rohwert zurück, wie im Original.
        return value;
    },

    /**
     * Liest einen spezifischen Cookie-Wert anhand seines Namens aus.
     * @param {string} name - Der Name des Cookies.
     */
    getCookie: function (name) {
        // Wir loggen nur den Namen der Anfrage, nicht den gesamten document.cookie (Datenschutz/Übersicht)
        pE_Common.log("Common", `getCookie aufgerufen für: ${name}`);

        try {
            const cookies = document.cookie.split(';');

            for (let i = 0; i < cookies.length; i++) {
                const cookie = cookies[i].trim();

                if (cookie.startsWith(name + '=')) {
                    const value = decodeURIComponent(cookie.substring(name.length + 1));

                    pE_Common.log("Common", `Cookie "${name}" gefunden.`, "log");
                    return value;
                }
            }

            pE_Common.log("Common", `Cookie "${name}" nicht im document.cookie vorhanden.`);
            return null;

        } catch (err) {
            pE_Common.log("Common", `Fehler beim Parsen des Cookies "${name}"`, "error", err);
            return null;
        }
    },

    /**
     * Löscht einen Wert sowohl aus den Cookies als auch aus dem localStorage.
     * @param {string} name - Der Name des zu löschenden Schlüssels.
     */
    resetCookie: function (name) {
        pE_Common.log("Common", `resetCookie aufgerufen für: ${name}`);

        try {
            const isHttps = location.protocol === "https:";
            const domain = location.hostname === "localhost"
                ? "localhost"
                : location.hostname;

            const secure = isHttps ? "; Secure" : "";

            // Cookie löschen (wichtig: Domain + Path müssen exakt passen)
            document.cookie =
                `${name}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/; domain=${domain}; SameSite=Lax${secure}`;

            pE_Common.log("Common", `Cookie "${name}" wurde durch Ablaufdatum zurückgesetzt.`);

            // LocalStorage bereinigen
            try {
                localStorage.removeItem(name);
                pE_Common.log("Common", `"${name}" erfolgreich aus localStorage gelöscht.`);
            } catch (e) {
                pE_Common.log("Common", `Fehler beim Entfernen aus localStorage für "${name}"`, "error", e);
            }

        } catch (e) {
            pE_Common.log("Common", `Fehler beim Löschen des Cookies "${name}"`, "error", e);
        }
    },

    /**
     * Speichert einen Wert sowohl im Cookie als auch im localStorage.
     * @param {string} name - Der Schlüsselname.
     * @param {string} value - Der zu speichernde Wert.
     * @param {number} days - Gültigkeit in Tagen (Standard: 365).
     */
    setValue: function (name, value, days = 365) {
        pE_Common.log("Common", `setValue aufgerufen für: ${name}`, "log", { valueLength: value?.length });
        try {
            // 1. In Cookie speichern
            const date = new Date();
            date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
            const expires = "; expires=" + date.toUTCString();
            const secure = location.protocol === "https:" ? "; Secure" : "";

            document.cookie = `${name}=${encodeURIComponent(value || "")}${expires}; path=/; SameSite=Lax${secure}`;
            pE_Common.log("Common", `Cookie "${name}" erfolgreich gesetzt.`);

            // 2. Backup in localStorage
            try {
                localStorage.setItem(name, value || "");
                pE_Common.log("Common", `"${name}" erfolgreich im localStorage gesichert.`);
            } catch (e) {
                pE_Common.log("Common", `localStorage.setItem fehlgeschlagen für "${name}"`, "warn", e);
            }

            return pE_Common.toScalar(true, true, null);
        } catch (e) {
            pE_Common.log("Common", `Fehler in setValue für "${name}"`, "error", e);
            return pE_Common.toScalar(false, false, e.message);
        }
    },

    /**
     * Führt einen Reset des Cookies und LocalStorages durch.
     * @param {string} name - Der Name des Schlüssels.
     * @param {string} value - Der Wert (aktuell nicht genutzt, da setCookie inaktiv).
     * @param {number} daysToExpire - Ablaufzeit (aktuell nicht genutzt).
     */
    deleteCookieAndLocalStorage: function (name, value, daysToExpire = 7) {
        pE_Common.log("Common", `deleteCookieAndLocalStorage aufgerufen für: ${name}`);

        try {
            // Wir nutzen die bereits definierte Methode für maximale Konsistenz
            this.resetCookie(name);

            pE_Common.log("Common", `Bereinigung für "${name}" abgeschlossen.`);

            // Hinweis: Falls du die setCookie Logik wieder aktivieren möchtest,
            // sollte sie hier folgen. Aktuell ist sie laut Vorlage inaktiv.

            return pE_Common.toScalar(true, true, null);
        } catch (e) {
            pE_Common.log("Common", `Fehler in deleteCookieAndLocalStorage für "${name}"`, "error", e);
            return pE_Common.toScalar(false, false, e.message);
        }
    },

    /**
     * Entfernt einen Wert aus Storage (Web/PWA).
     * Ruft intern deleteCookieAndLocalStorage auf.
     * @param {string} name - Der Name des Schlüssels.
     */
    removeStorage: function (name) {
        pE_Common.log("Common", `removeStorage (alias) aufgerufen für: ${name}`);
        return this.deleteCookieAndLocalStorage(name);
    },

    /**
     * Ermittelt die aktuellen Browser-Fenstermaße.
     * Nützlich für UI-Anpassungen in Blazor.
     */
    getWindowDimensions: function () {
        const dimensions = {
            width: window.innerWidth,
            height: window.innerHeight
        };

        pE_Common.log("Common", `getWindowDimensions: ${dimensions.width}x${dimensions.height}`, "log", dimensions);

        // Da dies ein komplexeres Objekt ist, geben wir es direkt zurück.
        // Falls du es für Blazor in ein ScalarModel pressen willst, 
        // müssten wir es als JSON-String verpacken.
        return dimensions;
    },

    /**
     * Ermöglicht den Download einer Datei direkt aus einem Blazor-Stream.
     * @param {string} fileName - Der gewünschte Dateiname für den User.
     * @param {object} contentStreamReference - Die Referenz auf den .NET Stream.
     */
    downloadFileFromStream: async function (fileName, contentStreamReference) {
        pE_Common.log("Common", `Download gestartet: ${fileName}`);

        try {
            // 1. Daten aus dem Stream in ein ArrayBuffer laden
            const arrayBuffer = await contentStreamReference.arrayBuffer();
            pE_Common.log("Common", `Stream geladen (${arrayBuffer.byteLength} Bytes).`);

            // 2. Blob und temporäre URL erstellen
            const blob = new Blob([arrayBuffer]);
            const url = URL.createObjectURL(blob);

            // 3. Temporäres Link-Element für den Download-Trigger erstellen
            const link = document.createElement('a');
            link.href = url;
            link.download = fileName;

            // Link kurzzeitig ins DOM einhängen, klicken und wieder entfernen
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);

            // 4. Speicher freigeben (Wichtig!)
            URL.revokeObjectURL(url);

            pE_Common.log("Common", `Download-Trigger für "${fileName}" erfolgreich ausgeführt.`);
            return pE_Common.toScalar(true, true, null);
        } catch (err) {
            pE_Common.log("Common", `Fehler beim Download von "${fileName}"`, "error", err);
            return pE_Common.toScalar(false, false, err.message);
        }
    },

    /**
     * Analysiert den UserAgent und gibt das Betriebssystem inkl. Version zurück.
     * @returns {string} - Lesbarer OS-Name (z.B. "Windows 10", "iOS 15.4")
     */
    getOSVersion: function () {
        const userAgent = navigator.userAgent;
        let detectedOS = "Unknown OS";

        try {
            // Windows
            if (userAgent.indexOf("Windows NT") != -1) {
                const osVersion = userAgent.match(/Windows NT ([\d._]+)/);
                if (osVersion && osVersion.length > 1) {
                    const versionMap = {
                        '10.0': '10',
                        '6.3': '8.1',
                        '6.2': '8',
                        '6.1': '7'
                    };
                    detectedOS = `Windows ${versionMap[osVersion[1]] || osVersion[1]}`;
                }
            }
            // macOS
            else if (userAgent.indexOf("Macintosh") != -1) {
                const osVersion = userAgent.match(/Mac OS X ([\d_]+)/);
                if (osVersion && osVersion.length > 1) {
                    detectedOS = `macOS ${osVersion[1].replace(/_/g, '.')}`;
                }
            }
            // Android
            else if (userAgent.indexOf("Android") != -1) {
                const osVersion = userAgent.match(/Android ([\d.]+)/);
                if (osVersion && osVersion.length > 1) {
                    detectedOS = `Android ${osVersion[1]}`;
                }
            }
            // iOS
            else if (userAgent.indexOf("iPhone OS") != -1 || userAgent.indexOf("iPad OS") != -1) {
                const osVersion = userAgent.match(/(iPhone|iPad) OS ([\d_]+)/);
                if (osVersion && osVersion.length > 2) {
                    detectedOS = `iOS ${osVersion[2].replace(/_/g, '.')}`;
                }
            }
            // Linux
            else if (userAgent.indexOf("Linux") != -1) {
                detectedOS = "Linux";
            }

            pE_Common.log("Common", `OS erkannt: ${detectedOS}`);
            return detectedOS;

        } catch (err) {
            pE_Common.log("Common", "Fehler bei OS-Erkennung", "warn", err);
            return "Unknown OS";
        }
    },

    /**
     * Öffnet eine URL in einem neuen Browser-Tab.
     * @param {string} url - Die Ziel-URL.
     */
    openExternalUrl: function (url) {
        pE_Common.log("Common", `Versuche externe URL zu öffnen: ${url}`);

        try {
            if (!url) {
                pE_Common.log("Common", "Abbruch: Keine URL übergeben.", "warn");
                return;
            }

            const win = window.open(url, '_blank');

            if (win) {
                pE_Common.log("Common", "URL erfolgreich im neuen Tab geöffnet.");
                // Fokus auf den neuen Tab setzen (optional)
                win.focus();
            } else {
                pE_Common.log("Common", "Pop-up wurde vom Browser blockiert.", "warn");
                alert("Bitte erlauben Sie Pop-ups für diese Seite, um den Link zu öffnen.");
            }
        } catch (err) {
            pE_Common.log("Common", "Fehler beim Öffnen der URL", "error", err);
        }
    },

    /**
     * Schaltet die Textrichtung (RTL/LTR) und die entsprechenden Stylesheets um.
     * @param {boolean} isRtl - True für Right-to-Left (z.B. Arabisch), False für Left-to-Right.
     */
    setRtl: function (isRtl) {
        pE_Common.log("Common", `setRtl aufgerufen: ${isRtl ? "RTL (Arabisch)" : "LTR (Deutsch)"}`);

        try {
            const rtl = document.getElementById('bootstrap-rtl');
            const ltr = document.getElementById('bootstrap-ltr');
            const html = document.documentElement;

            // Sicherheitscheck: Existieren die Style-Elemente?
            if (!rtl || !ltr) {
                pE_Common.log("Common", "Stylesheets 'bootstrap-rtl' oder 'bootstrap-ltr' nicht gefunden!", "error");
                // Wir machen trotzdem weiter mit den HTML-Attributen, falls möglich
            }

            if (isRtl) {
                if (rtl) rtl.disabled = false;
                if (ltr) ltr.disabled = true;
                html.setAttribute("dir", "rtl");
                html.setAttribute("lang", "ar");
                pE_Common.log("Common", "Layout auf RTL umgestellt.");
            } else {
                if (rtl) rtl.disabled = true;
                if (ltr) ltr.disabled = false;
                html.setAttribute("dir", "ltr");
                html.setAttribute("lang", "de");
                pE_Common.log("Common", "Layout auf LTR umgestellt.");
            }

            return pE_Common.toScalar(true, true, null);
        } catch (err) {
            pE_Common.log("Common", "Fehler bei der RTL/LTR Umschaltung", "error", err);
            return pE_Common.toScalar(false, false, err.message);
        }
    },

    /**
     * Gibt eine kombinierte Kennung für das Gerät zurück.
     * @returns {string} - Format: "web-[Betriebssystem]"
     */
    getDeviceInfo: function () {
        pE_Common.log("Common", "getDeviceInfo aufgerufen");

        try {
            // Wir greifen auf unsere interne getOSVersion zurück
            const os = this.getOSVersion();
            const deviceString = `web-${os}`;

            pE_Common.log("Common", `Device-Info ermittelt: ${deviceString}`);
            return deviceString;
        } catch (err) {
            pE_Common.log("Common", "Fehler bei getDeviceInfo", "error", err);
            return "web-unknown";
        }
    },


    // ===== CONNECTIVITY MODUL =====
    //cloudConnectivity: {
    //    _notifyHandler: null,

    //    /**
    //     * Initialisiert die Überwachung des Online-Status.
    //     * @param {object} dotNet - Die DotNet-Referenz für Callbacks.
    //     */
    //    init: function (dotNet) {
    //        pE_Common.log("Common:Connectivity", "Initialisiere Online-Status Überwachung");

    //        if (!dotNet) {
    //            pE_Common.log("Common:Connectivity", "Abbruch: Keine DotNet-Referenz übergeben", "error");
    //            return;
    //        }

    //        // Wir speichern den Handler in einer internen Variable, damit wir ihn später wieder entfernen können (dispose)
    //        this._notifyHandler = () => {
    //            pE_Common.log("Common:Connectivity", `Internet-Status geändert: ${navigator.onLine ? "ONLINE" : "OFFLINE"}`);
    //            dotNet.invokeMethodAsync("OnBrowserInternetChanged", navigator.onLine);
    //        };

    //        window.addEventListener("online", this._notifyHandler);
    //        window.addEventListener("offline", this._notifyHandler);

    //        // Initialen Status sofort melden
    //        this._notifyHandler();
    //    },

    //    /**
    //     * Entfernt die Event-Listener, um Memory Leaks zu vermeiden.
    //     */
    //    dispose: function () {
    //        if (this._notifyHandler) {
    //            window.removeEventListener("online", this._notifyHandler);
    //            window.removeEventListener("offline", this._notifyHandler);
    //            pE_Common.log("Common:Connectivity", "Online-Status Überwachung beendet (disposed).");
    //            this._notifyHandler = null;
    //        }
    //    }
    //},
    cloudConnectivity: {
        _notifyHandler: null,
        _dotNetRef: null,

        /**
         * Initialisiert die Überwachung des Online-Status.
         * @param {object} dotNet - Die DotNet-Referenz für Callbacks.
         */
        init: function (dotNet) {
            pE_Common.log(
                "Common:Connectivity",
                "Initialisiere Online-Status Überwachung"
            );

            if (!dotNet) {
                pE_Common.log(
                    "Common:Connectivity",
                    "Abbruch: Keine DotNet-Referenz übergeben",
                    "error"
                );
                return;
            }

            // DotNet-Referenz explizit speichern
            this._dotNetRef = dotNet;

            // Wir speichern den Handler, damit wir ihn sauber entfernen können
            this._notifyHandler = () => {
                try {
                    if (!this._dotNetRef) {
                        // .NET wurde bereits disposed
                        return;
                    }

                    const isOnline = navigator.onLine;

                    pE_Common.log(
                        "Common:Connectivity",
                        `Internet-Status geändert: ${isOnline ? "ONLINE" : "OFFLINE"}`
                    );

                    // Invoke defensiv – Promise-Fehler abfangen!
                    this._dotNetRef
                        .invokeMethodAsync("OnBrowserInternetChanged", isOnline)
                        .catch(() => {
                            // DotNetObjectReference ist weg -> still ignorieren
                        });
                } catch {
                    // Absolute Sicherheit: kein JS-Error darf nach außen
                }
            };

            window.addEventListener("online", this._notifyHandler);
            window.addEventListener("offline", this._notifyHandler);

            // Initialen Status sofort melden
            this._notifyHandler();
        },

        /**
         * Entfernt die Event-Listener, um Memory Leaks zu vermeiden.
         */
        dispose: function () {
            if (this._notifyHandler) {
                window.removeEventListener("online", this._notifyHandler);
                window.removeEventListener("offline", this._notifyHandler);
                this._notifyHandler = null;
            }

            // DotNet-Referenz explizit freigeben
            this._dotNetRef = null;

            pE_Common.log(
                "Common:Connectivity",
                "Online-Status Überwachung beendet (disposed)."
            );
        }
    },

    // ===== SHARE MODUL =====
    share: {
        /**
         * Prüft, ob die native Web Share API vom Browser unterstützt wird.
         */
        isSupported: function () {
            const supported = !!navigator.share;
            pE_Common.log("Web:Share", `Prüfe Support: ${supported}`);
            return supported;
        },

        /**
         * Teilt Texte oder URLs über das native Share-Sheet des Betriebssystems.
         */
        shareText: async function (title, text, url) {
            pE_Common.log("Web:Share", "shareText aufgerufen", "log", { title, text, url });

            // Wenn keine URL übergeben wird, nutze aktuelle Seite
            let finalUrl = url;
            if (!finalUrl) {
                finalUrl = window.location.href;
                pE_Common.log("Web:Share", `Keine URL übergeben, nutze aktuelle Seite: ${finalUrl}`);
            }

            // Validierung: Mindestens ein Feld muss belegt sein
            if (!title && !text && !finalUrl) {
                pE_Common.log("Web:Share", "Abbruch: Keine Share-Daten vorhanden (title, text, url leer)", "warn");
                return pE_Common.toScalar(false, false, "No data to share");
            }

            // Prüfung auf Support
            if (!this.isSupported()) {
                pE_Common.log("Web:Share", "Native Share API nicht unterstützt - Fallback nötig", "warn");
                // Hinweis: Hier könnte ein Fallback (z.B. Clipboard oder Modal) aufgerufen werden.
                // Laut deiner Vorlage geben wir aktuell false zurück oder rufen fallbackShare auf.
                if (typeof window.fallbackShare === "function") {
                    const fallbackResult = await window.fallbackShare(text || finalUrl);
                    return pE_Common.toScalar(fallbackResult, true, null);
                }
                return pE_Common.toScalar(false, false, "Share API not supported and no fallback found");
            }

            //const shareData = {};
            //if (title) shareData.title = String(title);
            //if (text) shareData.text = String(text);
            //if (finalUrl) shareData.url = String(finalUrl);
            const shareData = {};

            // Titel und Text zusammenführen damit Apps wie WhatsApp
            // den Titel nicht doppelt anzeigen (z.B. "Todo: Todo: ...")
            if (title && text) {
                shareData.text = `${String(title)}: ${String(text)}`;
            } else if (title) {
                shareData.text = String(title);
            } else if (text) {
                shareData.text = String(text);
            }

            pE_Common.log("Web:Share", "Versuche navigator.share...", "log", shareData);

            try {
                await navigator.share(shareData);
                pE_Common.log("Web:Share", "Teilen-Dialog erfolgreich geschlossen.");
                return pE_Common.toScalar(true, true, null);
            } catch (err) {
                // AbortError passiert, wenn der User den Dialog einfach schließt (kein echter Fehler)
                if (err.name === 'AbortError') {
                    pE_Common.log("Web:Share", "Teilen vom Benutzer abgebrochen.");
                    return pE_Common.toScalar(false, true, "User aborted");
                }

                pE_Common.log("Web:Share", "Teilen fehlgeschlagen", "error", err);
                // Alert aus deinem Original-Code erhalten
                alert("Sharing failed: " + err.message);
                return pE_Common.toScalar(false, false, err.message);
            }
        }
    },

    // ===== OTP FOCUS MODUL =====
    otpFocus: {
        focusElement: function (element) {
            pE_Common.log("Web:OTP", "Fokussiere Element...", "log", element);
            if (element) {
                element.focus();
                return pE_Common.toScalar(true, true, null);
            }
            pE_Common.log("Web:OTP", "Fokus fehlgeschlagen: Element ist null", "warn");
            return pE_Common.toScalar(false, false, "Not found");
        },

        resetInputValue: function (element) {
            pE_Common.log("Web:OTP", "Setze Input zurück...", "log", element);
            if (element) {
                element.value = "";
                return pE_Common.toScalar(true, true, null);
            }
            return pE_Common.toScalar(false, false, "Not found");
        },

        focusNext: function (element) {
            pE_Common.log("Web:OTP", "Springe zum nächsten OTP-Feld", "log", element);
            if (element) {
                element.focus();
                return pE_Common.toScalar(true, true, null);
            }
            return pE_Common.toScalar(false, false, "Not found");
        },

        focusPrev: function (element) {
            pE_Common.log("Web:OTP", "Springe zum vorherigen OTP-Feld", "log", element);
            if (element) {
                element.focus();
                return pE_Common.toScalar(true, true, null);
            }
            return pE_Common.toScalar(false, false, "Not found");
        }
    },

    // ===== MEDIA MODUL =====
    media: {

        _clampQuality: function (q) {
            const originalQ = q;
            if (typeof q !== "number" || Number.isNaN(q)) return 0.8;
            if (q > 1) q = q / 100;
            const ua = navigator.userAgent;
            const isChrome = /Chrome/.test(ua);
            const isFirefox = /Firefox/.test(ua);
            const isSafari = /Safari/.test(ua) && !isChrome;

            if (isChrome && q > 0.92) q = 0.92;
            if (isFirefox && q > 0.95) q = 0.95;
            if (isSafari && q > 0.78) q = 0.78;

            if (q < 0.1) q = 0.1;
            if (q > 0.95) q = 0.95;

            if (originalQ !== q) {
                pE_Common.log("Web:Media", `Qualität angepasst: ${originalQ} -> ${q} (Browser-Optimierung)`, "log");
            }
            return q;
        },

        _fileToImage: function (file) {
            pE_Common.log("Web:Media", `FileReader startet für: ${file.name} (${file.size} Bytes)`, "log");
            return new Promise((resolve, reject) => {
                const reader = new FileReader();
                reader.onerror = (err) => {
                    pE_Common.log("Web:Media", "FileReader Fehler", "error", err);
                    reject(err);
                };
                reader.onload = (e) => {
                    const img = new Image();
                    img.onload = () => {
                        pE_Common.log("Web:Media", `Bild erfolgreich geladen: ${img.naturalWidth}x${img.naturalHeight}`, "log");
                        resolve(img);
                    };
                    img.onerror = (err) => {
                        pE_Common.log("Web:Media", "Image-Objekt Fehler", "error", err);
                        reject(err);
                    };
                    img.src = e.target.result;
                };
                reader.readAsDataURL(file);
            });
        },

        //processImage: async function (base64, options) {
        //    if (!base64) return null;

        //    // Konvertiere base64 zu Image Objekt
        //    const img = new Image();
        //    const loadPromise = new Promise((resolve) => {
        //        img.onload = () => resolve();
        //        img.src = base64.startsWith('data:') ? base64 : "data:image/jpeg;base64," + base64;
        //    });

        //    await loadPromise;

        //    // Nutze deine existierende interne Funktion
        //    // Beachte: Deine Funktion erwartet (img, size, quality, crop, mime)
        //    const mime = options.format === 1 ? "image/png" : "image/jpeg";

        //    return this._optimizeImageOnCanvas(
        //        img,
        //        options.maxSize,
        //        options.quality,
        //        options.cropToSquare,
        //        mime
        //    );
        //},
        processImage: async function (base64, options) {
            pE_Common.log(`Capacitor processImage: Start (Quality: ${options.quality}, Crop: ${options.cropToSquare})`);

            if (!base64) {
                pE_Common.log("processImage: Kein Input vorhanden.", "error");
                return null;
            }

            // --- 1. PARAMETER HARMONISIERUNG ---
            const targetQuality = options.quality || 80;
            const isCrop = !!options.cropToSquare;
            const mime = options.format === 1 ? "image/png" : "image/jpeg";

            // Falls maxWidth/Height gesendet wurde, aber kein maxSize
            const effectiveMaxSize = options.maxSize || Math.max(options.maxWidth || 0, options.maxHeight || 0);

            // --- 2. FAST-LANE CHECK (Bypass) ---
            // Wenn das Bild bereits optimiert aus der Kamera kam und kein Crop nötig ist
            if (options.isAlreadyOptimized && !isCrop) {
                pE_Common.log("processImage: Fast-Lane aktiv (Bypass Canvas).", "info");
                // Wir stellen sicher, dass ein valider Data-URL String zurückgeht
                return base64.startsWith('data:') ? base64 : `data:${mime};base64,${base64}`;
            }

            // --- 3. CANVAS VERARBEITUNG ---
            return new Promise((resolve) => {
                const img = new Image();
                img.onload = () => {
                    try {
                        const canvas = document.createElement('canvas');
                        const ctx = canvas.getContext('2d');
                        let targetWidth, targetHeight;

                        if (isCrop && effectiveMaxSize) {
                            // Quadratischer Zuschnitt
                            const size = Math.min(img.width, img.height);
                            const sx = (img.width - size) / 2;
                            const sy = (img.height - size) / 2;
                            canvas.width = effectiveMaxSize;
                            canvas.height = effectiveMaxSize;
                            ctx.drawImage(img, sx, sy, size, size, 0, 0, effectiveMaxSize, effectiveMaxSize);
                        } else {
                            // Proportionale Skalierung
                            let ratio = img.width / img.height;
                            targetWidth = options.maxWidth || effectiveMaxSize || img.width;
                            targetHeight = options.maxHeight || (effectiveMaxSize ? effectiveMaxSize / ratio : img.height);

                            // Falls nur maxSize gegeben war, das Seitenverhältnis wahren
                            if (effectiveMaxSize && !options.maxWidth && !options.maxHeight) {
                                if (img.width > img.height) {
                                    targetWidth = effectiveMaxSize;
                                    targetHeight = effectiveMaxSize / ratio;
                                } else {
                                    targetHeight = effectiveMaxSize;
                                    targetWidth = effectiveMaxSize * ratio;
                                }
                            }

                            canvas.width = targetWidth;
                            canvas.height = targetHeight;
                            ctx.drawImage(img, 0, 0, targetWidth, targetHeight);
                        }

                        // Rückgabe als DataURL (Konsistent mit Web-Version)
                        const result = canvas.toDataURL(mime, targetQuality / 100);
                        pE_Common.log("processImage: Verarbeitung erfolgreich beendet.");
                        resolve(result);

                    } catch (err) {
                        pE_Common.log("processImage: Fehler in Canvas-Logik", "error", err);
                        resolve(null);
                    }
                };

                img.onerror = (err) => {
                    pE_Common.log("processImage: Image Load Error", "error", err);
                    resolve(null);
                };

                img.src = base64.startsWith('data:') ? base64 : `data:image/jpeg;base64,${base64}`;
            });
        },

        _optimizeImageOnCanvas: function (imgOrCanvas, imageSize, imageQuality, cropToSquare, mime = "image/jpeg") {
            const startTime = performance.now();
            const q = this._clampQuality(imageQuality);
            const canvas = document.createElement("canvas");
            const ctx = canvas.getContext("2d");

            let w, h;
            if (imgOrCanvas instanceof HTMLCanvasElement) {
                w = imgOrCanvas.width; h = imgOrCanvas.height;
            } else {
                w = imgOrCanvas.naturalWidth || imgOrCanvas.width;
                h = imgOrCanvas.naturalHeight || imgOrCanvas.height;
            }

            if (cropToSquare) {
                const size = Math.min(w, h);
                const sx = Math.floor((w - size) / 2);
                const sy = Math.floor((h - size) / 2);
                canvas.width = imageSize; canvas.height = imageSize;
                ctx.drawImage(imgOrCanvas, sx, sy, size, size, 0, 0, imageSize, imageSize);
                pE_Common.log("Web:Media", `Canvas Crop & Resize: ${w}x${h} -> ${imageSize}x${imageSize}`, "log");
            } else {
                let targetW, targetH;
                if (w > h) {
                    targetH = Math.round(h * (imageSize / w));
                    targetW = imageSize;
                } else {
                    targetW = Math.round(w * (imageSize / h));
                    targetH = imageSize;
                }
                canvas.width = targetW; canvas.height = targetH;
                ctx.drawImage(imgOrCanvas, 0, 0, canvas.width, canvas.height);
                pE_Common.log("Web:Media", `Canvas Resize: ${w}x${h} -> ${targetW}x${targetH}`, "log");
            }

            const dataUrl = canvas.toDataURL(mime, q);
            const duration = (performance.now() - startTime).toFixed(2);
            pE_Common.log("Web:Media", `Optimierung abgeschlossen in ${duration}ms (MIME: ${mime})`, "log");
            return dataUrl;
        },

        _dataURLToBytes: function (dataUrl) {
            try {
                const base64 = dataUrl.split(',')[1];
                const binaryString = atob(base64);
                const bytes = new Uint8Array(binaryString.length);
                for (let i = 0; i < binaryString.length; i++) {
                    bytes[i] = binaryString.charCodeAt(i);
                }
                return bytes;
            } catch (e) {
                pE_Common.log("Web:Media", "Fehler bei Konvertierung DataURL zu Bytes", "error", e);
                return new Uint8Array(0);
            }
        },

        // ===== Öffentliche API (Spiegelbildlich zu cap.js) =====
        //capturePhoto: async function (dotNetHelper, videoElementId, imageSize, imageQuality, cropToSquare, thumbnailSize) {
        //    pE_Common.log("Web:Media", "capturePhoto: Starte Aufnahme mit Frame-Extraktion", "log");
        //    try {
        //        const video = document.getElementById(videoElementId);

        //        // 1. Validierung und Retry-Logik für WebView2 Hardware-Latch
        //        let retries = 0;
        //        while ((!video || video.videoWidth === 0) && retries < 10) {
        //            pE_Common.log("Web:Media", `Warte auf Video-Metadaten (Versuch ${retries + 1}/10)...`, "warn");
        //            await new Promise(r => setTimeout(r, 100));
        //            retries++;
        //        }

        //        if (!video || video.videoWidth === 0 || video.videoHeight === 0) {
        //            pE_Common.log("Web:Media", "Kamera-Stream liefert keine Pixeldaten (Timeout)", "error");
        //            return pE_Common.toScalar(null, false, "Kamera-Stream nicht bereit (0px Fehler).");
        //        }

        //        // 2. DER ZWISCHENCANVAS (WICHTIG für WPF WebView2)
        //        // Wir erzwingen hier das Auslesen der Pixel aus dem Hardware-Layer in einen Software-Buffer
        //        const bufferCanvas = document.createElement("canvas");
        //        bufferCanvas.width = video.videoWidth;
        //        bufferCanvas.height = video.videoHeight;
        //        const bufferCtx = bufferCanvas.getContext("2d");

        //        pE_Common.log("Web:Media", `Extrahiere Frame: ${bufferCanvas.width}x${bufferCanvas.height}`, "log");
        //        bufferCtx.drawImage(video, 0, 0, bufferCanvas.width, bufferCanvas.height);

        //        // 3. OPTIMIERUNG (Wir nutzen jetzt den stabilen Buffer anstatt des direkten Video-Elements)
        //        // Hauptbild generieren
        //        const mainDataUrl = this._optimizeImageOnCanvas(bufferCanvas, imageSize, imageQuality, cropToSquare);

        //        // Thumbnail generieren
        //        const thumbSize = thumbnailSize || 64;
        //        const thumbnailDataUrl = this._optimizeImageOnCanvas(bufferCanvas, thumbSize, imageQuality, true);

        //        // 4. DATENTRANSFER
        //        if (dotNetHelper) {
        //            pE_Common.log("Web:Media", "Sende optimierte Daten an Blazor...", "log");
        //            await dotNetHelper.invokeMethodAsync("SetOptimizedImageData", mainDataUrl, thumbnailDataUrl);
        //        }

        //        const bytes = this._dataURLToBytes(mainDataUrl);
        //        pE_Common.log("Web:Media", `capturePhoto Erfolg: ${bytes.length} Bytes verarbeitet.`, "log");

        //        return pE_Common.toScalar(null, true, null, bytes);

        //    } catch (e) {
        //        pE_Common.log("Web:Media", "capturePhoto kritischer Fehler", "error", e.message);
        //        return pE_Common.toScalar(null, false, e.message);
        //    }
        //},
        capturePhotoVoid: async function (dotNetHelper, videoId, size, quality, crop, thumbSize) {
            // Wir rufen die schwere Methode auf. Das Bild wird verarbeitet 
            // und bereits via Callback (SetOptimizedImageData) an C# gesendet.
            await this.capturePhoto(dotNetHelper, videoId, size, quality, crop, thumbSize);

            // Wir geben dem Server nur eine minimale Bestätigung zurück.
            // Keine Bytes, kein schweres Objekt -> SignalR bleibt extrem schnell.
            return;
        },

        capturePhoto: async function (dotNetHelper, videoElementId, imageSize, imageQuality, cropToSquare, thumbnailSize) {
            pE_Common.log("Web:Media", "=== CAPTURE PHOTO START ===", "log");
            pE_Common.log("Web:Media", `VideoElement: ${videoElementId}, ImageSize: ${imageSize}`, "log");

            try {
                const video = document.getElementById(videoElementId);

                // Detaillierte Video-Diagnose
                pE_Common.log("Web:Media", `Video Element gefunden: ${!!video}`, "log");
                if (video) {
                    pE_Common.log("Web:Media", `Video Eigenschaften:
                      - videoWidth: ${video.videoWidth}
                      - videoHeight: ${video.videoHeight}
                      - readyState: ${video.readyState}
                      - srcObject: ${video.srcObject ? 'vorhanden' : 'fehlt'}
                      - paused: ${video.paused}
                      - ended: ${video.ended}
                      - currentTime: ${video.currentTime}
                      - width: ${video.width}
                      - height: ${video.height}`, "debug");
                }

                // 1. ERWEITERTE STREAM-DIAGNOSE
                if (video && video.srcObject) {
                    const stream = video.srcObject;
                    pE_Common.log("Web:Media", "=== STREAM DIAGNOSE ===", "log");
                    pE_Common.log("Web:Media", `Stream aktiv: ${stream.active}`, "log");

                    const tracks = stream.getTracks();
                    pE_Common.log("Web:Media", `Anzahl Tracks: ${tracks.length}`, "log");

                    tracks.forEach((track, index) => {
                        pE_Common.log("Web:Media", `Track ${index}:
                          - kind: ${track.kind}
                          - label: ${track.label}
                          - enabled: ${track.enabled}
                          - muted: ${track.muted}
                          - readyState: ${track.readyState}
                          - constraints: ${JSON.stringify(track.getConstraints ? track.getConstraints() : 'N/A')}
                          - settings: ${JSON.stringify(track.getSettings ? track.getSettings() : 'N/A')}`, "debug");

                        // Track-Status überwachen
                        if (track.readyState === 'ended') {
                            pE_Common.log("Web:Media", `ACHTUNG: Track ${index} ist beendet!`, "error");
                        }
                    });
                } else {
                    pE_Common.log("Web:Media", "KEIN srcObject gefunden!", "error");
                }

                // 2. ERWEITERTE RETRY-LOGIK MIT FRAME-DETEKTION
                let retries = 0;
                let lastVideoWidth = 0;

                while (retries < 40) { // Erhöht von 25 auf 40
                    if (video) {
                        lastVideoWidth = video.videoWidth;

                        // Frame-Änderung erkennen
                        if (retries > 0 && video.videoWidth !== lastVideoWidth) {
                            pE_Common.log("Web:Media", `Frame-Änderung erkannt: ${lastVideoWidth} -> ${video.videoWidth}px`, "log");
                        }

                        if (video.videoWidth > 0 && video.readyState >= 2) {
                            pE_Common.log("Web:Media", `Video bereit nach ${retries} Versuchen: ${video.videoWidth}x${video.videoHeight}`, "success");
                            break;
                        }

                        // Aktivitätsprüfung
                        if (video.readyState >= 1) {
                            pE_Common.log("Web:Media", `Video läd... State: ${video.readyState}, Breite: ${video.videoWidth}`, "debug");
                        }

                        // MANUELLES TRIGGERN FÜR WASM
                        if (retries % 5 === 0) {
                            // Versuche, den Video-Player zu "wecken"
                            try {
                                video.play().catch(e => {
                                    pE_Common.log("Web:Media", `Play-Fehler bei Retry ${retries}: ${e.message}`, "debug");
                                });

                                // Frame-Update erzwingen
                                if (video.requestVideoFrameCallback) {
                                    video.requestVideoFrameCallback(() => {
                                        pE_Common.log("Web:Media", `requestVideoFrameCallback getriggert bei Retry ${retries}`, "debug");
                                    });
                                }
                            } catch (e) {
                                // Ignorieren
                            }
                        }
                    }

                    pE_Common.log("Web:Media", `Retry ${retries + 1}/40 - Warte auf Video-Daten...`, "warn");
                    await new Promise(r => setTimeout(r, 100));
                    retries++;
                }

                // 3. ERWEITERTE FALLBACK-LOGIK
                let fallbackUsed = false;
                if (video && video.srcObject && video.videoWidth === 0) {
                    pE_Common.log("Web:Media", "=== AKTIVIERE ERWEITERTE FALLBACK-LOGIK ===", "warn");

                    const stream = video.srcObject;
                    const tracks = stream.getVideoTracks();

                    if (tracks.length > 0) {
                        const track = tracks[0];
                        const settings = track.getSettings();

                        pE_Common.log("Web:Media", `Track-Settings: ${JSON.stringify(settings)}`, "debug");

                        if (settings && settings.width) {
                            pE_Common.log("Web:Media", `Setze Fallback-Dimensionen: ${settings.width}x${settings.height}`, "log");
                            video.width = settings.width;
                            video.height = settings.height;
                            video.style.width = settings.width + 'px';
                            video.style.height = settings.height + 'px';
                            fallbackUsed = true;

                            // VIDEO-ELEMENT NEU LADEN
                            try {
                                video.load();
                                await video.play();
                                pE_Common.log("Web:Media", "Video-Element nach Fallback neu geladen", "log");
                            } catch (e) {
                                pE_Common.log("Web:Media", `Fehler beim Neuladen: ${e.message}`, "warn");
                            }
                        }
                    }

                    // EXTREMFALL: Canvas direkt vom Stream
                    if (!fallbackUsed && stream.getVideoTracks().length > 0) {
                        pE_Common.log("Web:Media", "Versuche direkte Canvas-Extraktion vom Stream", "warn");
                        return await this._captureFromStreamDirectly(stream, dotNetHelper, imageSize, imageQuality, cropToSquare, thumbnailSize);
                    }
                }

                // 4. FINALE VALIDIERUNG
                if (!video || (!fallbackUsed && video.videoWidth === 0)) {
                    pE_Common.log("Web:Media", "=== KRITISCHER FEHLER ===", "error");
                    pE_Common.log("Web:Media", `Video-Element: ${!!video}, videoWidth: ${video ? video.videoWidth : 'N/A'}`, "error");
                    pE_Common.log("Web:Media", "Mögliche Ursachen:", "error");
                    pE_Common.log("Web:Media", "1. Browser-Berechtigungen fehlen", "error");
                    pE_Common.log("Web:Media", "2. Stream wurde vom Browser beendet", "error");
                    pE_Common.log("Web:Media", "3. WASM/Blazor Timing-Problem", "error");
                    pE_Common.log("Web:Media", "4. Hardware-Beschränkung", "error");

                    // Zusätzliche Diagnose
                    if (video && video.error) {
                        pE_Common.log("Web:Media", `Video-Error: ${video.error.code} - ${video.error.message}`, "error");
                    }

                    return pE_Common.toScalar(null, false, "Kamera-Stream liefert keine Pixeldaten (Timeout)");
                }

                // 5. WAIT FOR FRAME MIT VERBESSERTEM TIMEOUT
                try {
                    await this.waitForFirstVideoFrame(videoElementId, 5000);
                } catch (frameError) {
                    pE_Common.log("Web:Media", `Frame-Wait-Fehler: ${frameError.message}`, "error");

                    // Fallback: Direkter Canvas-Versuch
                    if (video.videoWidth > 0) {
                        pE_Common.log("Web:Media", "Überspringe frame-wait, nutze vorhandene Dimensionen", "warn");
                    } else {
                        throw frameError;
                    }
                }

                // 6. CANVAS ERSTELLUNG MIT DIAGNOSE
                const bufferCanvas = document.createElement("canvas");
                const finalWidth = fallbackUsed ? video.width : video.videoWidth;
                const finalHeight = fallbackUsed ? video.height : video.videoHeight;

                bufferCanvas.width = finalWidth;
                bufferCanvas.height = finalHeight;

                pE_Common.log("Web:Media", `Canvas-Dimensionen: ${finalWidth}x${finalHeight}`, "log");
                pE_Common.log("Web:Media", `Fallback verwendet: ${fallbackUsed}`, "log");

                const bufferCtx = bufferCanvas.getContext("2d");

                // TESTZEICHNUNG: Einfarbiger Hintergrund zum Testen
                if (finalWidth === 0 || finalHeight === 0) {
                    pE_Common.log("Web:Media", "CANVAS HAT 0-DIMENSIONEN - Zeichne Testmuster", "error");
                    bufferCanvas.width = 640;
                    bufferCanvas.height = 480;
                    bufferCtx.fillStyle = '#ff0000';
                    bufferCtx.fillRect(0, 0, 640, 480);
                    bufferCtx.fillStyle = '#ffffff';
                    bufferCtx.font = '20px Arial';
                    bufferCtx.fillText('TEST - NO VIDEO DATA', 50, 240);
                } else {
                    pE_Common.log("Web:Media", "Zeichne Video auf Canvas...", "log");
                    try {
                        bufferCtx.drawImage(video, 0, 0, finalWidth, finalHeight);
                        pE_Common.log("Web:Media", "Video erfolgreich auf Canvas gezeichnet", "success");
                    } catch (drawError) {
                        pE_Common.log("Web:Media", `Fehler beim drawImage: ${drawError.message}`, "error");
                        return pE_Common.toScalar(null, false, `Canvas-Fehler: ${drawError.message}`);
                    }
                }

                // 7. RESTLICHE VERARBEITUNG
                pE_Common.log("Web:Media", "Optimiere Bild...", "log");
                const mainDataUrl = this._optimizeImageOnCanvas(bufferCanvas, imageSize, imageQuality, cropToSquare);
                const thumbSize = thumbnailSize || 64;
                const thumbnailDataUrl = this._optimizeImageOnCanvas(bufferCanvas, thumbSize, imageQuality, true);

                if (dotNetHelper) {
                    pE_Common.log("Web:Media", "Sende Daten an Blazor...", "log");
                    await dotNetHelper.invokeMethodAsync("SetOptimizedImageData", mainDataUrl, thumbnailDataUrl);
                }

                const bytes = this._dataURLToBytes(mainDataUrl);
                pE_Common.log("Web:Media", `=== ERFOLG === ${bytes.length} Bytes`, "success");

                return pE_Common.toScalar(null, true, null, bytes);

            } catch (e) {
                pE_Common.log("Web:Media", "=== CAPTURE PHOTO FEHLGESCHLAGEN ===", "error");
                pE_Common.log("Web:Media", `Fehler: ${e.message}`, "error");
                pE_Common.log("Web:Media", `Stack: ${e.stack}`, "debug");
                return pE_Common.toScalar(null, false, `Kritischer Fehler: ${e.message}`);
            }
        },

        // NEUE METHODE: Direkte Stream-Extraktion
        _captureFromStreamDirectly: async function (stream, dotNetHelper, imageSize, imageQuality, cropToSquare, thumbnailSize) {
            pE_Common.log("Web:Media", "Starte direkte Stream-Extraktion", "warn");

            try {
                // ImageCapture API (modernere Browser)
                if (window.ImageCapture) {
                    const track = stream.getVideoTracks()[0];
                    const imageCapture = new ImageCapture(track);

                    const bitmap = await imageCapture.grabFrame();
                    const canvas = document.createElement('canvas');
                    canvas.width = bitmap.width;
                    canvas.height = bitmap.height;
                    const ctx = canvas.getContext('2d');
                    ctx.drawImage(bitmap, 0, 0);

                    pE_Common.log("Web:Media", `Direkte Extraktion via ImageCapture: ${bitmap.width}x${bitmap.height}`, "success");

                    const mainDataUrl = this._optimizeImageOnCanvas(canvas, imageSize, imageQuality, cropToSquare);
                    const thumbnailDataUrl = this._optimizeImageOnCanvas(canvas, thumbnailSize || 64, imageQuality, true);

                    if (dotNetHelper) {
                        await dotNetHelper.invokeMethodAsync("SetOptimizedImageData", mainDataUrl, thumbnailDataUrl);
                    }

                    const bytes = this._dataURLToBytes(mainDataUrl);
                    return pE_Common.toScalar(null, true, null, bytes);
                }

                throw new Error("ImageCapture API nicht verfügbar");

            } catch (e) {
                pE_Common.log("Web:Media", `Direkte Stream-Extraktion fehlgeschlagen: ${e.message}`, "error");
                throw e;
            }
        },

        // VERBESSERTE waitForFirstVideoFrame
        waitForFirstVideoFrame: async function (videoElementId, timeoutMs = 3000) {
            const startTime = Date.now();
            pE_Common.log("Web:Media", `Warte auf ersten Video-Frame (Timeout: ${timeoutMs}ms)`, "log");

            const video = document.getElementById(videoElementId);
            if (!video) {
                throw new Error(`Video-Element '${videoElementId}' nicht gefunden`);
            }

            // Log alle Eigenschaften
            pE_Common.log("Web:Media", `Video-Status vor wait:
              - videoWidth: ${video.videoWidth}
              - videoHeight: ${video.videoHeight}
              - readyState: ${video.readyState}
              - currentTime: ${video.currentTime}
              - paused: ${video.paused}`, "debug");

            // SCHNELLPRÜFUNG: Wenn Video bereits ready ist, sofort zurück
            if (video.readyState >= 2 && video.videoWidth > 0) {
                pE_Common.log("Web:Media", `Video bereits ready: ${video.videoWidth}x${video.videoHeight}`, "success");
                return;
            }

            // Moderne Browser mit requestVideoFrameCallback
            if (video.requestVideoFrameCallback) {
                return new Promise((resolve, reject) => {
                    let callbackFired = false;
                    let fallbackTimer = null;

                    // Fallback-Funktion für Timeout
                    const fallbackCheck = () => {
                        if (!callbackFired) {
                            pE_Common.log("Web:Media", "requestVideoFrameCallback Timeout - nutze Fallback", "warn");
                            if (video.readyState >= 2 && video.videoWidth > 0) {
                                pE_Common.log("Web:Media", "Fallback: Video ist trotzdem ready", "success");
                                resolve();
                            } else {
                                reject(new Error("Timeout bei requestVideoFrameCallback"));
                            }
                        }
                    };

                    // Timeout für requestVideoFrameCallback (kürzer als Gesamt-Timeout)
                    const rvfcTimeout = setTimeout(fallbackCheck, 1500);

                    // Fallback-Timer für generellen Timeout
                    fallbackTimer = setTimeout(fallbackCheck, timeoutMs);

                    video.requestVideoFrameCallback((now, metadata) => {
                        if (callbackFired) return;
                        callbackFired = true;

                        clearTimeout(rvfcTimeout);
                        clearTimeout(fallbackTimer);

                        pE_Common.log("Web:Media", `Frame empfangen via requestVideoFrameCallback! 
                          - Jetzt: ${now}
                          - MediaTime: ${metadata.mediaTime}
                          - Width: ${metadata.width}
                          - Height: ${metadata.height}
                          - PresentationTime: ${metadata.presentationTime}`, "success");

                        resolve();
                    });

                    // Falls Video pausiert, versuche zu spielen
                    if (video.paused) {
                        video.play().catch(e => {
                            pE_Common.log("Web:Media", `Play im wait: ${e.message}`, "debug");
                        });
                    }
                });
            }

            // Fallback für ältere Browser
            pE_Common.log("Web:Media", "Nutze readyState-Fallback", "debug");

            let lastState = '';
            while (true) {
                const currentState = `w:${video.videoWidth}, h:${video.videoHeight}, ready:${video.readyState}`;
                if (currentState !== lastState) {
                    pE_Common.log("Web:Media", `Status-Update: ${currentState}`, "debug");
                    lastState = currentState;
                }

                if (video.readyState >= 2 && video.videoWidth > 0) { // Geändert auf >= 2
                    pE_Common.log("Web:Media", `Frame verfügbar: ${video.videoWidth}x${video.videoHeight}`, "success");
                    return;
                }

                if (Date.now() - startTime > timeoutMs) {
                    // PRÜFE NOCHMALS BEVOR WIR EINEN FEHLER WERFEN
                    if (video.readyState >= 2 && video.videoWidth > 0) {
                        pE_Common.log("Web:Media", "Timeout, aber Video ist jetzt ready", "warn");
                        return;
                    }
                    pE_Common.log("Web:Media", `Timeout nach ${timeoutMs}ms - Video nicht ready`, "error");
                    throw new Error("Timeout: Kein renderbarer Video-Frame verfügbar");
                }

                await new Promise(r => setTimeout(r, 50));
            }
        },

        // In deiner Media-JS Logik
        pickPhotoVoid: function (
            fileInput,
            dotNetHelper,
            imageSize,
            imageQuality,
            cropToSquare,
            thumbnailSize
        ) {
            // NICHT awaiten
            this.pickPhoto(
                fileInput,
                dotNetHelper,
                imageSize,
                imageQuality,
                cropToSquare,
                thumbnailSize
            );

            // sofort zurück -> SignalR happy
            return;
        },

        pickPhoto: function (
            fileInput,
            dotNetHelper,
            imageSize,
            imageQuality,
            cropToSquare,
            thumbnailSize
        ) {
            pE_Common.log("Web:Media", "pickPhoto: Warte auf Dateiauswahl...", "log");

            if (!fileInput || !dotNetHelper) {
                pE_Common.log("Web:Media", "pickPhoto Abbruch: Parameter fehlen", "error");
                return;
            }

            fileInput.value = "";

            fileInput.onchange = async (evt) => {
                try {
                    const file = evt.target.files?.[0];
                    if (!file) {
                        pE_Common.log("Web:Media", "Dateiauswahl abgebrochen", "warn");
                        await dotNetHelper.invokeMethodAsync("SetOptimizedImageCanceled");
                        return;
                    }

                    pE_Common.log("Web:Media", `Datei gewählt: ${file.name}`, "log");

                    const img = await this._fileToImage(file);
                    const mainBase64 = this._optimizeImageOnCanvas(
                        img, imageSize, imageQuality, cropToSquare
                    );

                    const thumbSize = thumbnailSize || 64;
                    const thumbnailBase64 = this._optimizeImageOnCanvas(
                        img, thumbSize, imageQuality, cropToSquare
                    );

                    await dotNetHelper.invokeMethodAsync(
                        "SetOptimizedImageData",
                        mainBase64,
                        thumbnailBase64
                    );
                }
                catch (err) {
                    await dotNetHelper.invokeMethodAsync(
                        "SetOptimizedImageError",
                        err.message
                    );
                }
                finally {
                    fileInput.value = "";
                    fileInput.onchange = null;
                }
            };

            fileInput.click();
        },       

        startCamera: async function (videoElementId) {
            pE_Common.log("Web:Media", `Kamera-Anfrage für Element: ${videoElementId}`, "log");

            // Kleiner Delay mit JS-Mitteln, damit das Modal-DOM bereit ist
            await new Promise(resolve => setTimeout(resolve, 150));

            const video = document.getElementById(videoElementId);
            if (!video) {
                pE_Common.log("Web:Media", `Element ${videoElementId} nicht gefunden.`, "error");
                return;
            }

            try {
                // Stream anfordern
                const stream = await navigator.mediaDevices.getUserMedia({
                    video: {
                        facingMode: "environment",
                        width: { ideal: 1280 },
                        height: { ideal: 720 }
                    },
                    audio: false
                });

                video.srcObject = stream;

                // Warten, bis die Metadaten geladen sind (verhindert 0x0 Pixel Fehler)
                return new Promise((resolve, reject) => {
                    video.onloadedmetadata = async () => {
                        try {
                            await video.play();
                            pE_Common.log("Web:Media", "Kamera-Stream aktiv.");
                            resolve();
                        } catch (playErr) {
                            pE_Common.log("Web:Media", "video.play() fehlgeschlagen", "error", playErr);
                            reject(playErr);
                        }
                    };
                    video.onerror = (e) => reject(new Error("Video-Element Fehler"));
                });

            } catch (err) {
                pE_Common.log("Web:Media", "getUserMedia Fehler", "error", err);
                throw err; // Reicht den Fehler an Blazor's try-catch weiter 
            }
        },

        stopCamera: function (videoElementId) {
            const video = document.getElementById(videoElementId);
            if (!video) return;
            const stream = video.srcObject;
            if (stream && typeof stream.getTracks === "function") {
                stream.getTracks().forEach(t => {
                    t.stop();
                    pE_Common.log("Web:Media", `Track gestoppt: ${t.kind}`, "log");
                });
            }
            video.srcObject = null;
            pE_Common.log("Web:Media", "Kamera-Stream beendet.");
        },

        clickElement: function (element) {
            if (element) {
                pE_Common.log("Web:Media", "Programmgesteuerter Klick auf Element", "log");
                element.click();
            }
        }
    },

    // ===== NOTIFICATIONS MODUL =====
    notifications: {
        // --- Konfiguration (Original übernommen) ---
        _debugMode: true,
        _scheduled: new Map(),

        _log: function (message, data) {
            if (this._debugMode) {
                console.log(`[Notification DEBUG]: ${message}`, data || '');
            }
        },

        requestPermission: async function () {
            pE_Common.log("Anfrage für Benachrichtigungs-Berechtigungen (Browser)...");
            try {
                let status = 'denied';

                if (typeof Notification !== 'undefined') {
                    // Web-Standard: User-Dialog öffnen
                    status = await Notification.requestPermission();
                } else {
                    pE_Common.log("Browser unterstützt keine Notifications.", "error");
                    return { out_value_bool: false, out_value_str: "not_supported", out_err: "Notification API not found" };
                }

                pE_Common.log(`Ergebnis der Berechtigungsanfrage: ${status}`);

                // WICHTIG: Manuelle Rückgabe statt toScalar, um Logik-Konflikte zu vermeiden
                // Das entspricht exakt dem Typ ScalarModel in C#
                //return {
                //    out_value_bool: status === 'granted', // Dies ist das 'true' für dein C# If
                //    out_value_str: status,               // 'granted', 'denied' etc.
                //    out_err: ""                          // Leer lassen, damit es kein Error ist
                //};
                let result = pE_Common.toScalar(status, true, "");
                result.out_value_bool = (status === 'granted');
                return result;

            } catch (e) {
                pE_Common.log("Fehler in requestPermission", "error", e);
                return {
                    out_value_bool: false,
                    out_value_str: "error",
                    out_err: e.message
                };
            }
        },

        scheduleNotification: function (id, title, body, targetUnixSeconds) {
            // 1. Logge den empfangenen Timestamp statt der Verzögerung
            pE_Common.log(`Planung empfangen. ID: ${id}, Ziel-Unix: ${targetUnixSeconds}`);

            try {
                if (this._scheduled.has(id)) {
                    pE_Common.log(`Existierende Benachrichtigung mit ID '${id}' gefunden. Timer wird ersetzt.`);
                    clearTimeout(this._scheduled.get(id));
                }

                // 2. LOKALE BERECHNUNG: Jetzt gegen die Browser-Uhrzeit prüfen
                // Date.now() liefert Millisekunden, targetUnixSeconds sind Sekunden
                const nowMs = Date.now();
                const targetMs = targetUnixSeconds * 1000;
                const delay = targetMs - nowMs;

                // 3. Sicherheitscheck: Liegt der Termin in der Vergangenheit?
                if (delay <= 0) {
                    pE_Common.log(`Zeitpunkt für ID '${id}' liegt in der Vergangenheit (${delay}ms). Timer wird ignoriert.`);
                    return pE_Common.toScalar("Expired/Past", true, "");
                }

                // 4. Den Timer mit der lokal berechneten Verzögerung starten
                const timeoutId = setTimeout(() => {
                    pE_Common.log(`Timer für ID '${id}' abgelaufen. Prüfe Berechtigung...`);

                    if (Notification.permission === "granted") {
                        new Notification(title, { body: body });
                        pE_Common.log(`Notification angezeigt: ${id}`);
                    } else {
                        pE_Common.log(`Keine Berechtigung für ID: ${id}`);
                    }
                    this._scheduled.delete(id);
                }, delay);

                this._scheduled.set(id, timeoutId);
                pE_Common.log(`Timer gesetzt. Erscheint in ${Math.round(delay / 1000)} Sekunden.`);

                return pE_Common.toScalar("Timer set", true, "");
            } catch (e) {
                pE_Common.log(`Fehler in JS-Schedule: ${e.message}`);
                return pE_Common.toScalar(null, false, e.message);
            }
        },

        removeScheduledNotification: function (id) {
            pE_Common.log(`Versuch, geplante Benachrichtigung mit ID abzubrechen: ${id}`);
            if (this._scheduled.has(id)) {
                clearTimeout(this._scheduled.get(id));
                this._scheduled.delete(id);
                pE_Common.log(`Geplante Benachrichtigung mit ID '${id}' abgebrochen.`);
                return pE_Common.toScalar("Cancelled", true, "");
            } else {
                pE_Common.log(`Benachrichtigung mit ID '${id}' nicht in der geplanten Liste gefunden.`);
                return pE_Common.toScalar("Not found", true, "");
            }
        },

        getPendingIds: function () {
            // Gibt ein Array aller IDs zurück, für die aktuell ein Timer läuft
            const ids = Array.from(this._scheduled.keys());
            return pE_Common.toScalar(JSON.stringify(ids), true, "");
        },

        removeAllScheduledNotifications: function () {
            pE_Common.log("Versuch, alle geplanten Benachrichtigungen abzubrechen.");
            const count = this._scheduled.size;
            for (const timeoutId of this._scheduled.values()) {
                clearTimeout(timeoutId);
            }
            this._scheduled.clear();
            pE_Common.log(`Alle ${count} geplanten Benachrichtigungen abgebrochen.`);
            return pE_Common.toScalar(`All ${count} cleared`, true, "");
        },

        dispose: function () {
            pE_Common.log("Notifications", "Dispose: Alle geplanten Timer löschen.");

            for (const timeoutId of this._scheduled.values()) {
                clearTimeout(timeoutId);
            }
            this._scheduled.clear();
        }
    },

    // ===== SECURITY MODUL (Web/PWA) =====
    security: {
        /**
         * Entschlüsselt einen Pepper-String.
         * @param {string} encryptedBase64 - Der verschlüsselte Pepper.
         * @param {Uint8Array} key - Der abgeleitete Schlüssel.
         */
        // window.pE_Web.security
        decryptPepper: async function (encryptedBase64, keyBase64) {
            pE_Common.log("Web:Security", "decryptPepper (AES-GCM) gestartet (Base64 Mode)");

            try {
                // Validierung der Inputs
                if (!encryptedBase64 || !keyBase64) {
                    pE_Common.log("Web:Security", "Abbruch: encryptedBase64 oder keyBase64 fehlt", "error");
                    return pE_Common.toScalar("", false, "Missing input data");
                }

                // 1. Inputs von Base64 zu Bytes
                const encryptedBytes = pE_Common.base64ToBytes(encryptedBase64);
                const keyBytes = pE_Common.base64ToBytes(keyBase64);

                pE_Common.log("Web:Security", `Inputs konvertiert. Encrypted-Len: ${encryptedBytes.length}, Key-Len: ${keyBytes.length}`);

                if (encryptedBytes.length < 12 + 16) {
                    pE_Common.log("Web:Security", "Abbruch: Verschlüsselte Daten zu kurz (min 28 Bytes)", "error");
                    return pE_Common.toScalar("", false, "Invalid encrypted data length");
                }

                // 2. Zerlegen (Nonce | Ciphertext | Tag)
                const nonce = encryptedBytes.slice(0, 12);
                const tag = encryptedBytes.slice(-16);
                const ciphertext = encryptedBytes.slice(12, -16);

                pE_Common.log("Web:Security", `Struktur: Nonce(12), Tag(16), Ciphertext(${ciphertext.length})`);

                // Web Crypto API braucht Ciphertext + Tag kombiniert
                const dataToDecrypt = new Uint8Array(ciphertext.length + tag.length);
                dataToDecrypt.set(ciphertext);
                dataToDecrypt.set(tag, ciphertext.length);

                // 3. Key importieren
                pE_Common.log("Web:Security", "Importiere CryptoKey...");
                const cryptoKey = await crypto.subtle.importKey(
                    "raw", keyBytes, { name: "AES-GCM" }, false, ["decrypt"]
                );

                // 4. Entschlüsseln
                pE_Common.log("Web:Security", "Starte SubtleCrypto.decrypt...");
                const decryptedBuffer = await crypto.subtle.decrypt(
                    { name: "AES-GCM", iv: nonce, tagLength: 128 },
                    cryptoKey,
                    dataToDecrypt
                );

                // --- Konvertierung zu Base64 ---
                const decryptedBytes = new Uint8Array(decryptedBuffer);
                pE_Common.log("Web:Security", `Entschlüsselung erfolgreich. Ergebnis-Bytes: ${decryptedBytes.length}`);

                let binaryString = "";
                for (let i = 0; i < decryptedBytes.length; i++) {
                    binaryString += String.fromCharCode(decryptedBytes[i]);
                }
                const resultBase64 = btoa(binaryString);

                // WICHTIG: Loggen, ob wir tatsächlich einen String generiert haben
                if (resultBase64 && resultBase64.length > 0) {
                    pE_Common.log("Web:Security", `Ergebnis-Base64 erstellt (Länge: ${resultBase64.length})`);
                } else {
                    pE_Common.log("Web:Security", "WARNUNG: Ergebnis-Base64 ist leer!", "warn");
                }

                // Wir geben den Pepper als STRING zurück (out_value_str)
                return pE_Common.toScalar(resultBase64, true, "");

            } catch (err) {
                pE_Common.log("Web:Security", "Decryption failed (Catch)", "error", {
                    message: err.message,
                    stack: err.stack
                });
                return pE_Common.toScalar("", false, "Decryption failed: " + err.message);
            }
        },

        /**
         * PBKDF2 FALLBACK (WASM PERFORMANCE):
         * Diese Methode dient als High-Performance-Ersatz für die C# Key-Ableitung.
         * Falls die Iterationszahlen in WASM (C#) zu langsam werden, kann diese native 
         * Browser-Implementierung (SubtleCrypto) genutzt werden.
         * * ACHTUNG BEI REAKTIVIERUNG:
         * 1. Iterationszahl mit C# synchronisieren (aktuell 10.000 vs 1.000).
         * 2. Rückgabe muss auf Base64-String umgestellt werden (toScalar(btoa(...))),
         * da binäre Uint8Arrays beim Transport zu Blazor oft zu Länge 0 führen.
         */
        deriveKeyWasm: async function (hashedInputBase64, saltBase64) {
            pE_Common.log("Web:Security", "deriveKeyWasm (PBKDF2) gestartet");

            try {
                // 1. Validierung und Konvertierung der Inputs
                if (!hashedInputBase64 || !saltBase64) {
                    pE_Common.log("Web:Security", "Abbruch: hashedInput oder salt fehlt", "error");
                    return pE_Common.toScalar("", false, "Missing input data for key derivation");
                }

                const hashedInput = pE_Common.base64ToBytes(hashedInputBase64);
                const salt = pE_Common.base64ToBytes(saltBase64);

                pE_Common.log("Web:Security", `Inputs konvertiert. Input-Len: ${hashedInput.length}, Salt-Len: ${salt.length}`);

                // 2. Basis-Key für PBKDF2 importieren
                pE_Common.log("Web:Security", "Importiere PBKDF2 BaseKey...");
                const baseKey = await crypto.subtle.importKey(
                    "raw",
                    hashedInput,
                    "PBKDF2",
                    false,
                    ["deriveBits"]
                );

                // 3. Bits ableiten (PBKDF2-SHA512)
                pE_Common.log("Web:Security", "Starte deriveBits (Iterations: 1000, Hash: SHA-512)...");
                const derivedBits = await crypto.subtle.deriveBits(
                    {
                        name: "PBKDF2",
                        salt: salt,
                        iterations: 1000, // Muss exakt mit C# CreateDerivedKey übereinstimmen
                        hash: "SHA-512"
                    },
                    baseKey,
                    256 // 256 Bits = 32 Bytes
                );

                // 4. Umwandlung in Base64 String für C#
                const derivedBytes = new Uint8Array(derivedBits);
                pE_Common.log("Web:Security", `Ableitung erfolgreich. Resultat: ${derivedBytes.length} Bytes`);

                let binaryString = "";
                for (let i = 0; i < derivedBytes.length; i++) {
                    binaryString += String.fromCharCode(derivedBytes[i]);
                }
                const resultBase64 = btoa(binaryString);

                if (resultBase64 && resultBase64.length > 0) {
                    pE_Common.log("Web:Security", `derivedKey erfolgreich als Base64 erstellt (Länge: ${resultBase64.length})`);
                } else {
                    pE_Common.log("Web:Security", "WARNUNG: derivedKey Base64 ist leer!", "warn");
                }

                return pE_Common.toScalar(resultBase64, true, "");

            } catch (err) {
                pE_Common.log("Web:Security", "deriveKeyWasm fehlgeschlagen", "error", {
                    message: err.message,
                    stack: err.stack
                });
                return pE_Common.toScalar("", false, "Derivation failed: " + err.message);
            }
        },

        hashPbkdf2: async function (plainText, saltBase64, pepperBase64) {
            pE_Common.log("Web:Security", "hashPbkdf2 (PHC-Generation) gestartet");
            try {
                // 1. Input-Validierung
                if (!plainText) {
                    pE_Common.log("Web:Security", "Abbruch: plainText ist leer", "error");
                    return pE_Common.toScalar("", false, "Plaintext missing");
                }
                if (!saltBase64 || !pepperBase64) {
                    pE_Common.log("Web:Security", `Abbruch: Salt oder Pepper fehlt (Salt: ${!!saltBase64}, Pepper: ${!!pepperBase64})`, "error");
                    return pE_Common.toScalar("", false, "Salt or Pepper missing");
                }

                const enc = new TextEncoder();
                const plainBytes = enc.encode(plainText);

                // Konvertierung
                const salt = pE_Common.base64ToBytes(saltBase64);
                const pepper = pE_Common.base64ToBytes(pepperBase64);

                pE_Common.log("Web:Security", `Inputs konvertiert. Plain-Bytes: ${plainBytes.length}, Salt-Bytes: ${salt.length}, Pepper-Bytes: ${pepper.length}`);

                // 2. KeyMaterial erstellen (Passwort + Pepper)
                const combinedKeyMaterial = new Uint8Array(plainBytes.length + pepper.length);
                combinedKeyMaterial.set(plainBytes);
                combinedKeyMaterial.set(pepper, plainBytes.length);

                pE_Common.log("Web:Security", "Importiere KeyMaterial für PBKDF2...");
                const keyMaterial = await crypto.subtle.importKey(
                    "raw",
                    combinedKeyMaterial,
                    "PBKDF2",
                    false,
                    ["deriveBits"]
                );

                // 3. PBKDF2 Hashing
                const iterations = 10000;
                pE_Common.log("Web:Security", `Starte deriveBits (${iterations} iterations, SHA-512)...`);

                const derivedBits = await crypto.subtle.deriveBits(
                    {
                        name: "PBKDF2",
                        salt: salt,
                        iterations: iterations,
                        hash: "SHA-512"
                    },
                    keyMaterial,
                    256 // 32 Bytes Ergebnis
                );

                const hashBytes = new Uint8Array(derivedBits);
                pE_Common.log("Web:Security", `Hashing erfolgreich. Hash-Bytes: ${hashBytes.length}`);

                // 4. Konvertierung zu Base64
                let binaryHash = "";
                for (let i = 0; i < hashBytes.length; i++) {
                    binaryHash += String.fromCharCode(hashBytes[i]);
                }
                const hashBase64 = btoa(binaryHash);

                // 5. PHC-String zusammenbauen
                const phcString = `$pbkdf2-sha512$i=${iterations}$${saltBase64}$${hashBase64}`;

                pE_Common.log("Web:Security", "PHC-String erfolgreich generiert");

                // Optional: Nur die Länge loggen, um das Passwort-Derivat nicht im Klartext-Log zu haben, 
                // aber zu sehen, dass etwas da ist.
                pE_Common.log("Web:Security", `PHC-Result-Length: ${phcString.length}`);

                return pE_Common.toScalar(phcString, true, "");

            } catch (err) {
                pE_Common.log("Web:Security", "hashPbkdf2 fehlgeschlagen", "error", {
                    message: err.message,
                    stack: err.stack
                });
                return pE_Common.toScalar("", false, "Hashing failed: " + err.message);
            }
        },

        // Innerhalb von window.pE_Web.security:
        decryptAesGcm: async function (encryptedBase64, keyBase64, associatedDataBase64 = null) {
            pE_Common.log("Web:Security", "decryptAesGcm (Standard) aufgerufen");
            try {
                // 1. Validierung der Basiseingaben
                if (!encryptedBase64 || !keyBase64) {
                    pE_Common.log("Web:Security", "Abbruch: encryptedBase64 oder keyBase64 fehlt", "error");
                    return pE_Common.toScalar("", false, "Missing input data for AES decryption");
                }

                // Import der Daten
                const data = pE_Common.base64ToBytes(encryptedBase64);
                const keyBytes = pE_Common.base64ToBytes(keyBase64);

                pE_Common.log("Web:Security", `Inputs konvertiert. Total-Data-Len: ${data.length}, Key-Len: ${keyBytes.length}`);

                if (data.length < 12 + 16) {
                    pE_Common.log("Web:Security", "Abbruch: Daten zu kurz für AES-GCM (min 28 Bytes)", "error");
                    return pE_Common.toScalar("", false, "Invalid encrypted data length");
                }

                // 2. Zerlegen: Nonce (12) | Ciphertext | Tag (16)
                const nonce = data.slice(0, 12);
                const tag = data.slice(-16);
                const ciphertext = data.slice(12, -16);

                pE_Common.log("Web:Security", `Struktur extrahiert: Nonce(12), Tag(16), Ciphertext(${ciphertext.length})`);

                // Web Crypto API braucht Ciphertext + Tag kombiniert im Buffer
                const dataToDecrypt = new Uint8Array(ciphertext.length + tag.length);
                dataToDecrypt.set(ciphertext);
                dataToDecrypt.set(tag, ciphertext.length);

                // 3. Key importieren
                pE_Common.log("Web:Security", "Importiere AES-GCM Key...");
                const cryptoKey = await crypto.subtle.importKey(
                    "raw",
                    keyBytes,
                    { name: "AES-GCM" },
                    false,
                    ["decrypt"]
                );

                const decryptOptions = {
                    name: "AES-GCM",
                    iv: nonce,
                    tagLength: 128
                };

                // 4. Associated Data (AAD) verarbeiten
                if (associatedDataBase64) {
                    const adBytes = pE_Common.base64ToBytes(associatedDataBase64);
                    decryptOptions.additionalData = adBytes;
                    pE_Common.log("Web:Security", `Associated Data (AAD) hinzugefügt. Länge: ${adBytes.length}`);
                } else {
                    pE_Common.log("Web:Security", "Keine Associated Data (AAD) vorhanden.");
                }

                // 5. Entschlüsseln
                pE_Common.log("Web:Security", "Starte SubtleCrypto.decrypt...");
                const decryptedBuffer = await crypto.subtle.decrypt(
                    decryptOptions,
                    cryptoKey,
                    dataToDecrypt
                );

                // 6. Rückgabe als UTF-8 String (z.B. für Base32 Secrets)
                const plaintext = new TextDecoder().decode(decryptedBuffer);

                pE_Common.log("Web:Security", `AES-GCM Decryption erfolgreich. Plaintext-Länge: ${plaintext.length}`);

                // Sicherheit: Wir loggen nicht den Plaintext selbst, aber ob er erfolgreich erstellt wurde
                return pE_Common.toScalar(plaintext, true, "");

            } catch (err) {
                pE_Common.log("Web:Security", "AES-GCM Decryption fehlgeschlagen (Catch)", "error", {
                    message: err.message,
                    stack: err.stack
                });
                return pE_Common.toScalar("", false, "Decryption failed: " + err.message);
            }
        },

        // Innerhalb von window.pE_Web.security:
        encryptAesGcm: async function (plaintext, keyBase64, associatedDataBase64 = null) {
            pE_Common.log("Web:Security", "encryptAesGcm (Standard) gestartet");
            try {
                // 1. Validierung der Eingaben
                if (plaintext === null || plaintext === undefined) {
                    pE_Common.log("Web:Security", "Abbruch: Plaintext ist null/undefined", "error");
                    return pE_Common.toScalar("", false, "Plaintext missing");
                }
                if (!keyBase64) {
                    pE_Common.log("Web:Security", "Abbruch: keyBase64 fehlt", "error");
                    return pE_Common.toScalar("", false, "Key missing");
                }

                const enc = new TextEncoder();
                const plaintextBytes = enc.encode(plaintext);
                const keyBytes = pE_Common.base64ToBytes(keyBase64);

                pE_Common.log("Web:Security", `Inputs konvertiert. Plain-Bytes: ${plaintextBytes.length}, Key-Bytes: ${keyBytes.length}`);

                // 2. Nonce generieren (12 Bytes für AES-GCM Standard)
                const nonce = crypto.getRandomValues(new Uint8Array(12));
                pE_Common.log("Web:Security", "Random Nonce (12 Bytes) generiert");

                // 3. Key importieren
                pE_Common.log("Web:Security", "Importiere AES-GCM Key für Verschlüsselung...");
                const cryptoKey = await crypto.subtle.importKey(
                    "raw", keyBytes, { name: "AES-GCM" }, false, ["encrypt"]
                );

                // 4. Konfiguration
                const encryptOptions = {
                    name: "AES-GCM",
                    iv: nonce,
                    tagLength: 128 // 16 Bytes Auth Tag
                };

                // Associated Data (AAD) verarbeiten falls vorhanden
                if (associatedDataBase64) {
                    const adBytes = pE_Common.base64ToBytes(associatedDataBase64);
                    encryptOptions.additionalData = adBytes;
                    pE_Common.log("Web:Security", `Associated Data (AAD) hinzugefügt. Länge: ${adBytes.length}`);
                }

                // 5. Verschlüsseln
                pE_Common.log("Web:Security", "Starte SubtleCrypto.encrypt...");
                const encryptedBuffer = await crypto.subtle.encrypt(
                    encryptOptions,
                    cryptoKey,
                    plaintextBytes
                );

                const encryptedBytes = new Uint8Array(encryptedBuffer);
                pE_Common.log("Web:Security", `Verschlüsselung erfolgreich. Ciphertext+Tag: ${encryptedBytes.length} Bytes`);

                // 6. Ergebnis zusammenbauen: Nonce (12) + [Ciphertext + Tag]
                const combined = new Uint8Array(nonce.length + encryptedBytes.length);
                combined.set(nonce, 0);
                combined.set(encryptedBytes, nonce.length);

                pE_Common.log("Web:Security", `Combined Package erstellt. Gesamt-Bytes: ${combined.length}`);

                // 7. Sichere Konvertierung zu Base64
                let binaryString = "";
                for (let i = 0; i < combined.length; i++) {
                    binaryString += String.fromCharCode(combined[i]);
                }
                const base64Result = btoa(binaryString);

                if (base64Result && base64Result.length > 0) {
                    pE_Common.log("Web:Security", `Verschlüsselung abgeschlossen. Result-Base64 Länge: ${base64Result.length}`);
                } else {
                    pE_Common.log("Web:Security", "WARNUNG: Result-Base64 ist leer!", "warn");
                }

                return pE_Common.toScalar(base64Result, true, "");

            } catch (err) {
                pE_Common.log("Web:Security", "AES-GCM Encryption fehlgeschlagen (Catch)", "error", {
                    message: err.message,
                    stack: err.stack
                });
                return pE_Common.toScalar("", false, "Encryption failed: " + err.message);
            }
        },

        /**
         * Leitet deterministisch Bytes ab. 
         * @param {string} userInput - Klartext Eingabe
         * @param {number} iterations - PBKDF2 Iterationen
         */
        deriveBytesPbkdf2: async function (userInput, iterations, length = 32) {
            pE_Common.log("Web:Security", "deriveBytesPbkdf2 (Key Derivation) gestartet");
            try {
                // 1. Validierung der Eingaben
                if (!userInput) {
                    pE_Common.log("Web:Security", "Abbruch: userInput ist leer", "error");
                    return pE_Common.toScalar("", false, "User input missing");
                }

                pE_Common.log("Web:Security", `Parameter: Iterations=${iterations}, Target-Length=${length} Bytes`);

                const enc = new TextEncoder();
                const userInputBytes = enc.encode(userInput);

                // 2. KeyMaterial importieren
                pE_Common.log("Web:Security", "Importiere PBKDF2 KeyMaterial...");
                const keyMaterial = await crypto.subtle.importKey(
                    "raw",
                    userInputBytes,
                    "PBKDF2",
                    false,
                    ["deriveBits"]
                );

                // 3. Bits ableiten
                // Hinweis: Wir nutzen hier einen leeren Salt (0 Bytes), 
                // das muss in C# (Rfc2898DeriveBytes) exakt so eingestellt sein!
                pE_Common.log("Web:Security", "Starte deriveBits (Salt: 0 bytes, Hash: SHA-256)...");
                const derivedBits = await crypto.subtle.deriveBits(
                    {
                        name: "PBKDF2",
                        salt: new Uint8Array(0),
                        iterations: iterations,
                        hash: "SHA-256"
                    },
                    keyMaterial,
                    length * 8 // Umrechnung von Bytes in Bits
                );

                // 4. Konvertierung zu Base64 für C#
                const derivedBytes = new Uint8Array(derivedBits);
                pE_Common.log("Web:Security", `Ableitung erfolgreich. Resultat: ${derivedBytes.length} Bytes`);

                let binaryString = "";
                for (let i = 0; i < derivedBytes.length; i++) {
                    binaryString += String.fromCharCode(derivedBytes[i]);
                }
                const resultBase64 = btoa(binaryString);

                if (resultBase64 && resultBase64.length > 0) {
                    pE_Common.log("Web:Security", `Result-Base64 erstellt (Länge: ${resultBase64.length})`);
                } else {
                    pE_Common.log("Web:Security", "WARNUNG: Result-Base64 ist leer!", "warn");
                }

                return pE_Common.toScalar(resultBase64, true, "");

            } catch (err) {
                pE_Common.log("Web:Security", "deriveBytesPbkdf2 fehlgeschlagen (Catch)", "error", {
                    message: err.message,
                    stack: err.stack
                });
                return pE_Common.toScalar("", false, "Derivation failed: " + err.message);
            }
        },

        /**
         * Verschlüsselt mit einer FESTEN Nonce (Deterministisch).
         */
        encryptAesGcmDeterministic: async function (plainTextBase64, keyBase64, fixedNonceBase64) {
            pE_Common.log("Web:Security", "encryptAesGcmDeterministic gestartet (Base64 Mode)");
            try {
                // 1. Validierung der Inputs
                if (!plainTextBase64 || !keyBase64 || !fixedNonceBase64) {
                    pE_Common.log("Web:Security", "Abbruch: Einer der Parameter (Plaintext, Key, Nonce) ist leer", "error");
                    return pE_Common.toScalar("", false, "Missing input data for deterministic encryption");
                }

                // Sicherer Import aller binären Daten aus Base64
                const plaintextBytes = pE_Common.base64ToBytes(plainTextBase64);
                const keyBytes = pE_Common.base64ToBytes(keyBase64);
                const fixedNonce = pE_Common.base64ToBytes(fixedNonceBase64);

                pE_Common.log("Web:Security", `Inputs konvertiert. Plain-Bytes: ${plaintextBytes.length}, Key-Bytes: ${keyBytes.length}, Nonce-Bytes: ${fixedNonce.length}`);

                // 2. Key importieren
                pE_Common.log("Web:Security", "Importiere AES-GCM Key...");
                const cryptoKey = await crypto.subtle.importKey(
                    "raw", keyBytes, { name: "AES-GCM" }, false, ["encrypt"]
                );

                // 3. Verschlüsseln
                // Hinweis: Hier wird KEINE zufällige Nonce generiert, sondern die übergebene 'fixedNonce' genutzt
                pE_Common.log("Web:Security", "Starte SubtleCrypto.encrypt (Deterministic)...");
                const encryptedBuffer = await crypto.subtle.encrypt(
                    { name: "AES-GCM", iv: fixedNonce, tagLength: 128 },
                    cryptoKey,
                    plaintextBytes
                );

                const encryptedBytes = new Uint8Array(encryptedBuffer);
                pE_Common.log("Web:Security", `Verschlüsselung erfolgreich. Ciphertext+Tag: ${encryptedBytes.length} Bytes`);

                // 4. Ergebnis zusammenbauen: Nonce + [Ciphertext + Tag]
                const combined = new Uint8Array(fixedNonce.length + encryptedBytes.length);
                combined.set(fixedNonce, 0);
                combined.set(encryptedBytes, fixedNonce.length);

                pE_Common.log("Web:Security", `Combined Package erstellt. Gesamt-Bytes: ${combined.length}`);

                // 5. Sicherer Base64 Export
                let binaryString = "";
                for (let i = 0; i < combined.length; i++) {
                    binaryString += String.fromCharCode(combined[i]);
                }
                const base64Result = btoa(binaryString);

                if (base64Result && base64Result.length > 0) {
                    pE_Common.log("Web:Security", `Ergebnis-Base64 erstellt (Länge: ${base64Result.length})`);
                } else {
                    pE_Common.log("Web:Security", "WARNUNG: Ergebnis-Base64 ist leer!", "warn");
                }

                return pE_Common.toScalar(base64Result, true, "");

            } catch (err) {
                pE_Common.log("Web:Security", "Deterministic Encryption fehlgeschlagen", "error", {
                    message: err.message,
                    stack: err.stack
                });
                return pE_Common.toScalar("", false, "Deterministic encryption failed: " + err.message);
            }
        },

        // Innerhalb von window.pE_Web.security:
        generateEncryptedPepper: async function (keyBase64) {
            pE_Common.log("Web:Security", "generateEncryptedPepper (New Pepper) gestartet");
            try {
                // 1. Input-Check
                if (!keyBase64) {
                    pE_Common.log("Web:Security", "Abbruch: keyBase64 für Verschlüsselung fehlt", "error");
                    return pE_Common.toScalar("", false, "Key for pepper encryption missing");
                }

                const keyBytes = pE_Common.base64ToBytes(keyBase64);
                pE_Common.log("Web:Security", `Verschlüsselungs-Key importiert (${keyBytes.length} Bytes)`);

                // 2. Krypto-Operationen (Generierung)
                const pepper = crypto.getRandomValues(new Uint8Array(32));
                const nonce = crypto.getRandomValues(new Uint8Array(12));
                pE_Common.log("Web:Security", "Neuer 32-Byte Pepper und 12-Byte Nonce generiert");

                // 3. Key für AES-GCM importieren
                pE_Common.log("Web:Security", "Importiere CryptoKey...");
                const cryptoKey = await crypto.subtle.importKey(
                    "raw",
                    keyBytes,
                    { name: "AES-GCM" },
                    false,
                    ["encrypt"]
                );

                // 4. Den Pepper verschlüsseln
                pE_Common.log("Web:Security", "Verschlüssele Pepper mit AES-GCM...");
                const encryptedBuffer = await crypto.subtle.encrypt(
                    { name: "AES-GCM", iv: nonce, tagLength: 128 },
                    cryptoKey,
                    pepper
                );

                // 5. Binäre Daten zusammenführen
                const encryptedBytes = new Uint8Array(encryptedBuffer);
                const combined = new Uint8Array(nonce.length + encryptedBytes.length);
                combined.set(nonce, 0);
                combined.set(encryptedBytes, nonce.length);

                pE_Common.log("Web:Security", `Paket geschnürt: Nonce(12) + Ciphertext/Tag(${encryptedBytes.length}) = Gesamt ${combined.length} Bytes`);

                // 6. Sicherer Base64 Export (Vermeidung von Stack Overflow)
                let binaryString = "";
                for (let i = 0; i < combined.length; i++) {
                    binaryString += String.fromCharCode(combined[i]);
                }
                const finalBase64 = btoa(binaryString);

                if (finalBase64 && finalBase64.length > 0) {
                    pE_Common.log("Web:Security", `Erfolgreich: Pepper generiert und verschlüsselt (Base64-Länge: ${finalBase64.length})`);
                } else {
                    pE_Common.log("Web:Security", "WARNUNG: finalBase64 ist leer!", "warn");
                }

                // Rückgabe über deine Scalar-Hilfsfunktion
                return pE_Common.toScalar(finalBase64, true, "");

            } catch (err) {
                pE_Common.log("Web:Security", "generateEncryptedPepper fehlgeschlagen", "error", {
                    message: err.message,
                    stack: err.stack
                });
                return pE_Common.toScalar("", false, "Pepper generation failed: " + err.message);
            }
        },


        /**
         * Verschlüsselt 2FA Secrets.
         */
        //encryptBase32Secret: async function (base32Secret, pepper) {
        //    pE_Common.log("Web:Security", "encryptBase32Secret aufgerufen");
        //    try {
        //        return pE_Common.toScalar("", true, null);
        //    } catch (err) {
        //        return pE_Common.toScalar(null, false, err.message);
        //    }
        //},

        /**
         * Entschlüsselt 2FA Secrets.
         */
        //decryptBase32Secret: async function (encryptedBase64, pepper) {
        //    pE_Common.log("Web:Security", "decryptBase32Secret aufgerufen");
        //    try {
        //        return pE_Common.toScalar("", true, null);
        //    } catch (err) {
        //        return pE_Common.toScalar(null, false, err.message);
        //    }
        //}
    },

    // --- WEB STORAGE MODULE (IndexedDB) ---
    storage: {
        _db: null,
        _activeDbName: null,
        _storeName: "Files",

        /**
         * Interne Hilfsmethode, um die korrekte DB zu öffnen.
         * @param {string} dbName - Der Name der App (aus C# DbName).
         */
        _getDb: async function (dbName) {
            const fullDbName = "DB_" + dbName;

            // Falls die DB bereits offen ist und dem Namen entspricht, direkt zurückgeben
            if (this._db && this._activeDbName === fullDbName) {
                return this._db;
            }

            // Falls eine andere DB offen war, schließen
            if (this._db) {
                this._db.close();
                this._db = null;
            }

            return new Promise((resolve, reject) => {
                const request = indexedDB.open(fullDbName, 1);

                request.onupgradeneeded = (event) => {
                    const db = event.target.result;
                    if (!db.objectStoreNames.contains(this._storeName)) {
                        db.createObjectStore(this._storeName);
                    }
                };

                request.onsuccess = () => {
                    this._db = request.result;
                    this._activeDbName = fullDbName;
                    resolve(this._db);
                };

                request.onerror = (e) => {
                    pE_Common.log("Web:Storage", "IndexedDB Fehler", "error", e);
                    reject("IndexedDB Open Error");
                };
            });
        },

        /**
         * Speichert einen verschlüsselten Datensatz (Datei) in der IndexedDB.
         * @param {string} dbName - Der App-Name für die DB-Isolierung.
         * @param {string} accountHash - Der User-Hash.
         * @param {string} fileName - Der Dateiname inkl. Tabellenpfad (Key).
         * @param {string} encryptedContent - Der verschlüsselte Inhalt.
         */
        writeFile: async function (dbName, accountHash, fileName, encryptedContent) {
            const path = `${accountHash}/${fileName}`;
            pE_Common.log("Web:Storage", `writeFile -> DB: ${dbName}, Path: ${path}`);

            try {
                const db = await this._getDb(dbName);

                return new Promise((resolve) => {
                    const tx = db.transaction(this._storeName, "readwrite");
                    const store = tx.objectStore(this._storeName);

                    // put überschreibt existierende Einträge mit demselben Key (Pfad)
                    const request = store.put(encryptedContent, path);

                    request.onsuccess = () => {
                        // Der Request war erfolgreich, wir warten aber das Ende der Transaktion ab
                        pE_Common.log("Web:Storage", `Schreibvorgang vorgemerkt: ${path}`);
                    };

                    tx.oncomplete = () => {
                        pE_Common.log("Web:Storage", `Schreibvorgang finalisiert: ${path}`);
                        resolve(pE_Common.toScalar(true, true, ""));
                    };

                    tx.onerror = (e) => {
                        const errMsg = e.target.error ? e.target.error.message : "Write failed";
                        pE_Common.log("Web:Storage", `Fehler beim Schreiben: ${errMsg}`, "error");
                        resolve(pE_Common.toScalar(false, false, errMsg));
                    };

                    tx.onabort = () => {
                        resolve(pE_Common.toScalar(false, false, "Transaction aborted"));
                    };
                });
            } catch (e) {
                pE_Common.log("Web:Storage", "Fataler Fehler in writeFile", "error", e);
                return pE_Common.toScalar(false, false, e.toString());
            }
        },

        /**
         * Liest alle Datensätze einer "Tabelle" (Präfix-Suche).
         * @param {string} dbName - Der App-Name für die DB-Isolierung.
         * @param {string} accountHash - Der User-Hash.
         * @param {string} tableName - Der Name der Tabelle (Ordner).
         */
        readAllTableFiles: async function (dbName, accountHash, tableName) {
            const prefix = `${accountHash}/${tableName}/`;
            pE_Common.log("Web:Storage", `readAllTableFiles -> DB: ${dbName}, Prefix: ${prefix}`);

            try {
                const db = await this._getDb(dbName);

                return new Promise((resolve) => {
                    const tx = db.transaction(this._storeName, "readonly");
                    const store = tx.objectStore(this._storeName);

                    // Wir nutzen den Unicode-Bereich \uffff um alle Keys unter dem Präfix zu erfassen
                    const range = IDBKeyRange.bound(prefix, prefix + '\uffff');
                    const request = store.getAll(range);

                    request.onsuccess = () => {
                        // Falls nichts gefunden wurde, ist request.result ein leeres Array []
                        const results = request.result || [];

                        // WICHTIG: Wir serialisieren das Array von Strings für die C#-Seite
                        const jsonResult = JSON.stringify(results);

                        pE_Common.log("Web:Storage", `Read erfolgreich: ${results.length} Dateien gefunden.`);

                        // --- ANPASSUNG GEMÄSS PUNKT 1 ---
                        // Wir erzeugen das Scalar-Objekt
                        const res = pE_Common.toScalar(jsonResult, true, "");
                        // Da toScalar bei JSON-Strings out_value_bool auf false setzen würde,
                        // forcieren wir hier den Erfolg für die C#-Prüfung.
                        res.out_value_bool = true;

                        resolve(res);
                    };

                    request.onerror = (e) => {
                        const errMsg = e.target.error ? e.target.error.message : "Read failed";
                        pE_Common.log("Web:Storage", `Fehler beim Lesen: ${errMsg}`, "error");

                        // Im Fehlerfall geben wir ein serialisiertes leeres Array zurück
                        // Hier lassen wir out_value_bool auf false (Standard durch toScalar bei success=false)
                        resolve(pE_Common.toScalar(JSON.stringify([]), false, errMsg));
                    };
                });
            } catch (e) {
                pE_Common.log("Web:Storage", "Fataler Fehler in readAllTableFiles", "error", e);
                return pE_Common.toScalar(JSON.stringify([]), false, e.toString());
            }
        },

        /**
         * Liest einen einzelnen Datensatz aus der Tabelle.
         * @param {string} dbName - Der App-Name für die DB-Isolierung.
         * @param {string} accountHash - Der User-Hash.
         * @param {string} fileName - Der vollständige Dateiname (z.B. "AuthUsers/123456.json").
         */
        readFile: async function (dbName, accountHash, fileName) {
            pE_Common.log("Web:Storage", `readFile -> DB: ${dbName}, File: ${fileName}`);

            try {
                const db = await this._getDb(dbName);
                const key = `${accountHash}/${fileName}`;

                return new Promise((resolve) => {
                    const tx = db.transaction(this._storeName, "readonly");
                    const store = tx.objectStore(this._storeName);
                    const request = store.get(key);

                    request.onsuccess = () => {
                        const content = request.result || null;

                        if (content) {
                            pE_Common.log("Web:Storage", `readFile erfolgreich: ${fileName}`);
                            const res = pE_Common.toScalar(content, true, "");
                            res.out_value_bool = true;
                            resolve(res);
                        } else {
                            pE_Common.log("Web:Storage", `readFile: Datei nicht gefunden: ${fileName}`);
                            // Datei existiert nicht -> kein Fehler, sondern null
                            const res = pE_Common.toScalar(null, true, "");
                            res.out_value_bool = true;
                            resolve(res);
                        }
                    };

                    request.onerror = (e) => {
                        const errMsg = e.target.error ? e.target.error.message : "Read file failed";
                        pE_Common.log("Web:Storage", `Fehler beim Lesen von ${fileName}: ${errMsg}`, "error");
                        resolve(pE_Common.toScalar(null, false, errMsg));
                    };
                });
            } catch (e) {
                pE_Common.log("Web:Storage", "Fataler Fehler in readFile", "error", e);
                return pE_Common.toScalar(null, false, e.toString());
            }
        },

        /**
         * Löscht eine spezifische Datei (Eintrag) aus der IndexedDB.
         * @param {string} dbName - Der App-Name für die DB-Isolierung.
         * @param {string} accountHash - Der User-Hash.
         * @param {string} fileName - Der Dateiname (Teil des Keys).
         */
        deleteFile: async function (dbName, accountHash, fileName) {
            const path = `${accountHash}/${fileName}`;
            pE_Common.log("Web:Storage", `deleteFile -> DB: ${dbName}, Path: ${path}`);

            try {
                const db = await this._getDb(dbName);

                return new Promise((resolve) => {
                    const tx = db.transaction(this._storeName, "readwrite");
                    const store = tx.objectStore(this._storeName);

                    // Löschvorgang starten
                    const request = store.delete(path);

                    request.onsuccess = () => {
                        pE_Common.log("Web:Storage", `Datei erfolgreich gelöscht: ${path}`);
                        resolve(pE_Common.toScalar(true, true, ""));
                    };

                    request.onerror = (e) => {
                        const errMsg = e.target.error ? e.target.error.message : "Delete failed";
                        pE_Common.log("Web:Storage", `Fehler beim Löschen: ${errMsg}`, "error");
                        resolve(pE_Common.toScalar(false, false, errMsg));
                    };

                    // Falls die gesamte Transaktion abbricht
                    tx.onabort = () => {
                        resolve(pE_Common.toScalar(false, false, "Transaction aborted"));
                    };
                });
            } catch (e) {
                pE_Common.log("Web:Storage", "Fataler Fehler in deleteFile", "error", e);
                return pE_Common.toScalar(false, false, e.toString());
            }
        },

        prepareStorage: async function (dbName, accountHash) {
            pE_Common.log("Web:Storage", `Pre-loading DB: DB_${dbName}`);
            await this._getDb(dbName); // Öffnet die DB und triggert onupgradeneeded falls nötig
        },
               
        /**
         * Deletes all data associated with a user within this application.
         * @param {string} dbName - The name of the application.
         * @param {string} accountHash - The hashed user identifier.
         */
        purgeUserStorage: async function (dbName, accountHash) {
            const prefix = `${accountHash}/`;
            pE_Common.log("Web:Storage", `purgeUserStorage -> ${dbName} / Account: ${accountHash}`);

            try {
                const db = await this._getDb(dbName);

                return new Promise((resolve) => {
                    const tx = db.transaction(this._storeName, "readwrite");
                    const store = tx.objectStore(this._storeName);

                    // Define the range: everything starting with 'accountHash/'
                    const range = IDBKeyRange.bound(prefix, prefix + '\uffff');
                    const request = store.openCursor(range);

                    request.onsuccess = (event) => {
                        const cursor = event.target.result;
                        if (cursor) {
                            cursor.delete();
                            cursor.continue(); // Move to the next entry
                        } else {
                            // Cursor reached the end of the range.
                            // We do not resolve here to ensure the transaction fully flushes to disk first.
                            pE_Common.log("Web:Storage", "Cursor iteration finished. Waiting for transaction commit...");
                        }
                    };

                    tx.oncomplete = () => {
                        // Guaranteed safety: The browser has physical committed the changes to disk
                        pE_Common.log("Web:Storage", "Purge successfully completed and committed.");
                        resolve(pE_Common.toScalar(true, true, ""));
                    };

                    tx.onerror = (e) => {
                        const errMsg = e.target.error ? e.target.error.message : "Purge transaction failed";
                        pE_Common.log("Web:Storage", `Error during purge transaction: ${errMsg}`, "error");
                        resolve(pE_Common.toScalar(false, false, errMsg));
                    };
                });
            } catch (e) {
                pE_Common.log("Web:Storage", "Fatal error in purgeUserStorage", "error", e);
                return pE_Common.toScalar(false, false, e.toString());
            }
        },

        /**
         * Löscht alle Daten, die zu einer spezifischen Tabelle eines Benutzers gehören.
         * @param {string} dbName - Der App-Name.
         * @param {string} accountHash - Der User-Hash.
         * @param {string} tableName - Der Name der Tabelle (z.B. "Cycles").
         */
        purgeTable: async function (dbName, accountHash, tableName) {
            // Der Pfad beginnt mit dem User-Hash und der Tabelle
            const prefix = `${accountHash}/${tableName}/`;
            pE_Common.log("Web:Storage", `purgeTable -> ${dbName} / Account: ${accountHash} / Table: ${tableName}`);

            try {
                const db = await this._getDb(dbName);

                return new Promise((resolve) => {
                    const tx = db.transaction(this._storeName, "readwrite");
                    const store = tx.objectStore(this._storeName);

                    // Bereich definieren: Alle Einträge, die mit 'accountHash/tableName/' beginnen
                    const range = IDBKeyRange.bound(prefix, prefix + '\uffff');
                    const request = store.openCursor(range);

                    request.onsuccess = (event) => {
                        const cursor = event.target.result;
                        if (cursor) {
                            cursor.delete();
                            cursor.continue();
                        }
                    };

                    // Sicherstellen, dass die Transaktion vollständig auf Disk geschrieben wurde
                    tx.oncomplete = () => {
                        pE_Common.log("Web:Storage", `Tabelle ${tableName} erfolgreich geleert und commitet.`);
                        resolve(pE_Common.toScalar(true, true, ""));
                    };

                    tx.onerror = (e) => {
                        const errMsg = e.target.error ? e.target.error.message : "Table purge transaction failed";
                        pE_Common.log("Web:Storage", `Fehler beim Löschen der Tabelle ${tableName}: ${errMsg}`, "error");
                        resolve(pE_Common.toScalar(false, false, errMsg));
                    };
                });
            } catch (e) {
                pE_Common.log("Web:Storage", `Fataler Fehler in purgeTable für ${tableName}`, "error", e);
                return pE_Common.toScalar(false, false, e.toString());
            }
        }
    },


};

// Diese Funktion klickt einfach auf ein DOM-Element, das als Argument übergeben wird.
// Sie wird verwendet, um die unsichtbaren <input> Elemente aus Blazor heraus zu aktivieren.
window.clickElement = (element) => {
    if (element) {
        element.click();
    }
};
