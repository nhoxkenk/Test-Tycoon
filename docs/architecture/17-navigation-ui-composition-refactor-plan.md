# Plan refactor Navigation, UI và Composition Root

**Ngày:** 2026-09-24. **Trạng thái:** kế hoạch giao Claude triển khai; lượt viết tài liệu chưa sửa runtime/prefab/scene và chưa chạy test.

## 1. Mục tiêu và quyết định đã chốt

Đọc file này như chỉ dẫn triển khai dựa trên yêu cầu mới nhất của người dùng. Khi khác với đề xuất audit trước hoặc tài liệu 01–16, các quyết định dưới đây được ưu tiên:

1. Navigation dùng Strategy Pattern, chọn qua cấu hình scene. NavMesh tiếp tục là mặc định; giữ A* dưới một strategy riêng, bao gồm setup và validation đặc thù. Không xóa A* để đơn giản hóa.
2. Reference UI/Unity Component được author bằng `[SerializeField]`. Gom đầu vào khởi tạo liên quan vào context theo feature; ScriptableObject chỉ giữ config/reference asset, không giữ instance scene hoặc state session.
3. Popup nâng level của từng plot dùng **chính Canvas của `PlotUiView` tương ứng**. Bỏ Canvas riêng trên `ConstructionUpgradeView`; không chuyển thành popup dùng chung trên Main Canvas.
4. Thu gọn lớp forwarding `ActorSceneInstaller` nếu làm đường gọi rõ hơn; giữ ownership và lifecycle.
5. Bootstrap vẫn là composition root. Mỗi dữ liệu/reference có một nguồn chuẩn (SOT). Resources path tập trung trong một class; load một lần tại composition boundary, truyền kết quả xuống consumer.

Đích đến là dễ đọc, ít dependency ngầm và dễ chọn navigation backend, không phải giảm số dòng bằng mọi giá. Giữ sáu assembly hiện có trong lượt này. Không VContainer, reflection factory, global event bus, service locator hoặc dependency mới.

## 2. Hiện trạng phải kiểm tra lại trước khi sửa

- `WorkerFactory` và `CustomerFactory` trực tiếp tạo `NavMeshAgentNavigation`.
- `WorkerActor`/`CustomerActor` có `RequireComponent(NavMeshAgent)`, làm actor bị khóa vào backend dù nhận `IActorNavigation`.
- `ActorSceneInstaller` vẫn tạo `AstarWorldQuery`; không tìm thấy consumer nghiệp vụ của `TryGetPathLength` trong lần rà soát.
- Actor/flow/factory/navigation đã ở `Farm.Actors`; Actors có UnityEngine/A* reference, Simulation chỉ reference Economy/Farming. Không thực hiện lại thao tác di chuyển của plan 16.
- `PlotUiView` tạo World UI Canvas theo plot; `PlotUpgradeView` lại instantiate popup dưới Main Canvas. Prefab popup còn có Canvas riêng.
- `ConstructionUpgradeView`/`UpgradeMenuView` tìm object qua tên/path; hàm validate prefab hiện rỗng. `UpgradeItemView` cũng tra component bằng tên.
- `GameBootstrap.Compose` trộn load, mapping config, validate save, compose và bind presentation. Có fallback tìm scene object và fallback giá level.
- Tài liệu 01–16 có nhiều status/path/navigation cũ; không coi kết quả test lịch sử là bằng chứng cho code đang sửa.

**Thay đổi đang có của người dùng:** tại thời điểm viết plan, `git status --short` báo `Assets/Resources/Farm/UI/ConstructionUpgradeView.prefab` đã modified. Đọc diff trước khi sửa; giữ các chỉnh sửa của người dùng, không reset/checkout đè asset. Kiểm tra lại toàn bộ worktree vì có thể phát sinh thay đổi mới.

## 3. Navigation Strategy

### 3.1 Thiết kế chọn

Giữ `IActorNavigation` là contract vận hành từng actor và giữ `WorldPoint` trong lượt này để không mở rộng phạm vi migration. Thêm một strategy ở Actors cho **cách chuẩn bị backend và tạo adapter**, không tạo FSM/navigation framework mới.

