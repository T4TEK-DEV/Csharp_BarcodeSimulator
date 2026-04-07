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
        private Button btnSimulateKeyboard;
        private Button btnSimulateWebSocket;
        private Label lblStatus;
        private Label lblWsStatus;
        private NumericUpDown numDelay;
        private NumericUpDown numDelayBarcode;
        private NumericUpDown numReadDuration;
        
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
                _deviceManager.StartServer(8181, message => {
                    this.Invoke((MethodInvoker)delegate {
                        LogMessage($"[WS Client] {message}");
                        
                        string action = "";
                        int timeout = (int)numReadDuration.Value;

                        try {
                            using (var doc = System.Text.Json.JsonDocument.Parse(message))
                            {
                                if (doc.RootElement.TryGetProperty("action", out var actElem))
                                    action = actElem.GetString() ?? "";
                                if (doc.RootElement.TryGetProperty("duration", out var durElem) && durElem.ValueKind == System.Text.Json.JsonValueKind.Number)
                                    timeout = durElem.GetInt32();
                            }
                        } catch { 
                            if (message.Contains("READ_RFID_KEYBOARD")) action = "READ_RFID_KEYBOARD";
                            else if (message.Contains("READ_RFID")) action = "READ_RFID";
                        }

                        if (action == "READ_RFID_KEYBOARD")
                        {
                            LogMessage($">> Odoo triggered RFID read (KEYBOARD). Reading for {timeout}ms...");
                            string[] lines = txtBarcodes.Lines;
                            int charDelay = (int)numDelay.Value;
                            int barDelay = (int)numDelayBarcode.Value;
                            
                            System.Threading.Tasks.Task.Run(async () => 
                            {
                                await System.Threading.Tasks.Task.Delay(timeout);
                                _deviceManager.SendViaKeyboard(lines, 0, charDelay, barDelay);
                            });
                        }
                        else if (action == "READ_RFID")
                        {
                            LogMessage($">> Odoo triggered RFID read (WS). Reading for {timeout}ms...");
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
                lblWsStatus.Text = "WS Server: ws://127.0.0.1:8181 (Running 🟢)";
                lblWsStatus.ForeColor = Color.Green;
                LogMessage("Server started on port 8181.");
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
            this.btnSimulateKeyboard = new Button();
            this.btnSimulateWebSocket = new Button();
            this.lblStatus = new Label();
            this.lblWsStatus = new Label();
            this.numDelay = new NumericUpDown();
            this.numDelayBarcode = new NumericUpDown();
            this.numReadDuration = new NumericUpDown();
            
            Label lbl1 = new Label() { Text = "Delay char (ms):", AutoSize = true, Location = new Point(12, 180) };
            Label lbl2 = new Label() { Text = "Delay bar (ms):", AutoSize = true, Location = new Point(200, 180) };
            Label lbl3 = new Label() { Text = "RFID Read Time (ms):", AutoSize = true, Location = new Point(12, 213) };
            
            ((System.ComponentModel.ISupportInitialize)(this.numDelay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDelayBarcode)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numReadDuration)).BeginInit();
            this.SuspendLayout();

            this.lblWsStatus.AutoSize = true;
            this.lblWsStatus.Location = new Point(12, 10);

            // txtBarcodes
            this.txtBarcodes.Location = new Point(12, 35);
            this.txtBarcodes.Multiline = true;  
            this.txtBarcodes.Size = new Size(400, 140);
            this.txtBarcodes.Text = "RFID_001\r\nRFID_002\r\nRFID_003\r\nRFID_004\r\nRFID_005";

            this.numDelay.Location = new Point(110, 178);
            this.numDelay.Size = new Size(60, 23);
            this.numDelay.Value = 20;

            this.numDelayBarcode.Location = new Point(290, 178);
            this.numDelayBarcode.Size = new Size(60, 23);
            this.numDelayBarcode.Value = 50;

            this.numReadDuration.Location = new Point(140, 210);
            this.numReadDuration.Size = new Size(80, 23);
            this.numReadDuration.Value = 1000;
            this.numReadDuration.Maximum = 60000;

            // btnSimulateKeyboard
            this.btnSimulateKeyboard.Location = new Point(12, 240);
            this.btnSimulateKeyboard.Size = new Size(190, 40);
            this.btnSimulateKeyboard.Text = "Send via Keyboard (3s wait)";
            this.btnSimulateKeyboard.Click += btnSimulateKeyboard_Click;

            // btnSimulateWebSocket
            this.btnSimulateWebSocket.Location = new Point(222, 240);
            this.btnSimulateWebSocket.Size = new Size(190, 40);
            this.btnSimulateWebSocket.Text = "Send via WebSocket (Instant)";
            this.btnSimulateWebSocket.Click += btnSimulateWebSocket_Click;

            // txtLog
            this.txtLog.Location = new Point(12, 290);
            this.txtLog.Multiline = true;
            this.txtLog.ScrollBars = ScrollBars.Vertical;
            this.txtLog.ReadOnly = true;
            this.txtLog.Size = new Size(400, 120);

            // lblStatus
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new Point(12, 420);
            this.lblStatus.Text = "Ready.";

            // Form1
            this.ClientSize = new Size(424, 450);
            this.Controls.Add(lbl1);
            this.Controls.Add(lbl2);
            this.Controls.Add(lbl3);
            this.Controls.Add(this.lblWsStatus);
            this.Controls.Add(this.numDelay);
            this.Controls.Add(this.numDelayBarcode);
            this.Controls.Add(this.numReadDuration);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnSimulateKeyboard);
            this.Controls.Add(this.btnSimulateWebSocket);
            this.Controls.Add(this.txtBarcodes);
            this.Text = "Device Simulator";
            
            ((System.ComponentModel.ISupportInitialize)(this.numDelay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDelayBarcode)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numReadDuration)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private async void btnSimulateKeyboard_Click(object sender, EventArgs e)
        {
            var lines = txtBarcodes.Lines;
            if (lines.Length == 0) return;

            int delay = (int)numDelay.Value;
            int delayBar = (int)numDelayBarcode.Value;
            
            btnSimulateKeyboard.Enabled = false;
            btnSimulateWebSocket.Enabled = false;
            lblStatus.Text = "Waiting 3 seconds...";
            LogMessage("Starting Keyboard Emulation...");
            
            await System.Threading.Tasks.Task.Run(() => 
            {
                _deviceManager.SendViaKeyboard(lines, 3000, delay, delayBar);
            });

            LogMessage("Keyboard Emulation completed.");
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
