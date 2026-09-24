# Chốt phạm vi triển khai cuối: phần 10–14

[← Mục lục](README.md)

**Trạng thái:** đã triển khai flow nâng cấp level từng plot, bảy buff trong mục Upgrade chung, save/load và presentation cơ bản theo quyết định mới nhất. Các mục bên dưới giữ thiết kế và tiêu chí kiểm chứng; kết quả chạy thực tế nằm ở cuối tài liệu.

## Điểm xuất phát trước khi triển khai phần 10–14

- Vòng 01–09 đã có scene dọc 1080 × 1920, bốn plot, worker/customer pool, build/harvest/sale, ví Coin/Gem bằng `BigInteger`. Giá cả lô được snapshot lúc thu hoạch và làm tròn xuống một lần; ba Tomato là một lô, không phải ba giao dịch.
- `GameBootstrap` hiện chỉ compose DI và lifecycle. Graph asmdef: `Farming → Economy`; `Simulation → Actors, Economy, Farming`; `UnityAdapters → Actors, Simulation, Economy, Farming`; `Bootstrap → Economy, Farming, Simulation, UnityAdapters`. Không thêm reference ngược tới Bootstrap hoặc UnityEngine vào core.
- Có sẵn `ConstructionUpgradeView` (popup có slider/level/nút), `Assets/Views/UpgradeView` (khung danh sách scroll), `UpgradeItemView`, `EffPay`, `EffBuildDone` nhưng chưa nối hành vi. Còn một prefab trùng tên ở `Assets/Prefabs/Views/UpgradeView.prefab` hiện gắn `UnlockPanelView`; lúc code phải phân biệt rõ. `ResourceConfig` đã có `upgradeCost` và `profitPercentPerLevel` mẫu: Wheat 4.000/10%, Wood 6.000/15%, Clay 8.000/20%, Steel 10.000/25%. Chưa có config giá cho bảy buff chung, chưa có save adapter, và checkout hiện tại không có thư mục `Assets/Tests`.
- Build Settings có `Farm.unity`; Git `main` có remote `origin` tới `nhoxkenk/Test-Tycoon`. Người dùng sẽ tự build APK; build/ký/cài APK không nằm trong phạm vi mình triển khai.

## Ràng buộc xuyên suốt

1. Không VContainer. Dependency bắt buộc qua constructor/`Initialize` tường minh; Bootstrap chỉ chọn implementation, cấp config/scene references, load và dọn vòng đời. Click, nâng cấp, harvest, sale không đặt trong Bootstrap.
2. Không tạo class cho từng resource chỉ vì icon/giá khác nhau. Bốn resource là dữ liệu ScriptableObject, visual Construction/Tomato dùng chung. Bảy buff chung dùng ba dạng effect (cộng khách, x2 toàn farm, x2 một plot) đăng ký tường minh theo `UpgradeKind`; **không triển khai reflection factory ở lượt này** trừ khi một yêu cầu mới tạo ra nhiều handler độc lập và lợi ích vượt chi phí IL2CPP/stripping.
3. Chỉ use case mua/xây/bán được nhận `IWalletTransactions`; UI, actor và VFX nhận dữ liệu đọc/sự kiện đã commit. Tiền nguyên, số lớn lưu bằng chuỗi thập phân, `CurrencyId` lưu bằng ID ổn định. Giá lô đã snapshot không đổi khi người chơi mua upgrade lúc worker đang mang hàng.
4. `PlotId` là key cho tiến trình và buff riêng cây; `ResourceId` là key cho dữ liệu loại hàng. Global buff phải tác dụng lên cả plot xây sau. `ProgressionService` tính target khách từ mức khởi đầu và purchase records; `ActorCoordinator` nhận target đó để vận hành, không lưu một bản progression độc lập.

## Phần 10 — nâng cấp và modifier