Phương án cụ thể: `ActorNavigationStrategy : MonoBehaviour` abstract với hai implementation `NavMeshNavigationStrategy` và `AstarNavigationStrategy`. `ActorSceneInstaller` serialize một reference strategy. Đây là nguồn duy nhất chọn backend; scene Farm bind NavMesh. Không thêm enum selector độc lập trên Bootstrap/factory/actor.

Trách nhiệm contract (tên method có thể điều chỉnh cho code hiện tại):

- Validate scene reference và yêu cầu component của worker/customer prefab trước spawn.
- Prepare backend một lần cho scene/session, nếu backend cần bước chuẩn bị runtime.
- Tạo `IActorNavigation` cho actor instance được lấy từ pool.
- Cleanup phần backend do strategy thực sự tạo/sở hữu; cleanup lặp an toàn.

Flow:

```text
Scene binding chọn strategy
  → Bootstrap lấy prefab phù hợp và validate đầu vào
  → strategy prepare backend
  → installer tạo factories nhận strategy
  → factory lấy actor → strategy tạo adapter → actor.Initialize
```

Factory không `new NavMeshAgentNavigation`, không reference `AIPath`/`Seeker`, không switch theo backend. Actor không có `RequireComponent` khóa vào NavMesh/A*. Actor và FSM chỉ biết contract vận hành.

### 3.2 Setup từng backend

**NavMesh strategy:** nhận reference scene cần thiết qua serialize, kiểm tra dữ liệu navigation đã sẵn sàng; tạo adapter từ `NavMeshAgent`. Giữ thông số và hành vi di chuyển hiện có, không tự thay đổi bán kính sample/stopping distance trong refactor. Không auto-bake mỗi spawn.

**A* strategy:** toàn bộ yêu cầu `AstarPath`, graph, `AIPath`, `Seeker`, modifier và RVO (nếu cấu hình sử dụng) nằm tại implementation này. Reference component scene phải tường minh. Nếu cần scan/khởi tạo, làm một lần đúng lifecycle trước spawn; không scan mỗi actor, không tự tìm global scene object để che binding thiếu.

Kiểm tra code A* đã cài tại project để xác định API/lifecycle thực tế. Có thể giữ adapter A* hiện có, nhưng dependency scene cần được strategy cấp/kiểm soát; không để setup A* chạy khi chọn NavMesh.

### 3.3 Prefab và cấu hình backend

- Giữ hai prefab NavMesh hiện tại và Resources key hiện có.
- Tạo variant A* cho worker/customer nếu chưa có prefab tương thích; mỗi variant chỉ có một movement driver có quyền điều khiển transform. Giữ Animator/carry socket/visual và actor script dùng chung.
- Không cho NavMeshAgent và AIPath đồng thời chạy trên một actor. Ưu tiên prefab phù hợp đã author sẵn, tránh thêm/xóa hàng loạt component theo backend ở mỗi spawn.
- Strategy có định danh backend cố định theo implementation, chỉ dùng ở composition để chọn cặp prefab. Bootstrap/loader được phép có một mapping tường minh từ định danh đó tới cặp Resources path; không lặp switch trong factory/actor/FSM. Khi thêm backend mới, thêm strategy và một entry mapping là đủ.
- Backend được chọn theo session, không yêu cầu hot-swap khi actor đang giữ cargo. Đổi cấu hình rồi khởi tạo lại scene/session.
- Backend không được chọn không bắt buộc có scene reference hợp lệ và không được initialize. Thiếu config backend đang chọn phải lỗi rõ, không fallback sang backend khác.

### 3.4 World query và lifetime

Không tạo `AstarWorldQuery` vô điều kiện trong installer. Rà caller lần nữa: nếu không có consumer, bỏ property/wiring `WorldQuery` và contract/implementation không còn dùng; bảo toàn `WorldPointExtensions` đang nằm chung file trước khi xóa. Strategy không phải lý do tạo sẵn world-query API chưa dùng. Nếu phát hiện consumer thực sự, giữ query nhưng để strategy đang chọn tạo implementation cùng backend.

Strategy sống theo scene; adapter sống theo lượt spawn. Actor reset/pool phải Stop và Dispose adapter có subscription. Factory phải dọn adapter khi initialize thất bại hoặc ném lỗi. Coordinator trả active actor về pool trước khi pool/strategy cleanup. Không destroy scene object/asset mà strategy không sở hữu.

