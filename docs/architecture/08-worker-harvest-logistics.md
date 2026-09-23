# Nhân viên, phân việc và snapshot thu hoạch

[← Mục lục](README.md)

**Trạng thái:** đề xuất để phân tích và thảo luận; chưa phải quyết định triển khai. DI thuần và Bootstrap Root là ràng buộc đã chốt. Các lựa chọn khác bên dưới còn có thể sửa.

## Bài toán

Giá bán phải tính tại thời điểm thu hoạch. Đây là boundary quan trọng nhất giữa Farming, actor và Market.

## Thiết kế đang đề xuất

HarvestJobSelector chọn cây khả dụng, reserve slot bằng token rồi giao worker. Sau khi tới đích và timer hoàn tất, CropService tạo HarvestBatch bất biến: BatchId, PlotId, quantity, SaleValue.

```text
Reserve → Move → Harvest complete → Snapshot batch
→ Carry → Wait at market → Sale
```
Giá chốt khi hoàn tất harvest là đề xuất cách hiểu requirement. CropService release reservation và cập nhật cooldown; worker/session sở hữu cargo cho tới sale.

## Phương án và trade-off

Worker tự chọn job đơn giản khi ít nhân viên; dispatcher tập trung dễ bảo đảm phân phối công bằng khi nhiều người. Reservation theo cây đơn giản nhưng chặn thu hoạch song song; theo slot linh hoạt hơn nếu prefab/demo hỗ trợ.

## Điểm cần thảo luận

- Chọn việc theo cây gần nhất hay ưu tiên lợi nhuận/thời gian?
- Giá chốt khi bắt đầu hay hoàn tất động tác harvest?
- Capacity là số quả, số lô hay trọng lượng?
- Nếu quầy bị chặn, hàng được giữ trên worker hay chuyển kho chờ?

## Điều kiện cần giữ

Upgrade giữa lúc mang hàng không sửa SaleValue. Hai worker không chiếm cùng slot độc quyền. Callback harvest lặp chỉ tạo một batch; cancel release đúng token.

## Liên quan

Xem [chủ đề liên quan](09-market-transactions.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.

