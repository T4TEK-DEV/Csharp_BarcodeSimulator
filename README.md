# Hybrid Hardware Integration Simulator (Odoo & C#)

Test simulator cho hệ thống tích hợp phần cứng (máy quét mã vạch + trạm đọc RFID) vào Odoo browser bằng mô hình **Hybrid dual transport** (HID + WebSocket).

> **Trạng thái (2026-05+):** Test app dùng tích cực trong dev workflow. KHÔNG legacy.
> Production modules (`t4_sequential_auto_input` v1.2 + `t4_passivehid_bridge` v4.0.1)
> giao tiếp với simulator này qua WS port 9001 và Clipboard/SendKeys path.

## 🏗 Cấu trúc Dự án

2 C# project trong solution `BarcodeSimulator.sln` (.NET 8 WinForms + Fleck v1.2.0):

1. **`KeyboardEmulator/`** (Class Library): `DeviceIntegrationManager.cs` — Fleck WebSocket server
   trên `ws://0.0.0.0:9001`, HID injection (SendKeys + Clipboard batch), uppercase + dedup
   convention (passive device).
2. **`ScannerApp/`** (WinForms WinExe): `Program.cs` (`[STAThread]` bắt buộc cho Clipboard),
   `Form1.cs` (UI 424×480, 5 RFID default), `ActiveDeviceSimulator.cs` (preserve case
   — active scanner convention).

**Production Odoo modules giao tiếp với app này:**
- `addons/t4_sequential_auto_input/` — nhận barcode keystroke (active scanner path)
- `addons/t4_passivehid_bridge/` — gửi WS commands (`READ_RFID_KEYBOARD`, `READ_RFID`,
  `STOP_RFID_KEYBOARD`), nhận data trở lại qua Clipboard paste hoặc WS broadcast

Cùng nằm trong monorepo `addons/` — không còn là repo GitHub riêng.

---

## ⚡ So sánh 2 Phương thức Giao tiếp (Core Concept)

### 1. Phương thức Giả Lập Bàn Phím (Keyboard Emulation / HID)
Được sử dụng cho **Máy quét Barcode Cầm tay truyền thống**.
- **Cách thức hoạt động**: Bắn từng ký tự một xuống hệ điều hành mô phỏng việc con người đang gõ phím siêu nhanh (`SendKeys`). Hệ điều hành gửi vào Trình duyệt -> sinh ra event `keydown`.
- **Ưu điểm**:
  - Không cần cài app, Plug and Play. Hỗ trợ ngay bởi `barcodeService` mặc định của Odoo.
- **Nhược điểm & Giới hạn**:
  - **Tốc độ chậm**: Phải có Delay giữa các phím (20ms) và các thẻ (50ms). Quét 100 mã thẻ sẽ làm màn hình bị giật và mất vài chục giây để nhận đủ sóng `keydown`.
  - **Dễ mất dữ liệu (Rủi ro Focus)**: Nếu người dùng lỡ bấm chuột ra ngoài Odoo (sang Zalo, Excel), dòng lệnh quét sẽ rơi vào chỗ khác gây hỏng dữ liệu.
  - Phù hợp với: Barcode lẻ, quét từng cái. KHÔNG phù hợp với quét RFID hàng loạt.

### 2. Phương thức Mạng WebSocket (Background Sync)
Được thiết kế chuyên biệt cho **Hệ thống Ăng-ten và Trạm đọc RFID Khối lượng lớn (Bulk-reading)**.
- **Cách thức hoạt động**: Trạm đọc RFID trả List phần cứng -> App C# đóng gói thành 1 mảng JSON `['IDA', 'IDB']` -> App C# đẩy trực tiếp qua cổng `ws://127.0.0.1:9001` lên Odoo.
- **Ưu điểm Tuyệt đối**:
  - **Tốc độ chớp mắt (Instant)**: Truyền 1000 thẻ RFID chỉ tốn đúng 1 nhịp (Ping 1ms) mà không cần phải giả vờ gõ bàn phím.
  - **Ngầm và Chắc chắn**: Không bị ảnh hưởng bởi việc User lướt tab khác hay mất Focus chuột. Dữ liệu chạy trực tiếp vào Javascript Code.
- **Nhược điểm**: 
  - Cần duy trì 1 ứng dụng C# trạm (Background Service) để mở Port. Web Framework (Odoo) phải viết riêng code Hook Socket để hứng luồng.

---

## 🛠 Tính năng nâng cao

1. **Quản trị Thời gian chờ (Timeout Handling)**:
   - Các nút quét từ Web Odoo được làm Chủ (Master). Khi nhấn Trigger, Odoo gửi cấu hình `{ "action": "READ_RFID", "duration": 2000 }` xuống dịch vụ C# để chờ đúng con số 2 giây rồi mới trả kết quả. 
   - Đồng thời, UI của Odoo tự Disable phím bấm. Nếu quá `duration + 1.5s` mà không thấy C# hồi đáp, Odoo tự động huỷ chặn và Alert kết nối thất bại để ngăn treo Odoo vĩnh viễn.

2. **Tiền Căn chỉnh Dữ liệu (Preprocessing)**:
   - C# tích hợp cơ chế làm sạch: Tự động xóa khoảng trắng, in hoa (`ToUpper()`) và Lọc bỏ sạch mọi thẻ trùng xuất hiện quanh trạm sóng (`Distinct()`), đảm bảo Mảng JSON Odoo nhận được là duy nhất và sẵn sàng vào Database.

---
*Dự án thuộc T4TEK-DEV.*
