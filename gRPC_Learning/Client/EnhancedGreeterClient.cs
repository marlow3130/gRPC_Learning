using Grpc.Net.Client;
using gRPC_Learning;

namespace gRPC_Learning.Client;

/// <summary>
/// Example client that demonstrates how to use all the RPC methods from the enhanced proto file
/// </summary>
public class EnhancedGreeterClient
{
    private readonly Greeter.GreeterClient _client;

    public EnhancedGreeterClient(string serverAddress = "https://localhost:5001")
    {
        var channel = GrpcChannel.ForAddress(serverAddress);
        _client = new Greeter.GreeterClient(channel);
    }

    /// <summary>
    /// Demonstrates the enhanced SayHello with all new parameters
    /// </summary>
    public async Task<HelloReply> SayHelloAsync(string name, string? greetingType = null, bool includeTime = false, string? userType = null)
    {
        var request = new HelloRequest
        {
            Name = name
        };

        // Set optional parameters
        if (!string.IsNullOrEmpty(greetingType))
            request.GreetingType = greetingType;

        if (includeTime)
            request.IncludeTime = includeTime;

        if (!string.IsNullOrEmpty(userType))
            request.UserType = userType;

        try
        {
            var response = await _client.SayHelloAsync(request);

            Console.WriteLine($"Greeting: {response.Message}");
            Console.WriteLine($"Processing Time: {response.ProcessingTimeMs}ms");
            Console.WriteLine($"Server Timestamp: {response.ServerTimestamp}");
            Console.WriteLine($"Time Greeting: {response.TimeOfDayGreeting}");
            Console.WriteLine($"Is Admin: {response.IsAdminUser}");
            Console.WriteLine();

            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calling SayHello: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Demonstrates the new SayGoodbye RPC method
    /// </summary>
    public async Task<GoodbyeReply> SayGoodbyeAsync(string name, string? farewellType = null)
    {
        var request = new GoodbyeRequest
        {
            Name = name
        };

        if (!string.IsNullOrEmpty(farewellType))
            request.FarewellType = farewellType;

        try
        {
            var response = await _client.SayGoodbyeAsync(request);

            Console.WriteLine($"Goodbye: {response.Message}");
            Console.WriteLine($"Server Timestamp: {response.ServerTimestamp}");
            Console.WriteLine();

            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calling SayGoodbye: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Demonstrates the GetServerInfo RPC method
    /// </summary>
    public async Task<ServerInfoReply> GetServerInfoAsync()
    {
        var request = new ServerInfoRequest();

        try
        {
            var response = await _client.GetServerInfoAsync(request);

            Console.WriteLine("=== Server Information ===");
            Console.WriteLine($"Server Name: {response.ServerName}");
            Console.WriteLine($"Version: {response.Version}");
            Console.WriteLine($"Uptime: {response.Uptime}");
            Console.WriteLine($"Total Requests Served: {response.TotalRequestsServed}");
            Console.WriteLine($"Average Response Time: {response.AverageResponseTimeMs:F2}ms");
            Console.WriteLine();

            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calling GetServerInfo: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Demonstrates the ValidateUser RPC method
    /// </summary>
    public async Task<UserValidationReply> ValidateUserAsync(string name)
    {
        var request = new UserValidationRequest
        {
            Name = name
        };

        try
        {
            var response = await _client.ValidateUserAsync(request);

            Console.WriteLine($"User Validation for '{name}':");
            Console.WriteLine($"Is Valid: {response.IsValid}");
            Console.WriteLine($"User Type: {response.UserType}");

            if (response.ValidationErrors.Count > 0)
            {
                Console.WriteLine("Validation Errors:");
                foreach (var error in response.ValidationErrors)
                {
                    Console.WriteLine($"  - {error}");
                }
            }
            Console.WriteLine();

            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calling ValidateUser: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Demonstrates the streaming SayHelloStream RPC method
    /// </summary>
    public async Task DemoStreamingAsync()
    {
        try
        {
            Console.WriteLine("=== Starting Streaming Demo ===");

            using var call = _client.SayHelloStream();

            // Send multiple requests
            var requests = new[]
            {
                new HelloRequest { Name = "Alice", GreetingType = "Hi", IncludeTime = true },
                new HelloRequest { Name = "admin", GreetingType = "Hello", IncludeTime = false, UserType = "admin" },
                new HelloRequest { Name = "test_user", GreetingType = "Hey", IncludeTime = true, UserType = "test" },
                new HelloRequest { Name = "Bob", GreetingType = "Greetings", IncludeTime = true }
            };

            // Send requests and receive responses concurrently
            var sendTask = Task.Run(async () =>
            {
                foreach (var request in requests)
                {
                    await call.RequestStream.WriteAsync(request);
                    await Task.Delay(500); // Small delay between sends
                }
                await call.RequestStream.CompleteAsync();
            });

            var receiveTask = Task.Run(async () =>
            {
                while (await call.ResponseStream.MoveNext(CancellationToken.None))
                {
                    var response = call.ResponseStream.Current;
                    Console.WriteLine($"Streamed Response: {response.Message}");
                    Console.WriteLine($"Processing Time: {response.ProcessingTimeMs}ms");
                    Console.WriteLine($"Admin User: {response.IsAdminUser}");
                    Console.WriteLine("---");
                }
            });

            await Task.WhenAll(sendTask, receiveTask);
            Console.WriteLine("=== Streaming Demo Completed ===");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in streaming demo: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Runs a comprehensive demo of all available RPC methods
    /// </summary>
    public async Task RunCompleteDemo()
    {
        Console.WriteLine("🚀 Starting Enhanced gRPC Client Demo");
        Console.WriteLine("=====================================");

        try
        {
            // 1. Basic Hello
            await SayHelloAsync("World");

            // 2. Hello with time
            await SayHelloAsync("Alice", "Hi", includeTime: true);

            // 3. Admin user
            await SayHelloAsync("admin", "Hello", includeTime: true, userType: "admin");

            // 4. Test user
            await SayHelloAsync("test_user", "Hey", includeTime: false, userType: "test");

            // 5. User validation tests
            await ValidateUserAsync("ValidUser");
            await ValidateUserAsync("");  // Invalid: empty
            await ValidateUserAsync("ThisIsAVeryLongNameThatExceedsFiftyCharactersLimit"); // Invalid: too long
            await ValidateUserAsync("Invalid@Name!"); // Invalid: special characters

            // 6. Goodbye messages
            await SayGoodbyeAsync("Alice", "See you later");
            await SayGoodbyeAsync("admin", "Farewell");

            // 7. Server information
            await GetServerInfoAsync();

            // 8. Streaming demo
            await DemoStreamingAsync();

            Console.WriteLine("✅ Demo completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Demo failed: {ex.Message}");
        }
    }
}

/// <summary>
/// Entry point for running the client demo
/// You can call this from your main program or create a separate console application
/// </summary>
public static class ClientDemo
{
    public static async Task RunAsync(string serverAddress = "https://localhost:5001")
    {
        var client = new EnhancedGreeterClient(serverAddress);
        await client.RunCompleteDemo();
    }
}