| Việc cần làm | Ownership dự kiến | Kết quả cần thấy |
|---|---|---|
| Nâng level một cây đã xây | `Simulation/ProgressionService` giữ purchase/level; `Farming` tính profit | Lv1 là giá gốc; mỗi lần mua tăng 1 level và income của plot theo % riêng resource, tối đa **Lv10**. Cây khóa/max level không trừ tiền. |
| X2 lợi nhuận riêng plot 1–4 | Bốn item config có `PlotId` cố định, dùng chung effect ở `Simulation`; modifier theo `PlotId` ở `Farming` | Chỉ batch tạo **sau** khi mua của plot đó tăng giá; plot khác giữ nguyên. Plot chưa xây chưa mua được. |
| X2 lợi nhuận tất cả cây | Effect tường minh ở `Simulation`, global modifier ở `Farming` | Áp dụng cho mọi plot đang mở và plot mở sau; không sửa batch đã snapshot. |
| +1 khách và +2 khách | Hai item config dùng chung effect ở `Simulation/ProgressionService`; `ActorCoordinator` nhận target runtime | Mỗi item mua một lần vĩnh viễn, cộng dồn: target ban đầu 1, mua cả hai lên 4; vẫn chỉ spawn khi có Dock trống. Market hiện có 4 Dock. |

Giá **một lô** là `floor(baseBatchValue × (1 + levelBonus) × localMultiplier × globalMultiplier)` bằng số nguyên/phân số, floor **một lần** khi `HarvestService` tạo `HarvestBatch`. `levelBonus = (level − 1) × profitPercentPerLevel` lấy riêng từ `ResourceConfig` của plot; ví dụ Wheat Lv2 +10%, Lv3 +20%, Lv10 +90%, còn Wood/Clay/Steel theo % riêng của chúng. Local x2 có một item mua tối đa một lần **cho mỗi PlotId 1–4**, global x2 mua một lần cho toàn farm; cùng tồn tại thì nhân thành x4. Không nhân giá từng quả và không sửa batch đã mang. Modifier key theo nguồn + phạm vi plot/global để có thể tái dựng từ purchase records. Purchase validate target, level, cost, giới hạn mua và effect trước khi trừ tiền; record chỉ ghi sau khi áp dụng thành công.

Config có bảng giá từng level tới Lv10 và **bảy item buff chung** gồm key ổn định, `UpgradeKind`, `PlotId` nếu là buff riêng plot, cost `Money`, icon, mô tả và giới hạn mua một lần. Chỉ có ba dạng effect nên không tạo bảy class. Giá là **mẫu chỉnh được** trong ScriptableObject; không bắt buộc trùng số liệu APK. Các `upgradeCost` hiện tại có thể làm giá từ Lv1→Lv2, còn từng bậc tiếp theo phải được author tường minh hoặc có công thức giá riêng trong config; không hardcode ở use case. Tiền đầu game 100.000 Coin vẫn đủ mở bốn box như hiện tại.

**Ngoài phạm vi lượt này theo xác nhận:** hệ buff chỉ số actor nhiều nguồn. Requirement gốc có nhắc đến khả năng này, nên đây là phần chưa được đáp ứng nếu đánh giá yêu cầu đó như một chức năng bắt buộc. Không thêm framework buff actor rỗng chỉ để có tên class.

## Phần 11 — UI, feedback và cấu hình

