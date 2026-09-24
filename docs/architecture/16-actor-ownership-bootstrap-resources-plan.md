# Plan chỉnh ownership Actor và load asset tại Bootstrap

[← Mục lục](README.md)

**Trạng thái: chỉ lên plan, chưa triển khai.** Rà soát ngày 2026-09-24 trên Unity 6000.3.16f1. Đã đối chiếu README, phần 01–15 và code/asmdef hiện tại. Tài liệu này mô tả thay đổi đề xuất; graph trong phần 01 vẫn là graph đang chạy cho đến khi thực hiện plan.

## 1. Kết quả rà soát

- `Farm.Actors` hiện chỉ chứa `ActorStateMachine.cs`, `IActorNavigation.cs`, đặt `noEngineReferences: true`. Phần 01/03 cố ý đặt MonoBehaviour actor trong UnityAdapters, nên code hiện tại đúng thiết kế cũ nhưng ownership chưa đúng mong muốn mới.
- `WorkerActor`, `CustomerActor`, hai factory/pool, `ActorCoordinator`, `ActorSceneInstaller`, `ActorCarryView`, `MarketLayout` và A* adapter đang ở `Assets/Game/UnityAdapters`. `WorkerFlow.cs`/`CustomerFlow.cs` lại ở Simulation. Feature actor bị chia qua ba assembly.
- Chỉ hai file flow nói trên dùng `Farm.Actors` trong code Simulation hiện tại. Chúng điều khiển state/navigation và phát callback; không cần Economy/Farming/Progression. Có thể đưa về Actors và bỏ cạnh `Simulation → Actors`.
- `GameBootstrap` serialize trực tiếp năm prefab UI/VFX và mảng upgrade. Upgrade đã có fallback `Resources.LoadAll<UpgradeConfig>("Upgrades")`; các prefab còn lại chưa dùng Resources. Prefab worker/customer được serialize ở `ActorSceneInstaller`.
- Bootstrap còn giữ **instance scene**: installer, plots, HUD, Canvas. Không thể thay các instance này bằng load prefab asset mà vẫn mong thao tác trên đúng scene hiện tại.
- Một số trạng thái tài liệu bị cũ: phần 03/04 còn nói harvest/sale chưa nối; phần 06 còn nói chưa có save adapter; phần 08 còn nói modifier rỗng, trái với phần 10/15 và code. Phần 03 nhắc NavMeshAgent trong khi runtime dùng A*. Phần 01 có câu hỏi “7 assembly” dù thực tế là 6. Phần 15 có mục lịch sử trước triển khai cần giữ nhãn lịch sử, không đọc như tình trạng hiện tại.

## 2. Ownership đề xuất

Giữ sáu asmdef, cho `Farm.Actors` trở thành module feature actor có UnityEngine và A*. Các class FSM/context vẫn viết C# thuần bên trong module này; **assembly Actors không còn được mô tả là engine-free**.

```text
Economy       → không phụ thuộc module gameplay khác
Farming       → Economy
Simulation    → Economy, Farming
Actors        → AstarPathfindingProject (+ UnityEngine)
UnityAdapters → Actors, Simulation, Economy, Farming, Unity UI/TMP
Bootstrap     → Actors, UnityAdapters, Simulation, Economy, Farming
```

Economy, Farming, Simulation giữ `noEngineReferences: true`. Actors không reference Simulation, UnityAdapters hoặc Bootstrap. Không tạo Core/Contracts asmdef mới. Phương án giữ Actors thuần và thêm Actors.Unity khả thi nhưng tăng assembly và không đáp ứng trực tiếp ý muốn đưa worker vào Actors; không chọn cho lượt này.

### File cần di chuyển khi thực hiện

Giữ tên file; chuyển cả `.meta` để bảo toàn GUID. Namespace đích là `Farm.Actors`.

| Nguồn | Đích dưới `Assets/Game/Actors/` |
|---|---|
| `UnityAdapters/WorkerActor.cs`, `WorkerFactory.cs` | `Worker/WorkerActor.cs`, `Worker/WorkerFactory.cs` |
| `Simulation/WorkerFlow.cs` | `Worker/WorkerFlow.cs` |
| `UnityAdapters/CustomerActor.cs`, `CustomerFactory.cs` | `Customer/CustomerActor.cs`, `Customer/CustomerFactory.cs` |
| `Simulation/CustomerFlow.cs` | `Customer/CustomerFlow.cs` |
| `UnityAdapters/ActorCoordinator.cs`, `ActorSceneInstaller.cs`, `MarketLayout.cs` | `Scene/ActorCoordinator.cs`, `Scene/ActorSceneInstaller.cs`, `Scene/MarketLayout.cs` |
| `UnityAdapters/ActorCarryView.cs` | `Views/ActorCarryView.cs` |
| `UnityAdapters/AstarActorNavigation.cs`, `AstarWorldQuery.cs` | `Navigation/AstarActorNavigation.cs`, `Navigation/AstarWorldQuery.cs` |

