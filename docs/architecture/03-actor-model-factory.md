# Actor model và Factory

[← Mục lục](README.md)

**Trạng thái:** đề xuất để phân tích và thảo luận; chưa phải quyết định triển khai. DI thuần và Bootstrap Root là ràng buộc đã chốt. Các lựa chọn khác bên dưới còn có thể sửa.

## Bài toán

Nhân viên và khách đều có prefab, vị trí, chuyển động, animation và lifecycle, nhưng nghiệp vụ khác nhau.

## Thiết kế đang đề xuất

Chia actor thành controller C# + view MonoBehaviour + movement/animation adapter. Mỗi actor sở hữu ID, stats và FSM riêng. WorkerFactory và CustomerFactory ở Bootstrap compose actor từ prefab/config và service cần thiết.

Factory trả handle để quản lý tick/despawn/dispose. State dùng chung về cơ chế, không dùng chung instance mutable. Tái sử dụng prefab Delivery và Customer hiện có.

## Phương án và trade-off

Composition tránh cây kế thừa Character → NPC → Worker/Customer nhiều tầng. Một base class nhỏ vẫn hợp lý nếu có logic lifecycle thực sự giống nhau. Hai factory tường minh dễ đọc hơn generic ActorFactory nhiều tham số. Builder chưa có nhu cầu rõ.

## Điểm cần thảo luận

- Có cần base ActorController hay chỉ chia sẻ FSM và adapters?
- Factory trả controller, handle hay ID?
- Actor đang mang hàng có được despawn giữa session không?

## Điều kiện cần giữ

Mỗi lần spawn có state riêng; despawn gỡ tick/subscription và hủy view. Owner hàng phải được giải quyết trước khi hủy nhân viên.

## Liên quan

Xem [chủ đề liên quan](04-actor-state-machines.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.

