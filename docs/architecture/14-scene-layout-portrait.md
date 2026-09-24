# Bố cục scene và camera dọc cho vòng chơi 07–09

[← Mục lục](README.md)

**Trạng thái:** đã dựng scene dọc 1080 × 1920, gồm HUD, bốn ô resource, UI xây dựng, quầy, A* GridGraph và actor. Chuỗi ảnh tham chiếu do người dùng gửi là nguồn cho bố cục và các mốc visual. Hai khối dài ở giữa là vật cản/trang trí.

## Trạng thái scene hiện có

`Farm.unity` có `GameBootstrap`, `Market` với 4 Dock, `NavigationGround`, `AstarPath` với GridGraph, một `RVOSimulator` và `RVONavmesh`, camera, đèn, bốn `PlotSlot`, hai khối giữa và `MainView` HUD với Canvas tỷ lệ 1080 × 1920. Camera Orthographic ở `(0, 18, -14.7)`, nhìn về `(0, 0, -4.7)`, size `11`. Player orientation là Portrait. `Market/CustomerStart` ở `(9, 0, 3.5)`, `CustomerEnd` ở `(-9, 0, 3.5)`, `DeliveryEnd` ở `(-9, 0, 0.5)`; các điểm spawn/exit này nằm ngoài khung hình nhưng trong vùng graph. Bốn Dock khách ở `z=3`, bốn điểm worker đối diện ở `z=0.89`.

## Bố cục world đề xuất

```text
                  +Z / phía trên ảnh
       CustomerStart → [Market + 4 Dock] → CustomerEnd
                         [quầy]
               worker đứng phía trước quầy

         [Plot trên trái] [khối giữa] [Plot trên phải]

         [Plot dưới trái] [khối giữa] [Plot dưới phải]
                  -Z / phía dưới ảnh
```

| Nhóm | Vị trí bắt đầu để tuning | Nội dung |
|---|---|---|
| `Market` | Giữ `(0, 0, 0)` | Quầy ở phần trên khung hình; 4 Dock hiện có là điểm khách/worker đối diện. |
| Hàng plot gần quầy | x `-3.4/+3.4`, z `-3.5` | Hai hộp đóng lúc vào màn, cây xuất hiện sau build. |
| Hàng plot phía dưới | x `-3.4/+3.4`, z `-8.5` | Hai hộp đóng còn lại. |
| Hai khối giữa | x `0`, z `-3.5/-8.5` | `Cube.prefab` dài `3.2`, có BoxCollider trên layer `NavigationObstacle`; GridGraph giữ đường vòng quanh chúng. |
| `NavigationGround` | scale `(3.5, 1, 4)` | `GroundMat` xanh; GridGraph của A* phủ cả spawn/exit ngoài màn hình. |

Các số tọa độ chỉ là mốc dựng scene, không phải config gameplay. Prefab `Construction` đã có `Box`, `Plant`, `Delivery` và `BuildFocus`; mỗi PlotSlot phải có một `PlotId` ổn định và harvest anchor trên vùng walkable. Graph chỉ kiểm tra collider trên layer `NavigationObstacle` (quầy và hai khối giữa); Box/Plant thay đổi visual nên không cần scan lại. `RVONavmesh` tạo biên tránh vật cản cho RVO.

Scene có một GridGraph ô `0.25` world unit và một `RVOSimulator` dùng chế độ XZ. Prefab worker/customer dùng `AIPath`, `Seeker`, `FunnelModifier` và `RVOController`. Đã xác minh actor di chuyển trong Play Mode và hai actor thử nghiệm đi ngược chiều trên cùng hành lang đều tới đích. Đường từ worker origin tới cả bốn harvest point, từ customer spawn tới bốn Dock và từ Dock tới exit đều được tính qua graph.

## Camera và UI dự kiến

- Main Camera nhìn chếch từ trên xuống, hướng về `+Z` để Market nằm phía trên ảnh và hai hàng plot nằm phía dưới. Hiện đặt position `(0, 18, -14.7)`, nhìn vào `(0, 0, -4.7)`, **Orthographic**, `orthographicSize` `11`. Ở tỉ lệ 9:16, kích thước này cho bề ngang nhìn thấy khoảng `12.4` world units; cần kiểm tra và tuning trên Game View portrait thực tế.
- Thiết lập Game View/Player orientation dọc 9:16, reference resolution Canvas `1080 × 1920`. `MainView.prefab` nằm trong Canvas riêng: thanh Coin/Gem bám mép trên, nút Upgrade bám mép dưới, popup Build/Upgrade xuất hiện gần plot nhưng không che đường nhân vật. Adapter safe area đã gắn ở phần 11; **chưa kiểm tra trên notch vật lý**. Thế giới và UI không dùng cùng tọa độ.
- Camera phải thấy trọn Market, hai hàng hộp/cây và khoảng trống phía dưới như ảnh. Điểm spawn/exit nằm ngoài màn hình nhưng trong GridGraph. Nếu đổi tỉ lệ thiết bị, ưu tiên giữ chiều ngang đủ thấy quầy/plot rồi kiểm tra lại mép trên/dưới; không kéo giãn world bằng transform.

## Mốc hình ảnh để kiểm tra

1. Vào scene: một khách tới Dock ở quầy trên, bốn hộp còn đóng, hai khối giữa giữ nguyên; chưa có worker.
2. Chọn hộp: popup Unlock/Build đúng vị trí; chưa spawn worker khi popup hoặc timer đang chạy.
3. Xác nhận build: hộp mở, có progress timer; sau khi hoàn tất mới hiện cây cà chua, nhãn tiền/tốc độ và worker của plot đó.
4. Worker đi tới `Delivery` anchor, thu hoạch, mang visual quả; khách vẫn đứng chờ ở Dock.
5. Worker tới điểm đối diện khách, nhận/trả một khoản `SaleValue` cho cả lô; khách rời quầy, worker về origin, UI Coin đổi sau commit.
6. Mở thêm plot: nhiều cây/worker có thể cùng chạy, không đè nhau, không xuyên khối giữa; camera vẫn bao trọn bố cục.

## Trình tự dựng và kiểm chứng scene

1. Chốt số PlotSlot và vai trò hai khối giữa; đặt prefab/anchor với ID ổn định, giữ `Market` và các Dock hiện có.
2. Chỉnh camera, ground, ánh sáng và Canvas theo 9:16; so sánh ảnh lúc tất cả plot còn đóng trước khi nối gameplay.
3. Scan GridGraph với collider layer `NavigationObstacle`; kiểm tra đường từ worker origin tới cả bốn `Delivery` anchor và tới từng Dock, từ customer spawn tới Dock và exit. Chạy Play Mode để quan sát hai actor đi ngược chiều ở lối hẹp.
4. Nối phần 07, rồi 08, rồi 09; kiểm tra từng mốc hình ảnh ở trên trên Game View `1080 × 1920` và một màn dọc có safe area.

## Câu hỏi còn mở

- Hai khối dài ở giữa đã được xác nhận là vật cản/trang trí; khi dựng cần kiểm tra collider/graph đúng footprint visual.
- Screenshot dùng projection Orthographic chếch góc hay Perspective FOV hẹp? Kế hoạch dùng Orthographic trước vì dễ giữ framing cố định; đối chiếu Game View rồi mới chốt.
- Cần giữ cùng bố cục trên các màn 9:16 khác kích thước hay hỗ trợ cả màn dọc dài hơn (19.5:9)?
