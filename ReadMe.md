## Lizenz

Copyright (c) 2026 **true-perfect-code**. All Rights Reserved.

Der Inhalt dieses Repositorys ist urheberrechtlich geschützt. Jegliche Nutzung, Vervielfältigung, Bearbeitung oder Verbreitung (auch nur von Teilen) ist ohne ausdrückliche schriftliche Genehmigung  des Urhebers untersagt.

Dieses Projekt wird ausschließlich zu Präsentations- und Bewerbungszwecken zur Verfügung gestellt. Eine kommerzielle oder private Nutzung des Quellcodes ist nicht gestattet.

---

# pFemme Example – Perioden-Tracking für Frauenarztpraxen

[![.NET](https://img.shields.io/badge/.NET-10-blueviolet)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-All%20Hosting%20Models-512BD4)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![License](https://img.shields.io/badge/License-All%20Rights%20Reserved-red)](LICENSE)
[![Status](https://img.shields.io/badge/Status-Showcase-brightgreen)](https://pfemme.de/)

## 📌 Projektbeschreibung

**pFemme** ist eine DSGVO-konforme Perioden-Tracking-App für Patientinnen von Frauenarztpraxen.  
Dieses Repository zeigt, wie wir **eine einzige Codebasis** für **alle Plattformen** nutzen – ein Kernargument für unsere White-Label-Arbeit.

| Plattform | Technologie | Live-Demo / Store |
|-----------|-------------|-------------------|
| **Web (Hauptanwendung)** | Blazor Server | [pfemme.de](https://pfemme.de/) |
| **Progressive Web App (PWA)** | Blazor WASM | [pwa.pfemme.de](https://pwa.pfemme.de/) |
| **Windows (Store)** | Blazor Hybrid + WPF | [Microsoft Store](https://apps.microsoft.com/detail/9mwtr7n65hgz) |
| **Windows (Portable)** | Blazor Hybrid + WPF | [portable.pfemme.de](https://portable.pfemme.de/) |
| **Android** | Blazor WASM + Capacitor | [Google Play](https://play.google.com/store/apps/details?id=ch.trueperfectcode.pfemme) |
| **iOS / macOS** | Blazor WASM + Capacitor / MAUI | *In Vorbereitung* |

**Unser Architektur-Ansatz:**  
Clean/Onion-Architektur mit Dependency Injection – derselbe Code für UI, Business-Logik und Datenzugriff läuft überall. Die Präsentationsschicht wird je nach Plattform ausgetauscht, während der Core vollständig wiederverwendet wird.

> **Hinweis:** Dieses Repository enthält einen **bereinigten Code-Auszug** unseres produktiven Systems und dient als **Showcase** für unsere Architektur- und Entwicklungsstandards.

---

## 🧩 Technologie-Stack

| Bereich | Technologie |
|---------|-------------|
| **Core & Backend** | .NET 10, C#, ASP.NET Core, Minimal APIs |
| **Web-UI** | Blazor Server + Blazor WASM (PWA) |
| **Mobile-UI** | Blazor WASM + Capacitor (iOS/Android/iMac mit ARM) |
| **Desktop-UI (Windows)** | Blazor Hybrid + WPF |
| **Datenbank** | Microsoft SQL Server (Azure SQL / Managed SQL / lokale Json-Files für Offline) |
| **Authentifizierung** | Individuell (E-Mail/Passwort) + Google, Microsoft, Apple |
| **KI & Analysen** | Azure OpenAI (RAG), Azure AI Search |
| **Hosting** | Azure App Services / IONOS / beliebig |
| **Barrierefreiheit** | WCAG 2.1 AA, dynamischer Sprachwechsel, Schriftgrößenanpassung |
| **Testing** | xUnit, bUnit |
| **CI/CD** | GitHub Actions |

---

## 📌 Zweck dieses Repositorys

Dieses Repository enthält eine **bereinigte und abgespeckte Version** unserer produktiven pFemme-App.  
Es richtet sich **ausschließlich an IT-Agenturen und IT-Vermittler**, die eine verlässliche Einschätzung unserer Arbeitsqualität suchen – bevor sie uns für ein Projekt in Betracht ziehen.

In der Praxis versprechen viele Outsourcing-Teams viel, aber die tatsächliche Code-Qualität bleibt oft eine Blackbox. Dieses Repository schafft Transparenz:

- **Architektonische Sauberkeit:** Clean/Onion-Architektur, klare Trennung von Core, Infrastructure und Presentation
- **Testbarkeit:** Abhängigkeiten sind über Interfaces injectet, Unit-Tests mit xUnit und bUnit sind integriert
- **Code-Stil & Wartbarkeit:** Konsistente Benennung, Dokumentation im Code, Einhaltung von .NET-Standards
- **Modernste Technologien:** .NET 10, Blazor (Server/WASM/Hybrid), Azure-Integration (OpenAI, AI Search)
- **Cross-Plattform-Ansatz:** Eine Codebasis – deployt als Web, PWA, Windows-App, Android/iOS (in Vorbereitung)
- **Offline-First & Sync:** Lokale Datenspeicherung (Json-Files) mit synchronisierter Cloud-Anbindung
