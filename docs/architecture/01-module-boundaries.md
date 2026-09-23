# Ranh giới module và asmdef

[← Mục lục](README.md)

**Trạng thái:** đề xuất để phân tích và thảo luận; chưa phải quyết định triển khai. DI thuần và Bootstrap Root là ràng buộc đã chốt. Các lựa chọn khác bên dưới còn có thể sửa.

## Bài toán

Requirement có kinh tế, cây trồng, nhân vật và giao dịch; cần quyết định phần nào được phép biết phần nào trước khi thiết kế class.

## Thiết kế đang đề xuất

Đề xuất giữ 7 assembly: Economy, Farming, Actors, Simulation, UnityAdapters, Presentation, Bootstrap. Đây là phương án để thảo luận, chưa phải số lượng bắt buộc.

Dependency dự kiến:
```text
Farming → Economy
Simulation → Economy, Farming, Actors
UnityAdapters → Simulation, Actors, Farming, Economy
Presentation → Simulation, Farming, Economy
Bootstrap → tất cả module trên
```
Economy, Farming, Simulation không dùng UnityEngine. Actors chứa cơ chế FSM/stat; state nghiệp vụ ở Simulation. Bootstrap biết implementation để wiring.

## Phương án và trade-off

Chia theo feature giúp ownership rõ nhưng Simulation có thể lớn dần. Tách Logistics/Market thành asmdef riêng chỉ khi cần boundary độc lập; hiện có thể giữ folder trong Simulation. Một assembly gameplay duy nhất đơn giản hơn nhưng không cưỡng chế được ranh giới nội bộ.

## Điểm cần thảo luận

- 7 assembly có quá nhiều cho bài test không?
- Simulation có đang ôm quá nhiều trách nhiệm không?
- Có cần Actors độc lập khi hiện chỉ có nhân viên và khách?

## Điều kiện cần giữ

Không cycle, không Gameplay → Presentation/Bootstrap. Interface ở module sở hữu nhu cầu; không có God Core.

## Liên quan

Xem [chủ đề liên quan](02-bootstrap-di-lifecycle.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.

