# UI, presentation và cấu hình

[← Mục lục](README.md)

**Trạng thái:** UI nâng level plot, section Upgrade chung, HUD tiền lớn, safe area và feedback đã được nối; xem [kết quả triển khai](15-final-implementation-review.md#kết-quả-triển-khai-và-giới-hạn-kiểm-chứng). Bootstrap Root + DI thuần được giữ; không thêm reflection.

## Bài toán

Project có sẵn Build/Upgrade/Management/Main views, animation và VFX. Cần phân định model, input và trình diễn.

## Thiết kế đang đề xuất

Đề xuất MVP nhỏ: View gửi ý định; Presenter gọi use case và bind dữ liệu; service quyết định. C# event theo service phục vụ HUD/VFX sau commit, không dùng global event bus để chạy nghiệp vụ.

ScriptableObject giữ config authoring ở UnityAdapters; chuyển thành runtime data tại Bootstrap. Không lưu balance/session state trên asset config. World view, navigation binding và các controller UI hiện nằm trong UnityAdapters; chỉ tách asmdef Presentation nếu thật sự cần boundary riêng.

- **Nâng level plot:** click plot đã unlock/xây xong mở popup `UpgradeView` theo nghĩa gameplay, bind đúng `PlotId`. Prefab `Assets/Views/ConstructionUpgradeView.prefab` hiện đã có slider, level, icon và nút Upgrade nên dùng/chỉnh nó cho popup này; nếu đổi tên prefab thành `PlotUpgradeView` lúc code thì giữ tên rõ để khỏi nhầm với danh sách chung. Hiển thị icon, `Lv hiện tại / Lv10`, progress theo level (`(level − 1) / 9`), giá level kế tiếp và button mua. Ở Lv10, progress đầy, button không mua được. Popup chỉ gọi use case, rồi refresh level/progress/giá sau giao dịch thành công.
- **Upgrade chung:** nút dưới `MainView` mở một section/panel danh sách độc lập với popup plot. `Assets/Views/UpgradeView.prefab` hiện là khung scroll, dùng/chỉnh cho section này; mỗi dòng dùng `Assets/Prefabs/Views/UpgradeItemView.prefab`. Danh sách có đúng bảy item: `+1 khách`, `+2 khách`, `x2 mọi plot`, `x2 plot 1`, `x2 plot 2`, `x2 plot 3`, `x2 plot 4`. Item x2 plot mang `PlotId` cố định từ config, nên không cần chọn plot trên map. Item plot chưa xây hiển thị khóa/không mua được; item đã mua hiển thị đã sở hữu. Hai item khách cộng dồn tới target 4.
- `Assets/Prefabs/Views/UpgradeView.prefab` cũng tồn tại nhưng đang gắn `UnlockPanelView`; flow nâng cấp hiện dùng các prefab khác đã nêu, không kéo nhầm asset này. `ProductionText` vẫn chỉ là text config.

## Phương án và trade-off

Presenter giúp test và tách logic nhưng không cần interface cho từng Text/Button. Giữ config runtime bất biến giảm mutation nhầm, đổi lại cần mapping SO. Với prototype nhỏ có thể ít mapping hơn nếu nới boundary engine.

## Điểm cần thảo luận

- Popup plot và section Upgrade chung có presenter/controller riêng theo feature; không đưa xử lý click vào Bootstrap.
- UI nên preview giá lô sau upgrade theo cùng hàm số học với runtime; không tự làm tròn khác service.
- Config được đọc khi tạo session; đổi asset giữa Play Mode không tự đổi dữ liệu đang chạy.
- Canvas tham chiếu 1080 × 1920 đã chốt; adapter safe area đã gắn trên MainView nhưng notch vật lý/màn dọc dài vẫn cần kiểm chứng trực quan.

## Điều kiện cần giữ

UI không tự trừ tiền/tính lại sale. Popup không cho click xuyên xuống cây; unsubscribe khi đóng/dispose. Animation event không là nguồn cấp tiền.

## Liên quan

Xem [chủ đề liên quan](12-persistence-validation.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.
