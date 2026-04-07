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

        public void StartServer(int port = 8181, Action<string>? onCommandReceived = null)
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

        public void SendViaKeyboard(string[] barcodes, int waitBeforeStartMs = 3000, int delayBetweenKeys = 20, int delayBetweenBarcodes = 50)
        {
            Thread.Sleep(waitBeforeStartMs);

            var processedData = ProcessData(barcodes);

            foreach (var barcode in processedData)
            {
                if (string.IsNullOrWhiteSpace(barcode)) continue;

                foreach (char c in barcode)
                {
                    string key = c.ToString();
                    if (key == "+" || key == "^" || key == "%" || key == "~" || key == "(" || key == ")" || key == "{" || key == "}" || key == "[" || key == "]")
                    {
                        key = "{" + key + "}";
                    }

                    SendKeys.SendWait(key);
                    Thread.Sleep(delayBetweenKeys);
                }

                SendKeys.SendWait("{ENTER}");
                Thread.Sleep(delayBetweenBarcodes);
            }
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
