using Grpc.Core;
using gRPC_Learning;
using System.Diagnostics;
using System.Linq;

namespace gRPC_Learning.Services;

/// <summary>
/// Enhanced gRPC Greeter Service that implements all the RPC methods defined in the updated proto file
/// This demonstrates how to implement a comprehensive gRPC service with multiple endpoints
/// </summary>
public class EnhancedGreeterService : Greeter.GreeterBase
{
    private readonly ILogger<EnhancedGreeterService> _logger;
    private static int _totalRequestsServed = 0;
    private static readonly DateTime _serverStartTime = DateTime.UtcNow;
    private static readonly List<double> _responseTimes = new();

    public EnhancedGreeterService(ILogger<EnhancedGreeterService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Enhanced SayHello implementation that uses the new proto fields
    /// </summary>
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            Interlocked.Increment(ref _totalRequestsServed);

            _logger.LogInformation("=== Enhanced SayHello AUFGERUFEN ===");
            _logger.LogInformation("Name: {Name}, GreetingType: {GreetingType}, IncludeTime: {IncludeTime}, UserType: {UserType}",
                request.Name, request.GreetingType, request.IncludeTime, request.UserType);

            // Validate request using the new validation method
            var validationResult = ValidateUserInternal(request.Name);
            if (!validationResult.IsValid)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    string.Join(", ", validationResult.ValidationErrors)));
            }

            // Generate greeting using enhanced parameters
            var greetingType = !string.IsNullOrEmpty(request.GreetingType) ? request.GreetingType : "Hello";
            var responseMessage = GenerateGreetingMessage(request.Name, greetingType);

            // Add time-based greeting if requested
            var timeGreeting = "";
            if (request.IncludeTime)
            {
                timeGreeting = GetTimeOfDayGreeting(DateTime.Now);
                responseMessage += $" {timeGreeting}";
            }

            stopwatch.Stop();
            var processingTime = stopwatch.ElapsedMilliseconds;

            // Track response time for statistics
            lock (_responseTimes)
            {
                _responseTimes.Add(processingTime);
                if (_responseTimes.Count > 1000) // Keep only last 1000 responses
                {
                    _responseTimes.RemoveAt(0);
                }
            }

            // Create enhanced reply with all the new fields
            var reply = new HelloReply
            {
                Message = responseMessage,
                ProcessingTimeMs = processingTime,
                ServerTimestamp = DateTime.UtcNow.ToString("O"),
                TimeOfDayGreeting = timeGreeting,
                IsAdminUser = IsAdminUser(request.Name, request.UserType)
            };

            _logger.LogInformation("Enhanced response sent: {ProcessingTime}ms", processingTime);

            return Task.FromResult(reply);
        }
        catch (RpcException)
        {
            stopwatch.Stop();
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error in Enhanced SayHello");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    /// <summary>
    /// New SayGoodbye RPC method implementation
    /// </summary>
    public override Task<GoodbyeReply> SayGoodbye(GoodbyeRequest request, ServerCallContext context)
    {
        try
        {
            Interlocked.Increment(ref _totalRequestsServed);

            _logger.LogInformation("SayGoodbye called for: {Name}", request.Name);

            var farewellType = !string.IsNullOrEmpty(request.FarewellType) ? request.FarewellType : "Goodbye";
            var message = GenerateGoodbyeMessage(request.Name, farewellType);

            var reply = new GoodbyeReply
            {
                Message = message,
                ServerTimestamp = DateTime.UtcNow.ToString("O")
            };

            return Task.FromResult(reply);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SayGoodbye");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    /// <summary>
    /// GetServerInfo RPC method - provides server statistics and health information
    /// </summary>
    public override Task<ServerInfoReply> GetServerInfo(ServerInfoRequest request, ServerCallContext context)
    {
        try
        {
            var uptime = DateTime.UtcNow - _serverStartTime;
            double averageResponseTime = 0;

            lock (_responseTimes)
            {
                if (_responseTimes.Count > 0)
                {
                    averageResponseTime = _responseTimes.Average();
                }
            }

            var reply = new ServerInfoReply
            {
                ServerName = Environment.MachineName,
                Version = "1.0.0",
                Uptime = $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s",
                TotalRequestsServed = _totalRequestsServed,
                AverageResponseTimeMs = averageResponseTime
            };

            _logger.LogInformation("Server info requested - Uptime: {Uptime}, Requests: {Requests}",
                reply.Uptime, reply.TotalRequestsServed);

            return Task.FromResult(reply);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetServerInfo");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    /// <summary>
    /// ValidateUser RPC method - validates user input without processing
    /// </summary>
    public override Task<UserValidationReply> ValidateUser(UserValidationRequest request, ServerCallContext context)
    {
        try
        {
            var validation = ValidateUserInternal(request.Name);
            var userType = DetermineUserType(request.Name);

            var reply = new UserValidationReply
            {
                IsValid = validation.IsValid,
                UserType = userType
            };

            reply.ValidationErrors.AddRange(validation.ValidationErrors);

            _logger.LogInformation("User validation for {Name}: {IsValid}", request.Name, validation.IsValid);

            return Task.FromResult(reply);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ValidateUser");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    /// <summary>
    /// Streaming RPC method - handles multiple greetings in a stream
    /// </summary>
    public override async Task SayHelloStream(IAsyncStreamReader<HelloRequest> requestStream,
        IServerStreamWriter<HelloReply> responseStream, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Starting streaming SayHello session");

            await foreach (var request in requestStream.ReadAllAsync())
            {
                var stopwatch = Stopwatch.StartNew();

                // Reuse the logic from SayHello
                var greetingType = !string.IsNullOrEmpty(request.GreetingType) ? request.GreetingType : "Hello";
                var responseMessage = GenerateGreetingMessage(request.Name, greetingType);

                if (request.IncludeTime)
                {
                    var timeGreeting = GetTimeOfDayGreeting(DateTime.Now);
                    responseMessage += $" {timeGreeting}";
                }

                stopwatch.Stop();

                var reply = new HelloReply
                {
                    Message = responseMessage,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    ServerTimestamp = DateTime.UtcNow.ToString("O"),
                    TimeOfDayGreeting = request.IncludeTime ? GetTimeOfDayGreeting(DateTime.Now) : "",
                    IsAdminUser = IsAdminUser(request.Name, request.UserType)
                };

                await responseStream.WriteAsync(reply);

                Interlocked.Increment(ref _totalRequestsServed);
            }

            _logger.LogInformation("Streaming SayHello session completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SayHelloStream");
            throw new RpcException(new Status(StatusCode.Internal, "Streaming error"));
        }
    }

    #region Helper Methods

    private string GenerateGreetingMessage(string name, string greetingType = "Hello")
    {
        var currentTime = DateTime.Now;

        if (name.ToLower() == "admin")
        {
            return $"{greetingType}, Administrator {name}! (Admin-Zugang erkannt)";
        }
        else if (name.Length > 15)
        {
            return $"{greetingType}, {name}! Das ist aber ein außergewöhnlich langer Name.";
        }
        else if (name.ToLower().Contains("test"))
        {
            return $"{greetingType}, {name}! Test-Benutzer erkannt.";
        }
        else
        {
            return $"{greetingType}, {name}! Schön dich zu sehen!";
        }
    }

    private string GenerateGoodbyeMessage(string name, string farewellType = "Goodbye")
    {
        if (name.ToLower() == "admin")
        {
            return $"{farewellType}, Administrator {name}! Vielen Dank für Ihren Besuch.";
        }
        else if (name.ToLower().Contains("test"))
        {
            return $"{farewellType}, {name}! Test-Session beendet.";
        }
        else
        {
            return $"{farewellType}, {name}! Bis zum nächsten Mal!";
        }
    }

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

    private (bool IsValid, List<string> ValidationErrors) ValidateUserInternal(string name)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Name darf nicht leer sein");
        }
        else
        {
            if (name.Length > 50)
            {
                errors.Add("Name darf maximal 50 Zeichen haben");
            }

            if (name.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c) && c != '-' && c != '_'))
            {
                errors.Add("Name enthält ungültige Zeichen");
            }
        }

        return (errors.Count == 0, errors);
    }

    private bool IsAdminUser(string name, string userType)
    {
        return name?.ToLower() == "admin" || userType?.ToLower() == "admin";
    }

    private UserType DetermineUserType(string name)
    {
        if (string.IsNullOrEmpty(name))
            return UserType.Unspecified;

        var lowerName = name.ToLower();

        if (lowerName == "admin")
            return UserType.Admin;

        if (lowerName.Contains("test"))
            return UserType.Test;

        return UserType.Regular;
    }

    #endregion
}
