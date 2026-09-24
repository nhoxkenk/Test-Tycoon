# Actor model và Factory

[← Mục lục](README.md)

**Trạng thái:** actor MonoBehaviour, pool, điều phối customer/worker và binding prefab đã được triển khai. Thu hoạch thực tế, tiền và buff thuộc các phần 08–10 chưa được nối.

## Bài toán

Nhân viên và khách đều có prefab, vị trí, chuyển động, animation và lifecycle, nhưng nghiệp vụ khác nhau.

## Thiết kế đang đề xuất

Mỗi prefab nhân viên/khách có một script gameplay `WorkerActor : MonoBehaviour` hoặc `CustomerActor : MonoBehaviour` gắn trên chính actor. Script này thuộc `Farm.UnityAdapters` để giữ UnityEngine ra khỏi `Farm.Simulation`. Nó nhận dependency và dữ liệu của lần spawn qua `Initialize`, tự tạo các state C# thường (logic state thuộc `Farm.Simulation`) và nối transition cho FSM của **instance đó**. Navigation, animation và trạng thái đang mang hàng được ghép bằng composition; không có `BaseActor` do dự án định nghĩa. Stat/buff nhiều nguồn sẽ được nối ở phần 10. `NavMeshAgent`/`Animator` vẫn là component Unity trên prefab, không phải các class gameplay kế thừa nhau.

`WorkerFactory` và `CustomerFactory` sở hữu pool prefab riêng. Factory lấy actor từ pool, cấp spawn point, ID và dependency rồi gọi `Initialize`; actor mới bắt đầu state sau khi đã đủ dữ liệu. Khi actor hoàn tất vòng đời, factory gọi cleanup/reset và trả vào pool. `ActorCoordinator` giữ resource assignment, table slot, số khách active và ghép worker đang có hàng với khách đã tới bàn. `ActorSceneInstaller` bind các reference Unity ở Bootstrap Root. Reflection factory ở [phần 13](13-reflection-factory.md) dành cho service/handler C#; nó không thay thế factory lấy/trả prefab.

### Khi nào tạo và trả nhân viên?

- Ban đầu các ô resource còn đóng, nên chưa có nhân viên gắn với chúng. Khi một ô được mở/xây xong thành cây có thể thu hoạch và có chỗ làm → đăng ký **một** nhân viên vào chỗ đó, rồi spawn/assign nhân viên. Chỉ hiện popup mở khóa hoặc đang chạy timer xây dựng chưa phải lúc spawn nhân viên. Resource và chỗ làm phải có ID để tránh hai nhân viên nhận cùng vị trí.
- Nhân viên giữ assignment với resource trong vòng thu hoạch → giữ hàng và chờ khách **đã tới vị trí mua hàng** → đi tới điểm đứng đối diện khách để cashout → trở lại điểm xuất phát → lặp lại, miễn resource vẫn còn chỗ làm hợp lệ. Khách đang đi tới table chưa làm nhân viên rời chỗ chờ.
- Không có resource/chỗ làm trống → nhân viên không được giữ active chỉ để chờ việc; nếu factory đã lấy ra thì trả về pool.
- Nhân viên đang giữ hàng hoặc giao dịch chưa xong không được trả pool ngay; trước hết phải giải quyết ownership của hàng/giao dịch.

### Khi nào tạo và trả khách?

`targetCustomerCount` ban đầu là **1**, có thể tăng bởi upgrade. Khi bắt đầu màn chơi, hệ thống spawn khách đầu tiên vào chỗ trống ở bàn phía trên màn hình để đứng chờ dù chưa mở resource nào; việc tạo khách không phụ thuộc vào việc đã có cây hay nhân viên. Chỉ spawn khi `activeCustomerCount < targetCustomerCount` **và** còn slot tại table. Slot được reserve và gắn với khách **trước khi spawn**, nên mỗi khách vừa xuất hiện đã có sẵn chỗ mua hàng; hai lệnh spawn không lấy trùng một chỗ. Khách đi tới đúng slot đó và chờ; sau khi nhân viên mang resource đến điểm đứng đối diện, khách nhận hàng và trả tiền tại bàn, rồi đi tới checkout/disappear point để trả pool. Giới hạn số khách là giới hạn actor đang active, không phải tổng số lượt khách từng spawn.

Tái sử dụng prefab Delivery và Customer hiện có. Khi lấy lại từ pool, actor có ID/lượt spawn mới và state mới hoặc state đã reset hoàn toàn; không giữ subscription, path, reservation, hàng hay callback của lượt trước.

## Phương án và trade-off

Composition tránh cây kế thừa Character → NPC → Worker/Customer. Hai factory tường minh dễ đọc hơn generic ActorFactory nhiều tham số; pooling có thể dùng `UnityEngine.Pool.ObjectPool<T>` với callback lấy/trả và giới hạn capacity. [Unity ObjectPool API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Pool.ObjectPool_1.html). Pool là cơ chế tái sử dụng prefab, không phải nơi giữ business state lâu dài.

## Điểm cần thảo luận

- Hiện tại nếu resource đóng lúc worker có hàng, worker tiếp tục chờ/giao hàng rồi trở về origin mới trả pool. Quy tắc chuyển ownership của batch nếu không thể bán thuộc phần 08–09.
- `targetCustomerCount` hiện tính toàn bộ khách active, kể cả khách đang đi tới checkout, cho tới khi trả pool.

## Điều kiện cần giữ

Mỗi lần spawn có state/transition hợp lệ riêng; return-to-pool gỡ tick/subscription, hủy reservation/path và deactivate view. Owner hàng phải được giải quyết trước khi trả nhân viên. Không gọi `Destroy` ở luồng despawn thông thường; chỉ hủy prefab dư khi pool/scene kết thúc.

## Liên quan

Xem [chủ đề liên quan](04-actor-state-machines.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.
