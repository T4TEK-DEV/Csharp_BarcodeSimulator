using System;
using System.Collections.Generic;
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

        public void SendViaKeyboard(string[] barcodes, int waitBeforeStartMs = 3000, int delayBetweenKeys = 20, int delayBetweenBarcodes = 50)
        {
            Thread.Sleep(waitBeforeStartMs);

            foreach (var barcode in barcodes)
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
            var cleanedData = new List<string>();
            foreach(var d in data)
            {
                if(!string.IsNullOrWhiteSpace(d)) {
                    cleanedData.Add(d.Trim());
                }
            }

            var payload = new
            {
                type = "rfid_bulk",
                data = cleanedData
            };
            
            string json = JsonSerializer.Serialize(payload);

            foreach (var socket in _allSockets)
            {
                socket.Send(json);
            }
        }
    }
}
