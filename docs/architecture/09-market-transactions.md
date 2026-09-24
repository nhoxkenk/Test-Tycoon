# Khách hàng, hàng đợi và thanh toán

[← Mục lục](README.md)

**Trạng thái:** đề xuất để phân tích và thảo luận; chưa phải quyết định triển khai. DI thuần và Bootstrap Root là ràng buộc đã chốt. Các lựa chọn khác bên dưới còn có thể sửa.

## Bài toán

Ban đầu có một khách; khách chờ tại quầy, nhận hàng rồi trả tiền. Requirement chưa xác định rõ chu kỳ thay khách và số hàng mỗi lượt mua.

## Thiết kế đang đề xuất

MarketService sở hữu table slot reservation và ghép khách với hàng. `targetCustomerCount` ban đầu là 1, upgrade có thể tăng; PopulationController dùng CustomerFactory để bù số thiếu **chỉ khi** `activeCustomerCount < targetCustomerCount` và còn table slot trống. Khách đầu tiên được spawn để đứng chờ tại bàn ngay từ đầu màn chơi, trước khi bất kỳ resource nào được mở. Slot được reserve và gắn với khách trước spawn, nên khách vừa xuất hiện đã có vị trí mua hàng cụ thể; không để ProgressionService gọi factory trực tiếp.

Nhân viên sau harvest giữ hàng và Idle cho tới khi khách **đã tới table slot được giữ cho mình**. Khách đang di chuyển chưa được ghép để kéo nhân viên ra quầy. Khi cả nhân viên có hàng và khách đang đứng chờ, MarketService ghép một worker/batch với một customer/slot; nhân viên mới đi tới `WorkerCashoutPoint` đứng đối diện khách. Khi nhân viên đến nơi và khách vẫn ở slot, MarketService validate batch ownership, cặp ghép và vị trí; consume batch + đánh dấu khách đã mua + credit ví trong một đoạn đồng bộ, rồi phát notification. Khách sau mua đi tới checkout/disappear point và trở về pool. Sale commit tại thời điểm nhân viên đến đúng vị trí đối diện khách và giao dịch thành công; checkout là điểm rời đi, không cộng tiền lần hai.

## Phương án và trade-off

FIFO dễ hiểu và công bằng; ghép theo loại hàng chỉ cần nếu khách có yêu cầu loại quả. Bán cả lô đơn giản; bán từng phần cần remaining quantity/value và quy tắc rounding khác. Tránh quyết định chi tiết này chỉ từ suy đoán.

## Điểm cần thảo luận

- +2 khách nghĩa là tăng `targetCustomerCount` lâu dài; đã ghi theo flow người dùng. Cần đối chiếu APK nếu hành vi demo khác.
- Một khách mua cả lô hay số lượng cố định?
- Table có bao nhiêu slot và slot được release khi khách rời bàn hay khi tới checkout? Đề xuất: release khi rời bàn; active count giảm khi về pool.
- Khách có patience/timeout không?

## Điều kiện cần giữ

Một batch chỉ được credit một lần. Khách chưa tới table không khiến nhân viên đi cashout và không nhận hàng. Một khách/slot chỉ được ghép với một worker tại một thời điểm; nếu khách hoặc slot mất hiệu lực trên đường đi, hủy cặp ghép và đưa worker về trạng thái chờ với cargo còn nguyên. Không có table slot trống thì không spawn khách mới. Target count không tăng lại khi chỉ bù khách đã rời.

## Liên quan

Xem [chủ đề liên quan](10-upgrades-modifiers.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.
