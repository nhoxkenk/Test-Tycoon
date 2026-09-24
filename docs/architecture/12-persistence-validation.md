# Save/load, lifecycle mobile và kiểm chứng

[← Mục lục](README.md)

**Trạng thái:** đã triển khai save tiền + tiến trình plot + record nâng cấp; actor/batch runtime reset khi load. Chi tiết và giới hạn kiểm chứng ở [phần 15](15-final-implementation-review.md). Người dùng tự build APK; phần mình làm kiểm chứng code/scene trong Editor.

## Bài toán

Requirement nói lưu trữ tiền lớn. Người dùng đã chọn lưu cả progression, nhưng không khôi phục worker/customer hoặc lô đang vận chuyển.

## Thiết kế đang đề xuất

`ProgressSnapshot` chứa schemaVersion, danh sách số dư `(CurrencyId, amount dạng chuỗi)`, `PlotId`/trạng thái xây/level và purchase records. CurrencyId lưu bằng tên/ID ổn định, không phụ thuộc thứ tự enum. `IProgressStore` thuộc Simulation, JSON adapter thuộc UnityAdapters. Load trước spawn/tick; tái dựng modifier và target khách từ records đúng một lần.

Reset actor/FSM và bỏ lô chưa bán khi load **đã được chốt**; không cộng tiền cho lô đó. Build đang dở dự kiến tiếp tục từ `remainingBuildSeconds` đã lưu, không có offline income. File hỏng/schema khác phải báo rõ và không bị ghi đè âm thầm.

## Phương án và trade-off

Chỉ lưu progression đơn giản nhưng người chơi có thể mất hàng đang mang. Snapshot đầy đủ giữ hàng nhưng tăng độ phức tạp reconciliation. Save theo giao dịch/debounce và flush khi pause giảm mất tiến trình; không đảm bảo cứu được mọi thay đổi khi OS kill đột ngột.

## Quy tắc đã chốt

- Lưu số dư, plot đã mở/cấp và purchase records. Batch chưa bán cùng worker/customer chỉ tồn tại trong session hiện tại.
- Target khách tính từ purchase records làm nguồn duy nhất; không apply +2 lần thứ hai sau load.
- Không có offline income. Chưa triển khai build/ký APK vì người dùng sẽ tự build.

## Điều kiện cần giữ

Không apply buff/+2 khách lần hai khi load. File hỏng/schema khác không bị ghi đè âm thầm. Kiểm chứng tiền lớn, pause/resume, navigation và vòng chơi trong Unity Editor; người dùng tự build và kiểm tra APK sau đó. Acceptance không đồng nghĩa code đã chạy.

## Liên quan

Xem [chủ đề liên quan](02-bootstrap-di-lifecycle.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.
