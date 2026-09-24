# Nhân viên, phân việc và snapshot thu hoạch

[← Mục lục](README.md)

**Trạng thái:** đã nối `HarvestService` với actor và scene. Mỗi `ResourceConfig` là một resource data riêng theo plot; tất cả dùng chung visual Tomato và Construction.

## Bài toán

Giá bán phải tính tại thời điểm thu hoạch. Đây là boundary quan trọng nhất giữa Farming, actor và Market.

## Thiết kế đang đề xuất

Khi resource đã mở/xây xong thành cây có thể thu hoạch và có chỗ làm, hệ thống đăng ký một nhân viên vào resource/slot trước khi spawn/assign. Trước thời điểm đó, khách có thể đã chờ ở bàn nhưng không có worker gắn với ô resource còn đóng. Resource không còn chỗ làm thì nhân viên dư được trả pool. Worker giữ assignment qua nhiều lượt nếu resource còn hợp lệ. `HarvestService` quản lý nhiều plot theo `PlotId`; mỗi lượt tạo `HarvestBatch` bất biến gồm `BatchId`, `PlotId`, `Quantity = 3` và `SaleValue`. Ba Tomato là số hàng thật trong lô, đồng thời có visual tương ứng trên cây/actor. `SaleValue` là giá trị **cả lô**, đã áp dụng modifier và làm tròn xuống một lần khi chốt thu hoạch; cashout không cộng tiền từng quả.

```text
Register resource/slot → Move → Harvest complete → Snapshot batch
→ Idle with cargo tới khi khách đã đứng tại table slot và được ghép với worker
→ Move tới điểm đứng đối diện khách → Cashout/Sale tại điểm đó
→ Return to origin → lặp lại nếu resource còn hợp lệ
```
Giá chốt khi hoàn tất harvest đã được thống nhất. Reservation chỗ làm gắn với worker cho tới khi resource đóng hoặc worker bị thu hồi; reservation cho **một lần harvest** được release/cập nhật cooldown sau harvest. Worker/session sở hữu cargo cho tới sale; không thu hoạch lô thứ hai khi còn giữ lô thứ nhất.

## Luồng triển khai phần 08

`ActorSceneInstaller` phát `HarvestRequested(workerId, plotId)` khi worker đến anchor `Delivery`. Một `HarvestService` cho toàn scene quản lý timer và tồn kho riêng từng plot. Sau một `HarvestSeconds`, nó lấy **cả ba quả trong một lượt**, tạo `HarvestBatch` gồm `BatchId`, `PlotId`, `ResourceId`, `Quantity = 3`, `SaleValue: Money`; ba visual bay từ cây lên ba socket trên đầu worker. `BatchSaleValue` trong `ResourceConfig` là giá **cả lô**, không nhân ba. Chỗ gọi `MoneyMath.FloorAfterRatios` là một lần khi chốt lô; hiện modifier của task 10 chưa được nối nên danh sách modifier rỗng. Sau khi batch đã có owner là worker, actor vào `IdleWithCargo`. Cây tự hồi đủ ba Tomato sau `RegrowSeconds` và có thể chuẩn bị lô sau trong khi worker giao lô hiện tại.

`HarvestService` giữ `workerId → HarvestBatch`; worker MonoBehaviour chỉ giữ trạng thái có hàng, không tự tính tiền. Cùng một worker không tạo batch thứ hai khi còn sở hữu lô trước. Sau sale, batch được xóa và worker chỉ bắt đầu harvest tiếp sau khi về origin. `ActorCarryView` dùng trên cả Worker và Customer, gắn tối đa 3 prefab Tomato vào socket đã bind và reset khi actor về pool. Vị trí `Construction/Delivery` là điểm đứng thu hoạch; hai hàng resource và hành lang quanh khối giữa ở [bố cục scene](14-scene-layout-portrait.md). Giai đoạn này chưa có thao tác đóng resource sau khi đã mở; `CancelWorker` đã có ở service nhưng chưa nối flow đóng ô.

Thứ tự công việc: (1) batch/owner và request idempotency, (2) timer/giá chốt, (3) nối `HarvestRequested` với `CompleteHarvest`, (4) visual mang hàng, (5) kiểm tra upgrade trong lúc harvest/đang mang hàng, resource đóng và hai worker tranh một slot.

## Phương án và trade-off

Worker tự chọn job đơn giản khi ít nhân viên; dispatcher tập trung dễ bảo đảm phân phối công bằng khi nhiều người. Reservation theo cây đơn giản nhưng chặn thu hoạch song song; theo slot linh hoạt hơn nếu prefab/demo hỗ trợ.

## Điểm cần thảo luận

- Chọn việc theo cây gần nhất hay ưu tiên lợi nhuận/thời gian?
- Giai đoạn đầu worker chỉ giữ một lô gồm 3 Tomato; nếu thêm loại hàng/sức chứa khác, `Quantity` và visual phải đi theo batch, còn cashout vẫn ghi có một `SaleValue`.
- Nếu resource đóng khi worker đang giữ cargo, worker hoàn tất giao dịch rồi trở về origin trước khi vào pool. Cần chốt cách xử lý riêng khi không còn khách/path để giao batch.

## Điều kiện cần giữ

Upgrade giữa lúc mang hàng không sửa SaleValue. Hai worker không chiếm cùng slot độc quyền. Callback harvest lặp chỉ tạo một batch; cancel release đúng token. Không trả worker có cargo về pool khi chưa giải quyết ownership.

## Liên quan

Xem [chủ đề liên quan](09-market-transactions.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.