**Dependency:** lượt này vẫn giữ `Farm.Actors → AstarPathfindingProject`. Strategy giảm coupling của factory/actor với backend, chưa có nghĩa gỡ được package A*. Không thêm asmdef plugin navigation chỉ để đạt mục tiêu optional package.

## 4. Quy định serialize, context Init và ScriptableObject

### 4.1 Phân biệt ba loại đầu vào

| Loại | Nguồn và cách truyền |
|---|---|
| Scene instance: Camera, Canvas, Button, Transform, installer, plot | Serialize tại owner scene/feature; gom reference liên quan trong serializable bindings hoặc runtime init context |
| Asset: prefab, icon, cấu hình kinh tế | Reference asset trên prefab/SO hoặc load tập trung bằng Resources theo quy tắc phần 6 |
| Service và dữ liệu runtime: progression, wallet reader, PlotId, customer target | Constructor/Initialize tường minh; không lưu trên ScriptableObject |

Asset ScriptableObject không giữ Canvas/GameObject instance của scene. Nó có thể giữ prefab GameObject hoặc cấu hình số/chuỗi. Init context là object C# thường, tạo lúc compose, không static và không `Resolve<T>()`.

### 4.2 Quy tắc context

- Một context chỉ dành cho một nhu cầu khởi tạo, ví dụ `PlotUiInitContext` hoặc `UpgradeMenuInitContext`; không truyền toàn bộ `FarmContext` cho mọi consumer.
- Gom nhóm Unity reference thường đi cùng nhau. Không gom mọi service vào context chỉ để constructor còn một tham số.
- Service bắt buộc vẫn có thể đứng riêng: `Initialize(context, progression, walletReader)` là hợp lệ và dễ đọc.
- Ưu tiên type cụ thể: `RectTransform` cho parent UI, `ConstructionUpgradeView` cho prefab/view, thay vì `GameObject` rồi đoán component.
- Context chỉ dùng để cấp đầu vào; consumer giữ đúng field cần thiết. Không tạo hai bản config mutable có thể lệch nhau.
- Không tạo context cho method nhỏ chỉ có một hoặc hai tham số rõ nghĩa.

### 4.3 Serialize component nội bộ UI

Sửa `ConstructionUpgradeView`, `UpgradeItemView` và phần panel/menu binding sang reference serialize: icon, label, slider, button, close, content root. Nút menu thuộc scene cấp qua scene bindings; component trong panel thuộc prefab panel. Nếu `UpgradeMenuView` hiện chỉ được AddComponent runtime, chuyển thành component được author hoặc cấp binding component có sẵn trên prefab, để reference có thể được kéo thả thật.

Bỏ `FindDeep`, path `Bot/MainBotBarView/BotBarItem/Button`, tìm theo tên text và fallback tự thêm component cho prefab thiếu cấu hình. Gắn sẵn component bắt buộc trên prefab; validate có lỗi cụ thể tên asset/field. Không giữ hàm validate rỗng.

`MarketLayout` serialize spawn/exit/origin và danh sách cặp anchor customer/worker theo slot. Giữ thứ tự slot hiện có. Không thêm scan hierarchy dự phòng nếu reference chưa được kéo.

Không thay đổi quy tắc hiển thị item đã mua/thiếu tiền trong lượt refactor này nếu không cần cho migration; vấn đề hành vi ngoài phạm vi phải báo riêng.

## 5. Upgrade dùng chung Canvas với PlotUiView

### 5.1 Hierarchy đích

```text
PlotSlot
  World UI (Canvas WorldSpace + GraphicRaycaster, do PlotUiView sở hữu)
    Unlock
    Build
    Information
    ConstructionUpgradeView (RectTransform + UI, không có Canvas riêng)
```

- Giữ một world Canvas cho mỗi plot; Main Canvas tiếp tục dành cho HUD/menu chung.
- Sửa prefab `Assets/Resources/Farm/UI/ConstructionUpgradeView.prefab`: bỏ Canvas và component phụ thuộc Canvas riêng không còn cần, kể cả CanvasScaler/GraphicRaycaster nếu có. Rà nested Canvas để bảo đảm nội dung upgrade thực sự dùng Canvas của plot.
- Giữ layout/visual người dùng đã chỉnh. Điều chỉnh local scale, size, anchor, pivot theo world UI root hiện tại để không bị nhân scale hoặc dùng kích thước fullscreen.
- Gắn component view và serialize reference trực tiếp trên prefab.
- `PlotUiView` instantiate upgrade dưới `worldUiRoot` bằng local transform phù hợp; không truyền `mainCanvas` xuống flow nâng level plot.
- Nếu giữ `PlotUpgradeView` như presenter, nó nhận instance `ConstructionUpgradeView` đã tạo và dịch vụ; không tự tạo canvas/prefab/host thừa. Có thể chuyển presenter thành C# thường có Dispose để loại GameObject host chỉ dùng làm cầu nối.
- View nhận dữ liệu qua `Render(level, cost, canBuy)` và callback buy/close; không cần ba `Func` để pull state từ progression. Presenter/controller refresh khi mở và khi dữ liệu liên quan đổi.