`WorldPointExtensions` đi cùng `AstarWorldQuery.cs`; không bỏ sót extension dùng bởi coordinator. Hai file Actors hiện có giữ nguyên vị trí. Sửa cả tên đầy đủ `Farm.Simulation.WorkerState` trong coordinator, không chỉ các dòng `using`.

Giữ `HarvestMarketCoordinator`, `ProgressFeedbackView`, `FarmTickRunner`, UI, config SO, persistence ở UnityAdapters: đây là nơi nối nhiều feature với scene. Harvest/tồn kho/batch vẫn thuộc Farming; tiền thuộc Economy; sale/progression/save contracts thuộc Simulation. Actor chỉ phát yêu cầu và nhận kết quả, không nhận quyền ghi Wallet.

## 3. Bootstrap dùng Resources

### Phạm vi và cách load

Load trực tiếp từng asset cần cho composition bằng API đúng tên `Resources.Load<T>`, một lần lúc startup; cấp kết quả qua constructor/Initialize. Không thêm resource manager, service locator, reflection hoặc Addressables.

Đề xuất chuyển asset gốc cùng `.meta`, không copy tạo hai bản:

| Asset hiện tại | Đích trong `Assets/Resources/` | Key load |
|---|---|---|
| `Assets/Views/ConstructionUpgradeView.prefab` | `Farm/UI/ConstructionUpgradeView.prefab` | `Farm/UI/ConstructionUpgradeView` |
| `Assets/Views/UpgradeView.prefab` | `Farm/UI/UpgradeView.prefab` | `Farm/UI/UpgradeView` |
| `Assets/Prefabs/Views/UpgradeItemView.prefab` | `Farm/UI/UpgradeItemView.prefab` | `Farm/UI/UpgradeItemView` |
| `Assets/Prefabs/Effects/EffPay.prefab` | `Farm/Effects/EffPay.prefab` | `Farm/Effects/EffPay` |
| `Assets/Prefabs/Effects/EffBuildDone.prefab` | `Farm/Effects/EffBuildDone.prefab` | `Farm/Effects/EffBuildDone` |
| `Assets/Prefabs/Delivery.prefab` | `Farm/Actors/Delivery.prefab` | `Farm/Actors/Delivery` |
| `Assets/Prefabs/Customer.prefab` | `Farm/Actors/Customer.prefab` | `Farm/Actors/Customer` |

Không lấy nhầm `Assets/Prefabs/Views/UpgradeView.prefab`, asset này thuộc flow khác. Bảy config đang ở `Assets/Resources/Upgrades` giữ nguyên đường dẫn; dùng `LoadAll<UpgradeConfig>("Upgrades")` làm nguồn duy nhất và giữ thứ tự UI tường minh hiện tại. Bỏ serialized override để tránh hai nguồn cấu hình khác nhau.

Ví dụ dự kiến trong Bootstrap:

```csharp
var plotUpgradePrefab = Resources.Load<GameObject>("Farm/UI/ConstructionUpgradeView");
var workerObject = Resources.Load<GameObject>("Farm/Actors/Delivery");
var workerPrefab = workerObject != null ? workerObject.GetComponent<WorkerActor>() : null;
if (workerPrefab == null)
    throw new InvalidOperationException("Farm/Actors/Delivery is missing WorkerActor.");
```

Kiểm tra đầy đủ prefab/component UI, worker/customer và cấu hình upgrade **trước** bind UI, spawn hoặc khởi động persistence/tick. Thiếu key, sai type/component, thiếu/trùng ID trong bảy upgrade phải báo rõ asset/ID rồi dừng compose; không âm thầm bỏ menu/VFX hoặc fallback sang serialized field cũ.

`ActorSceneInstaller` bỏ hai serialized prefab; đổi entry point thành `Initialize(WorkerActor workerPrefab, CustomerActor customerPrefab, int customerTarget)`. Bootstrap là caller cấp prefab đã load. Rà các overload/caller hiện có và bỏ overload không còn dùng; giữ constructor của hai factory nhận prefab như hiện tại. Installer vẫn sở hữu market/actorParent và pool lifecycle, không tự load asset mỗi lần spawn.

Theo [Unity Resources.Load](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Resources.Load.html), key tương đối với thư mục Resources, dùng `/`, không có extension; asset không tìm thấy trả null. Load prefab không tự tạo instance: factory/UI owner hiện có vẫn gọi Instantiate và cleanup.

### Scene references và dữ liệu plot

- Giữ binding scene của installer, plot array, HUD, Canvas trên Bootstrap; giữ market/actorParent trên installer, anchor/socket trong scene/prefab. Không dùng Resources để thay các instance này.
- Không mở rộng thành dựng lại toàn bộ Farm scene từ prefab. Việc bootstrap tự tìm scene objects hiện có không phải trọng tâm refactor; validate đúng binding trước side effect.
- `ResourceConfig` của từng plot hiện nằm ở `PlotSlotView`, không phải serialized field của Bootstrap. Giữ mapping `PlotId → ResourceConfig` hiện tại; không chuyển bốn config sang LoadAll theo index vì sẽ làm mapping ngầm và tăng phạm vi ngoài yêu cầu bootstrap.
- Reference nội bộ prefab như Animator, carry socket, Tomato, Box/Construction tiếp tục author trực tiếp. Tách chúng khỏi prefab không giúp composition root gọn hơn.
- Nếu muốn loại bỏ cả scene binding hoặc config binding của plot, đó là phạm vi bổ sung cần thiết kế riêng; plan này giả định yêu cầu Resources nhắm tới asset dependency của bootstrap và actor prefab.