- Click **plot đã unlock và xây xong** mở popup nâng level per plot (`UpgradeView` theo tên UI mong muốn; hiện có thể tái dùng/chỉnh prefab `ConstructionUpgradeView`). Popup nhận `PlotId`, hiển thị icon resource, `Lv hiện tại / Lv10`, progress theo level (`(level − 1) / 9`), giá level kế tiếp và một button mua. Lv10 hiển thị đầy thanh và khóa nút mua; thiếu tiền cũng khóa mua. Sau giao dịch thành công, refresh level/progress/giá. UI Information trên cây cập nhật giá lô cho **lô tương lai** theo modifier, còn `ProductionText` vẫn là text config như đã chốt.
- Nút dưới `MainView` mở **section Upgrade chung**, dùng khung scroll hiện có `Assets/Views/UpgradeView.prefab` và tạo đúng bảy `UpgradeItemView`: `+1 khách`, `+2 khách`, `x2 income mọi plot`, `x2 income plot 1`, `x2 income plot 2`, `x2 income plot 3`, `x2 income plot 4`. Mỗi item bind icon, giá, mô tả, trạng thái chưa mua/đã mua/plot khóa/thiếu tiền. PlotId của bốn item riêng đã cố định trong config, nên **không có bước chọn plot**. Button chỉ gửi ý định mua tới Progression. Đóng section/popup, chặn click xuyên world và unsubscribe khi dispose.
- HUD dùng formatter chuỗi K/M/B/… cho số lớn nhưng dữ liệu ví/giá không qua `float`/`double`; nơi cần xác nhận giá phải đọc được số tiền chính xác. `EffPay` nghe transaction đã commit; `EffBuildDone` nghe build hoàn tất. Animation/VFX không phải nơi cộng/trừ tiền.
- Kiểm tra Canvas 1080 × 1920, safe area của notch và màn dọc dài hơn 9:16; giữ quầy/bốn plot trong khung. Chỉ thêm adapter UI cần thiết, không tách asmdef Presentation nếu chưa có boundary độc lập.

## Phần 12 — save/load và lifecycle

`ProgressSnapshot` có `schemaVersion`, balances `(currencyId, decimalAmountString)`, plot build/level và purchase records. Theo quyết định đã chốt, worker/customer, path, reservation và batch đang mang sẽ **khởi tạo lại** khi load; lô chưa bán không được cộng tiền hoặc chuyển sang actor mới. Target khách và modifier được dựng lại **một lần** từ records, không vừa lưu target vừa cộng +2 lần nữa. Với build đang dở, phương án triển khai là lưu thời gian còn lại rồi tiếp tục sau load; không áp dụng tiến trình offline.

`IProgressStore` là port thuần ở `Simulation`; JSON/file adapter ở `UnityAdapters`. Bootstrap **load trước khi tạo Wallet/plot/actor và trước tick**, sau đó compose services từ snapshot. Save sau giao dịch đã commit và các đổi trạng thái quan trọng, debounce ghi thường xuyên, flush khi pause/quit. Ghi file tạm rồi thay thế/giữ backup; file hỏng hoặc schema không hỗ trợ phải báo lỗi, không âm thầm ghi đè bằng save mới. Không thêm offline income vì requirement chưa nêu.

## Phần 13 — quyết định reflection

**Không làm reflection factory mặc định.** Resource là dữ liệu, ba dạng effect đang biết rõ lúc compile và đủ dùng registration tường minh. Chỉ mở lại [phương án reflection](13-reflection-factory.md) nếu xuất hiện nhiều implementation hành vi độc lập phải khám phá theo key; lúc đó cần kiểm chứng duplicate/missing key, lifetime, preserve type và APK IL2CPP. Không dùng `Activator.CreateInstance` cho `Money`, `HarvestBatch`, ScriptableObject hay prefab actor.

## Phần 14 — kiểm chứng scene và bàn giao mã nguồn

1. **EditMode tests mới:** tiền vượt `Int64`, parse/save lại chính xác; công thức level 1–10/local/global và làm tròn một lần; mua thiếu tiền/max level/nhấn lặp không trừ tiền; bốn plot x2 độc lập và không mua khi plot khóa; +1/+2 khách cộng dồn thành 4 nhưng không apply lại sau load; global buff áp lên plot mở sau; snapshot/schema/file hỏng.
2. **PlayMode/manual matrix:** 4 box mua được với 100.000 ban đầu; click cây mở popup level với progress đúng và mua đến Lv10; Information đổi đúng; section chung hiển thị bảy item với trạng thái đúng; worker mang batch cũ vẫn bán giá cũ, batch mới dùng giá mới; khách chỉ spawn với Dock trống kể cả khi target = 4; đóng/mở popup không click xuyên; pool không giữ buff/cargo; save/restart khôi phục tiền và progression theo chính sách đã chốt.
3. **Visual/navigation:** 1080 × 1920 và màn dài có safe area; quầy trên, hai hàng plot, chướng ngại giữa; actor không xuyên vật cản/quầy, vẫn đến được bốn Delivery/Dock; build/pay VFX hiện sau commit; Canvas không che nút quan trọng.
4. **Bàn giao mã nguồn:** project mở/compile được trong Unity Editor, test và vòng chơi trong Editor đạt. Người dùng tự build và kiểm tra APK; không lập build pipeline, keystore hoặc phát hành APK trong phạm vi này. Commit/push chỉ làm khi có yêu cầu riêng.