### 5.2 Interaction và lifecycle

- `PlotConstructionController` quản lý plot đang mở upgrade. Khi mở plot B, đóng popup A qua reference đã biết, bỏ `FindObjectsOfType<PlotUpgradeView>`.
- Bấm upgrade/close không click xuyên xuống plot. GraphicRaycaster của world Canvas và camera phải được bind đúng; Information giữ trạng thái không chặn raycast như hiện tại.
- Không thêm blocker fullscreen vào world UI; panel/nút có vùng hit đúng kích thước. Giữ cách đóng qua nút Close, không tự thêm hành vi click ngoài nếu chưa có.
- Mở lại popup refresh đúng PlotId, level, giá, affordability; Lv10 khóa mua. Mỗi click chỉ gọi use case một lần.
- `PlotUiView` sở hữu UI instance; presenter sở hữu subscription của nó. Dispose/unbind trước destroy, không double-destroy popup giữa presenter và owner.
- Không tăng số Canvas khi mở/đóng popup; không để panel upgrade trôi sang Main Canvas.

## 6. Bootstrap, SOT và Resources paths

### 6.1 Một nguồn cho từng dữ liệu

| Dữ liệu | Nguồn chuẩn |
|---|---|
| Reference scene cấp root | Một `FarmSceneBindings` serializable trên Bootstrap, gồm camera, actors, plots, HUD, binding menu cấp scene |
| Component con UI/market | Prefab hoặc owner feature tương ứng; Bootstrap không giữ lại bản sao Button/Text/socket |
| Backend navigation | Reference strategy duy nhất trên ActorSceneInstaller |
| Resource của plot | `PlotSlotView.Resource`, giữ mapping PlotId hiện có |
| Chín giá nâng level | `ResourceConfig.LevelUpgradeCosts`; migrate dữ liệu trước khi bỏ fallback `UpgradeCost` |
| Bảy upgrade chung | `Resources.LoadAll<UpgradeConfig>` một lần từ folder chuẩn; không thêm serialized override cùng dữ liệu |
| Tiến trình đã mua/số dư | Runtime services khởi tạo từ save hợp lệ; config chỉ là mặc định khi không có save |

`FarmSceneBindings` chỉ là nhóm field scene, không phải service locator hoặc bản sao toàn bộ hierarchy. Context cấp cho feature được rút từ bindings/asset đã load, không truyền bindings root xuống mọi class. Xóa fallback `FindObject(s)OfType`/chọn Canvas đầu tiên ở Bootstrap; Camera cũng bind tường minh.

### 6.2 Resources paths và load

Thêm `FarmResourcePaths` ở Bootstrap, chứa constant cho các key đang dùng:

- `Farm/UI/ConstructionUpgradeView`
- `Farm/UI/UpgradeView`
- `Farm/UI/UpgradeItemView`
- `Farm/Effects/EffPay`
- `Farm/Effects/EffBuildDone`
- `Farm/Actors/Delivery`
- `Farm/Actors/Customer`
- `Upgrades`

Thêm key variant A* vào cùng class khi author asset, ví dụ `Farm/Actors/Astar/Delivery` và `Farm/Actors/Astar/Customer`. Không đổi key NavMesh hiện có. Class chỉ chứa đường dẫn, không cache mutable/static service, không lấy state scene.

Load tại Bootstrap hoặc một helper nội bộ `FarmAssets.Load(...)`, trả bộ asset typed sống theo session. Chỉ chọn một nơi thực hiện Resources.Load/LoadAll. Bộ asset runtime không phải một nguồn authoring thứ hai; không serialize override trùng với Resources. Consumer nhận đúng prefab/context cần dùng, không gọi load theo chuỗi.

