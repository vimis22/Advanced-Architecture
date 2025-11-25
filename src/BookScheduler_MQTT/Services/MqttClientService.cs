// Services/MqttClientService.cs
using System;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace BookScheduler_MQTT.Services
{
    public class MqttClientService : IDisposable
    {
        private readonly IMqttClient _client;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Func<string, Task>> _handlers = new();

        // optional stored default connect info (for convenience)
        private readonly string _defaultHost;
        private readonly int _defaultPort;
        private readonly string? _defaultClientId;

        // Parameterless
        public MqttClientService() : this("localhost", 1883, null) { }

        // Constructor matching your Program.cs usage: new MqttClientService(host, port, clientId)
        public MqttClientService(string host, int port, string? clientId = null)
        {
            _defaultHost = host;
            _defaultPort = port;
            _defaultClientId = clientId;

            var factory = new MqttFactory();
            _client = factory.CreateMqttClient();

            _client.ApplicationMessageReceivedAsync += async e =>
            {
                try
                {
                    var topic = e.ApplicationMessage.Topic ?? string.Empty;

                    // use PayloadSegment safely (it may have Array == null)
                    string payload;
                    var seg = e.ApplicationMessage.PayloadSegment;
                    if (seg.Array == null || seg.Count == 0)
                    {
                        payload = string.Empty;
                    }
                    else
                    {
                        payload = Encoding.UTF8.GetString(seg.Array, seg.Offset, seg.Count);
                    }

                    foreach (var kv in _handlers)
                    {
                        var filter = kv.Key;
                        if (TopicMatches(filter, topic))
                        {
                            // dispatch but don't block other handlers
                            try { await kv.Value(payload); } catch (Exception ex) { Console.WriteLine($"Handler error for {filter}: {ex}"); }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"MQTT receive handler error: {ex}");
                }
            };

            _client.DisconnectedAsync += async e =>
            {
                // simple reconnect logic using defaults
                Console.WriteLine("MQTT disconnected, will try to reconnect in 2s...");
                await Task.Delay(2000);
                try
                {
                    var builder = new MQTTnet.Client.MqttClientOptionsBuilder()
                        .WithTcpServer(_defaultHost, _defaultPort)
                        .WithClientId(_defaultClientId ?? $"BookScheduler_{Guid.NewGuid()}");
                    var opts = builder.Build();
                    await _client.ConnectAsync(opts);
                    Console.WriteLine("MQTT reconnected.");
                }
                catch
                {
                    // swallow - will attempt again next disconnect
                }
            };
        }

        // Connect using provided values or defaults stored in ctor
        public async Task ConnectAsync(string? brokerHost = null, int? port = null, string? clientId = null)
        {
            var host = brokerHost ?? _defaultHost;
            var prt = port ?? _defaultPort;
            var id = clientId ?? _defaultClientId ?? $"BookScheduler_{Guid.NewGuid()}";

            if (_client.IsConnected) return;

            var builder = new MQTTnet.Client.MqttClientOptionsBuilder()
                .WithTcpServer(host, prt)
                .WithClientId(id);

            var options = builder.Build();
            await _client.ConnectAsync(options);
            Console.WriteLine($"Connected to MQTT broker at {host}:{prt}");
        }

        public async Task PublishAsync(string topic, string payload)
        {
            if (!_client.IsConnected) await ConnectAsync();

            var msg = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _client.PublishAsync(msg);
        }

        // Subscribe with a per-filter handler (keeps old convenient signature)
        public async Task SubscribeAsync(string topicFilter, Func<string, Task> handler)
        {
            if (!_client.IsConnected) await ConnectAsync();

            _handlers[topicFilter] = handler;
            await _client.SubscribeAsync(topicFilter);
            Console.WriteLine($"Subscribed to {topicFilter}");
        }

        private bool TopicMatches(string filter, string topic)
        {
            if (filter == topic) return true;
            var fParts = filter.Split('/');
            var tParts = topic.Split('/');
            for (int i = 0; i < fParts.Length; i++)
            {
                if (i >= tParts.Length) return false;
                if (fParts[i] == "#") return true;
                if (fParts[i] == "+") continue;
                if (fParts[i] != tParts[i]) return false;
            }
            return fParts.Length == tParts.Length;
        }

        public void Dispose()
        {
            try { _client?.Dispose(); } catch { }
        }
    }
}
