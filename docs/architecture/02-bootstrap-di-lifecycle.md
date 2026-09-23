# DI thuần, Bootstrap Root và lifecycle

[← Mục lục](README.md)

**Trạng thái:** đề xuất để phân tích và thảo luận; chưa phải quyết định triển khai. DI thuần và Bootstrap Root là ràng buộc đã chốt. Các lựa chọn khác bên dưới còn có thể sửa.

## Bài toán

Đã chốt theo yêu cầu người dùng: không VContainer; dependency được cấp từ bên ngoài và wire tại Bootstrap Root.

## Thiết kế đang đề xuất

GameBootstrap giữ scene/config/view references, tạo service bằng constructor, tạo factory/presenter rồi mới bật simulation. MonoBehaviour nhận Initialize tường minh.

```text
Validate config → Load state → Tạo services → Tạo factories
→ Bind views → Spawn actors → Start tick
```
Teardown dừng tick trước, dispose presenter/actor rồi giải phóng service có tài nguyên. Factory chịu trách nhiệm object do nó spawn. Không truyền Bootstrap vào gameplay.

## Phương án và trade-off

Một MonoBehaviour root đủ cho một scene. Khi wiring dài có thể tách hàm composition theo feature; chưa cần installer framework. AppBootstrap xuyên scene chỉ hữu ích khi thật sự có nhiều scene và service sống lâu hơn scene.

## Điểm cần thảo luận

- Bootstrap có trực tiếp giữ tất cả service hay giữ một session object?
- Tick tập trung qua runner hay actor tự Update?
- Scene reload có reset hoàn toàn session không?

## Điều kiện cần giữ

Awake/OnEnable không dùng dependency chưa initialize. Cleanup gọi lặp an toàn; startup lỗi giữa chừng dọn được phần đã tạo. Không static Instance/GetService.

## Liên quan

Xem [chủ đề liên quan](01-module-boundaries.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.

