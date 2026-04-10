# Technical Stack & Architecture

## Tech Stack
- **Framework**: .NET 8.0 (Windows Desktop Environment)
- **UI Framework**: Windows Forms (WinForms)
- **Core Library**: `Fleck` (v1.2.0) - A lightweight C# WebSocket implementation.

## Architectural Decisions
- **Decoupled Architecture**: Hardware communication logic and WebSocket transmission are abstracted into a separate class library (`KeyboardEmulator.dll` -> `DeviceIntegrationManager.cs`). This ensures it can be portably integrated into WPF or Windows Services without relying on the WinForm UI.
- **WebSocket Server (`ws://0.0.0.0:9001`)**: Utilizes Fleck to maintain a Persistent local server.
- **Multithreading**: Uses `System.Threading.Tasks.Task.Run` for blocking delays (`Task.Delay`) and Keyboard Emulation (`SendKeys.SendWait`), preventing the main UI thread from freezing.
- **Data Preprocessing**: Uses LINQ (`.Select().ToUpper()`, `.Distinct()`) to ensure no duplicated RFID tags are sent, reducing Payload overhead on the client.
