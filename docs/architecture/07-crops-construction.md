# Cây trồng và xây dựng

[← Mục lục](README.md)

**Trạng thái:** đã nối vòng xây dựng vào `Farm.unity`. Bốn `PlotSlot` dùng bốn `ResourceConfig` khác nhau; Bootstrap wire DI thuần, `PlotConstructionController` sở hữu tương tác xây dựng.

## Bài toán

Click hộp → `UnlockView` hiện icon và giá mở khóa → xác nhận thanh toán → `Build` đếm thời gian → thay Box bằng Construction → hiện `Information`. Mỗi ô giữ `PlotId` ổn định trong toàn bộ quá trình.

## Thiết kế đang đề xuất

`PlotId` ổn định cho mỗi vị trí. `ResourceConfig` giữ dữ liệu cấu hình, `ConstructionService` ở Farming kiểm tra cost/Wallet và chuyển trạng thái xây dựng, còn `HarvestService` giữ tồn kho. `PlotSlotView` ở UnityAdapters điều phối hai visual: `BoxView` cho hộp chưa mở/animation và `ConstructionView` cho cây sau build. `PlotConstructionController` xử lý tương tác/UI; MonoBehaviour view không sở hữu số tiền hay tồn kho.

```text
Box → Building → Ready → Reserved/Harvesting → Cooldown → Ready
```
Dùng enum và transition methods trước. Cây chưa cần một class cho mỗi state. Theo chuỗi ảnh tham chiếu, màn chơi bắt đầu với các ô resource còn đóng nhưng khách đã đứng chờ tại bàn. Tương tác mở khóa và timer xây dựng dẫn tới cây xuất hiện, ví dụ cây cà chua trong ảnh; **khi resource đã thành cây có thể thu hoạch và còn chỗ làm**, hệ thống mới đăng ký/spawn nhân viên cho resource đó. Popup mở khóa hoặc timer đang chạy không tự tạo nhân viên. Một ô vẫn đóng không có nhân viên thu hoạch.

## Thành phần phần 07

| Thành phần | Trách nhiệm hiện tại |
|---|---|
| `ResourceConfig` | ScriptableObject của từng ô: ID, icon, giá mở khóa, timer xây/thu hoạch/hồi cây, giá **cả lô**, text sản lượng và dữ liệu nâng cấp dành cho task 10. |
| `PlotDefinition` / `PlotSession` | Giá mở, thời gian xây và trạng thái khóa/đang xây/sẵn sàng của một `PlotId`. |
| `ConstructionService` | Kiểm tra điều kiện mở khóa và thanh toán qua `IWalletTransactions` một lần; bắt đầu/kết thúc build; phát thay đổi trạng thái sau commit. |
| `HarvestService` | Quản lý tồn kho/timer riêng từng `PlotId`, hồi 3 Tomato, tạo batch và chốt giá khi thu hoạch hoàn tất theo phần 08. |
| `PlotSlotView : MonoBehaviour` | Giữ ID/anchor và chuyển từ prefab Box sang Construction sau build; chỉ điều phối vòng đời hai visual. |
| `BoxView` | Nhận click và phát animation `BoxOpen`; không giữ giá/cây/tồn kho. |
| `ConstructionView` + `TomatoStockView` | Giữ `Delivery` harvest anchor và đúng 3 socket trên cây; gắn/gỡ prefab Tomato theo số lượng mà `HarvestService` công bố. Không tự quyết định số hàng. |
| `PlotUiView` | Tạo ba UI prefab theo ô: `UnlockView`, `Build`, `Information`. |
| `PlotConstructionController` | Điều phối Box/Unlock/Build/Information và `ConstructionService` cho các plot. |
| `HarvestMarketCoordinator` | Khi xây xong, kích hoạt tồn kho và đăng ký worker bằng `PlotId`/`Delivery` anchor. Farming không tham chiếu `MonoBehaviour` hay `ActorCoordinator`. |

Scene có bốn `PlotSlot` với ID và anchor cố định. Mỗi slot bắt đầu bằng prefab `Box`; nhấn Box hiện prefab `UnlockView` với icon resource và nút giá. Xác nhận hợp lệ trừ tiền đúng một lần, chạy timer với prefab `Build`, đồng thời phát animation hộp. Hết timer và animation, Box đổi thành prefab `Construction` cùng ba Tomato ở ba `Point`; lúc này mới đăng ký worker. Prefab `Information` trên Construction hiển thị icon, giá bán **cả lô** và `ProductionText` lấy từ `ResourceConfig`; production hiện chỉ là text, chưa tính từ logic. Bốn asset `Resource_1`–`Resource_4` chứa dữ liệu riêng (cost, giá lô, timer và tham số upgrade về sau), còn Construction/Tomato là visual dùng chung. Tổng giá mở bốn ô là 70.000 Coin, thấp hơn số dư khởi đầu 100.000. Số và vị trí slot ở [bố cục scene](14-scene-layout-portrait.md).

Thứ tự công việc: (1) định nghĩa config/ID và state thuần C#, (2) dựng slot + visual/anchor trong scene, (3) nối build purchase/timer, (4) chuyển visual và register worker đúng lúc, (5) kiểm tra build lặp, thiếu tiền, scene load và NavMesh quanh cây. Các con số thấy trong ảnh là mốc tham chiếu để tuning, không hardcode thành quy tắc kinh tế trước khi đối chiếu config asset.

## Phương án và trade-off

Plot và Crop tách riêng dễ hỗ trợ thay loại cây nhưng có thể dư nếu mỗi ô chỉ có đúng một cây cố định. Một model Construction đơn giản hơn cho demo; lựa chọn dựa trên nhu cầu thay cây thực tế.

## Giới hạn giai đoạn này

Mỗi ô có một worker và một resource data cố định. Build và harvest đều dùng timer thật; chưa có thời gian lớn/chín riêng. Model xây và tồn kho đã tách trong hai service, chưa cần class `Crop` độc lập. Nâng cấp và lưu trạng thái sau khi thoát game thuộc các phần sau.

## Điều kiện cần giữ

Build lặp không trừ tiền hai lần. Animation kết thúc không tự tạo cây lần hai. ID/anchor ổn định để navigation và save dùng chung.

## Liên quan

Xem [chủ đề liên quan](08-worker-harvest-logistics.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.
