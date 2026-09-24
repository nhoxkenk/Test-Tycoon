# Upgrade Strategy và buff nhiều nguồn

[← Mục lục](README.md)

**Trạng thái:** đã triển khai nâng cấp level từng plot và bảy buff trong mục Upgrade chung; xem [kết quả triển khai](15-final-implementation-review.md#kết-quả-triển-khai-và-giới-hạn-kiểm-chứng). Max level plot = 10, bonus/cấp riêng từng resource, các hệ số nhân và floor một lần. Buff chỉ số actor chưa có item ở lượt này.

## Bài toán

Requirement có nâng level cây, buff một cây, buff tất cả cây và thêm khách. Requirement gốc còn nhắc buff chỉ số actor nhiều nguồn, nhưng phần đó đã được người dùng loại khỏi lượt triển khai này.

## Thiết kế đang đề xuất

Nâng level là use case theo `PlotId`, không phải item của danh sách buff chung. Chỉ plot đã xây mới mở UI nâng cấp level; mỗi lần mua tăng đúng một level, tối đa Lv10, với giá của level kế tiếp trong `ResourceConfig`.

`ProgressionService` ở Simulation sở hữu purchase validation/records cho **bảy item buff chung**: `CustomerCount+1`, `CustomerCount+2`, `AllPlotsIncome×2`, `Plot1Income×2`, `Plot2Income×2`, `Plot3Income×2`, `Plot4Income×2`. Đây là bảy cấu hình item nhưng chỉ có ba dạng hành vi: cộng target khách, nhân income mọi plot, nhân income plot đích. Item riêng plot mang `PlotId` cố định; không cần flow chọn cây đích. Module có thể thêm hiệu ứng actor về sau bằng handler mới khi có yêu cầu cụ thể, không dựng sẵn buff actor chưa dùng. Mapping `UpgradeKind → handler` tường minh, không quét assembly/reflection và không tạo class cho từng item.

Upgrade/booster không nằm trong Economy. `UpgradeDefinition` có cost là `Money(currency, amount)`; ProgressionService sở hữu điều kiện mua và record nâng cấp, rồi dùng Wallet để trả tiền. Economy không phụ thuộc Progression. Booster có thời hạn được quản lý ở `Simulation/Boosters`; Farming hoặc Actors chỉ nhận/gỡ modifier theo `SourceId`, không biết người chơi đã mua hay kích hoạt booster thế nào.

Thêm một item **dùng effect có sẵn** chỉ thêm config với key item ổn định, target và giá. Thêm effect mới thì đăng ký handler có contract/key rõ; Bootstrap chỉ đổi nếu graph cần dependency mới, scene reference mới hoặc chọn implementation khác. Không cần một class riêng cho từng item nâng cấp.

Profit modifier của plot dùng key nguồn/phạm vi để không apply trùng sau load. Với level từ 1 đến 10, `levelBonus = (level - 1) × profitPercentPerLevel` của resource đó. Ba nguồn level, x2 riêng plot và x2 toàn farm kết hợp theo công thức đã chốt:
```text
floor(baseBatchValue × (1 + levelBonus) × localMultiplier × globalMultiplier)
```
Farming giữ profit modifiers với số học chính xác. `localMultiplier` và `globalMultiplier` là 1 hoặc 2 vì mỗi item mua một lần; mua cả hai thành x4. Các hệ số kết hợp trước, rồi làm tròn xuống đúng một lần khi chốt `SaleValue` lúc thu hoạch. Batch đã mang giữ snapshot cũ. Actor stat modifier chưa triển khai trong lượt này.

## Phương án và trade-off

Ba dạng effect có thể dùng switch nhỏ nếu không cần mở rộng; Strategy rõ khi mỗi hiệu ứng có logic/validation riêng. Không cần generic buff framework dùng chung khi tiền và movement có quy tắc số học khác nhau.

## Quyết định đã chốt và còn mở

- Bảy item buff chung mua vĩnh viễn, tối đa một lần **cho từng item**. Bốn item plot x2 độc lập theo `PlotId` 1–4; chỉ cho mua khi plot đích đã xây. Item toàn farm có thể mua trước khi mở plot và tác dụng cả plot xây sau. Hai item khách `+1` và `+2` cộng dồn: target ban đầu 1, mua cả hai thành 4; vẫn chỉ spawn khi còn Dock trống.
- Lv1 là base; Lv2, Lv3… Lv10 tăng tuyến tính theo %/cấp của **từng** `ResourceConfig`, không cộng dồn lãi kép từ giá lô trước.
- Giá nâng cấp level và bảy buff là giá mẫu chỉnh được trong ScriptableObject. Buff plot 1–4 tự mang `PlotId` tương ứng, nên không có bước chọn plot trong mục Upgrade chung.

## Điều kiện cần giữ

Global buff áp dụng cả cây xây sau. Remove source không làm mất nguồn khác. Purchase không trừ tiền khi target/effect không hợp lệ; cargo đã snapshot không bị sửa.

## Liên quan

Xem [chủ đề liên quan](06-economy-money.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.
