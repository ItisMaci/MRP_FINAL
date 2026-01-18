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

### Phase 1: Infrastruktur & Basis (Zwischenabgabe)
Der erste Schritt bestand in der Bereitstellung einer stabilen Datenbasis und des Server-Grundgerüsts.
*   **Modellierung:** Erstellung des ER-Diagramms für User, Media und Ratings.
*   **Docker:** Setup von PostgreSQL und pgAdmin auf Port 5432.
*   **HttpServer:** Implementierung der Event-Loop, die `HttpListenerContext` in `HttpRestEventArgs` umwandelt.
*   **User System:** Implementierung von `UserHandler` und `SessionHandler` für Login/Register (Token-based Auth).

### Phase 2: Erweiterte Geschäftslogik (Finalisierung)
Nach der Zwischenabgabe wurde der Fokus auf komplexe Business-Logik und Datenintegrität gelegt.
*   **Rating-System:** Implementierung von `RatingHandler`. Eine wichtige Logik-Erweiterung war die **Unique Constraint Prüfung**: Ein User darf ein Medium nur einmal bewerten. Dies wird nun vor dem Insert geprüft.
*   **Moderations-Feature:** Kommentare zu Bewertungen werden standardmäßig als "unconfirmed" (`is_confirmed = false`) in der Datenbank gespeichert. Die API (`GetRatings`) filtert diese Texte aus, solange sie nicht bestätigt sind.
*   **Favorites & Likes:** Implementierung der Many-to-Many Beziehungen für Favoriten und Likes auf Ratings.

### Phase 3: Qualitätssicherung & Refactoring
Um die Anforderungen an Unit-Tests zu erfüllen, musste die Architektur angepasst werden.
*   **Problem:** Die Repositories waren eng mit der statischen `DatabaseConnection` gekoppelt.
*   **Lösung:** Einführung von Mock-Repositories (`MockRatingRepository`). Dies ermöglichte das Schreiben von **20 Unit Tests**, die Geschäftslogik (z.B. Score-Range 1-5, Passwort-Hashing) validieren, ohne eine laufende Datenbank zu benötigen.
*   **Bugfixing:** Behebung von Namespace-Konflikten und Anpassung der SQL-Queries an die exakten Spaltennamen (`user_id` vs `userid`).

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
