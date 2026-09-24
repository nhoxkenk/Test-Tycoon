# Upgrade Strategy và buff nhiều nguồn

[← Mục lục](README.md)

**Trạng thái:** đề xuất để phân tích và thảo luận; chưa phải quyết định triển khai. DI thuần và Bootstrap Root là ràng buộc đã chốt. Các lựa chọn khác bên dưới còn có thể sửa.

## Bài toán

Requirement có nâng level cây, buff một cây, buff tất cả cây và thêm khách; nhân vật cần hỗ trợ buff chỉ số nhiều nguồn.

## Thiết kế đang đề xuất

ProgressionService sở hữu purchase validation/records. Ba effect strategy: SingleCropProfitEffect, AllCropsProfitEffect, CustomerCapacityEffect. Reflection factory ở composition layer khám phá handler theo EffectKind và kiểm tra key trùng/thiếu lúc startup; xem [phần 13](13-reflection-factory.md).

Upgrade/booster không nằm trong Economy. `UpgradeDefinition` có cost là `Money(currency, amount)`; ProgressionService sở hữu điều kiện mua và record nâng cấp, rồi dùng Wallet để trả tiền. Economy không phụ thuộc Progression. Booster có thời hạn được quản lý ở `Simulation/Boosters`; Farming hoặc Actors chỉ nhận/gỡ modifier theo `SourceId`, không biết người chơi đã mua hay kích hoạt booster thế nào.

Thêm một upgrade **dùng effect có sẵn** chỉ thêm config. Thêm effect mới thì thêm handler có contract/metadata hợp lệ để factory quét; Bootstrap chỉ đổi nếu effect cần dependency mới từ scene hoặc cần chọn giữa nhiều implementation. Không cần một class riêng cho từng item nâng cấp.

Modifier key gồm SourceId/TargetId/StatId. Cùng key cập nhật, khác nguồn kết hợp theo công thức đề xuất:
```text
(base + Σflat) × (1 + Σpercent) × Πmultiplier
```
Actors giữ stat modifiers cho tốc độ/capacity; Farming giữ profit modifiers với số học chính xác. Các hệ số tác động đến giá trị lô được kết hợp trước, rồi làm tròn xuống đúng một lần khi chốt `SaleValue` lúc thu hoạch.

## Phương án và trade-off

Ba effect có thể dùng switch nhỏ nếu không cần mở rộng; Strategy rõ khi mỗi hiệu ứng có logic/validation riêng. Không cần generic buff framework dùng chung khi tiền và movement có quy tắc số học khác nhau.

## Điểm cần thảo luận

- Buff x2 cùng loại mua nhiều lần cộng dồn hay thay thế?
- Level 3 +20% tính từ base hay level trước?
- Buff có duration không, và timer có chạy khi pause/offline không?
- Thay harvest speed giữa lượt có ảnh hưởng lượt đang làm không?

## Điều kiện cần giữ

Global buff áp dụng cả cây xây sau. Remove source không làm mất nguồn khác. Purchase không trừ tiền khi target/effect không hợp lệ; cargo đã snapshot không bị sửa.

## Liên quan

Xem [chủ đề liên quan](06-economy-money.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.
