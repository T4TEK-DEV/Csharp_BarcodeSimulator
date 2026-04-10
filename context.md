# Business Context & Requirements

## The Problem
Modern warehouse operations utilize extensive automated devices, commonly Traditional Barcode Scanners and RFID Gates. 
1. **Barcode Scanners (Active)** act as Keyboards, suffering from sequential typing delays and browser focus issues.
2. **RFID Gates (Passive)** read hundreds of tags simultaneously. Attempting to pass hundreds of tags sequentially via Keyboard Emulation to a web browser (like Odoo) causes severe lag, UI choking, and catastrophic Data Loss if the user alters screen focus.

## The Solution
This C# application acts as a Local Hardware Gateway. It provides a **Hybrid Integration**:
- It proxies single-item barcode scans via the traditional OS Keyboard HID protocol for immediate compatibility.
- It clusters massive RFID collections into optimized JSON arrays and routes them instantaneously through a local WebSocket server (Port `9001`), overriding interface latency and preventing focus-loss errors.

## Application Scenarios
- **Warehouse Packing**: An employee walks through an RFID portal; the portal pushes 50 tags via WebSocket.
- **Manual Overrides**: An employee manually keys a missing tag via a Handheld Barcode scanner; it pipes through the Keyboard HID flow.
