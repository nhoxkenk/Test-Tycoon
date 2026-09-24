# State Pattern cho nhân viên và khách

[← Mục lục](README.md)

**Trạng thái:** FSM, các state và guard `Func` đã được triển khai và kiểm tra bằng EditMode/PlayMode. `HarvestRequested`/`SaleRequested` đang chờ service nghiệp vụ ở phần 08–09 xử lý.

## Bài toán

Requirement yêu cầu trạng thái và hành vi nhân vật. Cần phân biệt cơ chế FSM với quyết định nghiệp vụ.

## Thiết kế đang đề xuất

Actors sở hữu cơ chế FSM nhỏ với Enter/Tick/Exit. `WorkerActor` và `CustomerActor` MonoBehaviour tạo/ghép các state C# của chính mình trong `Initialize`; logic thu hoạch, giao hàng và mua hàng vẫn gọi đúng service nghiệp vụ thay vì tự sửa tiền/cây trong MonoBehaviour.

```text
Worker: Spawn/RegisterResource → MoveToResource → Harvest
        → IdleWithCargo (chờ khách tới đúng table slot)
        → MoveToCashout → Cashout → ReturnToOrigin → MoveToResource
        → ...
        NoFreeResource / ResourceClosed (khi không giữ hàng) → ReturnToPool

Customer: ReserveTableSlot → Spawn → MoveToTable → WaitForWorker
          → ConfirmPurchase → MoveToExit → ReturnToPool
```

Ở trạng thái ban đầu, khách đầu tiên được spawn và đứng chờ tại table trong khi mọi resource vẫn là ô đóng; không có `WorkerActor` nào cho các ô đó. Chỉ khi resource đã mở/xây xong thành cây có thể thu hoạch thì mới đăng ký chỗ làm và spawn nhân viên tương ứng. Nhân viên chỉ thu hoạch lại khi lô trước đã được giải quyết. Sau harvest, `IdleWithCargo` giữ hàng và assignment với resource cho tới khi một khách **đã đến và đang đứng tại table slot của mình** được ghép với lô hàng. Khách còn trên đường không kích hoạt `MoveToCashout`. Nếu khách đến trước khi có hàng, khách chờ tại slot; nhân viên chỉ đi khi đã có hàng và đã ghép với khách đang đứng chờ. Cashout xảy ra khi nhân viên mang hàng đến đúng điểm đứng đối diện khách tại slot đã ghép: khách nhận hàng và trả tiền. Sau cashout nhân viên về **điểm xuất phát của lượt spawn** rồi bắt đầu loop cho resource đã đăng ký nếu resource còn hợp lệ. Nếu không còn resource trống sau khi kết thúc lượt, trả pool. Khách chỉ xuất hiện khi đã reserve được table slot.

### Transition condition bằng Func

Mỗi transition gồm state nguồn, state đích, guard kiểu `Func<WorkerContext, bool>` hoặc `Func<CustomerContext, bool>` và thứ tự ưu tiên. FSM kiểm tra guard sau khi state hiện tại cập nhật; tối đa một transition trong một tick, rồi gọi Exit/Enter đúng thứ tự. Guard **chỉ đọc dữ liệu**: đã tới resource, harvest hoàn tất, đã có cặp worker–khách đang đứng tại table slot, nhân viên đã tới điểm đứng đối diện khách, cashout thành công, đã tới checkout, resource bị đóng hoặc không còn chỗ làm. `ActorCoordinator` hiện ghép và giữ cặp worker–khách khi cả hai sẵn sàng; guard chỉ đọc kết quả ghép. State `Harvest` phát `HarvestRequested`, state `Cashout` phát `SaleRequested`; service nghiệp vụ sau này tạo batch, cộng tiền và gọi `CompleteHarvest`/`ConfirmSale`, không thực hiện các lệnh đó trong guard.

Ví dụ các cạnh: `Harvest → IdleWithCargo` khi service xác nhận đã có hàng; `MoveToTable → WaitForWorker` khi khách đã tới slot; `IdleWithCargo → MoveToCashout` khi có cặp ghép với khách đang đứng chờ; `MoveToCashout → Cashout` khi nhân viên đến đúng điểm đối diện và khách vẫn ở slot; `Cashout → ReturnToOrigin` và `WaitForWorker → MoveToExit` khi giao dịch được xác nhận. Lỗi path/target mất có cạnh recovery riêng. Khi trả pool, xóa guard/delegate đang capture context của lượt trước.

Timer và transaction thuộc logic; animation chỉ phản ánh state. State exit release tài nguyên tạm, còn cargo thuộc worker/session cho đến khi sale hoàn tất hoặc được bàn giao owner khác.

## Phương án và trade-off

Class theo state rõ khi mỗi state có timer, cancellation và dependency. `Func` cho guard nhỏ giúp khai báo transition gọn, nhưng quá nhiều lambda capture state mutable sẽ khó debug; tên guard và log cạnh đã chuyển cần rõ. Không cần generic hierarchical FSM hoặc behavior tree ngay. Cây có thể dùng enum + transition methods.

## Điểm cần thảo luận

- Cashout hoàn tất khi nhân viên đến điểm đứng đối diện khách tại table và giao dịch thành công; checkout là đường rời đi của khách.
- Nếu không có khách, nhân viên đứng chờ tại resource hay ở một điểm chờ gần quầy? Đề xuất hiện tại: IdleWithCargo tại resource/điểm chờ của resource.
- Pause dùng deltaTime dừng hay thêm Paused state?

## Điều kiện cần giữ

Không chuyển state lặp gây harvest/sale hai lần. Đường cụt có recovery; không bỏ lô hàng khi thoát state vận chuyển.

## Liên quan

Xem [chủ đề liên quan](03-actor-model-factory.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.
