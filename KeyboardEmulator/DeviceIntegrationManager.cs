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

            // Có prefix (button id) ⇒ thiết bị BỊ ĐỘNG (passive HID, vd. RFID
            // reader): dùng clipboard + Ctrl+V để paste 1 lần "prefix:val|val".
            // Browser nhận paste event → t4_passivehid_bridge.barcode_service_patch
            // pasteHandler bắt qua "prefix:" rồi fire t4_passive_scanned.
            if (!string.IsNullOrEmpty(prefix))
            {
                string batchString = $"{prefix}:{string.Join(delimiter, processedData)}";
                var swPaste = System.Diagnostics.Stopwatch.StartNew();
                Clipboard.SetText(batchString);
                SendKeys.SendWait("^v");
                swPaste.Stop();
                Clipboard.Clear();
                return (processedData.Count, swPaste.ElapsedMilliseconds);
            }

            // Không prefix ⇒ thiết bị CHỦ ĐỘNG (active barcode scanner /
            // keyboard wedge): mô phỏng gõ phím thật. Mỗi barcode được type
            // thành chuỗi ký tự + ENTER (kết thúc barcode). Cursor đang focus
            // trên field nào thì giá trị vào field đó, Enter trigger
            // addTableEnterListener của t4_sequential_auto_input → fill
            // ORM + nhảy sang data-auto-input-order kế tiếp.
            var sw = System.Diagnostics.Stopwatch.StartNew();
            foreach (var raw in processedData)
            {
                string escaped = EscapeForSendKeys(raw);
                SendKeys.SendWait(escaped);
                SendKeys.SendWait("{ENTER}");
                // Cho OWL re-render + handler t4_sequential_auto_input
                // moveToNextEmptyTarget xong (activateEditMode + focus) trước
                // khi gõ barcode kế tiếp; nếu gửi quá nhanh, ký tự barcode
                // tiếp theo sẽ rơi vào field cũ chưa kịp đổi.
                Thread.Sleep(250);
            }
            sw.Stop();
            return (processedData.Count, sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Escape các ký tự đặc biệt của SendKeys: + ^ % ~ ( ) { } [ ]
        /// (xem https://learn.microsoft.com/dotnet/api/system.windows.forms.sendkeys).
        /// </summary>
        private static string EscapeForSendKeys(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var sb = new System.Text.StringBuilder(text.Length + 8);
            foreach (var ch in text)
            {
                switch (ch)
                {
                    case '+': sb.Append("{+}"); break;
                    case '^': sb.Append("{^}"); break;
                    case '%': sb.Append("{%}"); break;
                    case '~': sb.Append("{~}"); break;
                    case '(': sb.Append("{(}"); break;
                    case ')': sb.Append("{)}"); break;
                    case '{': sb.Append("{{}"); break;
                    case '}': sb.Append("{}}"); break;
                    case '[': sb.Append("{[}"); break;
                    case ']': sb.Append("{]}"); break;
                    default:  sb.Append(ch);   break;
                }
            }
            return sb.ToString();
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
