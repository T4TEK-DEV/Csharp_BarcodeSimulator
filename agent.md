# AI Agent Prompting Rules

When modifying this repository, adhere to the following strict guidelines:

1. **Compilation Validation**: Always run `dotnet build BarcodeSimulator.sln` inside the root folder to verify syntax. If the file is locked by an active process (`ScannerApp.exe`), forcefully kill it or notify the user before compiling.
2. **Library Abstraction**: Any new hardware logic (e.g. SerialPort reading, TCP listening) MUST be added to `KeyboardEmulator/DeviceIntegrationManager.cs`, NOT in `Form1.cs`. The WinForm is strictly for configuration and UI triggering.
3. **Threading**: Remember that `Fleck`'s `OnMessage` callbacks execute on a Background Thread. Invoking WinForms UI controls requires `this.Invoke(...)`.
4. **JSON Handling**: Always use `System.Text.Json` since it's the native, high-performance standard in .NET 8. Extract configurations dynamically using `JsonDocument.Parse()`.
5. **Nullability**: Use C# 8 Nullable reference types appropriately (e.g. `WebSocketServer?`). Resolve any CS8618 warnings gracefully.
