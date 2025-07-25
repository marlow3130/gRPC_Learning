// Importiert die gRPC Core-Bibliothek für ServerCallContext und andere gRPC-spezifische Klassen
using Grpc.Core;
// Importiert die generierten gRPC-Klassen aus dem aktuellen Projekt (HelloRequest, HelloReply, Greeter)
using gRPC_Learning;
// DEMO: Zusätzliche Imports für erweiterte Funktionalität
using System.Diagnostics;
using System.Linq;

namespace gRPC_Learning.Services;

// Diese Klasse implementiert den gRPC-Service und erbt von der automatisch generierten Greeter.GreeterBase
// Die GreeterBase-Klasse wird aus der greet.proto Datei generiert
public class GreeterService : Greeter.GreeterBase
{
    // Private readonly Variable für den Logger - wird nur einmal bei der Erstellung gesetzt
    // ILogger wird für das Protokollieren von Ereignissen und Fehlern verwendet
    private readonly ILogger<GreeterService> _logger;

    // Konstruktor wird beim Start der Anwendung aufgerufen, wenn der Service registriert wird
    // Dependency Injection Container übergibt automatisch eine ILogger-Instanz
    public GreeterService(ILogger<GreeterService> logger)
    {
        // Logger wird in der privaten Variable gespeichert für spätere Verwendung
        _logger = logger;
    }

    // Diese Methode überschreibt die virtuelle SayHello-Methode aus der Greeter.GreeterBase
    // Sie wird aufgerufen, wenn ein Client eine SayHello-RPC-Anfrage sendet
    // 'override' ist notwendig, da die Methode in der Basisklasse bereits definiert ist
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew(); // DEMO: Performance-Messung

        try
        {
            // DEMO: Logging der eingehenden Anfrage für Debugging-Zwecke
            _logger.LogInformation("=== SayHello AUFGERUFEN ===");
            _logger.LogInformation("Empfangener Name: {Name}", request.Name);
            _logger.LogInformation("Anfrage-Zeitpunkt: {Timestamp}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            // DEMO: Umfassende Request-Validierung mit unserer Hilfsmethode
            ValidateRequest(request, context);

            // DEMO: Client-Informationen sammeln und loggen
            LogClientInformation(context);

            // DEMO: Simulation einer Business-Logik-Verzögerung (z.B. Database-Lookup)
            if (request.Name.ToLower().Contains("slow"))
            {
                _logger.LogInformation("Simuliere langsame Verarbeitung für {Name}", request.Name);
                Task.Delay(500).Wait(); // Simuliert eine langsamere Verarbeitung
            }

            // DEMO: Verwendung unserer Hilfsmethode für intelligente Nachrichtenerstellung
            var responseMessage = GenerateGreetingMessage(request.Name);

            // DEMO: Zusätzliche Statistiken zur Antwort hinzufügen
            var processingTime = stopwatch.ElapsedMilliseconds;
            var enhancedMessage = $"{responseMessage}\n" +
                                $"[Server-Info: Verarbeitet in {processingTime}ms um {DateTime.Now:HH:mm:ss}]";

            // 1. request.Name wird aus der eingehenden gRPC-Nachricht extrahiert
            // 2. Eine neue HelloReply-Instanz wird erstellt mit der intelligenten Nachricht
            // 3. Task.FromResult erstellt einen bereits abgeschlossenen Task (synchrone Ausführung)
            // 4. Das Ergebnis wird sofort an den Client zurückgesendet
            var reply = new HelloReply
            {
                Message = enhancedMessage
            };

            // DEMO: Success-Logging mit Timing-Informationen
            _logger.LogInformation("Erfolgreiche Antwort gesendet an {Name}", request.Name);
            _logger.LogInformation("Nachricht: {Message}", reply.Message);
            _logger.LogInformation("Verarbeitungszeit: {ProcessingTime}ms", processingTime);
            _logger.LogInformation("=== SayHello BEENDET ===");

            return Task.FromResult(reply);
        }
        catch (RpcException)
        {
            // DEMO: gRPC-spezifische Fehler weiterleiten (bereits korrekt formatiert)
            stopwatch.Stop();
            _logger.LogWarning("gRPC-Fehler nach {ElapsedTime}ms", stopwatch.ElapsedMilliseconds);
            throw; // RpcException wird unverändert weitergegeben
        }
        catch (Exception ex)
        {
            // DEMO: Unerwartete Fehler abfangen und in gRPC-Format konvertieren
            stopwatch.Stop();
            _logger.LogError(ex, "Unerwarteter Fehler in SayHello für {Name} nach {ElapsedTime}ms",
                request?.Name ?? "NULL", stopwatch.ElapsedMilliseconds);

            // Interner Fehler als gRPC-Exception zurückgeben
            throw new RpcException(new Status(StatusCode.Internal,
                "Ein unerwarteter Serverfehler ist aufgetreten. Bitte versuchen Sie es später erneut."));
        }
    }

