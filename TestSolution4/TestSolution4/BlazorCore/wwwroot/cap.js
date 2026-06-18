// pE/wwwroot/cap.js

/**
 * =================================================================================
 * GUIDELINES FOR EXPANDING CAPACITOR BRIDGE (cap.js)
 * =================================================================================
 * * 1. LOGGING & TRACING
 * - Always use the global 'pE_Common.log(msg, type, data)' for standard operations.
 * - Success messages should use 'info' or 'log'. These are suppressed when 
 * _DB_DEBUG is false to save performance and keep the console clean.
 * - Error and Warn types are ALWAYS displayed, regardless of _DB_DEBUG.
 * - For critical modules (like SQLite), use a specialized '_logError' to capture 
 * stack traces and provide high-visibility formatting (using %c).
 * * 2. ERROR HANDLING & BLAZOR COMMUNICATION
 * - Never let an async function crash silently. Always use try-catch blocks.
 * - Every bridge function intended for Blazor must return the '_toScalar' object.
 * - This ensures Blazor WASM receives a predictable C# object:
 * { 
 * out_value_str:  "The actual result or empty", 
 * out_value_bool: true/false for success/fail, 
 * out_err:        "The error message for UI feedback"
 * }
 * * 3. TYPE SAFETY (THE "CODE 20" RULE)
 * - Native SQLite (especially on Android/iOS) is stricter than JS.
 * - Always validate and "heal" data types before passing them to native plugins.
 * - Convert numeric strings to actual Numbers to avoid 'datatype mismatch (code 20)'.
 * * 4. PLATFORM DUALITY (WEB FALLBACK)
 * - Always check 'window.Capacitor.isNativePlatform()' before calling plugins.
 * - Provide a meaningful 'Web-Fallback' or a warning log if a feature is 
 * native-only. This prevents the WASM app from freezing during browser-debug.
 * * 5. PARAMETER MAPPING
 * - Blazor often sends Dictionaries (Objects). Convert these into flat Arrays 
 * if the Capacitor Plugin requires it (as seen in the sqlite.query module).
 * =================================================================================
 */