Không đưa prefab con/Animator/Tomato/socket đang author tốt thành Resources độc lập. Không yêu cầu một catalog SO mới chỉ để thay class constant. Không dùng Addressables hay asset manager tổng quát.

### 6.3 Thứ tự compose

```text
Validate scene bindings
→ Load asset theo paths và strategy được chọn
→ Validate prefab/component/config, chuyển SO thành runtime definition
→ Load và validate save
→ Tạo services + restore progression
→ Prepare navigation + tạo coordinator/pool (chưa spawn)
→ Bind controllers/UI/events
→ Áp customer target đã restore, spawn và restore plot presentation
→ Cho phép persistence và tick
```

Chia `Compose` thành private method theo bước khi giúp đọc dễ hơn; đoạn tạo graph và thứ tự lifecycle vẫn phải nhìn thấy được. Không tạo hierarchy bootstrap class.

Giữ chính sách save hỏng: log rõ, không ghi đè, có thể dùng session mặc định với saving disabled như code hiện tại. Startup lỗi cấu hình phải dừng session và cleanup; chưa bind persistence ghi file trong graph khởi tạo dở.

Giữ bảy ID, thứ tự menu và giá hiện tại. Chưa mở catalog tùy ý/DisplayOrder trong lượt này. Có thể thống nhất `UpgradeConfig.Effect` với `UpgradeKind` để bỏ mapping trùng nếu bảo toàn serialized value và kiểm tra cả bảy asset; không biến việc này thành framework effect.

## 7. Thu gọn installer và tránh vòng khởi tạo

- Installer giữ scene references, strategy và ownership coordinator/pool; bỏ forwarding event/method không còn cần.
- Sau khi tạo coordinator, truyền trực tiếp nó vào `HarvestMarketCoordinator` và `ProgressFeedbackView`; origin/anchor lấy từ owner đã bind, không đưa dependency ngược vào Actors.
- Tách tạo coordinator khỏi bắt đầu spawn. Progression restore cần hoàn tất trước khi áp target/spawn; không tạo vòng `progression cần coordinator đang spawn` và `coordinator cần progression đã restore`.
- Phương án gọn: Progression giữ target và phát thông báo target đổi; controller đã có ở UnityAdapters nối với coordinator. Sau restore/bind, áp target hiện tại một lần rồi bắt đầu session. Không thêm PopulationController riêng chỉ để forward một event.
- Có thể giữ callback hiện tại nếu chứng minh thứ tự tạo/bind an toàn và rõ; không dùng closure nullable âm thầm bỏ lệnh để né dependency chưa sẵn sàng.
- Rà và bỏ overload/API không có caller sau migration; không bỏ guard cargo/pair/reservation, cleanup hoặc recovery path đang dùng.

## 8. Phạm vi giữ nguyên và phần hoãn

Giữ nguyên tiền BigInteger, currency ID save, một lần floor khi tạo batch, snapshot giá cargo, ba quả một lô, sale một lần, build/harvest timer, Lv10, bảy item, +1/+2 khách thành 4, save reset actor/batch và không offline income. Giữ State Pattern, hai factory pool riêng và ranh giới service nghiệp vụ.

Chưa làm trong lượt này: gộp asmdef; xóa vendor A*; thay WorldPoint; viết generic factory/base actor; gộp Construction/Harvest; gom state nội bộ HarvestService; tái cấu trúc transaction/progression/save sâu; nâng cấp package; đổi input system; build APK. Những offer audit đó có thể làm lượt sau, không chèn vào refactor navigation/UI.

## 9. Trình tự triển khai và kiểm chứng

### Bước 1 — Baseline và ownership

- [ ] Đọc git diff, các file mục 2 và prefab/scene thực tế; giữ thay đổi người dùng.
- [ ] Ghi baseline compile/test, hierarchy Canvas, prefab actor/navigation đang dùng; không dùng save thật làm fixture.
- [ ] Rà caller và serialized reference trước khi xóa API hoặc đổi type. Di chuyển asset/script phải giữ `.meta`/GUID.

### Bước 2 — Navigation strategy

