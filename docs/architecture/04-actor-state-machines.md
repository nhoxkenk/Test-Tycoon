# State Pattern cho nhân viên và khách

[← Mục lục](README.md)

**Trạng thái:** đề xuất để phân tích và thảo luận; chưa phải quyết định triển khai. DI thuần và Bootstrap Root là ràng buộc đã chốt. Các lựa chọn khác bên dưới còn có thể sửa.

## Bài toán

Requirement yêu cầu trạng thái và hành vi nhân vật. Cần phân biệt cơ chế FSM với quyết định nghiệp vụ.

## Thiết kế đang đề xuất

Actors sở hữu IActorState và FSM nhỏ với Enter/Tick/Exit. Simulation sở hữu các state cụ thể.

```text
Worker: Idle → MoveToCrop → Harvesting → MoveToMarket
        → WaitingForCustomer → Delivering → Idle
Customer: Spawn → MoveToQueue → Waiting → Receiving
          → Leaving → Despawn
```
Timer và transaction thuộc logic; animation chỉ phản ánh state. State exit release tài nguyên tạm, còn cargo thuộc worker/session.

## Phương án và trade-off

Class theo state rõ khi mỗi state có timer, cancellation và dependency. Enum + switch ngắn hơn khi logic ít. Không cần generic hierarchical FSM hoặc behavior tree ngay. Có thể dùng class states cho actor và enum cho cây.

## Điểm cần thảo luận

- MoveToCrop và MoveToMarket nên chung movement state hay riêng vì cleanup khác nhau?
- Ai ra quyết định chuyển state: state hiện tại hay controller?
- Pause dùng deltaTime dừng hay thêm Paused state?

## Điều kiện cần giữ

Không chuyển state lặp gây harvest/sale hai lần. Đường cụt có recovery; không bỏ lô hàng khi thoát state vận chuyển.

## Liên quan

Xem [chủ đề liên quan](03-actor-model-factory.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.