window.pE_Capacitor = {

    // Definition der Zustände (identisch mit C# Enum)
    DB_STATE: { ERROR: 0, INITIALIZING: 1, READY: 2, NEW: 3 },

    // Internal state to hold the path once it's resolved
    _baseDir: "/",
    _deviceCache: "loading...",
    _prefCache: {},
    _deviceType: "unknown",

    /**
     * Initializes platform-specific settings and caches native states.
     * @returns {Object} ScalarModel {out_value_str: deviceType, out_value_bool: success, out_err: errorMessage}
     */
    init: async function () {
        pE_Common.log("Starte Native-Bridge Initialisierung...");

        // PROTECTIVE SHIELD: Check if Capacitor and Native Platform are available
        if (typeof window.Capacitor !== 'undefined' && window.Capacitor.isNativePlatform()) {
            try {
                const plugins = window.Capacitor.Plugins;

                // Zuweisung mit Fallback auf null, damit kein ReferenceError entsteht
                this.Filesystem = plugins.Filesystem || null;
                this.Device = plugins.Device || null;
                this.App = plugins.App || null; // Hinzugefügt für Hardware-Back-Button
                this.SecureStoragePlugin = plugins.SecureStoragePlugin || null;
                //this.SecureStoragePlugin = plugins.SecureStorage || null;
                //this.SecureStoragePlugin = plugins.Preferences || null;
                this.CapacitorSQLite = plugins.CapacitorSQLite || null;
                this.Camera = plugins.Camera || null;
                this.Clipboard = plugins.Clipboard || null;
                this.Browser = plugins.Browser || null;
                this.Share = plugins.Share || null;
                this.LocalNotifications = plugins.LocalNotifications || null;

                // 1. Resolve base directories and device information
                let result, info;
                try {
                    pE_Common.log("Lade Filesystem URI und Device Info...");

                    // WICHTIG: Zugriff über this., da lokale Variablen oben auskommentiert sind
                    if (!this.Filesystem || !this.Device) {
                        throw new Error("Core Plugins (Filesystem/Device) nicht initialisiert.");
                    }

                    result = await this.Filesystem.getUri({ path: '', directory: 'DATA' });
                    info = await this.Device.getInfo();

                    pE_Common.log("Device Info geladen", "log", {
                        platform: info.platform,
                        model: info.model,
                        osVersion: info.osVersion
                    });
                } catch (fsErr) {
                    pE_Common.log("INIT-FS-DEVICE Error", "error", fsErr);
                    if (this.sqlite && this.sqlite._logError) this.sqlite._logError("INIT-FS-DEVICE", fsErr);
                    return pE_Common.toScalar(null, false, "Filesystem/Device Info access failed: " + fsErr.message);
                }

                this._baseDir = result.uri;
                this._deviceCache = `${info.platform}-${info.model}`;

                // Neue, saubere Prüfung inklusive Preferences (als SecureStorage Ersatz) und App-Plugin
                if (!this.SecureStoragePlugin || !this.CapacitorSQLite || !this.LocalNotifications || !this.App) {
                    const missing = !this.SecureStoragePlugin ? "Preferences (SecureStorage Ersatz)" :
                        (!this.CapacitorSQLite ? "CapacitorSQLite" :
                            (!this.LocalNotifications ? "LocalNotifications" : "App"));

                    pE_Common.log(`FATAL: Plugin ${missing} nicht gefunden!`, "error");
                    return pE_Common.toScalar(null, false, `Plugin missing: ${missing}`);
                }

                pE_Common.log("Native Plugins erfolgreich validiert.", "log");

                // 4. Determine Device Type Logic
                if (info.platform === 'ios') {
                    if (info.model.toLowerCase().includes('ipad')) {
                        this._deviceType = 'tablet';
                    } else if (info.operatingSystem === 'mac') {
                        this._deviceType = 'desktop';
                    } else {
                        this._deviceType = 'mobile';
                    }
                } else if (info.platform === 'android') {
                    const isTablet = window.innerWidth > 900 && window.innerHeight > 600;
                    this._deviceType = isTablet ? 'tablet' : 'mobile';
                }

                // TV Detection
                if (navigator.userAgent.includes("TV") || navigator.userAgent.includes("Large Screen")) {
                    this._deviceType = 'tv';
                }

                pE_Common.log(`Native Init Successful. Detected Type: ${this._deviceType}`, "info");
                return pE_Common.toScalar(this._deviceType, true, "");

            } catch (e) {
                pE_Common.log("Kritischer Fehler während Bridge-Init", "error", e);
                if (this.sqlite && this.sqlite._logError) {
                    this.sqlite._logError("INIT-FATAL", e);
                }
                this._deviceCache = "native-error";
                return pE_Common.toScalar(null, false, "Init failed: " + e.message);
            }
        } else {
            pE_Common.log("Native Bridge nicht gefunden. Wechsel in WASM-Web-Mode.", "warn");
            this._deviceType = "Web";
            this._baseDir = "/";
            this._deviceCache = "wasm-browser-debug";
            return pE_Common.toScalar("Web", true, "");
        }
    },

    /**
     * Determines the device info.
     * @returns {string} Returns e.g., "android-Pixel 6-MyPhone"
     */
    getDeviceInfo: function () {
        pE_Common.log("Abfrage DeviceInfo aus Cache...");

        // If _deviceCache is empty, it means init() hasn't finished or failed
        if (!this._deviceCache || this._deviceCache === "" || this._deviceCache === "loading...") {
            pE_Common.log("getDeviceInfo aufgerufen, bevor Initialisierung abgeschlossen war oder Cache leer ist.", "warn");
        } else {
            pE_Common.log(`DeviceInfo geliefert: ${this._deviceCache}`);
        }

        return this._deviceCache;
    },

    /**
     * Synchronous getter for the base directory.
     * @returns {string} The path to the data directory.
     */
    getBaseDirectory: function () {
        pE_Common.log("Abfrage Basis-Verzeichnis...");

        // Critical for file operations: log if directory is missing
        if (!this._baseDir || this._baseDir === "" || this._baseDir === "/") {
            pE_Common.log("getBaseDirectory angefordert, aber _baseDir ist nicht gesetzt oder steht auf Root!", "error");
        } else {
            pE_Common.log(`Basis-Verzeichnis geliefert: ${this._baseDir}`);
        }

        return this._baseDir;
    },

    /**
     * Determines the current platform.
     * @returns {string} "android", "ios" or "wasm" as fallback.
     */
    //getPlatform: function () {
    //    pE_Common.log("Erkenne Plattform...");
    //    try {
    //        if (window.Capacitor && window.Capacitor.isNativePlatform()) {
    //            const platform = window.Capacitor.getPlatform();
    //            pE_Common.log(`Native Plattform erkannt: ${platform}`, "info");
    //            return platform;
    //        } else {
    //            pE_Common.log("Keine native Plattform erkannt, Fallback auf 'wasm'.", "log");
    //        }
    //    } catch (e) {
    //        // Log error if Capacitor object is corrupted or inaccessible
    //        pE_Common.log("Fehler bei der Plattformerkennung", "error", e);
    //    }
    //    return "wasm";
    //},
    /**
     * Determines the current platform.
     * @returns {string} "android", "ios", "ios-mac" oder "wasm" als fallback.
     */
    getPlatform: function () {
        pE_Common.log("Erkenne Plattform...");

        try {
            // 1. Prüfe ob native Plattform
            if (!window.Capacitor || !window.Capacitor.isNativePlatform()) {
                pE_Common.log("Keine native Plattform -> 'wasm'", "log");
                return "wasm";
            }

            // 2. Basis-Plattform von Capacitor
            const platform = window.Capacitor.getPlatform();

            // 3. iOS genauer untersuchen (SYNCHRON via Cache)
            if (platform === 'ios') {
                // Methode A: Explizites Flag aus init()
                if (this._isIosOnMac === true) {
                    pE_Common.log("iOS auf Mac (via _isIosOnMac Flag) -> 'ios-mac'", "info");
                    return "ios-mac";
                }

                // Methode B: Device Cache analysieren
                if (this._deviceCache) {
                    const cacheLower = this._deviceCache.toLowerCase();
                    if (cacheLower.includes('mac') || cacheLower.includes('macbook')) {
                        pE_Common.log("iOS auf Mac (via _deviceCache) -> 'ios-mac'", "info");
                        return "ios-mac";
                    }
                }

                // Methode C: _deviceInfo falls vorhanden (aus init())
                if (this._deviceInfo &&
                    (this._deviceInfo.operatingSystem === 'mac' ||
                        this._deviceInfo.model?.toLowerCase().includes('mac'))) {
                    pE_Common.log("iOS auf Mac (via cached _deviceInfo) -> 'ios-mac'", "info");
                    return "ios-mac";
                }

                // Normales iOS-Gerät
                pE_Common.log("Standard iOS-Gerät -> 'ios'", "info");
                return "ios";
            }

            // 4. Alle anderen Plattformen
            pE_Common.log(`Native Plattform: ${platform}`, "info");
            return platform; // "android", "electron", etc.

        } catch (e) {
            pE_Common.log("Fehler bei Plattform-Erkennung", "error", e);
            return "wasm";
        }
    },

    ///**
    // * Stores a value and returns a pE ScalarModel.
    // * @returns {Object} ScalarModel
    // */
    //setStorage: async function (key, value) {
    //    pE_Common.log(`Speichere Key in SecureStorage: ${key}...`);

    //    try {
    //        const { SecureStoragePlugin } = Capacitor.Plugins;

    //        // Wenn das Plugin nicht geladen ist, sofort Fehler zurückgeben
    //        if (!SecureStoragePlugin) {
    //            const errMs = "FATAL: SecureStoragePlugin is not registered/installed on this device.";
    //            pE_Common.log(errMs, "error");
    //            return pE_Common.toScalar(null, false, errMs);
    //        }

    //        // Native Operation ausführen
    //        await SecureStoragePlugin.set({ key: key, value: value });

    //        // Internen Cache für synchrones GetPreference befüllen
    //        this._prefCache[key] = value;

    //        pE_Common.log(`Erfolgreich gespeichert und gecached: ${key}`, "info");

    //        return pE_Common.toScalar(value, true, "");
    //    } catch (e) {
    //        // Jeder native Fehler (z.B. Keystore-Zugriff verweigert) landet hier
    //        pE_Common.log(`Kritischer Fehler beim Schreiben von '${key}'`, "error", e);

    //        if (this.sqlite && this.sqlite._logError) {
    //            this.sqlite._logError("STORAGE-SET", e);
    //        }

    //        return pE_Common.toScalar(null, false, "NATIVE-ERROR: " + e.message);
    //    }
    //},
    /**
     * Stores a value and returns a pE ScalarModel.
     * @returns {Object} ScalarModel
     */
    setStorage: async function (key, value) {
        pE_Common.log(`Speichere Key in SecureStorage: ${key}...`);

        try {
            // NEU: Zugriff über die validierte Referenz aus init()
            const plugin = this.SecureStoragePlugin;

            if (!plugin) {
                const errMs = "SecureStoragePlugin nicht initialisiert. Prüfe die init() Funktion.";
                pE_Common.log(errMs, "error");
                return pE_Common.toScalar(null, false, errMs);
            }

            // Native Operation ausführen
            //await plugin.set({ key: key, value: value });
            await plugin.set({ key: key, value: String(value) });

            // Internen Cache für synchrones GetPreference befüllen
            if (!this._prefCache) this._prefCache = {};
            this._prefCache[key] = value;

            pE_Common.log(`Erfolgreich gespeichert und gecached: ${key}`, "info");
            return pE_Common.toScalar(value, true, "");
        } catch (e) {
            pE_Common.log(`Kritischer Fehler beim Schreiben von '${key}'`, "error", e);

            // Optional: Error-Logging via SQLite Modul falls vorhanden
            if (this.sqlite?._logError) {
                this.sqlite._logError("STORAGE-SET", e);
            }

            return pE_Common.toScalar(null, false, "NATIVE-ERROR: " + e.message);
        }
    },

    //setStorageSync: function (key, value) {
    //    pE_Common.log(`Synchrones Speichern (Fire & Forget): ${key}...`);

    //    if (window.Capacitor?.isNativePlatform()) {
    //        const { SecureStoragePlugin } = Capacitor.Plugins;

    //        if (SecureStoragePlugin) {
    //            // 1. Cache befüllen (Sofort verfügbar für Blazor)
    //            this._prefCache[key] = value;

    //            // 2. Nativ anstoßen (Hintergrund-Operation ohne await)
    //            SecureStoragePlugin.set({ key: key, value: value })
    //                .then(() => {
    //                    pE_Common.log(`Hintergrund-Speicherung erfolgreich: ${key}`);
    //                })
    //                .catch(e => {
    //                    pE_Common.log(`Hintergrund-Speicherung fehlgeschlagen für Key ${key}`, "error", e);
    //                });

    //            return pE_Common.toScalar(value, true, "");
    //        } else {
    //            const errMs = "CRITICAL: SecureStoragePlugin missing!";
    //            pE_Common.log(errMs, "error");
    //            return pE_Common.toScalar(null, false, "FATAL: SecureStoragePlugin missing. Data NOT saved.");
    //        }
    //    }

    //    const contextErr = "ERROR: Not a native platform context.";
    //    pE_Common.log(contextErr, "warn");
    //    return pE_Common.toScalar(null, false, contextErr);
    //},
    setStorageSync: function (key, value) {
        pE_Common.log(`Synchrones Speichern (Fire & Forget): ${key}...`);

        if (window.Capacitor?.isNativePlatform()) {
            // NEU: Nutze die validierte Referenz
            const plugin = this.SecureStoragePlugin;

            if (plugin) {
                // 1. Cache befüllen (Sofort verfügbar für Blazor/JS)
                if (!this._prefCache) this._prefCache = {};
                this._prefCache[key] = value;

                // 2. Nativ anstoßen (Hintergrund-Operation ohne await)
                //plugin.set({ key: key, value: value })
                //    .then(() => {
                //        pE_Common.log(`Hintergrund-Speicherung erfolgreich: ${key}`);
                //    })
                //    .catch(e => {
                //        pE_Common.log(`Hintergrund-Speicherung fehlgeschlagen für Key ${key}`, "error", e);
                //    });
                plugin.set({ key: key, value: String(value) })
                    .then(() => {
                        pE_Common.log(`Hintergrund-Speicherung erfolgreich: ${key}`);
                    })
                    .catch(e => {
                        pE_Common.log(`Hintergrund-Speicherung fehlgeschlagen für Key ${key}`, "error", e);
                    });

                return pE_Common.toScalar(value, true, "");
            } else {
                const errMs = "CRITICAL: SecureStoragePlugin nicht initialisiert!";
                pE_Common.log(errMs, "error");
                return pE_Common.toScalar(null, false, "FATAL: SecureStoragePlugin missing.");
            }
        }

        const contextErr = "ERROR: Not a native platform context.";
        pE_Common.log(contextErr, "warn");
        return pE_Common.toScalar(null, false, contextErr);
    },

    ///**
    // * Retrieves a value from Capacitor SecureStorage and returns a ScalarModel.
    // * @param {string} key - The key to look up.
    // * @returns {Object} ScalarModel {out_value_str, out_value_bool, out_err}
    // */
    //getStorage: async function (key) {
    //    pE_Common.log(`Lese Key aus SecureStorage: ${key}...`);

    //    try {
    //        const { SecureStoragePlugin } = Capacitor.Plugins;

    //        if (!SecureStoragePlugin) {
    //            const errMs = "SecureStoragePlugin missing during getStorage";
    //            pE_Common.log(errMs, "error");
    //            return pE_Common.toScalar(null, false, errMs);
    //        }

    //        const { value } = await SecureStoragePlugin.get({ key: key });

    //        if (value !== null && value !== undefined) {
    //            pE_Common.log(`Key '${key}' erfolgreich gelesen.`);
    //            // Cache synchronisieren, falls wir ihn später brauchen
    //            this._prefCache[key] = value;
    //            return pE_Common.toScalar(value, true, "");
    //        } else {
    //            pE_Common.log(`Key '${key}' ist leer (null/undefined).`);
    //            return pE_Common.toScalar(null, true, "");
    //        }

    //    } catch (e) {
    //        // "Item not found" ist ein valider Zustand (Key existiert einfach noch nicht)
    //        if (e.message && (e.message.includes("not exist") || e.message.includes("No value found"))) {
    //            pE_Common.log(`Key '${key}' existiert noch nicht im Storage.`);
    //            return pE_Common.toScalar(null, true, "");
    //        }

    //        // Echter technischer Fehler (z.B. Hardware-Keystore Problem)
    //        pE_Common.log(`Kritischer Fehler beim Lesen von '${key}'`, "error", e);
    //        if (this.sqlite && this.sqlite._logError) {
    //            this.sqlite._logError("STORAGE-GET", e);
    //        }

    //        return pE_Common.toScalar(null, false, "READ-ERROR: " + e.message);
    //    }
    //},
    /**
     * Retrieves a value from Capacitor SecureStorage and returns a ScalarModel.
     * @param {string} key - The key to look up.
     * @returns {Object} ScalarModel {out_value_str, out_value_bool, out_err}
     */
    getStorage: async function (key) {
        pE_Common.log(`Lese Key aus SecureStorage: ${key}...`);

        try {
            // NEU: Nutze die validierte Referenz
            const plugin = this.SecureStoragePlugin;

            if (!plugin) {
                const errMs = "SecureStoragePlugin nicht initialisiert.";
                pE_Common.log(errMs, "error");
                return pE_Common.toScalar(null, false, errMs);
            }

            const { value } = await plugin.get({ key: key });

            if (value !== null && value !== undefined) {
                pE_Common.log(`Key '${key}' erfolgreich gelesen.`);
                // Cache synchronisieren
                if (!this._prefCache) this._prefCache = {};
                this._prefCache[key] = value;
                return pE_Common.toScalar(value, true, "");
            } else {
                pE_Common.log(`Key '${key}' ist leer (null/undefined).`);
                return pE_Common.toScalar(null, true, "");
            }

        } catch (e) {
            // "Item not found" ist ein valider Zustand (Key existiert einfach noch nicht)
            if (e.message && (e.message.includes("not exist") || e.message.includes("No value found"))) {
                pE_Common.log(`Key '${key}' existiert noch nicht im Storage.`);
                return pE_Common.toScalar(null, true, "");
            }

            pE_Common.log(`Kritischer Fehler beim Lesen von '${key}'`, "error", e);
            if (this.sqlite?._logError) {
                this.sqlite._logError("STORAGE-GET", e);
            }

            return pE_Common.toScalar(null, false, "READ-ERROR: " + e.message);
        }
    },
    //getStorageSync: function (key) {
    //    if (window.Capacitor?.isNativePlatform()) {
    //        const { SecureStoragePlugin } = Capacitor.Plugins;

    //        // Erst prüfen!
    //        if (!SecureStoragePlugin) {
    //            return pE_Common.toScalar("", false, "FATAL: SecureStoragePlugin missing.");
    //        }

    //        // Wenn Plugin da: Aus Cache lesen
    //        const val = this._prefCache[key] || "";
    //        return pE_Common.toScalar(val, true, "");
    //    }
    //    return pE_Common.toScalar("", false, "Not a native platform");
    //},
    getStorageSync: function (key) {
        return pE_Common.toScalar(null, false, "Sync-Access is disabled for security reasons. Use async getStorage.");
    },

    ///**
    // * Removes a key from secure storage.
    // * @param {string} key - The key to be removed.
    // * @returns {Object} ScalarModel
    // */
    //removeStorage: async function (key) {
    //    pE_Common.log(`Lösche Key aus SecureStorage: ${key}...`);

    //    try {
    //        const { SecureStoragePlugin } = Capacitor.Plugins;

    //        // Check if plugin is available
    //        if (!SecureStoragePlugin) {
    //            const errMs = "SecureStoragePlugin missing during removeStorage";
    //            pE_Common.log(errMs, "error");
    //            return pE_Common.toScalar(null, false, errMs);
    //        }

    //        // Native Löschung
    //        await SecureStoragePlugin.remove({ key: key });

    //        // Update internal cache if it exists (Level 0 sync logic)
    //        if (this._prefCache && this._prefCache[key] !== undefined) {
    //            delete this._prefCache[key];
    //            pE_Common.log(`Key '${key}' aus lokalem Cache entfernt.`);
    //        }

    //        pE_Common.log(`Key '${key}' erfolgreich dauerhaft gelöscht.`, "info");
    //        return pE_Common.toScalar(null, true, "");

    //    } catch (e) {
    //        // Falls der Key gar nicht existiert, betrachten wir das als ERFOLG
    //        if (e.message && (e.message.includes("does not exist") || e.message.includes("not found"))) {
    //            pE_Common.log(`Key '${key}' existierte nicht oder war bereits entfernt (idempotent).`, "log");
    //            return pE_Common.toScalar(null, true, "");
    //        }

    //        // Log real native errors (e.g., hardware access issues)
    //        pE_Common.log(`Kritischer Fehler beim Löschen von '${key}'`, "error", e);
    //        if (this.sqlite && this.sqlite._logError) {
    //            this.sqlite._logError("STORAGE-REMOVE", e);
    //        }

    //        return pE_Common.toScalar(null, false, "NATIVE-REMOVE-ERROR: " + e.message);
    //    }
    //},
    /**
     * Removes a key from secure storage.
     * @param {string} key - The key to be removed.
     * @returns {Object} ScalarModel
     */
    removeStorage: async function (key) {
        pE_Common.log(`Lösche Key aus SecureStorage: ${key}...`);

        try {
            // NEU: Nutze die validierte Referenz
            const plugin = this.SecureStoragePlugin;

            if (!plugin) {
                const errMs = "SecureStoragePlugin nicht initialisiert.";
                pE_Common.log(errMs, "error");
                return pE_Common.toScalar(null, false, errMs);
            }

            // Native Löschung
            await plugin.remove({ key: key });

            // Update internal cache
            if (this._prefCache && this._prefCache.hasOwnProperty(key)) {
                delete this._prefCache[key];
                pE_Common.log(`Key '${key}' aus lokalem Cache entfernt.`);
            }

            pE_Common.log(`Key '${key}' erfolgreich dauerhaft gelöscht.`, "info");
            return pE_Common.toScalar(null, true, "");

        } catch (e) {
            // Idempotenz: Wenn Key nicht existiert, ist das für uns ein Erfolg
            if (e.message && (e.message.includes("does not exist") || e.message.includes("not found"))) {
                pE_Common.log(`Key '${key}' existierte nicht oder war bereits entfernt.`, "log");
                return pE_Common.toScalar(null, true, "");
            }

            pE_Common.log(`Kritischer Fehler beim Löschen von '${key}'`, "error", e);
            if (this.sqlite?._logError) {
                this.sqlite._logError("STORAGE-REMOVE", e);
            }

            return pE_Common.toScalar(null, false, "NATIVE-REMOVE-ERROR: " + e.message);
        }
    },
    //removeStorageSync: function (key) {
    //    if (window.Capacitor?.isNativePlatform()) {
    //        const { SecureStoragePlugin } = Capacitor.Plugins;

    //        // 1. STRIKTER CHECK: Erst prüfen!
    //        if (!SecureStoragePlugin) {
    //            return pE_Common.toScalar("", false, "FATAL: SecureStoragePlugin missing.");
    //        }

    //        // 2. Wenn Plugin da: Aus Cache löschen
    //        if (this._prefCache) {
    //            delete this._prefCache[key];
    //        }

    //        // 3. Nativ (async) entfernen
    //        SecureStoragePlugin.remove({ key: key })
    //            .catch(err => console.error(`NATIVE-REMOVE-ERROR for ${key}:`, err));

    //        // 4. Erfolg an Blazor melden
    //        return pE_Common.toScalar("", true, "");
    //    }

    //    return pE_Common.toScalar("", false, "Not a native platform");
    //},

    /**
     * Synchronous getter for the cached device type (mobile, tablet, tv, etc.).
     * @returns {string} The detected device idiom.
     */
    getDeviceType: function () {
        pE_Common.log("Abfrage Gerätetyp...");

        if (!this._deviceType || this._deviceType === "" || this._deviceType === "unknown") {
            pE_Common.log("getDeviceType aufgerufen, bevor init abgeschlossen war oder Typ unbekannt ist.", "warn");
        } else {
            pE_Common.log(`Gerätetyp geliefert: ${this._deviceType}`, "info");
        }

        return this._deviceType;
    },

    /**
     * Returns a combined string of device type and platform.
     * Uses cached values from init() for performance.
     * @returns {string} Format: "Tablet-ios", "Desktop-mac", or "Mobile-android".
     */
    getIdiomPlatform: function () {
        pE_Common.log("Generiere Idiom-Plattform-String...");
        try {
            const platform = (window.Capacitor && window.Capacitor.isNativePlatform())
                ? window.Capacitor.getPlatform()
                : "wasm";

            // Capitalize the first letter of device type for better readability
            const type = this._deviceType ? this._deviceTyBlazorCore.charAt(0).toUpperCase() + this._deviceTyBlazorCore.slice(1) : "Unknown";

            const idiom = `${type}-${platform}`;
            pE_Common.log(`Idiom-Plattform generiert: ${idiom}`, "info");

            return idiom;
        } catch (e) {
            pE_Common.log("Fehler in getIdiomPlatform", "error", e);
            return "Unknown-Platform";
        }
    },

    ///**
    // * Copies a string to the native system clipboard.
    // * @param {string} textToCopy - The string to be copied.
    // * @returns {boolean} True if successful, false if not on native or failed.
    // */
    //copyToClipboard: async function (textToCopy) {
    //    pE_Common.log("Kopiere Text in die Zwischenablage...");
    //    try {
    //        if (typeof window.Capacitor !== 'undefined' && window.Capacitor.isNativePlatform()) {
    //            const { Clipboard } = Capacitor.Plugins;

    //            if (!Clipboard) {
    //                pE_Common.log("Clipboard-Plugin nicht verfügbar.", "error");
    //                return false;
    //            }

    //            await Clipboard.write({
    //                string: textToCopy
    //            });

    //            pE_Common.log("Text erfolgreich in native Zwischenablage kopiert.", "info");
    //            return true;
    //        }

    //        pE_Common.log("Clipboard.write ignoriert: Keine native Plattform.", "warn");
    //        return false;
    //    } catch (e) {
    //        pE_Common.log("Fehler beim Kopieren in die Zwischenablage", "error", e);
    //        return false;
    //    }
    //},
    /**
     * Copies a string to the native system clipboard.
     * @param {string} textToCopy - The string to be copied.
     * @returns {boolean} True if successful, false if not on native or failed.
     */
    copyToClipboard: async function (textToCopy) {
        pE_Common.log("Kopiere Text in die Zwischenablage...");
        try {
            if (window.Capacitor?.isNativePlatform()) {
                // NEU: Nutze die validierte Referenz von 'this'
                const plugin = this.Clipboard;

                if (!plugin) {
                    pE_Common.log("Clipboard-Plugin nicht initialisiert.", "error");
                    return false;
                }

                await plugin.write({
                    string: textToCopy
                });

                pE_Common.log("Text erfolgreich in native Zwischenablage kopiert.", "info");
                return true;
            }

            pE_Common.log("Clipboard.write ignoriert: Keine native Plattform.", "warn");
            return false;
        } catch (e) {
            pE_Common.log("Fehler beim Kopieren in die Zwischenablage", "error", e);
            return false;
        }
    },

    ///**
    // * Opens a URL in a native in-app browser overlay.
    // * This is crucial for the Polling-Bridge flow, as the main WebView 
    // * remains active in the background, maintaining the SignalR/Polling connection.
    // * @param {string} url - The authentication URL (e.g., Google/MS via Web-API Proxy)
    // */
    //openAuthBrowser: async function (url) {
    //    pE_Common.log(`Öffne Auth-Browser für URL: ${url}...`);

    //    if (typeof window.Capacitor !== 'undefined' && window.Capacitor.isNativePlatform()) {
    //        try {
    //            const { Browser } = Capacitor.Plugins;

    //            if (!Browser) {
    //                pE_Common.log("Browser-Plugin nicht verfügbar. Nutze window.open Fallback.", "warn");
    //                window.open(url, '_blank');
    //                return;
    //            }

    //            // Open the browser as a native system overlay
    //            await Browser.open({
    //                url: url,
    //                windowName: "_self", // Ensures consistency between iOS and Android
    //                presentationStyle: "popover" // Provides a clean UI, especially on tablets
    //            });

    //            pE_Common.log("Nativer Auth-Browser erfolgreich geöffnet.", "info");
    //        } catch (e) {
    //            pE_Common.log("Fehler beim Öffnen des nativen Browsers", "error", e);

    //            // Fallback: If the native plugin fails, try opening via standard window.open
    //            pE_Common.log("Versuche Fallback: window.open...", "log");
    //            window.open(url, '_blank');
    //        }
    //    } else {
    //        // WEB FALLBACK: For local WASM/Browser debugging.
    //        pE_Common.log("Native Browser plugin nicht verfügbar (Web-Mode). Nutze window.open.", "warn");
    //        window.open(url, '_blank');
    //    }
    //},
    /**
     * Opens a URL in a native in-app browser overlay.
     * Crucial for Polling-Bridge: keeps the main WebView active in the background.
     * @param {string} url - The authentication URL.
     */
    openAuthBrowser: async function (url) {
        pE_Common.log(`Öffne Auth-Browser für URL: ${url}...`);

        if (window.Capacitor?.isNativePlatform()) {
            try {
                // NEU: Nutze die validierte Referenz
                const plugin = this.Browser;

                if (!plugin) {
                    pE_Common.log("Browser-Plugin nicht initialisiert. Nutze window.open Fallback.", "warn");
                    window.open(url, '_blank');
                    return;
                }

                // Open as native system overlay
                await plugin.open({
                    url: url,
                    windowName: "_self",
                    presentationStyle: "popover"
                });

                pE_Common.log("Nativer Auth-Browser erfolgreich geöffnet.", "info");
            } catch (e) {
                pE_Common.log("Fehler beim Öffnen des nativen Browsers", "error", e);
                window.open(url, '_blank');
            }
        } else {
            // WEB FALLBACK
            pE_Common.log("Browser-Plugin nicht verfügbar (Web-Mode). Nutze window.open.", "warn");
            window.open(url, '_blank');
        }
    },

    ///**
    //  * Closes the in-app browser overlay manually.
    //  * Typically called when the SignalR Hub or Polling-Bridge in C# 
    //  * reports a "Success" state.
    //  */
    //closeAuthBrowser: async function () {
    //    pE_Common.log("Anfrage zum Schließen des Auth-Browsers...");

    //    if (typeof window.Capacitor !== 'undefined' && window.Capacitor.isNativePlatform()) {
    //        try {
    //            const { Browser } = Capacitor.Plugins;

    //            if (!Browser) {
    //                pE_Common.log("Browser-Plugin nicht verfügbar für close().", "warn");
    //                return;
    //            }

    //            await Browser.close();
    //            pE_Common.log("Nativer Auth-Browser wurde programmatisch geschlossen.", "info");
    //        } catch (e) {
    //            // Fehler tritt oft auf, wenn der User den Browser bereits manuell geschlossen hat
    //            pE_Common.log("Fehler beim Schließen des Browsers (evtl. bereits geschlossen)", "warn", e);
    //        }
    //    } else {
    //        // In Web/WASM mode, we cannot close a separate tab opened via window.open('_blank')
    //        pE_Common.log("closeAuthBrowser im Web-Mode: Tab-Schließen durch User erforderlich (Sicherheitsrichtlinie).", "log");
    //    }
    //},
    /**
     * Closes the in-app browser overlay manually.
     * Typically called when the SignalR Hub or Polling-Bridge in C# 
     * reports a "Success" state.
     */
    closeAuthBrowser: async function () {
        pE_Common.log("Anfrage zum Schließen des Auth-Browsers...");

        if (window.Capacitor?.isNativePlatform()) {
            try {
                // NEU: Nutze die validierte Referenz
                const plugin = this.Browser;

                if (!plugin) {
                    pE_Common.log("Browser-Plugin nicht initialisiert für close().", "warn");
                    return;
                }

                await plugin.close();
                pE_Common.log("Nativer Auth-Browser wurde programmatisch geschlossen.", "info");
            } catch (e) {
                // Oft bereits manuell durch User geschlossen
                pE_Common.log("Fehler beim Schließen des Browsers (evtl. bereits geschlossen)", "warn", e);
            }
        } else {
            pE_Common.log("closeAuthBrowser im Web-Mode: Tab-Schließen durch User erforderlich.", "log");
        }
    },

    /**
     * Returns the base path for physical file storage.
     * On mobile devices, this points to the secure app data directory.
     * @returns {string} The resolved base directory URI.
     */
    getDirectoryPath: function () {
        pE_Common.log("Abfrage Verzeichnispfad (DATA)...");

        // This value is populated during init() via Filesystem.getUri({ directory: 'DATA' })
        if (!this._baseDir || this._baseDir === "" || this._baseDir === "/") {
            pE_Common.log("getDirectoryPath: Basis-Verzeichnis ist noch nicht initialisiert!", "error");
        } else {
            pE_Common.log(`Verzeichnispfad geliefert: ${this._baseDir}`);
        }

        return this._baseDir;
    },

    ///**
    // * Triggers the native share sheet of the mobile device.
    // * Falls back to the Web Share API if running in a supported browser.
    // * @param {string} title - The title of the sharing dialog.
    // * @param {string} text - The message or content to be shared.
    // */
    //shareText: async function (title, text) {
    //    pE_Common.log(`Starte Share-Aktion: ${title}...`);
    //    try {
    //        if (typeof window.Capacitor !== 'undefined' && window.Capacitor.isNativePlatform()) {
    //            const { Share } = Capacitor.Plugins;

    //            if (!Share) {
    //                pE_Common.log("Share-Plugin nicht verfügbar.", "error");
    //                return;
    //            }

    //            await Share.share({
    //                title: title,
    //                text: text,
    //                dialogTitle: title // Wichtig für Android Konsistenz
    //            });

    //            pE_Common.log("Natives Share-Sheet erfolgreich geöffnet.", "info");
    //        } else {
    //            // Fallback für moderne Browser (WASM/Web debugging)
    //            if (navigator.share) {
    //                pE_Common.log("Nutze Web Share API...", "log");
    //                await navigator.share({ title: title, text: text });
    //                pE_Common.log("Web Share API erfolgreich ausgelöst.", "info");
    //            } else {
    //                pE_Common.log("Sharing wird in dieser Umgebung nicht unterstützt (kein navigator.share).", "warn");
    //            }
    //        }
    //    } catch (e) {
    //        // Log wenn User abbricht oder System blockiert
    //        pE_Common.log("Fehler oder Abbruch bei der Share-Aktion", "warn", e);
    //    }
    //},
    /**
     * Triggers the native share sheet of the mobile device.
     * Falls back to the Web Share API if running in a supported browser.
     * @param {string} title - The title of the sharing dialog.
     * @param {string} text - The message or content to be shared.
     */
    shareText: async function (title, text) {
        pE_Common.log(`Starte Share-Aktion: ${title}...`);
        try {
            if (window.Capacitor?.isNativePlatform()) {
                // NEU: Nutze die validierte Referenz von 'this'
                const plugin = this.Share;

                if (!plugin) {
                    pE_Common.log("Share-Plugin nicht initialisiert.", "error");
                    return;
                }

                await plugin.share({
                    title: title,
                    text: text,
                    dialogTitle: title // Wichtig für Android Konsistenz
                });

                pE_Common.log("Natives Share-Sheet erfolgreich geöffnet.", "info");
            } else {
                // Fallback für moderne Browser (WASM/Web debugging)
                if (navigator.share) {
                    pE_Common.log("Nutze Web Share API...", "log");
                    await navigator.share({ title: title, text: text });
                    pE_Common.log("Web Share API erfolgreich ausgelöst.", "info");
                } else {
                    pE_Common.log("Sharing wird in dieser Umgebung nicht unterstützt.", "warn");
                }
            }
        } catch (e) {
            pE_Common.log("Fehler oder Abbruch bei der Share-Aktion", "warn", e);
        }
    },

    ///**
    // * Opens any external URL in the system's default browser or an in-app overlay.
    // * @param {string} url - The destination URL to be opened.
    // */
    //openExternalUrl: async function (url) {
    //    const moduleName = "Capacitor:Browser";
    //    try {
    //        if (typeof window.Capacitor !== 'undefined' && window.Capacitor.isNativePlatform()) {
    //            const { Browser } = Capacitor.Plugins;

    //            if (!Browser) {
    //                pE_Common.log(moduleName, "Browser plugin not available. Using fallback.", "warn");
    //                window.open(url, '_blank');
    //                return;
    //            }

    //            // Opens the URL using the Capacitor Browser plugin
    //            await Browser.open({ url: url });
    //            pE_Common.log(moduleName, "External URL opened: " + url, "info");
    //        } else {
    //            // WEB FALLBACK
    //            pE_Common.log(moduleName, "Opening in new tab (Web-Mode): " + url, "info");
    //            window.open(url, '_blank');
    //        }
    //    } catch (e) {
    //        pE_Common.log(moduleName, "Failed to open external URL: " + url, "error", e);
    //    }
    //},
    /**
     * Opens any external URL in the system's default browser or an in-app overlay.
     * @param {string} url - The destination URL to be opened.
     */
    openExternalUrl: async function (url) {
        pE_Common.log(`Öffne externe URL: ${url}...`);

        try {
            if (window.Capacitor?.isNativePlatform()) {
                // NEU: Nutze die validierte Referenz
                const plugin = this.Browser;

                if (!plugin) {
                    pE_Common.log("Browser-Plugin nicht initialisiert. Nutze window.open Fallback.", "warn");
                    window.open(url, '_blank');
                    return;
                }

                // Nutzt das Capacitor Browser Plugin
                await plugin.open({ url: url });
                pE_Common.log(`Externe URL erfolgreich geöffnet: ${url}`, "info");
            } else {
                // WEB FALLBACK
                pE_Common.log(`Öffne in neuem Tab (Web-Mode): ${url}`, "info");
                window.open(url, '_blank');
            }
        } catch (e) {
            pE_Common.log(`Fehler beim Öffnen der externen URL: ${url}`, "error", e);
        }
    },

    /**
     * Checks synchronously if a key exists in storage.
     * On native platforms, it checks the fast memory cache.
     * In web mode, it checks both cookies and local storage.
     * @param {string} key - The key to check for existence.
     * @returns {boolean} True if the key exists.
     */
    containsStorageSync: function (key) {
        pE_Common.log(`Prüfe Vorhandensein von Key (synchron): ${key}...`);

        if (window.Capacitor?.isNativePlatform()) {
            // High-speed check within the local memory cache (populated during init)
            const exists = this._prefCache.hasOwnProperty(key);
            pE_Common.log(`Key '${key}' im Native-Cache ${exists ? "gefunden" : "nicht gefunden"}.`);
            return exists;
        } else {
            // WEB FALLBACK: Check cookies first, then LocalStorage
            const cookieExists = document.cookie.split(';').some((item) => item.trim().startsWith(key + '='));
            const localStorageExists = localStorage.getItem(key) !== null;

            if (cookieExists || localStorageExists) {
                pE_Common.log(`Key '${key}' im Web-Storage (Cookie oder LocalStorage) gefunden.`, "log");
                return true;
            }

            pE_Common.log(`Key '${key}' im Web-Storage nicht vorhanden.`);
            return false;
        }
    },

    /**
     * Speichert eine Datei im Cache und öffnet den nativen Share-Dialog.
     * @param {string} filename - Der Zielname der Datei (z.B. "Daten.csv").
     * @param {string} base64Data - Der Inhalt als Base64-String.
     * @param {string} title - Der lokalisierte Titel für den Dialog.
     * @returns {Promise<string>} JSON-String des ScalarModels
     */
    saveFileNative: async function (filename, base64Data, title) {
        try {
            if (!this.Filesystem || !this.Share) {
                return JSON.stringify(pE_Common.toScalar(null, false, "Filesystem or Share plugin not initialized."));
            }

            // 1. String säubern: Entferne Data-URL-Präfixe und alle Whitespaces/Umbrüche
            let cleanBase64 = base64Data.includes(',') ? base64Data.split(',')[1] : base64Data;
            cleanBase64 = cleanBase64.replace(/[^A-Za-z0-9+/=]/g, "");

            // 2. Datei schreiben
            // WICHTIG (laut Forum): KEIN 'encoding' angeben! 
            // Wenn 'encoding' fehlt, erkennt Capacitor automatisch, 
            // dass es sich um Base64 handelt und dekodiert es binär.
            const saveResult = await this.Filesystem.writeFile({
                path: filename,
                data: cleanBase64,
                directory: 'CACHE'
                // encoding: 'base64' -> Lassen wir weg oder setzen es auf undefined
            });

            // 3. URI für den Share-Dialog abrufen
            const uriResult = await this.Filesystem.getUri({
                path: filename,
                directory: 'CACHE'
            });

            // 4. Den Share-Dialog öffnen
            await this.Share.share({
                title: filename,
                text: title,
                url: uriResult.uri,
                dialogTitle: title
            });

            return JSON.stringify(pE_Common.toScalar(true, true, ""));

        } catch (err) {
            pE_Common.log("Capacitor", `Export Fehler: ${err.message}`, "error");
            return JSON.stringify(pE_Common.toScalar(null, false, err.message));
        }
    },

    // --- APP MANAGEMENT MODULE ---
    app: {
        _dotNetHelper: null,

        /**
         * Registers a DotNetObjectReference to communicate back to Blazor
         * when native app events (like the back button) occur.
         * @param {Object} helper - The DotNetObjectReference from Blazor.
         */
        registerNavigationHelper: function (helper) {
            pE_Common.log("Navigation-Helper für native App-Events registriert.");
            this._dotNetHelper = helper;

            // Sobald der Helper da ist, lauschen wir auf den Hardware-Back-Button
            const parent = window.pE_Capacitor;
            if (parent.App) {
                parent.App.addListener('backButton', async (data) => {
                    pE_Common.log("Hardware Back-Button Event empfangen.");

                    if (this._dotNetHelper) {
                        // Wir rufen die C# Logik im MainLayout auf
                        await this._dotNetHelper.invokeMethodAsync('HandleNativeBack', data.canGoBack);
                    } else {
                        // Fallback, falls kein Helper registriert ist
                        if (!data.canGoBack) {
                            parent.App.exitApp();
                        } else {
                            window.history.back();
                        }
                    }
                });
            }
        },

        /**
         * Programmatically closes the app.
         */
        exitApp: function () {
            pE_Common.log("App-Exit angefordert.");
            const parent = window.pE_Capacitor;
            if (parent.App) {
                parent.App.exitApp();
            } else {
                pE_Common.log("App-Plugin nicht verfügbar (Web-Modus?).", "warn");
            }
        }
    },

    // --- MEDIA MODULE ---
    media: {
        /**
         * Captures a photo using the native camera or a web stream fallback.
         * Returns a ScalarModel with binary data in out_bytes.
         */
        capturePhoto: async function (dotNetHelper, videoElementId, imageSize, imageQuality, cropToSquare, thumbnailSize) {
            const startTime = Date.now();
            pE_Common.log(`[${startTime}] Foto-Prozess gestartet...`);

            if (window.Capacitor?.isNativePlatform()) {
                // Plugin-Referenz sicherstellen (Fallback auf Standard-Pfad)
                const cameraPlugin = window.Capacitor.Plugins.Camera || (window.pE_Capacitor ? window.pE_Capacitor.Camera : null);

                if (!cameraPlugin) {
                    pE_Common.log("FEHLER: Camera-Plugin nicht gefunden.", "error");
                    return pE_Common.toScalar(null, false, "Kamera-Plugin nicht verfügbar.");
                }

                try {
                    // --- 1. SCHRITT: Permission-Polling (Sicherstellen, dass Hardware-Rechte aktiv sind) ---
                    let permissions = await cameraPlugin.checkPermissions();
                    pE_Common.log(`Initialer Permission-Status: ${permissions.camera}`);

                    if (permissions.camera !== 'granted') {
                        pE_Common.log("Starte requestPermissions...", "info");
                        permissions = await cameraPlugin.requestPermissions({ permissions: ['camera'] });

                        let checkCount = 0;
                        while (permissions.camera !== 'granted' && checkCount < 10) {
                            checkCount++;
                            await new Promise(r => setTimeout(r, 150));
                            permissions = await cameraPlugin.checkPermissions();
                            pE_Common.log(`Polling Permission (Versuch ${checkCount}): ${permissions.camera}`);
                            if (permissions.camera === 'granted') break;
                        }
                    }

                    if (permissions.camera !== 'granted') {
                        pE_Common.log("Abbruch: Permission dauerhaft verweigert.", "warn");
                        return pE_Common.toScalar(null, false, "Kamera-Berechtigung abgelehnt.");
                    }

                    // --- 2. SCHRITT: Kamera-Retry mit stabilen String-Werten ---
                    let image = null;
                    for (let attempt = 1; attempt <= 2; attempt++) {
                        const attemptTime = Date.now() - startTime;
                        try {
                            pE_Common.log(`[T+${attemptTime}ms] Starte Kamera-Versuch ${attempt}...`, "info");

                            // WICHTIG: Kein Zugriff auf Capacitor.CameraResultType (verhindert den 'Base64' Crash)
                            //image = await cameraPlugin.getPhoto({
                            //    quality: imageQuality || 90,
                            //    width: imageSize,
                            //    resultType: 'Base64', // String statt Enum
                            //    source: 'CAMERA'      // PROMPT statt CAMERA für bessere Stabilität
                            //});
                            image = await cameraPlugin.getPhoto({
                                quality: imageQuality || 80,
                                width: imageSize,
                                resultType: 'Base64',
                                source: 'PROMPT', // Ermöglicht Kamera UND Galerie-Fallback
                                presentationStyle: 'popover' // Wichtig für iPad-Stabilität
                            });

                            if (image && image.base64String) {
                                pE_Common.log(`[T+${Date.now() - startTime}ms] Erfolg im Versuch ${attempt}!`, "info");
                                break;
                            }

                            pE_Common.log(`[T+${Date.now() - startTime}ms] Versuch ${attempt} lieferte kein Bild.`, "warn");
                            await new Promise(r => setTimeout(r, 600));

                        } catch (innerError) {
                            pE_Common.log(`[T+${Date.now() - startTime}ms] Catch im Versuch ${attempt}: ${innerError.message}`, "error");
                            if (attempt < 2) {
                                await new Promise(r => setTimeout(r, 600));
                            }
                        }
                    }

                    // --- 3. SCHRITT: Finale Validierung ---
                    if (!image || !image.base64String) {
                        const finalTime = Date.now() - startTime;
                        pE_Common.log(`[T+${finalTime}ms] Beide Versuche ohne Bilddaten beendet.`, "error");
                        return pE_Common.toScalar(null, false, "Kamera nicht bereit (Hardware-Timeout).");
                    }

                    // --- 4. SCHRITT: Verarbeitung ---
                    const mainBase64WithHeader = "data:image/jpeg;base64," + image.base64String;
                    pE_Common.log("Base64-String erfolgreich erstellt. Rufe Blazor-Callback auf...");

                    // Konvertierung zu Bytes für die neue einheitliche Rückgabe (out_bytes)
                    const binaryString = atob(image.base64String);
                    const bytes = new Uint8Array(binaryString.length);
                    for (let i = 0; i < binaryString.length; i++) {
                        bytes[i] = binaryString.charCodeAt(i);
                    }

                    // Wir behalten den Callback für Abwärtskompatibilität innerhalb der UI
                    await dotNetHelper.invokeMethodAsync("SetOptimizedImageData", mainBase64WithHeader, mainBase64WithHeader);

                    pE_Common.log(`[T+${Date.now() - startTime}ms] Foto-Prozess erfolgreich abgeschlossen.`);

                    // Rückgabe: out_data bleibt null (oder Header), out_bytes bekommt das Array
                    return pE_Common.toScalar(null, true, null, bytes);

                } catch (e) {
                    pE_Common.log(`KRITISCHER FEHLER nach ${Date.now() - startTime}ms: ${e.message}`, "error");
                    return pE_Common.toScalar(null, false, "Kamera-Fehler: " + (e.message || "Unbekannt"));
                }
            } else {
                // WEB FALLBACK
                pE_Common.log("Nutze Web-Fallback.");
                if (window.pE_Web?.media?.capturePhoto) {
                    return await window.pE_Web.media.capturePhoto(dotNetHelper, videoElementId, imageSize, imageQuality, cropToSquare, thumbnailSize);
                }
                return pE_Common.toScalar(null, false, "Web-Fallback nicht gefunden");
            }
        },

        /**
         * Universal image processing function (GPU accelerated via Canvas).
         * Note: This returns a base64 string (without header) which can be wrapped in ScalarModel.
         */
        processImage: async function (base64, options) {
            pE_Common.log(`Verarbeite Bild (Ziel-Größe: ${options.maxSize}, Format: ${options.format}, Optimized: ${options.isAlreadyOptimized})...`);

            // Falls kein String reinkommt, sofort abbrechen
            if (!base64) {
                pE_Common.log("processImage: Kein Input-String vorhanden.", "error");
                return null;
            }

            // --- FAST-LANE CHECK ---
            // Wenn das Bild bereits durch Capacitor optimiert wurde und kein quadratischer 
            // Zuschnitt (Crop) angefordert ist, geben wir den Input-String sofort zurück.
            if (options.isAlreadyOptimized && !options.cropToSquare) {
                pE_Common.log("processImage: Bild bereits nativ optimiert. Überspringe Canvas-Verarbeitung.", "info");
                if (base64.startsWith('data:')) {
                    return base64;
                } else {
                    return "data:image/jpeg;base64," + base64;
                }
            }

            return new Promise((resolve, reject) => {
                const img = new Image();
                img.onload = () => {
                    try {
                        let targetWidth = img.width;
                        let targetHeight = img.height;
                        const canvas = document.createElement('canvas');
                        const ctx = canvas.getContext('2d');

                        if (options.cropToSquare) {
                            const size = Math.min(img.width, img.height);
                            const sx = (img.width - size) / 2;
                            const sy = (img.height - size) / 2;
                            canvas.width = options.maxSize;
                            canvas.height = options.maxSize;
                            ctx.drawImage(img, sx, sy, size, size, 0, 0, options.maxSize, options.maxSize);
                        } else {
                            let ratio = img.width / img.height;
                            // Skalierung basierend auf maxSize/maxWidth
                            let maxW = options.maxWidth || options.maxSize || img.width;
                            let maxH = options.maxHeight || options.maxSize || img.height;

                            if (img.width > img.height) {
                                targetWidth = maxW;
                                targetHeight = targetWidth / ratio;
                            } else {
                                targetHeight = maxH;
                                targetWidth = targetHeight * ratio;
                            }
                            canvas.width = targetWidth;
                            canvas.height = targetHeight;
                            ctx.drawImage(img, 0, 0, targetWidth, targetHeight);
                        }

                        const mime = options.format === 1 ? "image/png" : "image/jpeg";
                        const quality = (options.quality || 80) / 100;

                        // Gibt den vollständigen String inkl. data:image/... zurück
                        const result = canvas.toDataURL(mime, quality);

                        pE_Common.log("Bildverarbeitung abgeschlossen.", "info");
                        resolve(result);
                    } catch (err) {
                        pE_Common.log("processImage: Fehler bei Canvas-Operation", "error", err);
                        resolve(null);
                    }
                };
                img.onerror = (err) => {
                    pE_Common.log("processImage: Bildquelle konnte nicht geladen werden.", "error", err);
                    resolve(null);
                };

                // Header-Check (Konsistent mit deinen anderen Funktionen)
                if (base64.startsWith('data:')) {
                    img.src = base64;
                } else {
                    // Wir nutzen JPEG als Standard-Fallback für den Input
                    img.src = "data:image/jpeg;base64," + base64;
                }
            });
        },
        
        /**
         * Opens the native photo gallery or a web file picker.
         * Returns a ScalarModel with binary data in out_bytes.
         */
        //pickPhoto: async function (fileInput, dotNetHelper, imageSize, imageQuality, cropToSquare, thumbnailSize) {
        //    const startTime = Date.now();
        //    pE_Common.log(`[${startTime}] Galerie-Auswahl gestartet...`);

        //    if (window.Capacitor?.isNativePlatform()) {
        //        // Plugin-Referenz sicherstellen (Prüft beide möglichen Pfade)
        //        const cameraPlugin = window.Capacitor.Plugins.Camera || (window.pE_Capacitor ? window.pE_Capacitor.Camera : null);

        //        if (!cameraPlugin || typeof cameraPlugin.getPhoto !== 'function') {
        //            pE_Common.log("FEHLER: Camera-Plugin oder getPhoto-Funktion nicht gefunden.", "error");
        //            return pE_Common.toScalar(null, false, "Kamera-Schnittstelle nicht bereit.");
        //        }

        //        try {
        //            // 1. Berechtigungen prüfen (für Galerie/Fotos)
        //            let permissions = await cameraPlugin.checkPermissions();
        //            pE_Common.log(`Galerie Permission-Status: ${permissions.photos}`);

        //            // Auf Android 11+ ist 'photos' oft 'granted' oder 'limited'
        //            if (permissions.photos !== 'granted' && permissions.photos !== 'limited') {
        //                pE_Common.log("Fordere Galerie-Berechtigung an...", "info");
        //                permissions = await cameraPlugin.requestPermissions({ permissions: ['photos'] });
        //            }

        //            // 2. Foto-Auswahl mit STRING-Werten (Verhindert den Base64-Crash)
        //            // Wir nutzen 'PROMPT', damit der User zwischen Kamera und Galerie wählen kann (stabilster Weg)
        //            const image = await cameraPlugin.getPhoto({
        //                quality: imageQuality || 90,
        //                width: imageSize,
        //                resultType: 'Base64',
        //                source: 'PHOTOS'
        //            });

        //            // 3. Sicherheits-Check auf Resultat
        //            if (!image || !image.base64String) {
        //                pE_Common.log("Keine Bilddaten erhalten (Abbruch durch User).", "warn");
        //                return pE_Common.toScalar(null, false, "Auswahl abgebrochen.");
        //            }

        //            const mainBase64WithHeader = "data:image/jpeg;base64," + image.base64String;

        //            // Rückmeldung an Blazor
        //            await dotNetHelper.invokeMethodAsync("SetOptimizedImageData", mainBase64WithHeader, mainBase64WithHeader);

        //            pE_Common.log(`[T+${Date.now() - startTime}ms] Galerie-Auswahl erfolgreich.`);

        //            return pE_Common.toScalar(mainBase64WithHeader, true, null, null);

        //        } catch (e) {
        //            pE_Common.log(`[T+${Date.now() - startTime}ms] Galerie-Fehler: ${e.message}`, "warn");

        //            // Spezifische Meldung bei Hardware-Blockade
        //            let errorMsg = e.message || "User cancelled";
        //            if (errorMsg.includes("undefined")) {
        //                errorMsg = "Hardware-Zugriff verweigert oder Plugin nicht bereit.";
        //            }

        //            return pE_Common.toScalar(null, false, errorMsg);
        //        }
        //    } else {
        //        // WEB FALLBACK
        //        pE_Common.log("Nutze Web-Fallback für Galerie-Auswahl.");
        //        if (window.pE_Web?.media?.pickPhoto) {
        //            // WICHTIG: Hier müssen die korrekten Parameter für die Web-Funktion stehen
        //            return await window.pE_Web.media.pickPhoto(fileInput, dotNetHelper, imageSize, imageQuality, cropToSquare, thumbnailSize);
        //        } else {
        //            pE_Common.log("Web fallback 'pE_Web.media.pickPhoto' nicht gefunden.", "error");
        //            return pE_Common.toScalar(null, false, "Web-Fallback nicht verfügbar.");
        //        }
        //    }
        //},
        pickPhoto: async function (fileInput, dotNetHelper, imageSize, imageQuality, cropToSquare, thumbnailSize) {
            const startTime = Date.now();
            pE_Common.log(`[${startTime}] Galerie-Auswahl gestartet...`);

            if (window.Capacitor?.isNativePlatform()) {
                // Plugin-Referenz sicherstellen (Prüft beide möglichen Pfade)
                const cameraPlugin = window.Capacitor.Plugins.Camera || (window.pE_Capacitor ? window.pE_Capacitor.Camera : null);

                if (!cameraPlugin || typeof cameraPlugin.getPhoto !== 'function') {
                    pE_Common.log("FEHLER: Camera-Plugin oder getPhoto-Funktion nicht gefunden.", "error");
                    return pE_Common.toScalar(null, false, "Kamera-Schnittstelle nicht bereit.");
                }

                try {
                    // 1. Berechtigungen prüfen (für Galerie/Fotos)
                    let permissions = await cameraPlugin.checkPermissions();
                    pE_Common.log(`Galerie Permission-Status: ${permissions.photos}`);

                    // Auf Android 11+ ist 'photos' oft 'granted' oder 'limited'
                    if (permissions.photos !== 'granted' && permissions.photos !== 'limited') {
                        pE_Common.log("Fordere Galerie-Berechtigung an...", "info");
                        permissions = await cameraPlugin.requestPermissions({ permissions: ['photos'] });
                    }

                    // 2. Foto-Auswahl mit STRING-Werten (Verhindert den Base64-Crash)
                    // Wir nutzen 'PROMPT', damit der User zwischen Kamera und Galerie wählen kann (stabilster Weg)
                    const image = await cameraPlugin.getPhoto({
                        quality: imageQuality || 90,
                        width: imageSize,
                        resultType: 'Base64',
                        source: 'PHOTOS'
                    });

                    // 3. Sicherheits-Check auf Resultat
                    //if (!image || !image.base64String) {
                    //    pE_Common.log("Keine Bilddaten erhalten (Abbruch durch User).", "warn");
                    //    return pE_Common.toScalar(null, false, "Auswahl abgebrochen.");
                    //}
                    if (!image || !image.base64String) {
                        pE_Common.log("User hat Kamera-Dialog abgebrochen.", "info");

                        return pE_Common.toScalar(
                            null,
                            false,
                            "User_Cancelled"   // GANZ WICHTIG
                        );
                    }

                    const mainBase64WithHeader = "data:image/jpeg;base64," + image.base64String;

                    // --- NEU: Umwandlung für C# out_bytes Kompatibilität ---
                    // Wir wandeln den Base64-String in ein Byte-Array um, 
                    // damit 'result.out_bytes' in deiner Blazor-Komponente gefüllt ist.
                    const bytes = pE_Common.base64ToBytes(image.base64String);
                    // -------------------------------------------------------

                    // Rückmeldung an Blazor
                    await dotNetHelper.invokeMethodAsync("SetOptimizedImageData", mainBase64WithHeader, mainBase64WithHeader);

                    pE_Common.log(`[T+${Date.now() - startTime}ms] Galerie-Auswahl erfolgreich. Bytes generiert: ${bytes.length}`);

                    // --- GEÄNDERT: Rückgabe von out_bytes statt out_string ---
                    // Wir geben 'null' als ersten Parameter (String) und 'bytes' als vierten Parameter an.
                    // Das entspricht exakt dem Verhalten deiner Web-Version.
                    return pE_Common.toScalar(null, true, null, bytes);
                    // -------------------------------------------------------

                } catch (e) {
                    pE_Common.log(`[T+${Date.now() - startTime}ms] Galerie-Fehler: ${e.message}`, "warn");

                    // Spezifische Meldung bei Hardware-Blockade
                    let errorMsg = e.message || "User cancelled";
                    if (errorMsg.includes("undefined")) {
                        errorMsg = "Hardware-Zugriff verweigert oder Plugin nicht bereit.";
                    }

                    return pE_Common.toScalar(null, false, errorMsg);
                }
            } else {
                // WEB FALLBACK
                pE_Common.log("Nutze Web-Fallback für Galerie-Auswahl.");
                if (window.pE_Web?.media?.pickPhoto) {
                    // WICHTIG: Hier müssen die korrekten Parameter für die Web-Funktion stehen
                    return await window.pE_Web.media.pickPhoto(fileInput, dotNetHelper, imageSize, imageQuality, cropToSquare, thumbnailSize);
                } else {
                    pE_Common.log("Web fallback 'pE_Web.media.pickPhoto' nicht gefunden.", "error");
                    return pE_Common.toScalar(null, false, "Web-Fallback nicht verfügbar.");
                }
            }
        },

        /**
         * Starts the camera stream (Web specific).
         */
        startCamera: async function (videoElementId) {
            if (!window.Capacitor?.isNativePlatform()) {
                if (window.pE_Web?.media?.startCamera) {
                    pE_Common.log(`Starte Web-Kamerastream für Element: ${videoElementId}`);
                    await window.pE_Web.media.startCamera(videoElementId);
                }
            }
        },

        /**
         * Stops the camera stream (Web specific).
         */
        stopCamera: function (videoElementId) {
            if (!window.Capacitor?.isNativePlatform()) {
                if (window.pE_Web?.media?.stopCamera) {
                    pE_Common.log(`Stoppe Web-Kamerastream für Element: ${videoElementId}`);
                    window.pE_Web.media.stopCamera(videoElementId);
                }
            }
        }
    },

    // --- NOTIFICATION MODULE ---
    notifications: {

        /**
         * Internal helper to convert string IDs (like GUIDs) to 32-bit integers.
         * Required by Capacitor LocalNotifications plugin.
         * @param {string} s - The source identifier string.
         * @returns {number} A stable, positive integer hash.
         */
        _hashId: function (s) {
            if (!s) return Math.floor(Math.random() * 1000);
            let hash = 0;
            for (let i = 0; i < s.length; i++) {
                hash = ((hash << 5) - hash) + s.charCodeAt(i);
                hash |= 0;
            }
            return Math.abs(hash);
        },

        /**
         * Requests notification permissions from the OS.
         * @returns {Promise<{out_value_bool: boolean, out_value_str: string, out_err: string}>} ScalarModel
         */
        requestPermission: async function () {
            pE_Common.log("Anfrage für Benachrichtigungs-Berechtigungen...");
            try {
                let status = 'denied';

                if (window.Capacitor?.isNativePlatform()) {
                    // NEU: Nutze die validierte Referenz vom Hauptobjekt
                    const plugin = window.pE_Capacitor.LocalNotifications;

                    if (plugin) {
                        const res = await plugin.requestPermissions();
                        status = res.display; // 'granted', 'denied', or 'prompt'
                    } else {
                        pE_Common.log("LocalNotifications Plugin nicht initialisiert.", "error");
                    }
                } else if (typeof Notification !== 'undefined') {
                    // Web-Standard Fallback
                    status = await Notification.requestPermission();
                }

                return {
                    out_value_bool: status === 'granted',
                    out_value_str: status,
                    out_err: ""
                };
            } catch (e) {
                pE_Common.log("Fehler in requestPermission", "error", e);
                return { out_value_bool: false, out_value_str: "error", out_err: e.message };
            }
        },

        /**
         * Schedules a local notification.
         * @param {string} id - The original string identifier from Blazor.
         * @param {string} title - Notification title.
         * @param {string} body - Notification body.
         * @param {number} delay - Delay in milliseconds.
         * @returns {Promise<{out_value_bool: boolean, out_value_str: string, out_err: string}>} ScalarModel
         */
        schedule: async function (id, title, body, delay) {
            pE_Common.log(`Plane Benachrichtigung: "${title}" in ${delay}ms...`);
            try {
                if (window.Capacitor?.isNativePlatform()) {
                    // NEU: Nutze die validierte Referenz
                    const plugin = window.pE_Capacitor.LocalNotifications;

                    if (!plugin) {
                        const errMs = "LocalNotifications Plugin nicht verfügbar.";
                        pE_Common.log(errMs, "error");
                        return { out_value_bool: false, out_value_str: "", out_err: errMs };
                    }

                    const hashedId = this._hashId(id);

                    await plugin.schedule({
                        notifications: [{
                            id: hashedId,
                            title: title,
                            body: body,
                            schedule: { at: new Date(Date.now() + delay) },
                            extra: { stringId: id } // Speichert die Original-ID für spätere Callbacks
                        }]
                    });

                    pE_Common.log(`Benachrichtigung erfolgreich geplant (HashID: ${hashedId}).`, "info");
                    return { out_value_bool: true, out_value_str: `Scheduled with HashID ${hashedId}`, out_err: "" };
                }

                return { out_value_bool: false, out_value_str: "web_mode_not_supported", out_err: "" };
            } catch (e) {
                pE_Common.log(`Fehler beim Planen der Benachrichtigung '${id}'`, "error", e);
                return { out_value_bool: false, out_value_str: "", out_err: e.message };
            }
        },

        /**
         * Cancels a pending (not yet shown) notification.
         * @param {string} id - The original string identifier.
         * @returns {Promise<{out_value_bool: boolean, out_err: string}>} ScalarModel
         */
        removePending: async function (id) {
            pE_Common.log(`Entferne geplante Benachrichtigung: ${id}...`);
            try {
                if (window.Capacitor?.isNativePlatform()) {
                    // NEU: Nutze die validierte Referenz
                    const plugin = window.pE_Capacitor.LocalNotifications;

                    if (plugin) {
                        const hashedId = this._hashId(id);
                        await plugin.cancel({
                            notifications: [{ id: hashedId }]
                        });
                        pE_Common.log(`Geplante Benachrichtigung (HashID: ${hashedId}) wurde storniert.`, "info");
                    } else {
                        pE_Common.log("LocalNotifications Plugin nicht initialisiert.", "error");
                        return { out_value_bool: false, out_err: "Plugin not available" };
                    }
                }
                return { out_value_bool: true, out_err: "" };
            } catch (e) {
                pE_Common.log(`Fehler beim Stornieren von '${id}'`, "error", e);
                return { out_value_bool: false, out_err: e.message };
            }
        },

        /**
         * Cancels all pending notifications.
         * @returns {Promise<{out_value_bool: boolean, out_err: string}>} ScalarModel
         */
        removeAllPending: async function () {
            pE_Common.log("Entferne alle geplanten Benachrichtigungen...");
            try {
                if (window.Capacitor?.isNativePlatform()) {
                    // NEU: Nutze die validierte Referenz
                    const plugin = window.pE_Capacitor.LocalNotifications;

                    if (!plugin) {
                        pE_Common.log("LocalNotifications Plugin nicht initialisiert.", "error");
                        return { out_value_bool: false, out_err: "Plugin not available" };
                    }

                    const pending = await plugin.getPending();

                    if (pending && pending.notifications && pending.notifications.length > 0) {
                        await plugin.cancel(pending);
                        pE_Common.log(`${pending.notifications.length} Benachrichtigungen erfolgreich storniert.`, "info");
                    } else {
                        pE_Common.log("Keine ausstehenden Benachrichtigungen zum Löschen gefunden.");
                    }
                }
                return { out_value_bool: true, out_err: "" };
            } catch (e) {
                pE_Common.log("Fehler beim Löschen aller geplanten Benachrichtigungen", "error", e);
                return { out_value_bool: false, out_err: e.message };
            }
        },

        /**
         * Removes notifications from the notification tray (Delivered).
         * @param {string} id - The original string identifier.
         * @returns {Promise<{out_value_bool: boolean, out_err: string}>} ScalarModel
         */
        removeDelivered: async function (id) {
            pE_Common.log(`Entferne zugestellte Benachrichtigung aus dem Tray: ${id}...`);
            try {
                if (window.Capacitor?.isNativePlatform()) {
                    // NEU: Nutze die validierte Referenz
                    const plugin = window.pE_Capacitor.LocalNotifications;

                    if (plugin) {
                        const hashedId = this._hashId(id);
                        await plugin.removeDeliveredNotifications({
                            notifications: [{ id: hashedId }]
                        });
                        pE_Common.log(`Benachrichtigung (HashID: ${hashedId}) aus Tray entfernt.`, "info");
                    } else {
                        pE_Common.log("LocalNotifications Plugin nicht initialisiert.", "error");
                        return { out_value_bool: false, out_err: "Plugin not available" };
                    }
                }
                return { out_value_bool: true, out_err: "" };
            } catch (e) {
                pE_Common.log(`Fehler beim Entfernen der zugestellten Benachrichtigung '${id}'`, "error", e);
                return { out_value_bool: false, out_err: e.message };
            }
        },

        getPendingIds: async function () {
            try {
                const plugin = window.pE_Capacitor.LocalNotifications;
                if (!plugin) return { out_value_bool: false, out_err: "Plugin not available" };

                const pending = await plugin.getPending();
                // Wir extrahieren die 'stringId' aus dem 'extra' Feld
                const ids = pending.notifications.map(n => n.extra ? n.extra.stringId : null).filter(id => id !== null);

                return {
                    out_value_bool: true,
                    out_value_str: JSON.stringify(ids),
                    out_err: ""
                };
            } catch (e) {
                return { out_value_bool: false, out_err: e.message };
            }
        },

        /**
         * Clears all delivered notifications from the notification tray.
         * @returns {Promise<{out_value_bool: boolean, out_err: string}>} ScalarModel
         */
        removeAllDelivered: async function () {
            pE_Common.log("Leere das gesamte Benachrichtigungs-Tray...");
            try {
                if (window.Capacitor?.isNativePlatform()) {
                    // NEU: Nutze die validierte Referenz
                    const plugin = window.pE_Capacitor.LocalNotifications;

                    if (plugin) {
                        await plugin.removeAllDeliveredNotifications();
                        pE_Common.log("Alle zugestellten Benachrichtigungen wurden entfernt.", "info");
                    } else {
                        pE_Common.log("LocalNotifications Plugin nicht initialisiert.", "error");
                        return { out_value_bool: false, out_err: "Plugin not available" };
                    }
                }
                return { out_value_bool: true, out_err: "" };
            } catch (e) {
                pE_Common.log("Fehler beim Leeren des Trays", "error", e);
                return { out_value_bool: false, out_err: e.message };
            }
        }
    },

    // --- SQLITE MODULE (COMPLETE & HARDENED) ---
    sqlite: {

        _db: null,
        _sqliteConnection: null,
        _dbName: "",        // Hält den vollen Namen: z.B. pMunus_a1b2c3
        _appPrefix: "",     // NEU: Hält das Präfix: z.B. pMunus
        _currentAccount: "",

        /**
         * Centralized Logger for Remote-Inspection.
         * Integrates with the bridge's _dbLog system.
         */
        _logError: function (context, err) {
            const timestamp = new Date().toISOString();
            const msg = err.message || "Unknown Error";

            // High-visibility console output for developers
            console.error(`%c[SQLite Bridge Error | ${context} | ${timestamp}]`, "color: white; background: red; font-weight: bold;");
            console.error("Error Message:", msg);
            if (err.stack) console.error("Stack Trace:", err.stack);

            // Send to our internal logging system
            pE_Common.log(`SQL-ERROR in ${context}: ${msg}`, "error", err);
        },

        /**
         * Generates a stable encryption key using SHA-256.
         */
        _generateDeterministicKey: async function (dbName, unixTS) {
            pE_Common.log("Generiere kryptographischen Schlüssel...");
            const msgUint8 = new TextEncoder().encode(dbName + "-" + unixTS);
            const hashBuffer = await crypto.subtle.digest('SHA-256', msgUint8);
            const hashArray = Array.from(new Uint8Array(hashBuffer));
            return btoa(String.fromCharCode.apply(null, hashArray));
        },

        /**
         * Initialisiert die Verbindung zur SQLite-Datenbank und führt ggf. Verschlüsselung durch.
         * @param {string} dbName - Der Basis-Name der Datenbank (z.B. "pMunus").
         * @param {string} accountIdentifier - E-Mail oder eindeutige ID zur Generierung des DB-Suffixes.
         * @param {boolean} register - Flag, ob eine neue Datenbank erstellt werden darf (Initialregistrierung).
         * @returns {Promise<string>} - Gibt ein ScalarModel als JSON-String zurück.
         */
        initConnection: async function (dbName, accountIdentifier, register) {
            try {
                const CapacitorSQLite = pE_Capacitor.CapacitorSQLite;
                const SecureStoragePlugin = pE_Capacitor.SecureStoragePlugin;

                if (!CapacitorSQLite) {
                    pE_Common.log("Plugin 'CapacitorSQLite' fehlt!", "error");
                    return JSON.stringify(pE_Common.toScalar(null, false, "PLUGIN_MISSING"));
                }

                // --- 1. STATE INITIALISIERUNG ---
                this._appPrefix = dbName;

                // --- 2. ACCOUNT-ERMITTLUNG ---
                let effectiveAccount = (accountIdentifier && accountIdentifier.trim() !== "") ? accountIdentifier : null;
                if (!effectiveAccount) {
                    pE_Common.log("Prüfe SecureStorage auf Account...");
                    try {
                        const stored = await SecureStoragePlugin.get({ key: "last_logged_in_account" });
                        effectiveAccount = (stored && stored.value) ? stored.value : null;
                    } catch (e) {
                        effectiveAccount = null;
                    }
                }
                if (!effectiveAccount) {
                    pE_Common.log("Abbruch: Kein Account gefunden.", "error");
                    return JSON.stringify(pE_Common.toScalar(null, false, "NO_USER_ACCOUNT_PROVIDED"));
                }
                const normalizedEmail = effectiveAccount.trim().toLowerCase();

                // Hash generieren
                const accountHash = await (async (str) => {
                    const msgUint8 = new TextEncoder().encode(str);
                    const hashBuffer = await crypto.subtle.digest('SHA-256', msgUint8);
                    const hashArray = Array.from(new Uint8Array(hashBuffer));
                    return hashArray.map(b => b.toString(16).padStart(2, '0')).join('').substring(0, 10);
                })(normalizedEmail);

                const effectiveDbName = `${this._appPrefix}_${accountHash}`;
                pE_Common.log(`[INIT] User: ${normalizedEmail} | DB: ${effectiveDbName} | Register: ${register}`);

                // === OPTIMIERT: REUSE-CHECK (Interner Cache-Check) ===
                if (this._db && this._dbName === effectiveDbName && this._currentAccount === normalizedEmail) {
                    try {
                        const openStatus = await CapacitorSQLite.isDBOpen({ database: effectiveDbName });
                        if (openStatus.result) {
                            pE_Common.log("SQLite bereits initialisiert & offen -> direkte Wiederverwendung", "info");
                            return JSON.stringify(pE_Common.toScalar("Connected (reused)", true, ""));
                        }
                    } catch (e) {
                        pE_Common.log(`Reuse-Check Hinweis: ${e.message}`, "log");
                    }
                }

                // --- 3. BRIDGE-GUARD ---
                await new Promise(r => setTimeout(r, 50));

                // --- 4. EXISTENZ-PRÜFUNG ---
                let dbExists = false;
                try {
                    const check = await CapacitorSQLite.isDatabase({ database: effectiveDbName });
                    dbExists = !!(check && check.result);
                    if (dbExists) pE_Common.log(`DB Existenz bestätigt.`);
                } catch (e) {
                    pE_Common.log("Fehler bei Existenzprüfung, nehme 'nicht vorhanden' an.", "warn");
                    dbExists = false;
                }

                if (!dbExists && !register) {
                    pE_Common.log(`Abbruch: DB ${effectiveDbName} nicht lokal und register=false.`, "warn");
                    return JSON.stringify(pE_Common.toScalar(null, false, "LOCAL_DB_NOT_FOUND"));
                }

                // --- 5. CONTEXT SWITCH ---
                if (this._db && (this._dbName !== effectiveDbName || this._currentAccount !== normalizedEmail)) {
                    pE_Common.log(`Kontextwechsel: Schließe ${this._dbName}`);
                    try {
                        await CapacitorSQLite.close({ database: this._dbName });
                    } catch (e) { }
                }

                // --- 6. PASSWORT GENERIERUNG ---
                const dbPassword = await this._generateDeterministicKey(effectiveDbName, normalizedEmail);

                // --- 7. ENCRYPTION SECRET (FIXED FOR IPHONE X / KEYCHAIN) ---
                // Wir rufen es immer auf, da das Plugin das Secret im Session-Speicher braucht.
                try {
                    pE_Common.log("Setze/Prüfe Encryption Secret...");
                    await CapacitorSQLite.setEncryptionSecret({ passphrase: dbPassword });
                } catch (e) {
                    const errMs = e.message ? e.message.toLowerCase() : "";
                    // Wir ignorieren den Fehler, wenn das Passwort bereits im iOS Keychain liegt.
                    if (errMs.includes("already been set") ||
                        errMs.includes("already stored") ||
                        errMs.includes("keychain")) {
                        pE_Common.log("Encryption Secret bereits im Keychain vorhanden - fahre fort.", "info");
                    } else {
                        pE_Common.log(`Kritischer Fehler bei setEncryptionSecret: ${e.message}`, "error");
                        return JSON.stringify(pE_Common.toScalar(null, false, "ENCRYPTION_INIT_FAILED"));
                    }
                }

                // --- 8. & 9. CONNECTION & OPEN (RELOAD-SAFE FIX) ---
                try {
                    let connOk = false;

                    // Zuerst Handle sicherstellen (retrieve)
                    try {
                        const conn = await CapacitorSQLite.retrieveConnection({ database: effectiveDbName });
                        if (conn && conn.result) {
                            connOk = true;
                            const checkOpen = await CapacitorSQLite.isDBOpen({ database: effectiveDbName });
                            if (!checkOpen.result) {
                                await CapacitorSQLite.open({ database: effectiveDbName, readonly: false });
                            }
                            pE_Common.log("initConnection: Verbindung wiederhergestellt und offen.");
                        }
                    } catch (e) {
                        connOk = false;
                    }

                    // Wenn kein Handle existiert (Erster Start oder nach App-Kill)
                    if (!connOk) {
                        try {
                            await CapacitorSQLite.createConnection({
                                database: effectiveDbName,
                                encrypted: true,
                                mode: "secret",
                                version: 1,
                                readonly: false
                            });
                        } catch (createErr) {
                            if (!createErr.message.includes("already exists")) throw createErr;
                        }
                        await CapacitorSQLite.open({ database: effectiveDbName, readonly: false });
                        pE_Common.log("Datenbank neu erstellt und geöffnet.");
                    }
                } catch (e) {
                    const msg = e.message || "";
                    if (msg.includes("encrypted") || msg.includes("passphrase")) {
                        return JSON.stringify(pE_Common.toScalar(null, false, "SECURITY_VIOLATION"));
                    }
                    if (!msg.includes("already open") && !msg.includes("already exists")) {
                        this._logError("OPEN_DATABASE", e);
                        return JSON.stringify(pE_Common.toScalar(null, false, "OPEN_FAILED: " + msg));
                    }
                }

                // --- 10. STATE SPEICHERN ---
                this._db = CapacitorSQLite;
                this._dbName = effectiveDbName;
                this._currentAccount = normalizedEmail;

                pE_Common.log(`>>> SQLITE SYSTEM BEREIT: ${effectiveDbName}`, "info");
                return JSON.stringify(pE_Common.toScalar(true, true, ""));

            } catch (err) {
                this._logError("INIT_FATAL", err);
                return JSON.stringify(pE_Common.toScalar(null, false, "BRIDGE_FATAL: " + err.message));
            }
        },
       
        /**
         * Prüft den aktuellen Status der Datenbank (Schema-Check).
         * @returns {Promise<string>} Gibt ein ScalarModel als JSON-String zurück.
         */
        getDatabaseStatus: async function () {
            try {
                pE_Common.log("JS-BRIDGE", "Prüfe Datenbank-Status (Schema-Check)...", "info");

                /**
                 * 1. CRITICAL SAFETY CHECK
                 * Nutzt die in initConnection gesetzte Instanz (_db).
                 * Falls gerufen wird, während das Plugin noch lädt oder initConnection noch läuft.
                 */
                if (!this._db || !this._dbName) {
                    pE_Common.log("JS-BRIDGE", "Status-Check: Plugin/DB nicht bereit.", "warn");

                    const initRes = pE_Common.toScalar("INITIALIZING", true, "");
                    initRes.out_value_int = pE_Common.DB_STATUS.INITIALIZING;
                    return JSON.stringify(initRes);
                }

                /**
                 * 2. V7 Direct-API Call:
                 * Wir prüfen, ob die Tabelle 'AppParameter' existiert.
                 */
                const result = await this._db.query({
                    database: this._dbName,
                    statement: "SELECT name FROM sqlite_master WHERE type='table' AND name='AppParameter';",
                    values: []
                });

                // --- NEW: iOS Metadaten-Shift ---
                let rows = (result && result.values) ? result.values : [];

                // Falls das erste Objekt nur die Spaltennamen für iOS enthält, entfernen wir es für die Validierung.
                if (rows.length > 1 && rows[0].hasOwnProperty('ios_columns')) {
                    pE_Common.log("iOS Metadaten-Header erkannt und für Validierung entfernt (getDatabaseStatus).", "info");
                    rows = rows.slice(1); // Erstellt eine Kopie ohne den Header
                }
                // --------------------------------

                // 3. Ergebnisse auswerten
                const tableExists = Array.isArray(rows) && rows.length > 0;
                let finalState = tableExists ? pE_Common.DB_STATUS.READY : pE_Common.DB_STATUS.NEW;
                let statusName = tableExists ? "READY" : "NEW";

                pE_Common.log("JS-BRIDGE", `Status-Check: ${statusName} erkannt.`, "info");

                // 4. ScalarModel bauen
                const finalResult = pE_Common.toScalar(statusName, true, "");
                finalResult.out_value_int = finalState;

                const jsonResult = JSON.stringify(finalResult);
                pE_Common.log("JS-BRIDGE", "getDatabaseStatus sendet JSON: " + jsonResult, "info");

                return jsonResult;

            } catch (e) {
                /**
                 * 5. Error Handling:
                 * Bei einem Fehler geben wir den ERROR Status zurück.
                 */
                this._logError("STATUS_CHECK_FAIL", e);

                const errResult = pE_Common.toScalar("ERROR", false, e.message);
                errResult.out_value_int = pE_Common.DB_STATUS.ERROR;
                return JSON.stringify(errResult);
            }
        },      
                
        /**
         * Führt eine SQL-Abfrage aus, die exakt einen einzelnen Wert (Scalar) zurückliefern muss.
         * Enthält eine strikte Validierung auf 1x1 Ergebnisse, um Fehlprogrammierungen im SQL zu verhindern.
         * * @param {string} sql - Das SELECT-Statement.
         * @param {Object|Array} typedParams - Parameter für die Query.
         * @returns {Promise<string>} ScalarModel als JSON-String.
         */
        /**
         * Führt eine SQL-Abfrage aus, die exakt einen einzelnen Wert (Scalar) zurückliefern muss.
         * Diese Version enthält strikte Validierungen und detailliertes Logging.
         */
        scalar: async function (sql, typedParams) {
            // Diagnose-Array für den Fehlerfall
            let paramDiagnostic = [];

            try {
                pE_Common.log(`[SQLite] SCALAR Start: ${sql.substring(0, 100)}...`);

                /**
                 * 1. CRITICAL SAFETY CHECK (SIGSEGV Protection)
                 * Überprüfung des internen States (this._dbName wurde in initConnection gesetzt).
                 */
                //if (!this._db || !this._dbName) {
                //    pE_Common.log("SCALAR abgebrochen: Datenbank noch nicht initialisiert.", "error");
                //    // Wir serialisieren das Fehler-Objekt von toScalar
                //    const errModel = pE_Common.toScalar(null, false, "DATABASE_NOT_INITIALIZED");
                //    return JSON.stringify(errModel);
                //}
                if (!this._db || !this._dbName) {
                    pE_Common.log("Abbruch: Datenbank nicht verbunden (Status: NOT_CONNECTED)", "warn");

                    // Wir übergeben den Integer-Wert des Enums (4) als Hauptwert.
                    // success: false signalisiert Blazor, dass die Query nicht ausgeführt wurde.
                    // Im err-Feld lassen wir zur Sicherheit den Namen stehen, falls wir ihn loggen wollen.
                    const errModel = pE_Common.toScalar(pE_Common.DB_STATUS.NOT_CONNECTED, false, "NOT_CONNECTED");

                    return JSON.stringify(errModel);
                }

                /**
                 * 2. Parameter-Handling & Type-Fixing (Präzisionsschutz)
                 * Wir wandeln numerische Strings nur in Zahlen um, wenn sie kürzer als 15 Zeichen sind,
                 * um Unix-Timestamps (ms) vor der wissenschaftlichen Notation in JS zu schützen.
                 */
                let cleanValues = [];
                if (typedParams && typeof typedParams === 'object' && !Array.isArray(typedParams)) {
                    cleanValues = Object.keys(typedParams).map(key => {
                        let val = typedParams[key];

                        // Type-Fixing mit Längenprüfung (< 15)
                        //if (typeof val === 'string' && val.trim() !== '' && !isNaN(val) && val.length < 15) {
                        //    val = Number(val);
                        //}

                        paramDiagnostic.push({ Parameter: key, Wert: val, JS_Typ: typeof val });
                        return val;
                    });
                } else if (Array.isArray(typedParams)) {
                    cleanValues = typedParams.map((val, index) => {

                        // Type-Fixing mit Längenprüfung (< 15)
                        //if (typeof val === 'string' && val.trim() !== '' && !isNaN(val) && val.length < 15) {
                        //    val = Number(val);
                        //}

                        paramDiagnostic.push({ Index: index, Wert: val, JS_Typ: typeof val });
                        return val;
                    });
                }

                /**
                 * 3. V7 Direct-API Call
                 * Nutzt den in initConnection festgesetzten Datenbanknamen.
                 */
                const result = await this._db.query({
                    database: this._dbName,
                    statement: sql,
                    values: cleanValues
                });

                // ==========================================================
                // DIAGNOSE-LOG
                // Wir loggen das rohe Ergebnis, bevor wir es für Blazor aufbereiten
                pE_Common.log(`[DIAGNOSE] Capacitor Raw Values: ${JSON.stringify(result.values)}`, "info");
                if (paramDiagnostic.length > 0) {
                    pE_Common.log(`[DIAGNOSE] Used Params: ${JSON.stringify(paramDiagnostic)}`, "info");
                }
                // ==========================================================
                /**
                 * 4. Ergebnis-Extraktion & STRIKTE VALIDIERUNG
                 */
                let finalModel;

                if (result && Array.isArray(result.values) && result.values.length > 0) {
                    // --- NEW: iOS Metadaten-Shift ---
                    let rows = result.values;

                    // If the first object only contains the column names for iOS, we remove it for validation.
                    if (rows.length > 1 && rows[0].hasOwnProperty('ios_columns')) {
                        pE_Common.log("iOS Metadaten-Header erkannt und für Validierung entfernt.", "info");
                        rows = rows.slice(1); // Erstellt eine Kopie ohne den Header
                    }
                    // --------------------------------

                    const firstRow = rows[0];
                    const keys = Object.keys(firstRow);

                    /**
                     * STRIKTE PRÜFUNG (Methodik: Fail-Fast)
                     * Wir prüfen nun gegen das bereinigte 'rows' Array.
                     */
                    if (rows.length > 1 || keys.length > 1) {
                        const violationMsg = `SCALAR_VIOLATION: SQL lieferte ${rows.length} Zeilen und ${keys.length} Spalten. Erwartet wird exakt 1x1.`;
                        pE_Common.log(violationMsg, "error");

                        finalModel = pE_Common.toScalar(null, false, violationMsg);
                        return JSON.stringify(finalModel);
                    }

                    const firstColKey = keys[0];
                    const val = firstRow[firstColKey];

                    pE_Common.log(`SCALAR Result gefunden: ${val} (Spalte: ${firstColKey})`, "info");

                    // Normalisieren via toScalar
                    finalModel = pE_Common.toScalar(
                        (val !== undefined && val !== null) ? val.toString() : null,
                        true,
                        ""
                    );
                } else {
                    /**
                     * Leeres Ergebnis bei Scalar ist oft ein valider Zustand (z.B. COUNT = 0).
                     */
                    pE_Common.log("SCALAR: Keine Daten gefunden (Empty Set).", "log");
                    finalModel = pE_Common.toScalar(null, true, "");
                }

                // NEU: Logge das finale Objekt vor der Serialisierung für das Debugging der Typen
                pE_Common.log(`[DIAGNOSE] ScalarModel vor JSON-Transfer: bool=${finalModel.out_value_bool}, str=${finalModel.out_value_str}`, "info");

                // Rückgabe als JSON-String an Blazor
                return JSON.stringify(finalModel);

            } catch (err) {
                /**
                 * 5. Detailed Error Reporting
                 */
                this._logError("SCALAR_EXEC", err);

                console.group(`%c[SQLite] SCALAR FAILED!`, "color: white; background: #E67E22; padding: 4px;");
                console.error("Fehler:", err.message);

                if (paramDiagnostic.length > 0) {
                    console.table(paramDiagnostic);
                    pE_Common.log(`Parameter-Analyse: ${JSON.stringify(paramDiagnostic)}`, "warn");
                }

                if (err.message.includes("code 20")) {
                    pE_Common.log("DIAGNOSE: SQLite Code 20 (Type Mismatch).", "error");
                }
                console.groupEnd();

                // Fehlerfall ebenfalls als JSON serialisieren
                const errorModel = pE_Common.toScalar(null, false, err.message);
                return JSON.stringify(errorModel);
            }
        },
                
        /**
         * Führt eine SQL-Abfrage aus und gibt das Ergebnis als ScalarModel-JSON zurück.
         * @param {string} sql - Das SQL-SELECT-Statement.
         * @param {Object} typedParams - Objekt oder Array mit typsicheren Parametern.
         * @returns {Promise<string>} JSON-String eines ScalarModels (Ergebnis-Array in out_value_str).
         */
        query: async function (sql, typedParams) {
            // Variable für die Diagnose-Ausgabe im Fehlerfall vorbereiten
            let paramDiagnostic = [];

            try {
                pE_Common.log(`[CAP pE_Capacitor.sqlite query] START`);
                pE_Common.log(`[CAP pE_Capacitor.sqlite query] sql = ${sql.substring(0, 100)}...`);
                /**
                 * 1. CRITICAL SAFETY CHECK (SIGSEGV Protection)
                 * Nutzt jetzt das zentrale Enum-System für Blazor.
                 */
                if (!this._db || !this._dbName) {
                    pE_Common.log("[CAP pE_Capacitor.sqlite query] Query CANCELLED: No active connection or plugin not ready.", "error");
                    // Rückgabe als ScalarModel mit dem Status NOT_CONNECTED
                    return JSON.stringify(pE_Common.toScalar(null, false, pE_Common.DB_STATUS.NOT_CONNECTED));
                }

                /**
                 * 2. Parameter-Handling & Type-Fixing (Präzisionsschutz)
                 * Bewahrt die Präzision von Unix-Timestamps (> 15 Zeichen).
                 */
                let cleanValues = [];

                if (typedParams && typeof typedParams === 'object' && !Array.isArray(typedParams)) {
                    cleanValues = Object.keys(typedParams).map(key => {
                        let val = typedParams[key];
                        //// Type-Fixing mit Längenprüfung (< 15)
                        //if (typeof val === 'string' && val.trim() !== '' && !isNaN(val) && val.length < 15) {
                        //    val = Number(val);
                        //}
                        paramDiagnostic.push({ Parameter: key, Wert: val, JS_Typ: typeof val });
                        return val;
                    });
                } else if (Array.isArray(typedParams)) {
                    cleanValues = typedParams.map((val, index) => {
                        //// Type-Fixing mit Längenprüfung (< 15)
                        //if (typeof val === 'string' && val.trim() !== '' && !isNaN(val) && val.length < 15) {
                        //    val = Number(val);
                        //}
                        paramDiagnostic.push({ Index: index, Wert: val, JS_Typ: typeof val });
                        return val;
                    });
                }

                /**
                 * 3. V7 Direct-API Call
                 */
                const result = await this._db.query({
                    database: this._dbName,
                    statement: sql,
                    values: cleanValues
                });

                // 4. Ergebnis-Verarbeitung
                let data = (result && Array.isArray(result.values)) ? result.values : [];

                // --- NEW: iOS Metadaten-Shift ---
                // Wenn das erste Objekt nur die Spaltennamen für iOS enthält, entfernen wir es.
                if (data.length > 1 && data[0].hasOwnProperty('ios_columns')) {
                    pE_Common.log("[CAP pE_Capacitor.sqlite query] iOS metadata headers detected and removed in QUERY.", "info");
                    data = data.slice(1); // Kopie ohne den Metadaten-Header
                }
                // --------------------------------

                pE_Common.log(`[CAP pE_Capacitor.sqlite query] Query successfully executed on ${this._dbName}. data sets: ${data.length}`, "info");

                /**
                 * 5. SUCCESS-RÜCKGABE ALS SCALARMODEL
                 * Das Ergebnis-Array wird als JSON-String in out_value_str verpackt.
                 */
                const successModel = pE_Common.toScalar(JSON.stringify(data), true, "");

                pE_Common.log(`[CAP pE_Capacitor.sqlite query] data = ${data}`);
                pE_Common.log(`[CAP pE_Capacitor.sqlite query] successModel = ${successModel}`);
                pE_Common.log(`[CAP pE_Capacitor.sqlite query] END`);

                return JSON.stringify(successModel);

            } catch (err) {
                /**
                 * 6. Detailliertes Fehler-Reporting (Unverändert)
                 */
                this._logError("QUERY_EXEC_FAIL", err);

                console.group(`%c[SQLite] KRITISCHER FEHLER in Query`, "color: white; background: red; font-weight: bold; padding: 4px;");
                console.error(`Nachricht: ${err.message}`);
                console.warn("SQL:", sql);

                if (paramDiagnostic.length > 0) {
                    console.log("Gesendete Parameter-Struktur:");
                    console.table(paramDiagnostic);
                    pE_Common.log(`Parameter-Diagnose: ${JSON.stringify(paramDiagnostic)}`, "warn");
                }

                if (err.message.includes("datatype mismatch") || err.message.includes("code 20")) {
                    pE_Common.log("DIAGNOSE: SQLite Code 20 (Type Mismatch).", "error");
                }
                console.groupEnd();

                /**
                 * 7. FEHLER-RÜCKGABE ALS SCALARMODEL
                 * Statt "[]" liefern wir nun den echten Fehlertext an Blazor.
                 */
                const errorModel = pE_Common.toScalar(null, false, err.message || "Unknown Query Error");
                return JSON.stringify(errorModel);
            }
        },
                
        /**
         * Führt ein SQL-Statement aus, das Daten verändert (INSERT, UPDATE, DELETE).
         * @param {string} sql - Das auszuführende SQL-Kommando.
         * @param {Object} typedParams - Objekt mit typsicheren Parametern.
         * @returns {Promise<string>} Gibt ein ScalarModel als JSON-String zurück.
         */
        execute: async function (sql, typedParams) {
            let paramDiagnostic = [];
            try {
                pE_Common.log(`[SQLite] EXECUTE/RUN Start: ${sql.substring(0, 50)}...`, "info");

                // --- 1. SAFETY BARRIER ---
                // Wenn die DB nicht bereit ist, brechen wir kontrolliert ab.
                if (!this._db || !this._dbName) {
                    pE_Common.log("EXECUTE ABGEBROCHEN: Datenbank noch nicht initialisiert.", "error");
                    // Rückgabe des Enum-Status über toScalar
                    return JSON.stringify(pE_Common.toScalar(null, false, pE_Common.DB_STATUS.NOT_CONNECTED));
                }

                /**
                 * 2. Parameter-Handling & Type-Fixing (Präzisionsschutz)
                 */
                let cleanValues = [];
                let isDictionary = (typedParams && typeof typedParams === 'object' && !Array.isArray(typedParams));

                if (isDictionary) {
                    cleanValues = Object.keys(typedParams).map(key => {
                        let val = typedParams[key];
                        //if (typeof val === 'string' && val.trim() !== '' && !isNaN(val) && val.length < 15) {
                        //    val = Number(val);
                        //}
                        paramDiagnostic.push({ Parameter: key, Wert: val, JS_Typ: typeof val });
                        return val;
                    });
                } else if (Array.isArray(typedParams)) {
                    cleanValues = typedParams.map((val, index) => {
                        //if (typeof val === 'string' && val.trim() !== '' && !isNaN(val) && val.length < 15) {
                        //    val = Number(val);
                        //}
                        paramDiagnostic.push({ Index: index, Wert: val, JS_Typ: typeof val });
                        return val;
                    });
                } else {
                    cleanValues = [];
                }

                const hasParams = cleanValues.length > 0;
                let result;

                /**
                 * 3. Fallunterscheidung nach V7-Standard
                 */
                if (hasParams) {
                    pE_Common.log(`[SQLite] Mode: RUN auf ${this._dbName} (mit Parametern)`, "info");
                    result = await this._db.run({
                        database: this._dbName,
                        statement: sql,
                        values: cleanValues
                    });
                } else {
                    pE_Common.log(`[SQLite] Mode: EXECUTE auf ${this._dbName} (Batch/Skript)`, "info");
                    result = await this._db.execute({
                        database: this._dbName,
                        statements: sql
                    });
                }

                /**
                 * 4. Ergebnis-Extraktion
                 */
                let changesCount = 0;
                if (result && result.changes) {
                    if (typeof result.changes.changes === 'number') {
                        changesCount = result.changes.changes;
                    } else if (typeof result.changes === 'number') {
                        changesCount = result.changes;
                    }
                }

                pE_Common.log(`[SQLite] EXECUTE erfolgreich auf ${this._dbName}. Betroffene Zeilen: ${changesCount}`, "info");

                /**
                 * 5. ERFOLGS-RÜCKGABE ALS SCALARMODEL (JSON-String)
                 * Wir übergeben true als Wert.
                 */
                return JSON.stringify(pE_Common.toScalar("true", true, ""));

            } catch (err) {
                /**
                 * 6. Detailliertes Fehler-Reporting (Deep-Diagnostic)
                 */
                this._logError("EXECUTE_WRITE_FAIL", err);

                console.group(`%c[SQLite] EXECUTE FAILED!`, "color: white; background: red; font-weight: bold; padding: 4px;");
                console.error("Fehlermeldung:", err.message);
                console.warn("SQL:", sql);

                if (paramDiagnostic.length > 0) {
                    console.log("Parameter-Analyse:");
                    console.table(paramDiagnostic);
                    pE_Common.log(`Execute-Fail Parameter: ${JSON.stringify(paramDiagnostic)}`, "warn");
                }
                console.groupEnd();

                // Rückgabe des Fehlers über toScalar (success = false)
                return JSON.stringify(pE_Common.toScalar(null, false, err.message || "SQL_EXEC_ERROR"));
            }
        },

        /**
         * Führt eine Liste von SQL-Statements (Batch) innerhalb einer Transaktion aus.
         * @param {string[]} statements - Ein Array von SQL-Strings.
         * @returns {Promise<string>} Gibt ein ScalarModel als JSON-String zurück.
         */
        executeBatch: async function (statements) {
            try {
                /**
                 * 1. CRITICAL SAFETY CHECK
                 */
                if (!this._db || !this._dbName) {
                    const errNoDb = pE_Common.toScalar(null, false, pE_Common.DB_STATUS.NOT_CONNECTED);
                    const jsonErrNoDb = JSON.stringify(errNoDb);
                    pE_Common.log("JS-BRIDGE", "executeBatch ABGEBROCHEN: Keine Verbindung.", "error");
                    return jsonErrNoDb;
                }

                let stmtsArray = Array.isArray(statements) ? statements : [statements];
                pE_Common.log("JS-BRIDGE", `Batch-Verarbeitung auf ${this._dbName}: ${stmtsArray.length} Statement(s)...`, "info");

                /**
                 * 2. Iterative Verarbeitung
                 */
                for (let i = 0; i < stmtsArray.length; i++) {
                    const cleanStmt = stmtsArray[i] ? stmtsArray[i].trim() : "";
                    if (cleanStmt.length === 0) continue;

                    try {
                        await this._db.execute({
                            database: this._dbName,
                            statements: cleanStmt
                        });

                        await new Promise(resolve => setTimeout(resolve, 20));

                    } catch (innerErr) {
                        /**
                         * 3. Deep-Diagnostic im Fehlerfall
                         */
                        console.group(`%c[SQLite] BATCH ERROR @ Statement #${i + 1}`, "color: white; background: #8B0000; padding: 4px;");
                        console.error("Datenbank:", this._dbName, "SQL:", cleanStmt, "Fehler:", innerErr.message);
                        console.groupEnd();

                        pE_Common.log("JS-BRIDGE", `Fehler bei Statement #${i + 1}: ${innerErr.message}`, "error");
                        throw innerErr; // Weiterwerfen für globalen Catch
                    }
                }

                pE_Common.log("JS-BRIDGE", `Batch auf ${this._dbName} erfolgreich abgeschlossen.`, "info");

                // 4. Erfolg als JSON-String zurückgeben
                const finalResult = pE_Common.toScalar("true", true, "");
                const jsonResult = JSON.stringify(finalResult);

                // WICHTIG: Das Log zeigt uns genau, was an Blazor geht
                pE_Common.log("JS-BRIDGE", "executeBatch FINAL JSON: " + jsonResult, "info");

                return jsonResult;

            } catch (err) {
                /**
                 * 5. Globales Fehler-Reporting
                 */
                this._logError("BATCH_EXEC_FAIL", err);

                const errResult = pE_Common.toScalar(null, false, "BATCH_ERROR: " + err.message);
                const errJson = JSON.stringify(errResult);

                pE_Common.log("JS-BRIDGE", "executeBatch ERROR JSON: " + errJson, "error");
                return errJson;
            }
        },  
        
        /**
         * Ruft die aktuelle Schema-Version der Datenbank ab.
         * @returns {Promise<string>} Gibt ein ScalarModel als JSON-String zurück.
         * HINWEIS: Die Versionsnummer befindet sich in out_value_int.
         */
        getVersion: async function () {
            try {
                pE_Common.log(`[SQLite] Lese DB-Version für ${this._dbName} (PRAGMA user_version)...`, "info");

                /**
                 * 1. CRITICAL SAFETY CHECK (SIGSEGV Protection)
                 */
                if (!this._db || !this._dbName) {
                    pE_Common.log("getVersion: Plugin oder DB-Name nicht bereit.", "warn");
                    return JSON.stringify(pE_Common.toScalar(null, false, pE_Common.DB_STATUS.NOT_CONNECTED));
                }

                /**
                 * 2. V7 Direct-API Call
                 */
                const result = await this._db.query({
                    database: this._dbName,
                    statement: "PRAGMA user_version;",
                    values: []
                });

                /**
                 * 3. Daten-Extraktion (iOS-Safe für V7/SPM-Preview)
                 * Wir müssen prüfen, ob die erste Zeile nur Metadaten (ios_columns) enthält.
                 */
                let version = 0;
                let rows = (result && Array.isArray(result.values)) ? result.values : [];

                // --- NEW: iOS Metadaten-Shift ---
                // Konsistente Bereinigung des Arrays wie in den anderen Funktionen
                if (rows.length > 1 && rows[0].hasOwnProperty('ios_columns')) {
                    pE_Common.log("[SQLite] iOS Metadaten-Header in getVersion erkannt und entfernt.", "info");
                    rows = rows.slice(1);
                }
                // --------------------------------

                if (rows.length > 0) {
                    let dataRow = rows[0];

                    // Robuste Extraktion aus der korrekten Zeile
                    if (dataRow.hasOwnProperty('user_version')) {
                        version = dataRow.user_version;
                    } else if (dataRow.hasOwnProperty('USER_VERSION')) {
                        version = dataRow.USER_VERSION;
                    } else {
                        const values = Object.values(dataRow);
                        if (values.length > 0 && values[0] !== undefined && values[0] !== null) {
                            version = values[0];
                        }
                    }
                }

                /**
                 * 4. Validierung & Fallback
                 */
                let finalVersion = parseInt(version);
                if (isNaN(finalVersion)) {
                    pE_Common.log(`[SQLite] Version war nicht numerisch (${version}), setze auf 0.`, "warn");
                    finalVersion = 0;
                }

                pE_Common.log(`[SQLite] Aktuelle DB-Version von ${this._dbName}: ${finalVersion}`, "info");

                /**
                 * 5. SUCCESS-RÜCKGABE ALS SCALARMODEL
                 * Wir übergeben die Zahl, damit out_value_int befüllt wird.
                 */
                const successResult = pE_Common.toScalar(finalVersion, true, "");

                /**
                 * FIX: Explizit auf true setzen.
                 * Da toScalar die Zahl 2 (oder höher) als bool 'false' interpretiert,
                 * erzwingen wir hier 'true', damit die Bridge-Validierung im C# 
                 * (VerifyJsonToScalarModel) auf jeden Fall grünes Licht gibt.
                 */
                successResult.out_value_bool = true;

                return JSON.stringify(successResult);

            } catch (e) {
                /**
                 * 6. EHRLICHES ERROR-HANDLING
                 * Wir geben success = false zurück. 
                 * Blazor wird die Exception werfen und den Fehler im UI/Log anzeigen.
                 */
                this._logError("GET_VERSION_FAIL", e);
                pE_Common.log(`[SQLite] Kritischer Fehler beim Lesen der user_version: ${e.message}`, "error");

                // success = false bewirkt, dass out_value_bool false wird und out_err befüllt wird
                const errorResult = pE_Common.toScalar(null, false, "GET_VERSION_FAIL: " + e.message);
                return JSON.stringify(errorResult);
            }
        },
        
        /**
         * Setzt die Schema-Version in der Datenbank (User Version).
         * @param {number} version - Die Ziel-Versionsnummer.
         * @returns {Promise<string>} ScalarModel als JSON-String.
         */
        setVersion: async function (version) {
            try {
                const targetVersion = parseInt(version);

                if (isNaN(targetVersion)) {
                    return JSON.stringify(pE_Common.toScalar(null, false, `Ungültige Version: ${version}`));
                }

                /**
                 * 1. SAFETY CHECK
                 */
                if (!this._db || !this._dbName) {
                    return JSON.stringify(pE_Common.toScalar(null, false, pE_Common.DB_STATUS.NOT_CONNECTED));
                }

                pE_Common.log(`[SQLite] Setze DB-Version auf: ${targetVersion}`, "info");

                /**
                 * 2. EXECUTE
                 * Nutzt 'execute' statt 'query', um den Datei-Header sicher zu schreiben.
                 */
                await this._db.execute({
                    database: this._dbName,
                    statements: `PRAGMA user_version = ${targetVersion};`
                });

                /**
                 * 3. VERIFIKATION (iOS-Safe gegen ios_columns Header)
                 * Auf iOS liefert das Plugin oft zuerst ein Objekt mit Spaltennamen.
                 */
                const result = await this._db.query({
                    database: this._dbName,
                    statement: "PRAGMA user_version;",
                    values: []
                });

                let currentVersion = -1;
                let rows = (result && Array.isArray(result.values)) ? result.values : [];

                // --- NEW: iOS Metadaten-Shift ---
                // Konsistente Bereinigung des Arrays wie in den anderen Funktionen
                if (rows.length > 1 && rows[0].hasOwnProperty('ios_columns')) {
                    pE_Common.log("[SQLite] iOS Metadaten-Header in setVersion erkannt und für Verifikation entfernt.", "info");
                    rows = rows.slice(1);
                }
                // --------------------------------

                if (rows.length > 0) {
                    // Nach dem Shift ist der echte Wert nun sicher in Index 0
                    let dataRow = rows[0];

                    // Robuste Extraktion des Wertes aus der gefundenen Zeile
                    if (dataRow.hasOwnProperty('user_version')) {
                        currentVersion = parseInt(dataRow.user_version);
                    } else if (dataRow.hasOwnProperty('USER_VERSION')) {
                        currentVersion = parseInt(dataRow.USER_VERSION);
                    } else {
                        const rowValues = Object.values(dataRow);
                        if (rowValues.length > 0) {
                            currentVersion = parseInt(rowValues[0]);
                        }
                    }
                }

                /**
                 * 4. ABGLEICH & RÜCKGABE
                 */
                if (currentVersion === targetVersion) {
                    pE_Common.log(`[SQLite] Version v${targetVersion} erfolgreich verifiziert.`, "info");

                    // Konvertierung in das ScalarModel
                    const successRes = pE_Common.toScalar("true", true, "");

                    /**
                     * WICHTIGSTER FIX FÜR BLAZOR:
                     * Da pE_Common.toScalar bei Werten ungleich 1 oder true das Feld out_value_bool 
                     * auf false setzt, müssen wir es hier explizit auf true setzen, damit die 
                     * C#-Prüfung (resultSetVersion.out_value_bool) erfolgreich ist.
                     */
                    successRes.out_value_bool = true;

                    return JSON.stringify(successRes);
                } else {
                    // Falls currentVersion immer noch NaN ist, wird dies hier sauber ausgegeben
                    const errorMsg = `Version mismatch: Soll ${targetVersion}, Ist ${currentVersion}`;
                    pE_Common.log(`[SQLite] ${errorMsg}`, "error");
                    return JSON.stringify(pE_Common.toScalar(null, false, errorMsg));
                }

            } catch (e) {
                this._logError("SET_VERSION_FAIL", e);
                return JSON.stringify(pE_Common.toScalar(null, false, "SET_VERSION_FAIL: " + e.message));
            }
        },
        
        /**
         * Löscht alle Daten aus der Datenbank (Truncate/Delete) oder setzt sie zurück.
         * @returns {Promise<string>} Gibt ein ScalarModel als JSON-String zurück.
         * HINWEIS: Wird verwendet, um die Tabellen zu leeren, ohne das Schema zu löschen.
         */
        clearAllData: async function () {
            try {
                pE_Common.log(`[SQLite] Starte vollständige Datenreinigung auf: ${this._dbName}...`, "info");

                /**
                 * 1. CRITICAL SAFETY CHECK (SIGSEGV Protection)
                 */
                if (!this._db || !this._dbName) {
                    pE_Common.log("clearAllData ABGEBROCHEN: Keine aktive Verbindung oder Plugin nicht bereit.", "error");
                    return JSON.stringify(pE_Common.toScalar(null, false, pE_Common.DB_STATUS.NOT_CONNECTED));
                }

                // 2. Foreign Keys ausschalten
                await this._db.execute({
                    database: this._dbName,
                    statements: "PRAGMA foreign_keys = OFF;"
                });

                try {
                    // 3. Alle benutzerdefinierten Tabellennamen abfragen
                    const res = await this._db.query({
                        database: this._dbName,
                        statement: "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';",
                        values: []
                    });

                    // --- NEW: iOS Metadaten-Shift ---
                    let rows = (res && Array.isArray(res.values)) ? res.values : [];

                    // Falls das erste Objekt nur die Spaltennamen für iOS enthält, entfernen wir es.
                    if (rows.length > 1 && rows[0].hasOwnProperty('ios_columns')) {
                        pE_Common.log("[SQLite] iOS Metadaten-Header in clearAllData erkannt und entfernt.", "info");
                        rows = rows.slice(1);
                    }
                    // --------------------------------

                    // 4. Jede Tabelle einzeln leeren
                    if (rows.length > 0) {
                        for (const tableRow of rows) {
                            const tableName = tableRow.name || tableRow.NAME;

                            if (!tableName) continue;

                            /**
                             * SYSTEMTABELLEN STRIKT IGNORIEREN
                             */
                            if (tableName === "sync_table" ||
                                tableName === "_cap_sqlite_metadata_" ||
                                tableName === "sqlite_sequence") {
                                pE_Common.log(`[SQLite] Überspringe Systemtabelle: ${tableName}`, "log");
                                continue;
                            }

                            pE_Common.log(`[SQLite] Lösche Inhalt von Tabelle: ${tableName}`, "info");

                            await this._db.run({
                                database: this._dbName,
                                statement: `DELETE FROM ${tableName};`,
                                values: []
                            });
                        }

                        // 5. Härtung: Auto-Increment Zähler zurücksetzen
                        try {
                            await this._db.run({
                                database: this._dbName,
                                statement: "DELETE FROM sqlite_sequence;",
                                values: []
                            });
                        } catch (seqErr) {
                            pE_Common.log("[SQLite] Info: sqlite_sequence nicht vorhanden oder bereits leer.", "log");
                        }
                    }

                    // 6. Speicherplatz freigeben (VACUUM)
                    await this._db.execute({
                        database: this._dbName,
                        statements: "VACUUM;"
                    });

                } finally {
                    /**
                     * 7. KRITISCH: Foreign Keys IMMER wieder aktivieren
                     */
                    pE_Common.log(`[SQLite] Re-activating foreign keys für ${this._dbName} (finally block)...`, "info");

                    if (this._db) {
                        await this._db.execute({
                            database: this._dbName,
                            statements: "PRAGMA foreign_keys = ON;"
                        });
                    }
                }

                pE_Common.log(`[SQLite] Datenbank ${this._dbName} erfolgreich geleert und optimiert.`, "info");
                console.log(`%c[SQLite] Datenbank ${this._dbName} erfolgreich geleert.`, "color: orange; font-weight: bold;");

                /**
                 * 8. SUCCESS-RÜCKGABE ALS JSON
                 */
                const successResult = pE_Common.toScalar("true", true, "");
                return JSON.stringify(successResult);

            } catch (e) {
                /**
                 * 9. Fehler-Reporting
                 */
                this._logError("CLEAR_DATA_FAIL", e);
                pE_Common.log(`[SQLite] Kritischer Fehler bei clearAllData: ${e.message}`, "error");

                const errorResult = pE_Common.toScalar(null, false, e.message);
                return JSON.stringify(errorResult);
            }
        },
                
        /**
         * Löscht alle Tabellen aus der Datenbank (Schema-Reset).
         * @returns {Promise<string>} Gibt ein ScalarModel als JSON-String zurück.
         * HINWEIS: Wird genutzt, um die Datenbank vollständig zu leeren, inklusive der Struktur.
         */
        dropAllTables: async function () {
            try {
                pE_Common.log(`[SQLite] Starte vollständiges Löschen aller Tabellen in ${this._dbName} (DROP ALL TABLES)...`, "info");

                /**
                 * 1. CRITICAL SAFETY CHECK (SIGSEGV Protection)
                 */
                if (!this._db || !this._dbName) {
                    pE_Common.log("dropAllTables abgebrochen: Keine aktive Verbindung oder Plugin nicht bereit.", "error");
                    return JSON.stringify(pE_Common.toScalar(null, false, pE_Common.DB_STATUS.NOT_CONNECTED));
                }

                /**
                 * 2. Daten leeren & Foreign Keys ausschalten
                 * HINWEIS: clearAllData muss ebenfalls ein ScalarModel (Objekt) zurückgeben, 
                 * damit wir hier den out_err prüfen können.
                 */
                const clearResultRaw = await this.clearAllData();
                const clearResult = JSON.parse(clearResultRaw);

                // Falls clearAllData bereits aufgrund einer verlorenen Verbindung abgebrochen hat
                if (clearResult && clearResult.out_err === pE_Common.DB_STATUS.NOT_CONNECTED) {
                    return JSON.stringify(clearResult);
                }

                /**
                 * 3. Alle benutzerdefinierten Tabellennamen abfragen
                 */
                const res = await this._db.query({
                    database: this._dbName,
                    statement: "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';",
                    values: []
                });

                // --- NEW: iOS Metadaten-Shift ---
                let rows = (res && Array.isArray(res.values)) ? res.values : [];

                // Falls das erste Objekt nur die Spaltennamen für iOS enthält, entfernen wir es.
                if (rows.length > 1 && rows[0].hasOwnProperty('ios_columns')) {
                    pE_Common.log("[SQLite] iOS Metadaten-Header in dropAllTables erkannt und entfernt.", "info");
                    rows = rows.slice(1);
                }
                // --------------------------------

                /**
                 * 4. Jede Tabelle physisch entfernen
                 */
                if (rows.length > 0) {
                    for (const tableRow of rows) {
                        const tableName = tableRow.name || tableRow.NAME;

                        if (!tableName) continue;

                        /**
                         * SYSTEMTABELLEN STRIKT SCHÜTZEN
                         */
                        if (tableName === "sync_table" ||
                            tableName === "_cap_sqlite_metadata_" ||
                            tableName === "sqlite_sequence") {
                            pE_Common.log(`[SQLite] Überspringe geschützte Systemtabelle: ${tableName}`, "log");
                            continue;
                        }

                        pE_Common.log(`[SQLite] Lösche Tabelle (DROP): ${tableName}`, "info");

                        await this._db.execute({
                            database: this._dbName,
                            statements: `DROP TABLE IF EXISTS ${tableName};`
                        });
                    }
                }

                /**
                 * 5. Version zurücksetzen
                 */
                await this.setVersion(0);

                /**
                 * 6. Datenbank-Datei bereinigen (VACUUM)
                 */
                if (this._db) {
                    await this._db.execute({
                        database: this._dbName,
                        statements: "VACUUM;"
                    });
                }

                pE_Common.log(`[SQLite] Alle Tabellen in ${this._dbName} erfolgreich entfernt und DB optimiert.`, "info");
                console.log(`%c[SQLite] Alle Tabellen in ${this._dbName} erfolgreich entfernt.`, "color: red; font-weight: bold;");

                /**
                 * 7. ERFOLGS-RÜCKGABE ALS JSON
                 * Wir setzen "true" als Value, damit out_value_bool in Blazor wahr ist.
                 */
                const successResult = pE_Common.toScalar("true", true, "");
                return JSON.stringify(successResult);

            } catch (e) {
                /**
                 * 8. Fehler-Reporting
                 */
                this._logError("DROP_TABLES_FAIL", e);
                pE_Common.log(`[SQLite] Kritischer Fehler bei dropAllTables auf ${this._dbName}: ${e.message}`, "error");

                const errorResult = pE_Common.toScalar(null, false, e.message);
                return JSON.stringify(errorResult);
            }
        },
        
        /**
         * Löscht die gesamte Datenbankdatei vom Gerät.
         * @returns {Promise<string>} Gibt ein ScalarModel als JSON-String zurück.
         * HINWEIS: Dies entfernt die physische Datei; danach ist eine Neu-Initialisierung erforderlich.
         */
        deleteDatabase: async function () {
            const nameToDelete = this._dbName;

            try {
                pE_Common.log(`[SQLite] START FULL PURGE: ${nameToDelete || 'UNDEFINED'}`, "warn");

                // 1. Plugin Check (Nutzt die zentralisierte Instanz aus dem State)
                //const plugin = this.CapacitorSQLite;
                const plugin = pE_Capacitor.CapacitorSQLite;

                if (!plugin) {
                    pE_Common.log("Löschen abgebrochen: CapacitorSQLite Plugin-Referenz fehlt.", "error");
                    return JSON.stringify(pE_Common.toScalar(null, false, "PLUGIN_MISSING"));
                }

                if (!nameToDelete) {
                    pE_Common.log("Löschen abgebrochen: Kein Datenbankname im State gefunden.", "error");
                    return JSON.stringify(pE_Common.toScalar(null, false, "NO_ACTIVE_DATABASE_TO_DELETE"));
                }

                // 2. Verbindung zwingend kappen
                try {
                    pE_Common.log(`[SQLite] Closing connection for ${nameToDelete} before deletion...`, "info");
                    // Wir nutzen das Plugin direkt, um die Connection-Ressourcen freizugeben
                    await plugin.close({ database: nameToDelete });
                } catch (e) {
                    pE_Common.log("Info: Verbindung war bereits geschlossen oder existierte nicht.", "log");
                }

                // 3. Physisches Löschen via Plugin-Basis
                pE_Common.log(`[SQLite] Deleting physical file and metadata for: ${nameToDelete}`, "warn");
                await plugin.deleteDatabase({
                    database: nameToDelete
                });

                // 4. RADIKALER STATE-RESET
                // Wir nullen alles, damit die Bridge wieder in den Urzustand geht
                this._db = null;
                this._dbName = "";
                this._sqliteConnection = null;

                pE_Common.log("FULL PURGE SUCCESSFUL - State cleared.", "info");

                /**
                 * 5. SUCCESS-RÜCKGABE
                 */
                return JSON.stringify(pE_Common.toScalar("true", true, ""));

            } catch (err) {
                // 6. Fehler-Reporting & Sicherheits-Reset
                this._dbName = "";
                this._db = null;
                this._sqliteConnection = null;

                this._logError("PURGE_FATAL", err);
                pE_Common.log(`[SQLite] Kritischer Fehler beim Löschen der DB: ${err.message}`, "error");

                return JSON.stringify(pE_Common.toScalar(null, false, "DELETE_FAILED: " + err.message));
            }
        }

       

    },

    // --- STORAGE MODULE (Native Filesystem) ---
    storage: {
        /**
         * Erstellt den vollständigen Pfad inkl. App-Isolierung.
         * @param {string} dbName - Der App-Name aus C#.
         * @param {string} accountHash - Der User-Hash.
         * @param {string} subPath - Dateiname oder Tabellenpfad.
         */
        _getFullPath: function (dbName, accountHash, subPath) {
            // Wir setzen das Präfix "DB_" davor, um konsistent mit PWA/WPF zu bleiben
            return `DB_${dbName}/${accountHash}/${subPath}`;
        },

        /**
         * Schreibt eine verschlüsselte Datei in das native Dateisystem.
         * @param {string} dbName - Der App-Name aus C#.
         * @param {string} accountHash - Der User-Hash (Ordner).
         * @param {string} fileName - Der Dateiname inkl. Tabellenpfad.
         * @param {string} encryptedContent - Der verschlüsselte Inhalt.
         */
        writeFile: async function (dbName, accountHash, fileName, encryptedContent) {
            const fullPath = this._getFullPath(dbName, accountHash, fileName);
            pE_Common.log("Cap:Storage", `writeFile -> ${fullPath}`);

            try {
                const parent = window.pE_Capacitor;

                // Nutzt das native Capacitor Filesystem Plugin
                await parent.Filesystem.writeFile({
                    path: fullPath,
                    data: encryptedContent,
                    directory: 'DATA',
                    encoding: 'utf8',
                    recursive: true // WICHTIG: Erstellt die gesamte Ordnerstruktur (DB/User/Tabelle) automatisch
                });

                pE_Common.log("Cap:Storage", `Datei erfolgreich geschrieben: ${fullPath}`);

                // Rückgabe an Blazor: Erfolg melden
                return pE_Common.toScalar(true, true, "");

            } catch (e) {
                pE_Common.log("Cap:Storage", "Kritischer Schreibfehler", "error", e);

                // Fehler an Blazor melden
                return pE_Common.toScalar(false, false, e.message);
            }
        },

        /**
          * Liest alle Dateien eines Tabellen-Ordners aus (Batch-Read).
          * @param {string} dbName - Der App-Name aus C#.
          * @param {string} accountHash - Der User-Hash.
          * @param {string} tableName - Der Name der Tabelle (Unterordner).
          */
        readAllTableFiles: async function (dbName, accountHash, tableName) {
            const folderPath = this._getFullPath(dbName, accountHash, tableName);
            pE_Common.log("Cap:Storage", `readAllTableFiles -> ${folderPath}`);

            try {
                const parent = window.pE_Capacitor;

                // 1. Liste der Dateien im Ordner abrufen
                const dirResult = await parent.Filesystem.readdir({
                    path: folderPath,
                    directory: 'DATA'
                });

                const contents = [];

                // 2. Parallelisiertes Einlesen der Dateien für bessere Performance
                if (dirResult.files && dirResult.files.length > 0) {
                    const readPromises = dirResult.files.map(async (file) => {
                        try {
                            const fileName = file.name;
                            const fileResult = await parent.Filesystem.readFile({
                                path: `${folderPath}/${fileName}`,
                                directory: 'DATA',
                                encoding: 'utf8'
                            });
                            return fileResult.data;
                        } catch (readErr) {
                            pE_Common.log("Cap:Storage", `Einzelner Read-Fehler bei ${file.name}`, "warn", readErr);
                            return null;
                        }
                    });

                    const results = await Promise.all(readPromises);
                    results.forEach(content => {
                        if (content !== null) contents.push(content);
                    });
                }

                // 3. Rückgabe als JSON-String für den C#-Deserializer
                const jsonResult = JSON.stringify(contents);
                pE_Common.log("Cap:Storage", `${contents.length} Dateien erfolgreich gelesen.`);

                // --- ERFOLGS-PFAD ---
                const res = pE_Common.toScalar(jsonResult, true, "");
                res.out_value_bool = true; // Force success for C# check (if result.out_value_bool)
                return res;

            } catch (e) {
                const msg = e.message ? e.message.toLowerCase() : "";
                if (msg.includes("not found") || msg.includes("does not exist")) {
                    pE_Common.log("Cap:Storage", `Ordner ${folderPath} existiert noch nicht. Rückgabe: leere Liste.`);

                    // --- AUCH HIER: Override für Initial-Start ---
                    const resInit = pE_Common.toScalar(JSON.stringify([]), true, "");
                    resInit.out_value_bool = true;
                    return resInit;
                }

                pE_Common.log("Cap:Storage", "Fehler beim Lesen des Verzeichnisses", "error", e);
                // Im echten Fehlerfall (Berechtigung etc.) lassen wir out_value_bool auf false
                return pE_Common.toScalar(JSON.stringify([]), false, e.message);
            }
        },

        /**
         * Löscht eine spezifische Datei aus dem nativen Dateisystem.
         * @param {string} dbName - Der App-Name aus C#.
         * @param {string} accountHash - Der User-Hash.
         * @param {string} fileName - Der Dateiname.
         */
        deleteFile: async function (dbName, accountHash, fileName) {
            const fullPath = this._getFullPath(dbName, accountHash, fileName);
            pE_Common.log("Cap:Storage", `deleteFile -> ${fullPath}`);

            try {
                const parent = window.pE_Capacitor;
                await parent.Filesystem.deleteFile({
                    path: fullPath,
                    directory: 'DATA'
                });

                pE_Common.log("Cap:Storage", `Datei erfolgreich gelöscht: ${fileName}`);
                return pE_Common.toScalar(true, true, "");
            } catch (e) {
                // Falls die Datei gar nicht existiert, ist das für uns ein Erfolg (Ziel erreicht)
                const msg = e.message ? e.message.toLowerCase() : "";
                if (msg.includes("not found") || msg.includes("does not exist")) {
                    pE_Common.log("Cap:Storage", "Datei existierte bereits nicht - Löschvorgang erfolgreich.");
                    return pE_Common.toScalar(true, true, "");
                }

                pE_Common.log("Cap:Storage", `Fehler beim Löschen von ${fileName}`, "error", e);
                return pE_Common.toScalar(false, false, e.message);
            }
        },

        prepareStorage: async function (dbName, accountHash) {
            const path = `DB_${dbName}/${accountHash}`;
            try {
                const parent = window.pE_Capacitor;
                await parent.Filesystem.mkdir({
                    path: path,
                    directory: 'DATA',
                    recursive: true
                });
            } catch (e) {
                // Fehler ignorieren, falls Ordner schon da ist
            }
        },

        /**
         * Löscht den gesamten Speicher eines Benutzers (Account-Ordner) für diese App.
         * @param {string} dbName - Der App-Name aus C#.
         * @param {string} accountHash - Der User-Hash.
         */
        purgeUserStorage: async function (dbName, accountHash) {
            const userPath = this._getFullPath(dbName, accountHash, "");
            pE_Common.log("Cap:Storage", `purgeUserStorage -> ${userPath}`);

            try {
                const parent = window.pE_Capacitor;

                await parent.Filesystem.rmdir({
                    path: userPath,
                    directory: 'DATA',
                    recursive: true
                });

                pE_Common.log("Cap:Storage", "User-Verzeichnis erfolgreich gelöscht.");
                return pE_Common.toScalar(true, true, "");
            } catch (e) {
                // WICHTIG: Wenn der Ordner nicht existiert, liefert Capacitor einen Fehler.
                // Für uns ist das Ziel (Daten weg) aber erreicht.
                const msg = e.message ? e.message.toLowerCase() : "";
                if (msg.includes("not found") || msg.includes("does not exist")) {
                    pE_Common.log("Cap:Storage", "Verzeichnis existierte nicht - Purge als Erfolg gewertet.");
                    return pE_Common.toScalar(true, true, "");
                }

                pE_Common.log("Cap:Storage", "Fehler beim Purge", "error", e);
                return pE_Common.toScalar(false, false, e.message);
            }
        }

    },


};