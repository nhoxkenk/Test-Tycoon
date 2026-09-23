# Cây trồng và xây dựng

[← Mục lục](README.md)

**Trạng thái:** đề xuất để phân tích và thảo luận; chưa phải quyết định triển khai. DI thuần và Bootstrap Root là ràng buộc đã chốt. Các lựa chọn khác bên dưới còn có thể sửa.

## Bài toán

Click hộp → Build → cây xuất hiện; cây có level và profit. Cần tách vị trí xây khỏi trạng thái cây và hình ảnh prefab.

## Thiết kế đang đề xuất

Đề xuất PlotId ổn định cho vị trí; Crop giữ level/base config và trạng thái sản xuất. ConstructionService ở Simulation kiểm tra cost/Wallet rồi chuyển model. PlotView ở UnityAdapters thể hiện Box/Construction.

```text
Box → Building → Ready → Reserved/Harvesting → Cooldown → Ready
```
Dùng enum và transition methods trước. Cây chưa cần một class cho mỗi state.

## Phương án và trade-off

Plot và Crop tách riêng dễ hỗ trợ thay loại cây nhưng có thể dư nếu mỗi ô chỉ có đúng một cây cố định. Một model Construction đơn giản hơn cho demo; lựa chọn dựa trên nhu cầu thay cây thực tế.

## Điểm cần thảo luận

- Cây có thời gian lớn/chín hay chỉ timer thu hoạch?
- Một cây có nhiều điểm cho nhiều nhân viên cùng làm không?
- Build có thời gian thật hay chỉ animation?
- Có cần Plot và Crop riêng ngay từ đầu?

## Điều kiện cần giữ

Build lặp không trừ tiền hai lần. Animation kết thúc không tự tạo cây lần hai. ID/anchor ổn định để navigation và save dùng chung.

## Liên quan

Xem [chủ đề liên quan](08-worker-harvest-logistics.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.

