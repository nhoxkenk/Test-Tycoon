# Khách hàng, hàng đợi và thanh toán

[← Mục lục](README.md)

**Trạng thái:** `MarketSaleService` ở `Farm.Simulation` xử lý giao dịch thuần C#; `HarvestMarketCoordinator` ở UnityAdapters nối actor/scene. Giao dịch lô được xác nhận tại quầy và ví chỉ được ghi có một lần. Nâng cấp sức chứa khách thuộc task 10.

## Bài toán

Ban đầu có một khách; khách chờ tại quầy, nhận một lô hàng rồi trả một khoản tiền. Sau khi tới điểm rời và vào pool, Coordinator bù một khách mới nếu target capacity còn thiếu và có Dock trống.

## Thiết kế đang đề xuất

`ActorCoordinator` hiện sở hữu table slot reservation, ghép worker–customer và bù số khách còn thiếu qua `CustomerFactory`; không tạo thêm `PopulationController` hay bảng reservation thứ hai. `targetCustomerCount` ban đầu là 1, upgrade có thể tăng; Coordinator chỉ spawn khi `activeCustomerCount < targetCustomerCount` và còn table slot trống. Khách đầu tiên được spawn để đứng chờ tại bàn ngay từ đầu màn chơi, trước khi bất kỳ resource nào được mở. Slot được reserve và gắn với khách trước spawn, nên khách vừa xuất hiện đã có vị trí mua hàng cụ thể; không để ProgressionService gọi factory trực tiếp.

Nhân viên sau harvest giữ hàng và Idle cho tới khi khách **đã tới table slot được giữ cho mình**. Khách đang di chuyển chưa được ghép để kéo nhân viên ra quầy. Khi cả nhân viên có hàng và khách đang đứng chờ, `ActorCoordinator` ghép một worker/batch với một customer/slot; nhân viên mới đi tới `WorkerCashoutPoint` đứng đối diện khách. Khi nhân viên đến nơi và khách vẫn ở slot, `MarketSaleService` xác nhận batch ownership và cặp ghép qua Coordinator; consume batch + đánh dấu khách đã mua + credit ví **một lần bằng toàn bộ `SaleValue` đã chốt của lô** trong một đoạn đồng bộ, rồi phát notification. Ba quả là số hàng thật được chuyển từ worker sang khách về mặt nghiệp vụ/visual, nhưng không tạo ba giao dịch tiền. Khách sau mua đi tới checkout/disappear point và trở về pool. Sale commit tại thời điểm nhân viên đến đúng vị trí đối diện khách và giao dịch thành công; checkout là điểm rời đi, không cộng tiền lần hai.

`ActorCoordinator` ghép khách–worker và điều hướng; `MarketSaleService` xác nhận batch và ghi giao dịch, không giữ bảng slot riêng. Phần sale không có hai nguồn sự thật cho cùng một customer/slot.

## Luồng triển khai phần 09

`ActorSceneInstaller.SaleRequested(workerId, customerId)` chỉ phát khi worker đã tới `WorkerCashoutPoint` của slot có khách đang đứng chờ. `MarketSaleService` lấy batch của worker, xác nhận cặp actor và chuyển ba visual Tomato từ đầu worker sang đầu customer; sau đó consume batch và credit **một khoản bằng giá cả lô** qua `IWalletTransactions`. Không còn batch thì callback sale lặp không thể cộng tiền. Khách mang ba quả visual tới `CustomerEnd` rồi về pool; worker về `DeliveryEnd` trước khi lặp. HUD theo dõi số dư đã commit, không gọi `Wallet.Credit`.

Thứ tự công việc: (1) contract xác nhận pair + batch, (2) commit sale idempotent và cập nhật owner batch, (3) nối `SaleRequested`/`ConfirmSale`, (4) hiệu ứng nhận hàng/trả tiền và UI, (5) kiểm tra khách rời slot, worker mất đường, callback sale lặp và nâng capacity khách. Market prefab hiện có 4 Dock; mỗi Dock có điểm đứng khách và child `Delivery` đối diện. Vị trí quầy ở hàng trên cùng theo [bố cục scene](14-scene-layout-portrait.md).

## Phương án và trade-off

FIFO dễ hiểu và công bằng; khi triển khai cần thứ tự chờ tường minh nếu muốn bảo đảm FIFO, không dựa vào thứ tự duyệt dictionary. Ghép theo loại hàng chỉ cần nếu khách có yêu cầu loại quả. Một khách mua trọn lô 3 quả và trả một khoản tiền; số quả thật/visual không làm phát sinh bán từng phần.

## Điểm cần thảo luận

- +2 khách nghĩa là tăng `targetCustomerCount` lâu dài; đã ghi theo flow người dùng. Cần đối chiếu APK nếu hành vi demo khác.
- Market prefab hiện có 4 Dock. Slot được release khi khách rời bàn, còn active count giảm khi khách về pool. Nếu upgrade nâng target vượt 4, cần thêm Dock hoặc giới hạn target/UI theo capacity thật.
- Khách có patience/timeout không?

## Điều kiện cần giữ

Một batch chỉ được credit một lần. Khách chưa tới table không khiến nhân viên đi cashout và không nhận hàng. Một khách/slot chỉ được ghép với một worker tại một thời điểm; nếu khách hoặc slot mất hiệu lực trên đường đi, hủy cặp ghép và đưa worker về trạng thái chờ với cargo còn nguyên. Không có table slot trống thì không spawn khách mới. Target count không tăng lại khi chỉ bù khách đã rời.

## Liên quan

Xem [chủ đề liên quan](10-upgrades-modifiers.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.
