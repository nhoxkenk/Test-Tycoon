# Navigation và world queries

[← Mục lục](README.md)

**Trạng thái:** NavMesh adapter, world query, điểm Market và mặt đất NavMesh trong scene `Farm` đã được triển khai. Điểm resource sẽ do phần xây dựng cung cấp khi đăng ký worker.

## Bài toán

Actor phải tìm đường, tránh/vượt chướng ngại và tới đúng điểm thu hoạch/quầy. AI Navigation đã có trong project.

## Thiết kế đang đề xuất

`ActorCoordinator` và `MarketLayout` ánh xạ resource/table slot sang điểm đến. Simulation truyền `WorldPoint` thuần C# cho `IActorNavigation`, nhận `Idle/Moving/Arrived/Unreachable`; `NavMeshAgentNavigation` ở UnityAdapters chuyển điểm đó thành `Vector3` và điều khiển `NavMeshAgent`. Simulation không tham chiếu UnityEngine.

`NavMeshWorldQuery` hiện thực `IWorldQuery.TryGetPathLength` bằng `NavMesh.CalculatePath`, chỉ trả độ dài khi có path hoàn chỉnh. Nó sẵn cho bước chọn resource/job về sau; không tính mỗi frame. Đích nằm ở interaction anchor, không tại tâm collider.

### Điểm đến và ownership

| Actor | Điểm | Sử dụng |
|---|---|---|
| Nhân viên | `WorkerSpawn`/origin | Factory đặt khi lấy từ pool; lưu origin để quay lại sau cashout |
| Nhân viên | `ResourceHarvestPoint(resourceId, slotId)` | Di chuyển tới chỗ làm đã đăng ký, không tới tâm collider |
| Nhân viên | `WorkerCashoutPoint(tableSlotId)` | Phía trước bàn theo góc nhìn màn chơi, đối diện `TablePoint` của khách; chỉ di chuyển tới sau khi khách đã đứng tại slot và được ghép với worker |
| Khách | `CustomerSpawn` | Factory đặt khi đã reserve được table slot |
| Khách | `TablePoint(tableSlotId)` | Chỗ đứng phía sau bàn ở trên cùng màn chơi, được giữ riêng cho khách trước lúc spawn; tới nơi thì chờ nhân viên, kể cả khi chưa mở resource |
| Khách | `CustomerCheckout` | Điểm rời khỏi bàn sau mua; tới nơi mới trả pool |

Các tên trên là **vai trò trong dữ liệu world**, không bắt buộc tên GameObject. `WorkerCashoutPoint` là điểm đứng của nhân viên ở phía đối diện khách, khác với `CustomerCheckout` là điểm khách rời đi. `MarketLayout` hiện bind `CustomerStart` làm điểm spawn khách, `CustomerEnd` làm điểm rời đi, `DeliveryEnd` làm origin worker; mỗi `Dock` là `TablePoint` và child `Dock/Delivery` là `WorkerCashoutPoint`. Table slot được reserve trước khi spawn và release theo owner khi khách rời bàn; actor đang đi tới checkout vẫn tính active cho tới khi về pool.

Không có đường tới resource/table/cashout/checkout thì adapter báo Unreachable; FSM dùng transition recovery. Nếu chưa tìm được resource/chỗ làm hợp lệ, không spawn nhân viên hoặc trả actor vừa lấy từ pool. Chỉ báo Arrived tại `WorkerCashoutPoint` sau khi đã kiểm tra khách vẫn đứng ở `TablePoint` tương ứng mới cho phép thực hiện cashout; không dùng việc đến gần mục tiêu một cách chung chung để tự động tiêu thụ batch/cộng tiền.

## Phương án và trade-off

`WorldPoint` giữ Simulation thuần mà không cần gateway tra ID cho từng lệnh di chuyển. ID vẫn dùng để giữ ownership resource và table slot; UnityAdapters đổi anchor đã chọn thành `WorldPoint` lúc assign. NavMesh phù hợp layout này; chỉ thêm thuật toán khác nếu demo có yêu cầu không đáp ứng được.

## Điểm cần thảo luận

- “Vượt chướng ngại” là đi vòng hay cần nhảy/link traversal?
- Xây cây có làm thay đổi đường đi không?
- Chọn cây gần nhất theo độ dài đường hay thời gian di chuyển?
- Có cần tách motor port và navigation gateway hay một port đã đủ?

## Điều kiện cần giữ

Chưa có path không được coi là Arrived. Target mất/đường không hoàn chỉnh trả lỗi có xử lý. Không khẳng định đường ngắn nhất tuyệt đối trong mọi hình học.

## Liên quan

Xem [chủ đề liên quan](08-worker-harvest-logistics.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.