    // ALTERNATIVE DEMO: Da SayGoodbye nicht in der Proto-Datei definiert ist,
    // zeigen wir hier eine zusätzliche private Hilfsmethode für die Geschäftslogik
    private string GenerateGreetingMessage(string name, string greetingType = "Hello")
    {
        // DEMO: Verschiedene Begrüßungen basierend auf dem Namen und Typ
        var currentTime = DateTime.Now;
        var timeOfDay = GetTimeOfDayGreeting(currentTime);

        // DEMO: Spezielle Behandlung für bestimmte Namen
        if (name.ToLower() == "admin")
        {
            return $"{greetingType}, Administrator {name}! {timeOfDay} (Admin-Zugang erkannt)";
        }
        else if (name.Length > 15)
        {
            return $"{greetingType}, {name}! Das ist aber ein außergewöhnlich langer Name. {timeOfDay}";
        }
        else if (name.ToLower().Contains("test"))
        {
            return $"{greetingType}, {name}! Test-Benutzer erkannt. {timeOfDay}";
        }
        else
        {
            return $"{greetingType}, {name}! {timeOfDay} Schön dich zu sehen!";
        }
    }

    // DEMO: Private Hilfsmethode für zeitbasierte Begrüßungen
    private string GetTimeOfDayGreeting(DateTime time)
    {
        return time.Hour switch
        {
            >= 5 and < 12 => "Guten Morgen!",
            >= 12 and < 17 => "Guten Tag!",
            >= 17 and < 22 => "Guten Abend!",
            _ => "Gute Nacht!"
        };
    }

    // DEMO: Private Methode für Request-Validierung
    private void ValidateRequest(HelloRequest request, ServerCallContext context)
    {
        // DEMO: Umfassende Validierung der Eingabe
        if (request == null)
        {
            _logger.LogError("Null request erhalten");
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Request darf nicht null sein"));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            _logger.LogWarning("Leerer oder ungültiger Name erhalten von {Client}", context.Peer);
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Name darf nicht leer sein"));
        }

        if (request.Name.Length > 50)
        {
            _logger.LogWarning("Zu langer Name erhalten: {NameLength} Zeichen", request.Name.Length);
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Name darf maximal 50 Zeichen haben"));
        }

        // DEMO: Prüfung auf verbotene Zeichen
        if (request.Name.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c) && c != '-' && c != '_'))
        {
            _logger.LogWarning("Ungültige Zeichen im Namen: {Name}", request.Name);
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Name enthält ungültige Zeichen"));
        }
    }

    // DEMO: Private Methode für erweiterte Client-Informationen
    private void LogClientInformation(ServerCallContext context)
    {
        try
        {
            var clientInfo = context.Peer;
            var userAgent = context.RequestHeaders.FirstOrDefault(h => h.Key == "user-agent")?.Value ?? "Unbekannt";
            var requestId = context.RequestHeaders.FirstOrDefault(h => h.Key == "x-request-id")?.Value ?? Guid.NewGuid().ToString();

            _logger.LogInformation("Client-Info: {ClientInfo}, UserAgent: {UserAgent}, RequestId: {RequestId}",
                clientInfo, userAgent, requestId);

            // DEMO: Response-Headers für Client-Tracking
            context.ResponseTrailers.Add("x-server-timestamp", DateTime.UtcNow.ToString("O"));
            context.ResponseTrailers.Add("x-request-processed-by", Environment.MachineName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fehler beim Sammeln von Client-Informationen");
        }
    }
}
