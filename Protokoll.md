# Protokoll Media Ratings Platform (MRP) - Final Submission

## 1. Architektur & Designentscheidungen
Die Anwendung wurde als eigenständiger RESTful HTTP Server in C# entwickelt, ohne auf High-Level Web-Frameworks (wie ASP.NET) zurückzugreifen. Die Architektur folgt strikten Prinzipien der Objektorientierung und Trennung der Verantwortlichkeiten.

*   **Server-Kern:** Der `HttpRestServer` nutzt den `System.Net.HttpListener` (Default Port 12000), um eingehende Anfragen zu verarbeiten. Die Kommunikation läuft asynchron über Events, um Blockaden zu vermeiden.
*   **Handler-Pattern:** Anfragen werden durch spezialisierte Handler verarbeitet. Das Interface `IHandler` sorgt dafür, dass der Server modular erweiterbar bleibt (**Open/Closed Principle**).
*   **Repository Pattern:** Die Datenzugriffslogik wurde vollständig in Repository-Klassen (`RatingRepository`, `UserRepository`, `MediaRepository`) gekapselt. Die Handler enthalten keine SQL-Befehle, sondern rufen Methoden der Repositories auf.
*   **Dependency Injection (Refactoring):** Um die Testbarkeit zu gewährleisten, wurde die harte Abhängigkeit zur `DatabaseConnection` in den Repositories aufgelöst. Repositories akzeptieren nun Interfaces (z. B. für Connections), was das "Mocking" in Unit-Tests ermöglicht (**Dependency Inversion Principle**).

**Implementierte Handler:**
`SessionHandler` (Auth), `UserHandler`, `MediaHandler`, `RatingHandler`, `GenreHandler`, `VersionHandler`.

## 2. Verwendete Technologien & Tools
*   **Sprache:** C# (.NET 10.0)
*   **Datenbank:** PostgreSQL (Containerisiert via Docker)
*   **Konnektivität:** Npgsql NuGet Package
*   **Testing:** NUnit, NUnit3TestAdapter, Microsoft.NET.Test.Sdk
*   **API Testing:** Postman (Integration Tests)

## 3. Entwicklungsverlauf

Der Entwicklungsprozess begann mit einem umfassenden Refactoring der Kernmodelle Media.cs, Ratings.cs, Session.cs und User.cs, um das Repository Pattern einzuführen und die Architektur zu modularisieren. In diesem Zuge wurde die Atom-Komponente vollständig entfernt, da ihre statische Struktur nicht mit den SOLID-Prinzipien, insbesondere der Testbarkeit und Erweiterbarkeit, vereinbar war. Im Anschluss erfolgte die Implementierung der eigentlichen Geschäftslogik auf der Basis dieser neuen Struktur. Die Qualitätssicherung wurde dabei iterativ gestaltet: Für jedes Systemmodell (wie z. B. Media) wurden Integrationstests durchgeführt und bestanden, bevor die Entwicklung des nächsten Moduls begann. Es wurde jedoch kein Test-Driven-Development (TDD) angewandt; die Tests wurden jeweils erst nach Abschluss der funktionalen Implementierung geschrieben, um die Korrektheit der Komponenten sicherzustellen.

## 4. Angewandte SOLID Prinzipien
1.  **Single Responsibility Principle (SRP):**
    *   Die `DatabaseConnection`-Klasse kümmert sich ausschließlich um den Verbindungsaufbau. Das Parsen von HTTP-Requests liegt strikt bei den `Handlern`.
2.  **Open/Closed Principle (OCP):**
    *   Neue Funktionalitäten (z. B. Genres) konnten durch Hinzufügen eines neuen `GenreHandler` implementiert werden, ohne den Kern-Server (`HttpRestServer.cs`) zu verändern.
3.  **Dependency Inversion Principle (DIP):**
    *   Durch das Refactoring hängen die Tests nicht mehr von der konkreten Datenbank ab, sondern nutzen Abstraktionen (Interfaces/Mocks), um die Logik isoliert zu prüfen.

## 5. Unit Testing Strategie
Es wurden **20 Unit Tests** mit dem NUnit-Framework implementiert.
*   **User Tests:** Validieren von Passwort-Hashing, Login-Logik und leeren Eingaben.
*   **Rating Tests:** Prüfen von Grenzwerten (Score < 1 oder > 5), Duplikat-Erkennung (simuliert im Mock) und Like-Toggles.
*   **Media & Session Tests:** Sicherstellung, dass Objekte korrekt initialisiert werden und Token generiert werden.

**Abdeckung:** Die Tests decken die kritische Business-Logik ab (Validation, Calculation), während die reine Datenpersistenz durch Integrationstests (Postman) verifiziert wurde.

## 6. Lessons Learned
*   **Testing Legacy Code:** Es ist schwierig, Unit Tests nachträglich in eine Architektur einzubauen, die statische Datenbankzugriffe nutzt. Das frühzeitige Nutzen von Interfaces (Dependency Injection) hätte Zeit gespart.
*   **SQL Injection Prevention:** Die konsequente Verwendung von parametrisierten Queries (`@parameter`) war entscheidend für die Sicherheit der Anwendung.

## 7. GitHub Repository
https://github.com/ItisMaci/MRP_FINAL.git
