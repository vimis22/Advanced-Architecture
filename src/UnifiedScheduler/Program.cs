using MQTTnet;
using MQTTnet.Client;
using Microsoft.Extensions.Configuration;
using UnifiedScheduler.Models;
using UnifiedScheduler.Services;
using Newtonsoft.Json;

namespace UnifiedScheduler;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("     UNIFIED BOOK SCHEDULER v1.0");
        Console.WriteLine("===========================================\n");

        // Load configuration
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var mqttBroker = config["MQTT:Broker"] ?? "localhost";
        var mqttPort = int.Parse(config["MQTT:Port"] ?? "1883");
        var redisConnection = config["Redis:ConnectionString"] ?? "localhost:6379";
        var timescaleConnection = config["TimescaleDB:ConnectionString"] ?? "";
        var heartbeatInterval = int.Parse(config["Scheduler:HeartbeatIntervalSeconds"] ?? "1");
        var heartbeatTimeout = int.Parse(config["Scheduler:HeartbeatTimeoutCycles"] ?? "3");
        var assignmentInterval = int.Parse(config["Scheduler:JobAssignmentIntervalMs"] ?? "500");

        Console.WriteLine($"Configuration:");
        Console.WriteLine($"  MQTT Broker: {mqttBroker}:{mqttPort}");
        Console.WriteLine($"  Redis: {redisConnection}");
        Console.WriteLine($"  TimescaleDB: {timescaleConnection.Split(';')[0]}");
        Console.WriteLine($"  Heartbeat: {heartbeatInterval}s (timeout: {heartbeatTimeout} cycles)");
        Console.WriteLine($"  Assignment Interval: {assignmentInterval}ms\n");

        // Initialize services
        Console.WriteLine("Initializing services...");

        var redis = new RedisService(redisConnection);
        var timescale = new TimescaleDBService(timescaleConnection);
        var queueManager = new JobQueueManager(redis);
        var orderManager = new OrderManager(redis, timescale, queueManager);

        // Initialize MQTT client
        var mqttFactory = new MqttFactory();
        var mqttClient = mqttFactory.CreateMqttClient();

        var mqttOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(mqttBroker, mqttPort)
            .WithClientId("UnifiedScheduler")
            .WithCleanSession()
            .Build();

        // Initialize scheduler components (BEFORE connecting to MQTT)
        var heartbeatObserver = new HeartbeatObserver(mqttClient, redis, timescale, queueManager);
        var heartbeatMonitor = new HeartbeatMonitor(redis, timescale, queueManager, heartbeatInterval, heartbeatTimeout);
        var jobAssigner = new JobAssigner(mqttClient, redis, timescale, queueManager, assignmentInterval);
        var orderSubscriber = new OrderSubscriber(mqttClient, orderManager);
        var queueStatusPublisher = new QueueStatusPublisher(mqttClient, queueManager);

        // Set up unified MQTT message handler BEFORE connecting
        mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            try
            {
                var topic = e.ApplicationMessage.Topic;

                // Debug logging for non-heartbeat messages
                if (!topic.Contains("/heartbeat"))
                {
                    Console.WriteLine($"[DEBUG] Received MQTT message on topic: {topic}");
                }

                if (topic.StartsWith("machines/") && topic.EndsWith("/heartbeat"))
                {
                    await heartbeatObserver.OnHeartbeatReceivedAsync(e);
                }
                else if (topic == "scheduler/orders/create")
                {
                    Console.WriteLine($"[DEBUG] Routing to OrderSubscriber");
                    await orderSubscriber.ProcessOrderMessageAsync(e);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Message handler exception: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            }
        };

        Console.WriteLine("Connecting to MQTT broker...");
        await mqttClient.ConnectAsync(mqttOptions);
        Console.WriteLine("✓ Connected to MQTT broker\n");

        // Start all components
        Console.WriteLine("Starting scheduler components...");
        await heartbeatObserver.StartAsync();
        await orderSubscriber.StartAsync();
        _ = heartbeatMonitor.StartAsync(); // Run in background
        _ = jobAssigner.StartAsync(); // Run in background
        queueStatusPublisher.Start(); // Start publishing queue status

        Console.WriteLine("✓ All components started\n");

        Console.WriteLine("===========================================");
        Console.WriteLine("Scheduler is running. Commands:");
        Console.WriteLine("  'order' - Create a new order");
        Console.WriteLine("  'status <order-id>' - Check order status");
        Console.WriteLine("  'queues' - Show queue lengths");
        Console.WriteLine("  'machines' - Show machine status");
        Console.WriteLine("  'stats' - Show statistics (order durations and recovery times)");
        Console.WriteLine("  'exit' - Shutdown scheduler");
        Console.WriteLine("===========================================\n");

        // Command loop
        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine()?.Trim().ToLower();

            if (string.IsNullOrEmpty(input))
                continue;

            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0];

            try
            {
                switch (command)
                {
                    case "order":
                        await CreateTestOrderAsync(orderManager);
                        break;

                    case "status":
                        if (parts.Length < 2)
                        {
                            Console.WriteLine("Usage: status <order-id>");
                            break;
                        }
                        if (int.TryParse(parts[1], out var orderId))
                        {
                            await orderManager.PrintOrderStatusAsync(orderId);
                        }
                        else
                        {
                            Console.WriteLine("Invalid order ID format");
                        }
                        break;

                    case "queues":
                        await queueManager.PrintQueueStatusAsync();
                        break;

                    case "machines":
                        await PrintMachineStatusAsync(redis);
                        break;

                    case "stats":
                        await PrintStatisticsAsync(timescale);
                        break;

                    case "exit":
                        Console.WriteLine("\nShutting down...");
                        heartbeatMonitor.Stop();
                        jobAssigner.Stop();
                        await mqttClient.DisconnectAsync();
                        return;

                    default:
                        Console.WriteLine($"Unknown command: {command}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    static async Task CreateTestOrderAsync(OrderManager orderManager)
    {
        Console.Write("  Title: ");
        var title = Console.ReadLine() ?? "Test Book";

        Console.Write("  Author: ");
        var author = Console.ReadLine() ?? "Test Author";

        Console.Write("  Pages: ");
        var pages = int.TryParse(Console.ReadLine(), out var p) ? p : 200;

        Console.Write("  Cover Type (hardcover/softcover): ");
        var coverType = Console.ReadLine() ?? "hardcover";

        Console.Write("  Paper Type (glossy/matte): ");
        var paperType = Console.ReadLine() ?? "glossy";

        Console.Write("  Quantity: ");
        var quantity = int.TryParse(Console.ReadLine(), out var q) ? q : 10;

        var request = new CreateOrderRequest
        {
            Title = title,
            Author = author,
            Pages = pages,
            CoverType = coverType,
            PaperType = paperType,
            Quantity = quantity
        };

        var orderId = await orderManager.CreateOrderAsync(request);
        Console.WriteLine($"\n✓ Order created: {orderId}\n");
    }

    static async Task PrintMachineStatusAsync(RedisService redis)
    {
        var machineIds = await redis.GetAllMachineIdsAsync();

        if (machineIds.Count == 0)
        {
            Console.WriteLine("No machines found");
            return;
        }

        Console.WriteLine("\n=== Machine Status ===");
        foreach (var machineId in machineIds)
        {
            var state = await redis.GetMachineStateAsync(machineId);
            if (state != null)
            {
                var timeSinceHeartbeat = (DateTime.UtcNow - state.LastHeartbeat).TotalSeconds;
                Console.WriteLine($"  {machineId} (Type {state.MachineType}): {state.Status}");
                Console.WriteLine($"    Current Unit: {state.CurrentUnitId ?? "none"}");
                Console.WriteLine($"    Progress: {state.Progress?.ToString() ?? "N/A"}");
                Console.WriteLine($"    Last Heartbeat: {timeSinceHeartbeat:F1}s ago");
            }
        }
        Console.WriteLine();
    }

    static async Task PrintStatisticsAsync(TimescaleDBService timescale)
    {
        Console.WriteLine("\n=== STATISTICS ===\n");

        // Order Duration Statistics
        var orderStats = await timescale.GetOrderDurationStatisticsAsync();
        if (orderStats.Count > 0)
        {
            Console.WriteLine("--- Order Completion Times ---");
            Console.WriteLine($"{"Order ID",-10} {"Title",-30} {"Units",-8} {"Wait Time",-12} {"Processing",-12}");
            Console.WriteLine(new string('-', 75));

            foreach (var order in orderStats)
            {
                var waitTime = order.WaitTimeSeconds < 1
                    ? $"{order.WaitTimeSeconds:F1}s"
                    : $"{order.WaitTimeSeconds / 60:F1}m";

                var duration = order.DurationMinutes < 1
                    ? $"{order.DurationSeconds:F1}s"
                    : $"{order.DurationMinutes:F1}m";

                Console.WriteLine($"{order.Id,-10} {order.Title.Substring(0, Math.Min(order.Title.Length, 28)),-30} {order.Quantity,-8} {waitTime,-12} {duration,-12}");
            }

            // Calculate summary statistics
            var avgWaitTime = orderStats.Average(o => o.WaitTimeSeconds);
            var avgDuration = orderStats.Average(o => o.DurationSeconds);
            var minDuration = orderStats.Min(o => o.DurationSeconds);
            var maxDuration = orderStats.Max(o => o.DurationSeconds);

            Console.WriteLine(new string('-', 75));
            Console.WriteLine($"Total Orders: {orderStats.Count}");
            Console.WriteLine($"Avg Wait Time: {avgWaitTime:F1}s ({avgWaitTime / 60:F1}m)");
            Console.WriteLine($"Avg Processing: {avgDuration:F1}s ({avgDuration / 60:F1}m)");
            Console.WriteLine($"Min Processing: {minDuration:F1}s ({minDuration / 60:F1}m)");
            Console.WriteLine($"Max Processing: {maxDuration:F1}s ({maxDuration / 60:F1}m)");
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine("No completed orders yet.\n");
        }

        // Recovery Statistics
        var recoverySummary = await timescale.GetRecoverySummaryAsync();
        if (recoverySummary.TotalRecoveries > 0)
        {
            Console.WriteLine("--- Failure Recovery Times ---");
            Console.WriteLine($"Total Recoveries: {recoverySummary.TotalRecoveries}");
            Console.WriteLine($"Avg Recovery:     {recoverySummary.AvgRecoveryMs:F1}ms");
            Console.WriteLine($"Min Recovery:     {recoverySummary.MinRecoveryMs}ms");
            Console.WriteLine($"Max Recovery:     {recoverySummary.MaxRecoveryMs}ms");
            Console.WriteLine($"Median Recovery:  {recoverySummary.MedianRecoveryMs:F1}ms");
            Console.WriteLine();

            // Show recent recoveries
            var recentRecoveries = await timescale.GetRecoveryStatisticsAsync();
            if (recentRecoveries.Count > 0)
            {
                Console.WriteLine("--- Recent Recovery Events (Last 10) ---");
                Console.WriteLine($"{"Machine",-15} {"Unit ID",-15} {"Recovery Time",-15} {"Timestamp",-20}");
                Console.WriteLine(new string('-', 70));

                foreach (var recovery in recentRecoveries.Take(10))
                {
                    var timestamp = recovery.Timestamp.ToString("HH:mm:ss");
                    Console.WriteLine($"{recovery.MachineId,-15} {recovery.UnitId,-15} {recovery.RecoveryDurationMs + "ms",-15} {timestamp,-20}");
                }
            }
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine("No failure recoveries recorded yet.\n");
        }
    }
}