### Thứ tự startup và cleanup

```text
Resolve scene binding + load assets → validate toàn bộ đầu vào
→ load/validate save → dựng services + restore progression
→ initialize actors bằng prefab đã load → bind coordinators/UI/VFX
→ restore presentation → bật persistence và tick
```

Giữ cleanup startup lỗi giữa chừng và Dispose gọi lặp an toàn. Asset được load không phải instance do bootstrap tạo: teardown chỉ hủy instance/subscription/pool thuộc session, không Destroy asset prefab/SO và không thêm UnloadUnusedAssets theo mỗi actor despawn.

## 4. Trình tự thực hiện sau khi được yêu cầu code

- [ ] **Bước 1 — chỉnh boundary actor:** di chuyển các file/.meta trong bảng, sửa namespace/caller (`GameBootstrap`, `HarvestMarketCoordinator`, `ProgressFeedbackView` và mọi kết quả tìm kiếm type cũ); bật engine reference và A* cho Actors, bỏ Actors khỏi Simulation, thêm Actors vào Bootstrap, bỏ A* khỏi UnityAdapters nếu không còn caller. Không thêm cạnh Actors → Simulation để chữa lỗi compile.
- [ ] **Kiểm chứng bước 1:** Unity compile không cycle/type lỗi; prefab Delivery/Customer, scene Market/installer không Missing Script; instance/override và socket còn đúng. Tìm toàn bộ `Farm.UnityAdapters.WorkerActor`, `Farm.Simulation.WorkerState` và các type đã chuyển trong code/serialized data; kiểm tra migration type nếu gặp tên assembly-qualified, không thay GUID hàng loạt.
- [ ] **Bước 2 — asset loading:** chuyển bảy prefab đúng bảng, thay serialized asset fields bằng load một lần trong `GameBootstrap.cs`, sửa API installer và caller, validate trước side effect; cập nhật `Assets/Scenes/Farm.unity` để bỏ binding obsolete. Không đổi số tiền/ID/giá/đường dẫn save.
- [ ] **Kiểm chứng bước 2:** vào Farm không cần kéo năm prefab và upgrade array lên Bootstrap hoặc hai prefab lên installer; đủ menu 7 item, popup plot, actor và VFX. Trong bản scene/asset thử nghiệm, thiếu prefab, thiếu component hoặc thiếu/trùng upgrade ID phải lỗi rõ, không spawn/tick/ghi save một session khởi tạo dở; khôi phục fixture sau thử.
- [ ] **Bước 3 — regression:** chạy lại EditMode hiện có `Assets/Tests/EditMode/ProgressionServiceTests.cs`; PlayMode từ save sạch có 1 khách/0 worker, build xong mới có worker, harvest ba quả → chờ khách tới Dock → sale một lần → về origin; thử pool reuse, scene reload, mua +1/+2 thành 4 và load không cộng lần hai. Kiểm tra giá batch đang mang giữ snapshot cũ, VFX/menu còn hoạt động. Dùng save thử riêng/backup, không ghi đè tiến trình thật.
- [ ] **Bước 4 — đồng bộ tài liệu:** cập nhật phần 01 graph/số assembly/engine boundary; 02 load/DI/lifecycle; 03–05 actor/FSM/navigation ownership; 07–09 đường nối worker với build/harvest/sale; 11 asset path/binding; 12 startup/load regression; 14 asset/scene binding; 15 ghi kết quả và giới hạn test mới. Phần 06/08 sửa status cũ; 10 giữ quy tắc progression; 13 giữ reflection tùy chọn, ghi rõ đổi ownership không kéo theo reflection. Không biến kết quả test lịch sử thành kết quả của lượt refactor.

## 5. Điều kiện hoàn tất

- Worker/customer runtime, flow, factory/pool và navigation cùng thuộc Farm.Actors; graph đúng phần 2, không cycle, Simulation vẫn thuần C#.
- Bootstrap lấy prefab/UI/VFX/upgrade từ Resources, truyền dependency tường minh; scene binding đúng instance và prefab reference nội bộ được bảo toàn.
- Không thay đổi các quyết định D1–D8 ở phần 15: giá/level/buff, snapshot, save reset actor/batch, không buff actor, không reflection, người dùng tự build APK.
- Các kiểm chứng trên có kết quả thực tế trước khi gọi refactor hoàn tất. Lượt viết plan này chỉ kiểm tra tài liệu và code; chưa chạy compile/PlayMode mới.
- Chỉ sửa code/asmdef/prefab/scene khi có yêu cầu triển khai tiếp. Không commit/push trong lượt lên plan.
