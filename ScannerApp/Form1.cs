using System;
using System.Drawing;
using System.Windows.Forms;
using KeyboardEmulator;

namespace ScannerApp
{
    public partial class Form1 : Form
    {
        private TextBox txtBarcodes;
        private TextBox txtLog;
        private TextBox txtDelimiter;
        private Button btnSimulateKeyboard;
        private Button btnSimulateWebSocket;
        private Label lblStatus;
        private Label lblWsStatus;

        private DeviceIntegrationManager _deviceManager;

        public Form1()
        {
            InitializeComponent();
            _deviceManager = new DeviceIntegrationManager();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            try
            {
                _deviceManager.StartServer(9001, message => {
                    this.Invoke((MethodInvoker)delegate {
                        LogMessage($"[WS Client] {message}");

                        string action = "";
                        string buttonId = "";
                        int timeout = 1000;

                        try {
                            using (var doc = System.Text.Json.JsonDocument.Parse(message))
                            {
                                if (doc.RootElement.TryGetProperty("action", out var actElem))
                                    action = actElem.GetString() ?? "";
                                if (doc.RootElement.TryGetProperty("duration", out var durElem) && durElem.ValueKind == System.Text.Json.JsonValueKind.Number)
                                    timeout = durElem.GetInt32();
                                if (doc.RootElement.TryGetProperty("id", out var idElem))
                                    buttonId = idElem.GetString() ?? "";
                            }
                        } catch {
                            if (message.Contains("READ_RFID_KEYBOARD")) action = "READ_RFID_KEYBOARD";
                            else if (message.Contains("READ_RFID")) action = "READ_RFID";
                        }

                        if (action == "READ_RFID_KEYBOARD")
                        {
                            LogMessage($">> Odoo triggered RFID read (KEYBOARD). Id={buttonId}, Duration={timeout}ms");
                            string[] lines = txtBarcodes.Lines;
                            string delimiter = txtDelimiter.Text;
                            string capturedId = buttonId;
                            int capturedTimeout = timeout;

                            System.Threading.Tasks.Task.Run(async () =>
                            {
                                await System.Threading.Tasks.Task.Delay(capturedTimeout);
                                this.Invoke((MethodInvoker)delegate {
                                    var (count, elapsed) = _deviceManager.SendViaKeyboard(lines, 0, delimiter, capturedId);
                                    LogMessage($">> KB done: {count} barcodes (prefix={capturedId}), paste took {elapsed}ms");
                                });
                            });
                        }
                        else if (action == "READ_RFID")
                        {
                            LogMessage($">> Odoo triggered RFID read (WS). Duration={timeout}ms");
                            string[] lines = txtBarcodes.Lines;
                            System.Threading.Tasks.Task.Run(async () =>
                            {
                                await System.Threading.Tasks.Task.Delay(timeout);
                                this.Invoke((MethodInvoker)delegate {
                                    _deviceManager.SendViaWebSocket(lines);
                                    LogMessage(">> Sent RFID array to Odoo instantly.");
                                });
                            });
                        }
                    });
                });
                lblWsStatus.Text = "WS Server: ws://127.0.0.1:9001 (Running 🟢)";
                lblWsStatus.ForeColor = Color.Green;
                LogMessage("Server started on port 9001.");
            }
            catch (Exception ex)
            {
                lblWsStatus.Text = "WS Server Error 🔴";
                lblWsStatus.ForeColor = Color.Red;
                LogMessage("Error starting server: " + ex.Message);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _deviceManager.StopServer();
            base.OnFormClosing(e);
        }

        private void LogMessage(string msg)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\r\n");
        }

        private void InitializeComponent()
        {
            this.txtBarcodes = new TextBox();
            this.txtLog = new TextBox();
            this.txtDelimiter = new TextBox();
            this.btnSimulateKeyboard = new Button();
            this.btnSimulateWebSocket = new Button();
            this.lblStatus = new Label();
            this.lblWsStatus = new Label();

            Label lblDelimiter = new Label() { Text = "Delimiter:", AutoSize = true, Location = new Point(12, 180) };

            this.SuspendLayout();

            this.lblWsStatus.AutoSize = true;
            this.lblWsStatus.Location = new Point(12, 10);

            // txtBarcodes
            this.txtBarcodes.Location = new Point(12, 35);
            this.txtBarcodes.Multiline = true;
            this.txtBarcodes.Size = new Size(400, 140);
            this.txtBarcodes.Text = "RFID_001\r\nRFID_002\r\nRFID_003\r\nRFID_004\r\nRFID_005";

            // txtDelimiter
            this.txtDelimiter.Location = new Point(80, 178);
            this.txtDelimiter.Size = new Size(30, 23);
            this.txtDelimiter.Text = "|";

            // btnSimulateKeyboard
            this.btnSimulateKeyboard.Location = new Point(12, 210);
            this.btnSimulateKeyboard.Size = new Size(195, 40);
            this.btnSimulateKeyboard.Text = "Send via Keyboard (3s)";
            this.btnSimulateKeyboard.Click += btnSimulateKeyboard_Click;

            // btnSimulateWebSocket
            this.btnSimulateWebSocket.Location = new Point(217, 210);
            this.btnSimulateWebSocket.Size = new Size(195, 40);
            this.btnSimulateWebSocket.Text = "Send via WebSocket";
            this.btnSimulateWebSocket.Click += btnSimulateWebSocket_Click;

            // txtLog
            this.txtLog.Location = new Point(12, 260);
            this.txtLog.Multiline = true;
            this.txtLog.ScrollBars = ScrollBars.Vertical;
            this.txtLog.ReadOnly = true;
            this.txtLog.Size = new Size(400, 140);

            // lblStatus
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new Point(12, 410);
            this.lblStatus.Text = "Ready.";

            // Form1
            this.ClientSize = new Size(424, 440);
            this.Controls.Add(lblDelimiter);
            this.Controls.Add(this.lblWsStatus);
            this.Controls.Add(this.txtDelimiter);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnSimulateKeyboard);
            this.Controls.Add(this.btnSimulateWebSocket);
            this.Controls.Add(this.txtBarcodes);
            this.Text = "Device Simulator";

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private async void btnSimulateKeyboard_Click(object sender, EventArgs e)
        {
            var lines = txtBarcodes.Lines;
            if (lines.Length == 0) return;

            string delimiter = txtDelimiter.Text;

            btnSimulateKeyboard.Enabled = false;
            btnSimulateWebSocket.Enabled = false;
            lblStatus.Text = "Waiting 3 seconds...";
            LogMessage($"Starting Keyboard Emulation (delimiter='{delimiter}')...");

            await System.Threading.Tasks.Task.Delay(3000);
            var (count, elapsed) = _deviceManager.SendViaKeyboard(lines, 0, delimiter);

            LogMessage($"Keyboard done: {count} barcodes, paste took {elapsed}ms");
            lblStatus.Text = "Ready.";
            btnSimulateKeyboard.Enabled = true;
            btnSimulateWebSocket.Enabled = true;
        }

        private void btnSimulateWebSocket_Click(object sender, EventArgs e)
        {
            var lines = txtBarcodes.Lines;
            if (lines.Length == 0) return;

            LogMessage("Broadcasting WS data directly.");
            _deviceManager.SendViaWebSocket(lines);
            lblStatus.Text = "Sent via WebSocket.";
        }
    }
}
