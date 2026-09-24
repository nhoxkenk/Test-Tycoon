# Save/load, lifecycle mobile và kiểm chứng

[← Mục lục](README.md)

**Trạng thái:** đề xuất để phân tích và thảo luận; chưa phải quyết định triển khai. DI thuần và Bootstrap Root là ràng buộc đã chốt. Các lựa chọn khác bên dưới còn có thể sửa.

## Bài toán

Requirement nói lưu trữ tiền lớn; phạm vi lưu cả tiến trình và hàng đang vận chuyển cần được thảo luận riêng.

## Thiết kế đang đề xuất

Đề xuất ProgressSnapshot chứa schemaVersion, danh sách số dư `(CurrencyId, amount dạng chuỗi)`, PlotId/built/level, purchase records và target population. CurrencyId lưu bằng tên/ID ổn định, không phụ thuộc thứ tự của enum. IProgressStore thuộc Simulation, JSON adapter thuộc UnityAdapters. Load trước spawn/tick, tái dựng buff từ records một lần.

Save tối thiểu trước đây đề xuất reset actor/FSM và bỏ lô chưa bán khi load. Đây là lựa chọn đang thảo luận, không phải requirement đã xác nhận. Nếu cần giữ hàng, lưu BatchId, owner và SaleValue cùng snapshot.

## Phương án và trade-off

Chỉ lưu progression đơn giản nhưng người chơi có thể mất hàng đang mang. Snapshot đầy đủ giữ hàng nhưng tăng độ phức tạp reconciliation. Save theo giao dịch/debounce và flush khi pause giảm mất tiến trình; không đảm bảo cứu được mọi thay đổi khi OS kill đột ngột.

## Điểm cần thảo luận

- Có chấp nhận mất hàng chưa bán khi restart không?
- Save bắt buộc cho toàn bộ tiến trình hay chỉ tiền?
- Target khách lưu trực tiếp hay tính từ upgrade records làm nguồn duy nhất?
- Có offline income không? Hiện chưa có trong requirement.

## Điều kiện cần giữ

Không apply buff/+2 khách lần hai khi load. File hỏng/schema khác không bị ghi đè âm thầm. Kiểm chứng tiền lớn, pause/resume, navigation và vòng chơi trên Android IL2CPP; acceptance không đồng nghĩa code đã chạy.

## Liên quan

Xem [chủ đề liên quan](02-bootstrap-di-lifecycle.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.
