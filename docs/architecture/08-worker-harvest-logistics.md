# Nhân viên, phân việc và snapshot thu hoạch

[← Mục lục](README.md)

**Trạng thái:** đề xuất để phân tích và thảo luận; chưa phải quyết định triển khai. DI thuần và Bootstrap Root là ràng buộc đã chốt. Các lựa chọn khác bên dưới còn có thể sửa.

## Bài toán

Giá bán phải tính tại thời điểm thu hoạch. Đây là boundary quan trọng nhất giữa Farming, actor và Market.

## Thiết kế đang đề xuất

Khi resource đã mở/xây xong thành cây có thể thu hoạch và có chỗ làm, hệ thống đăng ký một nhân viên vào resource/slot trước khi spawn/assign. Trước thời điểm đó, khách có thể đã chờ ở bàn nhưng không có worker gắn với ô resource còn đóng. Resource không còn chỗ làm thì nhân viên dư được trả pool. Worker giữ assignment qua nhiều lượt nếu resource còn hợp lệ. Sau khi tới đích và timer hoàn tất, CropService tạo HarvestBatch bất biến: BatchId, PlotId, quantity, SaleValue.

```text
Register resource/slot → Move → Harvest complete → Snapshot batch
→ Idle with cargo tới khi khách đã đứng tại table slot và được ghép với worker
→ Move tới điểm đứng đối diện khách → Cashout/Sale tại điểm đó
→ Return to origin → lặp lại nếu resource còn hợp lệ
```
Giá chốt khi hoàn tất harvest là đề xuất cách hiểu requirement. Reservation chỗ làm gắn với worker cho tới khi resource đóng hoặc worker bị thu hồi; reservation cho **một lần harvest** được release/cập nhật cooldown sau harvest. Worker/session sở hữu cargo cho tới sale; không thu hoạch lô thứ hai khi còn giữ lô thứ nhất.

## Phương án và trade-off

Worker tự chọn job đơn giản khi ít nhân viên; dispatcher tập trung dễ bảo đảm phân phối công bằng khi nhiều người. Reservation theo cây đơn giản nhưng chặn thu hoạch song song; theo slot linh hoạt hơn nếu prefab/demo hỗ trợ.

## Điểm cần thảo luận

- Chọn việc theo cây gần nhất hay ưu tiên lợi nhuận/thời gian?
- Giá chốt khi bắt đầu hay hoàn tất động tác harvest?
- Capacity là số quả, số lô hay trọng lượng?
- Nếu resource đóng khi worker đang giữ cargo, worker hoàn tất giao dịch rồi trả pool hay chuyển cargo sang owner khác?

## Điều kiện cần giữ

Upgrade giữa lúc mang hàng không sửa SaleValue. Hai worker không chiếm cùng slot độc quyền. Callback harvest lặp chỉ tạo một batch; cancel release đúng token. Không trả worker có cargo về pool khi chưa giải quyết ownership.

## Liên quan

Xem [chủ đề liên quan](09-market-transactions.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.
