# UI, presentation và cấu hình

[← Mục lục](README.md)

**Trạng thái:** đề xuất để phân tích và thảo luận; chưa phải quyết định triển khai. DI thuần và Bootstrap Root là ràng buộc đã chốt. Các lựa chọn khác bên dưới còn có thể sửa.

## Bài toán

Project có sẵn Build/Upgrade/Management/Main views, animation và VFX. Cần phân định model, input và trình diễn.

## Thiết kế đang đề xuất

Đề xuất MVP nhỏ: View gửi ý định; Presenter gọi use case và bind dữ liệu; service quyết định. C# event theo service phục vụ HUD/VFX sau commit, không dùng global event bus để chạy nghiệp vụ.

ScriptableObject giữ config authoring ở UnityAdapters; chuyển thành runtime data tại Bootstrap. Không lưu balance/session state trên asset config. World view và navigation bindings ở UnityAdapters; UI/presenter ở Presentation.

## Phương án và trade-off

Presenter giúp test và tách logic nhưng không cần interface cho từng Text/Button. Giữ config runtime bất biến giảm mutation nhầm, đổi lại cần mapping SO. Với prototype nhỏ có thể ít mapping hơn nếu nới boundary engine.

## Điểm cần thảo luận

- Selection thuộc một controller chung hay presenter từng popup?
- Có cần preview lợi nhuận chính xác sau upgrade trên UI?
- Config đổi trong Play Mode có được áp vào session đang chạy không?
- Portrait xác nhận là canvas 1080 × 1920 chứ?

## Điều kiện cần giữ

UI không tự trừ tiền/tính lại sale. Popup không cho click xuyên xuống cây; unsubscribe khi đóng/dispose. Animation event không là nguồn cấp tiền.

## Liên quan

Xem [chủ đề liên quan](12-persistence-validation.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.

