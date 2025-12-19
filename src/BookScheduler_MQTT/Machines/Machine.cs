// Machines/Machine.cs
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BookScheduler_MQTT.Services;
using BookScheduler_MQTT.Models;

namespace BookScheduler_MQTT.Machines
{
    // BaseMachine: common functionality for all simulated local machines.
    // Nullable-safe: uses guards, optional cancellation token support.
    public abstract class BaseMachine
    {
        public Guid Id { get; }
        public string Name { get; }
        public string Type { get; }
        protected readonly MqttClientService _mqtt;
        protected readonly DbHelper _db;

        private CancellationTokenSource? _heartbeatCts;

        protected BaseMachine(Guid id, string name, string type, MqttClientService mqtt, DbHelper db)
        {
            Id = id != Guid.Empty ? id : Guid.NewGuid();
            Name = name ?? string.Empty;
            Type = type ?? string.Empty;
            _mqtt = mqtt ?? throw new ArgumentNullException(nameof(mqtt));
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        // Each machine should implement how it handles incoming commands
        public abstract Task HandleCommandAsync(string payload);

        // Subscribe to commands topic for this machine and start heartbeat
        public virtual async Task HandleCommandsAsync()
        {
            // Subscribe to commands for this machine
            var topic = $"machines/{Id}/commands";
            await _mqtt.SubscribeAsync(topic, async payload =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(payload)) return;
                    await HandleCommandAsync(payload);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{Name} handle command error: {ex}");
                }
            });

            // Start periodic heartbeat (non-blocking)
            _heartbeatCts = new CancellationTokenSource();
            _ = Task.Run(() => HeartbeatLoopAsync(_heartbeatCts.Token));
        }

        public virtual Task StopAsync()
        {
            try
            {
                _heartbeatCts?.Cancel();
            }
            catch { /* ignore */ }
            return Task.CompletedTask;
        }

        protected async Task PublishStatusAsync(string status)
        {
            var payloadObj = new
            {
                machineId = Id,
                name = Name,
                type = Type,
                status = status,
                timestamp = DateTime.UtcNow.ToString("o")
            };

            var payload = JsonConvert.SerializeObject(payloadObj);
            await _mqtt.PublishAsync($"machines/{Id}/status", payload);
        }

        // Publish progress update for current stage
        protected async Task PublishProgressAsync(Guid bookId, string stage, int progress)
        {
            var payloadObj = new
            {
                bookId = bookId,
                stage = stage,
                progress = progress
            };
            var payload = JsonConvert.SerializeObject(payloadObj);
            await _mqtt.PublishAsync($"jobs/{bookId}/stages/{stage}/progress", payload);
        }

        // Publish done notification for stage
        protected async Task PublishDoneAsync(Guid bookId, string stage)
        {
            var payloadObj = new
            {
                bookId = bookId,
                stage = stage,
                timestamp = DateTime.UtcNow.ToString("o")
            };
            var payload = JsonConvert.SerializeObject(payloadObj);
            await _mqtt.PublishAsync($"jobs/{bookId}/stages/{stage}/done", payload);
        }

        // Simple heartbeat loop: publish "idle" status every 10s when idle
        protected async Task HeartbeatLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await PublishStatusAsync("idle");
                    // Update DB heartbeat (nullable-safe wrapper exists)
                    await _db.SetMachineHeartbeatAsync(Id, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Heartbeat error for {Name}: {ex}");
                }

                try { await Task.Delay(TimeSpan.FromSeconds(10), token); }
                catch (TaskCanceledException) { break; }
            }
        }

        // Helpers to mark machine busy/idle in DB
        protected Task SetBusyAsync(bool busy) => _db.SetMachineBusyAsync(Id, busy);
    }
}
