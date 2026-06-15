using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace ScannerApp
{
    // Mô phỏng thiết bị CHỦ ĐỘNG (active barcode scanner / keyboard wedge):
    // giữ nguyên case người dùng nhập, KHÔNG ToUpper. Tách khỏi
    // KeyboardEmulator.DeviceIntegrationManager — DeviceIntegrationManager là
    // library dùng cho thiết bị BỊ ĐỘNG (passive HID, RFID) với pipeline
    // ProcessData chuẩn hóa in hoa.
    internal static class ActiveDeviceSimulator
    {
        // Default khớp với BATCH_DELIMITER ở DeviceIntegrationManager + phía
        // receiver (t4_passivehid_bridge, t4_sequential_auto_input). Chỉ dùng
        // khi caller không truyền delimiter (vd. để trống ô cấu hình trên form).
        private const string Delimiter = "|";

        // Type từng barcode + ENTER. Mỗi barcode kết thúc bằng Enter để
        // trigger addTableEnterListener của t4_sequential_auto_input.
        public static (int count, long elapsedMs) SendViaKeyboard(string[] barcodes)
        {
            var processed = Process(barcodes);
            if (processed.Count == 0) return (0, 0);

            var sw = Stopwatch.StartNew();
            foreach (var raw in processed)
            {
                SendKeys.SendWait(EscapeForSendKeys(raw));
                SendKeys.SendWait("{ENTER}");
                // Cho OWL re-render + moveToNextEmptyTarget xong trước khi
                // gõ barcode kế tiếp; gửi quá nhanh sẽ rơi vào field cũ.
                Thread.Sleep(250);
            }
            sw.Stop();
            return (processed.Count, sw.ElapsedMilliseconds);
        }

        // Join bằng delimiter (cấu hình trên form) rồi paste 1 lần (Ctrl+V),
        // giữ nguyên case. delimiter rỗng → fallback về Delimiter mặc định.
        public static (int count, long elapsedMs) SendViaClipboard(string[] barcodes, string delimiter = Delimiter)
        {
            if (string.IsNullOrEmpty(delimiter)) delimiter = Delimiter;

            var processed = Process(barcodes);
            if (processed.Count == 0) return (0, 0);

            string batch = string.Join(delimiter, processed);
            var sw = Stopwatch.StartNew();
            Clipboard.SetText(batch);
            SendKeys.SendWait("^v");
            sw.Stop();
            Clipboard.Clear();
            return (processed.Count, sw.ElapsedMilliseconds);
        }

        private static List<string> Process(string[] data) =>
            data.Where(d => !string.IsNullOrWhiteSpace(d))
                .Select(d => d.Trim())
                .Distinct()
                .ToList();

        // Escape các ký tự đặc biệt của SendKeys: + ^ % ~ ( ) { } [ ]
        // (xem https://learn.microsoft.com/dotnet/api/system.windows.forms.sendkeys).
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
    }
}