- [ ] Thêm strategy, inject factories, bỏ RequireComponent backend trên actor; migrate scene chọn NavMesh.
- [ ] Author cặp A* variant và scene binding thử nghiệm; cô lập setup backend; sửa world-query wiring.
- [ ] Test fake strategy xác nhận hai factory dùng strategy được cấp, adapter reset khi reuse/spawn thất bại, cleanup đúng lượt.
- [ ] PlayMode NavMesh: spawn, tới harvest/dock/exit, mất đường, pool reuse; backend A* không được khởi tạo.
- [ ] PlayMode fixture A*: đủ setup thì đi được cùng các anchor đại diện; thiếu setup báo lỗi trước spawn. Dọn fixture theo ownership và trả Farm về NavMesh.

### Bước 3 — Binding/context và Resources

- [ ] Thêm FarmSceneBindings, init context theo feature và FarmResourcePaths; một nơi load typed assets.
- [ ] Serialize UI/market reference; thực sự gắn trên scene/prefab, bỏ tìm theo tên/fallback AddComponent.
- [ ] Tách Compose theo bước, migration giá level không thay số, validate thật trước side effect.
- [ ] Kiểm tra thiếu reference/prefab/component có lỗi tên field/key cụ thể; không spawn/tick/save session khởi tạo dở.

### Bước 4 — Canvas plot và forwarding

- [ ] Upgrade là child cùng Canvas của PlotUiView; bỏ Canvas riêng và dependency Main Canvas trong flow plot.
- [ ] Owner UI/presenter rõ, controller quản lý popup đang mở; bỏ scene-wide search.
- [ ] Coordinator được cấp trực tiếp cho consumer; installer tập trung lifecycle; restore/spawn đúng thứ tự.
- [ ] Kiểm tra popup cả bốn plot: vị trí/scale, close, không click xuyên, chuyển plot, mua thiếu tiền/Lv10; không phát sinh Canvas mới hoặc listener trùng sau mở lại.

### Bước 5 — Regression và tài liệu

- [ ] Unity compile không lỗi mới; chạy `Assets/Tests/EditMode/ProgressionServiceTests.cs` và test navigation/lifecycle mới có ý nghĩa.
- [ ] Save sạch: một khách, không worker; build xong mới có worker; harvest ba quả → chờ khách tại Dock → sale đúng một lần → về origin.
- [ ] Mua +1/+2 tới target 4; reload không cộng lại; batch đang mang giữ giá cũ khi upgrade; build dở tiếp tục từ save.
- [ ] Scene reload/startup lỗi giữa chừng không sót event, actor, UI instance hoặc ghi đè save hỏng.
- [ ] Kiểm tra portrait 1080×1920 và màn dọc dài: popup world UI đọc được, không che UI quan trọng; không khẳng định notch thiết bị thật nếu chưa thử.
- [ ] Cập nhật docs 01–05, 11–12, 14–16 và README theo hiện trạng; sửa status cũ của 06/08 khi gặp. Ghi rõ kết quả thực chạy và kiểm chứng chưa làm.

Không cần test chỉ để đếm constant/path hay mirror property. Tập trung strategy selection/lifetime, popup ownership và các invariant gameplay. Nếu môi trường không chạy được Unity, ghi rõ phần chưa kiểm chứng, không tự đánh dấu acceptance đạt.

## 10. Điều kiện bàn giao cho người dùng

- NavMesh mặc định, A* chọn qua strategy/config và có setup riêng; factory/FSM không hardcode backend.
- Scene/prefab đã được migrate với reference thực, không chỉ thêm field C# để trống rồi yêu cầu người dùng sửa toàn bộ.
- Upgrade từng plot dùng Canvas của PlotUiView, không Canvas riêng hoặc Main Canvas.
- Resources path tập trung, load có kiểm soát; SOT rõ cho scene/config/runtime, context không trở thành dependency bag toàn cục.
- Không thay rule tiền/progression/save; không mất chỉnh sửa prefab đang có.
- Báo danh sách file đổi, cấu hình backend, cách author bindings, kết quả test và giới hạn còn lại. Không tự commit/push hoặc build APK.

**Chỉ dẫn cho Claude:** thực hiện các bước trong phạm vi trên, đối chiếu code mới nhất trước khi sửa. Những lựa chọn triển khai nhỏ được tự quyết theo mục tiêu đơn giản; không cần thiết kế thêm framework. Nếu có mâu thuẫn làm thay đổi gameplay hoặc dữ liệu đã lưu, nêu rõ trước khi mở rộng phạm vi.
