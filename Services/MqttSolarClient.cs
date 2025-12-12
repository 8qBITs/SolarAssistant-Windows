using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Protocol;
using SolarTray.Model;
using SolarTray.Settings;

namespace SolarTray.Services
{
    public sealed class MqttSolarClient : IAsyncDisposable
    {
        private IMqttClient? _client;
        private readonly MqttClientFactory _factory = new();

        private MqttClientOptions? _options;

        public event Action? Connected;
        public event Action<string>? ConnectionFailed;
        public event Action<SolarSnapshot>? SnapshotUpdated;

        // internal mutable state
        private readonly SolarSnapshot _snapshot = new();

        private const string PV_TOPIC = "solar_assistant/total/pv_power/state";
        private const string LOAD_TOPIC = "solar_assistant/total/load_power/state";
        private const string SOC_TOPIC = "solar_assistant/total/battery_state_of_charge/state";
        private const string GRID_TOPIC = "solar_assistant/total/grid_power/state";
        private const string VOLT_TOPIC = "solar_assistant/battery_1/voltage/state";

        public async Task StartAsync(CancellationToken ct)
        {
            _client = _factory.CreateMqttClient();

            _client.ApplicationMessageReceivedAsync += e =>
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                ApplyMessage(topic, payload);

                // IMPORTANT: emit a fresh object so UI always sees a "new" snapshot
                SnapshotUpdated?.Invoke(new SolarSnapshot
                {
                    PvKw = _snapshot.PvKw,
                    LoadKw = _snapshot.LoadKw,
                    SocPercent = _snapshot.SocPercent,
                    GridKw = _snapshot.GridKw,
                    BatteryVolts = _snapshot.BatteryVolts
                });

                return Task.CompletedTask;
            };

            _client.ConnectedAsync += async _ =>
            {
                var subs = _factory.CreateSubscribeOptionsBuilder()
                    .WithTopicFilter(f => f.WithTopic(PV_TOPIC).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce))
                    .WithTopicFilter(f => f.WithTopic(LOAD_TOPIC).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce))
                    .WithTopicFilter(f => f.WithTopic(SOC_TOPIC).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce))
                    .WithTopicFilter(f => f.WithTopic(GRID_TOPIC).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce))
                    .WithTopicFilter(f => f.WithTopic(VOLT_TOPIC).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce))
                    .Build();

                await _client.SubscribeAsync(subs, CancellationToken.None);
                Connected?.Invoke();
            };

            _options = new MqttClientOptionsBuilder()
                .WithTcpServer(AppSettings.BrokerAddress, AppSettings.BrokerPort)
                .WithClientId($"SolarTray-{Environment.MachineName}")
                .WithCleanSession()
                .Build();

            // Auto-reconnect loop on disconnect
            _client.DisconnectedAsync += async _ =>
            {
                if (_client == null || _options == null) return;

                // Try forever (simple + robust)
                while (!_client.IsConnected)
                {
                    try
                    {
                        await Task.Delay(2000);
                        await _client.ConnectAsync(_options, CancellationToken.None);
                    }
                    catch
                    {
                        // swallow and keep trying
                    }
                }
            };

            var result = await _client.ConnectAsync(_options, ct);
            if (result.ResultCode != MqttClientConnectResultCode.Success)
                ConnectionFailed?.Invoke($"Solar: connect failed: {result.ResultCode}");
        }

        private void ApplyMessage(string topic, string payload)
        {
            var ci = CultureInfo.InvariantCulture;
            if (!double.TryParse(payload, NumberStyles.Any, ci, out var value))
                return;

            if (topic == PV_TOPIC) _snapshot.PvKw = value / 1000.0;
            if (topic == LOAD_TOPIC) _snapshot.LoadKw = value / 1000.0;
            if (topic == SOC_TOPIC) _snapshot.SocPercent = value;
            if (topic == GRID_TOPIC) _snapshot.GridKw = value / 1000.0;
            if (topic == VOLT_TOPIC) _snapshot.BatteryVolts = value;
        }

        public async ValueTask DisposeAsync()
        {
            if (_client == null) return;

            try { await _client.DisconnectAsync(); } catch { /* ignore */ }
            _client.Dispose();
            _client = null;
        }
    }
}