## Quyết định đã chốt và điểm còn mở

| Mã | Quyết định | Trạng thái |
|---|---|---|
| D1 | Max Lv10; mỗi resource có % tăng/cấp riêng; L1 là base. | Đã chốt |
| D2 | Bảy buff chung: +1 khách, +2 khách, x2 toàn farm, x2 riêng plot 1–4; mỗi item mua một lần vĩnh viễn. Hai buff khách cộng dồn từ 1 thành 4. | Đã chốt |
| D3 | Level bonus × local x2 × global x2, floor một lần cho lô. | Đã chốt |
| D4 | Giá mẫu chỉnh được bằng ScriptableObject. | Đã chốt |
| D5 | Lưu tiền + progression, reset actor/batch trên load. | Đã chốt; timer build tiếp tục từ số còn lại là chi tiết triển khai đề xuất |
| D6 | Chưa triển khai buff actor. | Đã chốt |
| D7 | Người dùng tự build APK; loại bỏ build/ký APK khỏi phần mình làm. | Đã chốt |
| D8 | Bốn item x2 riêng mang sẵn `PlotId` 1–4; không có bước chọn plot. Popup nâng level từng plot và section Upgrade chung là hai flow UI khác nhau. | Đã chốt |

## Kết quả triển khai và giới hạn kiểm chứng

- `ProgressionService` và `HarvestService` xử lý level, bảy item buff và snapshot giá một lô. Popup plot dùng prefab `Assets/Views/ConstructionUpgradeView.prefab`; section chung dùng `Assets/Views/UpgradeView.prefab` và bảy `Assets/Prefabs/Views/UpgradeItemView.prefab`. Nút ở `MainView/Bot` mở section; prefab cùng tên tại `Assets/Prefabs/Views/UpgradeView.prefab` không được dùng cho flow này.
- `ProgressSnapshot`/`IProgressStore` thuộc Simulation; `JsonProgressStore` và controller lưu thuộc UnityAdapters. Save dùng chuỗi thập phân cho tiền lớn, giữ backup, tiếp tục build timer từ số còn lại. Save hỏng hoặc schema không hỗ trợ được giữ nguyên và chặn ghi mới; backup chưa tự phục hồi. Actor/batch không được restore.
- HUD có formatter `BigInteger`, MainView có adapter safe area, `EffPay`/`EffBuildDone` nghe event giao dịch/build đã commit. Unity Editor compile không có lỗi/cảnh báo; 7/7 EditMode test đạt. PlayMode đã xác nhận menu bảy item, popup plot, build VFX và save/restart sau khi mua +1 khách: 85.000 Coin, target 2, hai khách spawn. File save do test tạo đã được dọn.
- Chưa kiểm tra notch vật lý, VFX sau một lượt sale/harvest thật, trường hợp khách thứ năm bị chặn vì hết Dock, và toàn bộ ma trận PlayMode ở trên. Người dùng tự build APK. Chưa commit/push các thay đổi.

Các lựa chọn khác như offline income, booster có thời hạn, gem kiếm được, reflection, thêm loại currency hoặc đổi toàn bộ art chỉ mở khi có yêu cầu mới; chúng không nằm ngầm trong phần 10–14.
