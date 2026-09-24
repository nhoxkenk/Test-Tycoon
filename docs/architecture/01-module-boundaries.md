# Ranh giới module và asmdef

[← Mục lục](README.md)

**Trạng thái:** graph asmdef dưới đây đã triển khai cho phần 01–09. Bootstrap Root và DI thuần là ràng buộc; reflection là tùy chọn theo [phần 13](13-reflection-factory.md).

## Bài toán

Requirement có kinh tế, cây trồng, nhân vật và giao dịch; cần quyết định phần nào được phép biết phần nào trước khi thiết kế class.

## Thiết kế đang đề xuất

Hiện có 6 assembly gameplay: Economy, Farming, Actors, Simulation, UnityAdapters, Bootstrap. UI/view đang nằm trong UnityAdapters; chỉ tách Presentation asmdef khi có ranh giới độc lập thật sự.

Dependency dự kiến:
```text
Farming → Economy
Simulation → Actors, Economy, Farming
UnityAdapters → Actors, Simulation, Economy, Farming (+ Unity UI/TMP)
Bootstrap → Economy, Farming, Simulation, UnityAdapters
```
Economy, Farming, Simulation và Actors không dùng UnityEngine. Actors chứa cơ chế FSM; state nghiệp vụ ở Simulation. `MarketSaleService` ở Simulation vì nó kết hợp batch của Farming với ví của Economy. `PlotConstructionController`, `HarvestMarketCoordinator` và `FarmTickRunner` ở UnityAdapters vì chúng nối scene/MonoBehaviour với service thuần. Bootstrap chỉ tạo graph và quản lý lifecycle. Không có chiều tham chiếu ngược về Bootstrap; [reflection factory](13-reflection-factory.md) chỉ dùng khi có nhu cầu khám phá nhiều implementation theo contract.

### Ai sở hữu tiền, upgrade và booster?

`Farm.Economy` chỉ sở hữu `CurrencyId`, `Money` (loại tiền + số lượng) và `Wallet` (số dư theo loại tiền). Nó không biết cây, nhân viên, upgrade hay booster. Thêm loại tiền như gem là thêm một `CurrencyId` và dữ liệu/config tương ứng; không cần `GemWallet` hay một assembly Economy mới.

Mua upgrade là use case của `Simulation/Progression`: nó biết cost thuộc loại tiền nào, hỏi Wallet có đủ tiền, rồi áp effect. Buff lợi nhuận cây thuộc `Farming`; buff chỉ số nhân vật thuộc `Actors`. Booster có thời hạn/điều kiện kích hoạt nên controller vòng đời của nó nằm ở `Simulation/Boosters`; hiệu ứng cụ thể được gửi tới module sở hữu stat. Nếu Progression/Boosters lớn lên, có thể tách asmdef sau khi dependency graph rõ.

```text
Simulation/Progression ──→ Economy.Wallet + Farming.CropService
Simulation/Boosters   ──→ Farming profit modifiers / Actors stat modifiers
```

Đường dẫn trên chỉ là quan hệ code trong `Farm.Simulation`; không tạo reference ngược từ Economy tới các module gameplay.

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
