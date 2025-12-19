using MQTTnet.Client;
using Newtonsoft.Json;
using UnifiedScheduler.Models;

namespace UnifiedScheduler.Services;

public class OrderSubscriber
{
    private readonly IMqttClient _mqttClient;
    private readonly OrderManager _orderManager;

    public OrderSubscriber(IMqttClient mqttClient, OrderManager orderManager)
    {
        _mqttClient = mqttClient;
        _orderManager = orderManager;
    }

    public async Task StartAsync()
    {
        // Subscribe to order creation topic from dashboard
        var subscribeOptions = new MQTTnet.Client.MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter("scheduler/orders/create", MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        var result = await _mqttClient.SubscribeAsync(subscribeOptions);
        var resultCode = result.Items.FirstOrDefault()?.ResultCode ?? MQTTnet.Client.MqttClientSubscribeResultCode.UnspecifiedError;
        Console.WriteLine($"[OrderSubscriber] ✓ Subscribed to scheduler/orders/create (result: {resultCode})");
    }

    public async Task ProcessOrderMessageAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var topic = e.ApplicationMessage.Topic;
            if (topic == "scheduler/orders/create")
            {
                var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                var orderRequest = JsonConvert.DeserializeObject<CreateOrderRequest>(payload);

                if (orderRequest != null)
                {
                    Console.WriteLine($"\n[OrderSubscriber] Received order creation request from dashboard");
                    var orderId = await _orderManager.CreateOrderAsync(orderRequest);
                    Console.WriteLine($"[OrderSubscriber] ✓ Order created: {orderId}\n");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OrderSubscriber] Error processing order: {ex.Message}");
            Console.WriteLine($"[OrderSubscriber] Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[OrderSubscriber] Inner exception: {ex.InnerException.Message}");
                Console.WriteLine($"[OrderSubscriber] Inner stack trace: {ex.InnerException.StackTrace}");
            }
        }
    }
}
