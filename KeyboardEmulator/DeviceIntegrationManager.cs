using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using Fleck;

namespace KeyboardEmulator
{
    public class DeviceIntegrationManager
    {
        private WebSocketServer? _server;
        private List<IWebSocketConnection> _allSockets;
        private Action<string>? _onCommandReceived;

        public DeviceIntegrationManager()
        {
            _allSockets = new List<IWebSocketConnection>();
        }

        public void StartServer(int port = 9001, Action<string>? onCommandReceived = null)
        {
            _onCommandReceived = onCommandReceived;
            FleckLog.Level = LogLevel.Error; // Reduce console spam
            
            _server = new WebSocketServer($"ws://0.0.0.0:{port}");
            
            _server.Start(socket =>
            {
                socket.OnOpen = () => _allSockets.Add(socket);
                socket.OnClose = () => _allSockets.Remove(socket);
                socket.OnMessage = message =>
                {
                    // Trigger when Odoo sends a message
                    _onCommandReceived?.Invoke(message);
                };
            });
        }

        public void StopServer()
        {
            if (_server != null)
            {
                foreach (var socket in _allSockets)
                {
                    socket.Close();
                }
                _server.Dispose();
                _server = null;
            }
        }

        private List<string> ProcessData(string[] data)
        {
            return data
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Select(d => d.Trim().ToUpper())
                .Distinct()
                .ToList();
        }

        public (int count, long elapsedMs) SendViaKeyboard(string[] barcodes, int waitBeforeStartMs = 3000, string delimiter = "|", string prefix = "")
        {
            Thread.Sleep(waitBeforeStartMs);

            var processedData = ProcessData(barcodes);
            if (processedData.Count == 0) return (0, 0);

            string batchString = string.Join(delimiter, processedData);

            // Prefix the batch with button id only once (e.g. "rfid:TAG001|TAG002")
            if (!string.IsNullOrEmpty(prefix))
            {
                batchString = $"{prefix}:{batchString}";
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            Clipboard.SetText(batchString);
            SendKeys.SendWait("^v");
            sw.Stop();

            Clipboard.Clear();

            return (processedData.Count, sw.ElapsedMilliseconds);
        }

        public void SendViaWebSocket(string[] data)
        {
            var processedData = ProcessData(data);

            var payload = new
            {
                type = "rfid_bulk",
                data = processedData
            };
            
            string json = JsonSerializer.Serialize(payload);

            foreach (var socket in _allSockets)
            {
                socket.Send(json);
            }
        }
    }
}
