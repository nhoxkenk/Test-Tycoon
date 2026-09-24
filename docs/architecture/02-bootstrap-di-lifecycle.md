# Bootstrap Root, DI thuần và lifecycle

[← Mục lục](README.md)

**Trạng thái:** composition root đã triển khai bằng constructor DI tường minh. Không dùng VContainer; reflection factory là phương án tùy chọn ở [phần 13](13-reflection-factory.md).

## Bài toán

Bootstrap nhận reference Unity/scene và dựng object graph bằng constructor. Nó không xử lý click box, thanh toán, thu hoạch hay cashout trong `Update`/event handler.

## Thiết kế đang đề xuất

`GameBootstrap` validate `ResourceConfig`, tạo `Wallet`, `ConstructionService`, `HarvestService`, `MarketSaleService`, rồi inject chúng vào `PlotConstructionController` và `HarvestMarketCoordinator`. `ActorSceneInstaller` nhận scene reference và khởi tạo actor. `FarmTickRunner` chuyển `Time.deltaTime` đến các service và cập nhật tiến độ build; actor giữ FSM/Update riêng.

```text
Validate config → Tạo services bằng constructor → Bind HUD/actors/views
→ Nối plot/harvest/market coordinators → Start FarmTickRunner
```
Teardown dừng runner trước, gỡ subscriptions của coordinators, rồi trả actor về pool. Lỗi lúc startup cũng đi qua cleanup. Không truyền Bootstrap, container hay `Resolve<T>()` vào gameplay.

### Khi nào phải sửa Bootstrap?

Bootstrap là entry point và cấp dependency từ scene. **Thêm một class không đồng nghĩa phải sửa Bootstrap**:

| Thay đổi | Có sửa Bootstrap? | Lý do |
|---|---|---|
| Thêm một loại tiền mới vào `CurrencyId` | Không, nếu chỉ là số dư trong Wallet hiện có | Wallet xử lý số dư theo currency |
| Thêm giá upgrade bằng gem | Thường không | ProgressionService nhận cost data và Wallet đã có |
| Thêm state của worker | Không | WorkerFactory/Controller tạo state của actor |
| Thêm class nội bộ cho thuật toán profit | Không | Được class sở hữu nó tạo/sử dụng |
| Thêm service scene mới | Có thể | Chỉ sửa composition khi đó là dependency của graph đang khởi tạo hoặc cần scene reference mới |
| Thêm giá trị gem khởi đầu trong Inspector | Có ở config/binding | Đây là dữ liệu khởi tạo mới, không phải dependency mới |

Bootstrap thay đổi khi root graph cần dependency mới, scene reference mới hoặc thay lựa chọn implementation. Bốn resource hiện chỉ khác dữ liệu ScriptableObject, nên không cần class riêng và không cần quét assembly. Nếu sau này có nhiều handler hành vi riêng, cân nhắc [reflection factory](13-reflection-factory.md) với key/lifetime rõ và kiểm chứng IL2CPP; không đưa cơ chế đó vào chỉ để tránh vài dòng `new`.

## Phương án và trade-off

Một MonoBehaviour root đủ cho một scene. Tạo graph tường minh giúp nhìn thấy dependency và lỗi startup ngay trong code. AppBootstrap xuyên scene chỉ hữu ích khi thật sự có nhiều scene và service sống lâu hơn scene.

## Điểm cần thảo luận

- Nếu thêm nhiều scene, service nào cần sống xuyên scene?
- Khi thêm hành vi resource/upgrade khác nhau, một registry tĩnh đã đủ hay reflection thực sự giảm chi phí bảo trì?

## Điều kiện cần giữ

Awake/OnEnable không dùng dependency chưa initialize. Cleanup gọi lặp an toàn; startup lỗi giữa chừng dọn được phần đã tạo. Không static Instance/GetService.

## Liên quan

Xem [chủ đề liên quan](01-module-boundaries.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.
