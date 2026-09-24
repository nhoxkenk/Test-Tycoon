# Reflection Factory cho class logic C#

[← Mục lục](README.md)

**Trạng thái:** phương án tùy chọn, chưa triển khai. DI thuần bằng constructor ở Bootstrap đang đáp ứng phần 01–09. Bốn resource chỉ khác `ResourceConfig` ScriptableObject và dùng chung service/visual, nên chưa có lý do tạo class bằng reflection. Chỉ đánh giá phương án dưới đây nếu resource hoặc upgrade có nhiều implementation hành vi riêng cần khám phá theo contract/key.

## Vai trò

```text
GameBootstrap (Unity entry point, scene references)
    ↓ cấp các instance và dữ liệu khởi tạo từ bên ngoài
Reflection Factory (khám phá type, nối constructor, giữ lifetime)
    ↓ tạo object graph C#
Wallet / ProgressionService / upgrade effects / booster controllers
```

Factory thuộc assembly composition/Bootstrap. Feature module chỉ biết contract nghiệp vụ và constructor của chính mình; chúng không reference factory, Bootstrap, `MonoBehaviour` hay `Activator`. Bootstrap cũng không cần liệt kê từng class logic được phát hiện. Nó vẫn chịu trách nhiệm cung cấp những thứ factory không thể tự tạo: prefab, scene references, config đã đọc, save data và lựa chọn implementation cho một dependency có nhiều ứng viên.

Đây là một DI container nhỏ do dự án sở hữu. Reflection chỉ thay cách tạo object; nó không tự giải quyết ownership, lifetime, thứ tự khởi động hay lỗi thiếu dependency. Những quy tắc đó thuộc hợp đồng của factory.

## Type nào được factory tạo?

- Class C# cụ thể, không abstract, không generic mở, không phải `MonoBehaviour`/`ScriptableObject`.
- Type tham gia hệ thống được đánh dấu rõ bằng contract/metadata. Factory chỉ quét danh sách assembly cho phép, ví dụ `Farm.Economy` và `Farm.Simulation`, thay vì mọi assembly Unity/package.
- Service dùng chung trong một scene và handler/effect C# có thể là ứng viên. Mỗi loại phải có lifetime khai báo rõ: một instance cho scene hoặc tạo mới theo lần yêu cầu. `WorkerActor`/`CustomerActor` MonoBehaviour và state riêng của từng actor theo [phần 03](03-actor-model-factory.md) do actor prefab/factory nghiệp vụ quản lý, không do reflection factory quét.
- Giá trị dữ liệu như `Money`, `HarvestBatch`, config DTO và modifier record **được tạo trực tiếp từ dữ liệu runtime**. Không quét và cache chúng như service. `Money` không chứa logic khởi động hoặc dependency cần DI.

Enum dùng để **định danh loại hành vi**, không phải contract mà class “implement”. Ví dụ class implement `IUpgradeEffect` và có metadata `UpgradeEffectKind.AllCropProfit`; factory lập bảng `(IUpgradeEffect, AllCropProfit) → concrete type`. Metadata có thể là attribute trên class để đọc mà chưa cần tạo instance. Enum `CurrencyId.Coin/Gem` chỉ định danh số dư trong Wallet: thêm gem không đồng nghĩa phải có một class `GemMoney`. Chỉ khi một currency có quy tắc hành vi riêng mới cân nhắc contract và handler riêng cho currency đó.

## Khám phá và kiểm tra lúc khởi động

1. Bootstrap truyền cho factory danh sách assembly được phép cùng các instance từ bên ngoài.
2. Factory quét mỗi assembly một lần lúc startup, lọc theo interface/metadata đã định nghĩa và cache mapping từ key sang `Type`.
3. Factory từ chối type abstract, generic mở, trùng key, contract không khớp hoặc implementation không có constructor hợp lệ.
4. Khi một contract có nhiều implementation, phải có key hoặc lựa chọn tường minh. Không chọn “class đầu tiên tìm thấy”, vì thứ tự quét không phải quy tắc nghiệp vụ.
5. Factory kiểm tra graph trước khi bắt đầu tick: thiếu dependency, vòng phụ thuộc, constructor mơ hồ và key cần thiết chưa có đều phải báo lỗi gắn tên type/parameter rõ ràng.

Quy tắc constructor đề xuất: mỗi type chỉ có **một public constructor** được factory sử dụng. Tham số constructor là dependency bắt buộc; factory tìm instance đã cấp từ ngoài, instance đã cache hoặc tạo tiếp một type hợp lệ. `Activator.CreateInstance(type, args)` chỉ gọi constructor khớp các `args`; nó không tự tìm dependency. Không dùng property/field injection để che dependency bắt buộc. Xem [Microsoft: Activator.CreateInstance](https://learn.microsoft.com/dotnet/api/system.activator.createinstance).

Giá trị chỉ biết lúc gọi, như `targetPlotId`, `quantity`, `ActorId` hay thời hạn booster, được **caller truyền tường minh theo lời gọi tạo object**. Factory không đoán primitive `int/string` từ registry toàn cục. Nếu một constructor cần cả service và runtime value, factory phải có API phân biệt hai nguồn dữ liệu, hoặc để một factory nghiệp vụ nhận service và truyền runtime value trực tiếp. Chưa cần biến mọi instance runtime thành đối tượng do reflection factory quản lý.

## Lifetime, sử dụng và cleanup

| Nhóm | Cách tạo/sở hữu | Ví dụ |
|---|---|---|
| Service dùng chung theo scene | Factory tạo một lần, trả cùng instance, dispose khi scene kết thúc | Wallet, ProgressionService |
| Handler không có state theo lượt | Tạo một lần cho scene và map theo enum | SingleCropProfitEffect |
| Object mang state riêng | Tạo mới cho owner; owner hủy hoặc trả về factory để cleanup | Booster instance; state riêng của actor do actor tạo/cleanup |
| Value object | Tạo trực tiếp ở use case; không giữ trong factory | Money, HarvestBatch |

Factory resolve graph ở composition boundary rồi **đưa dependency qua constructor**. Gameplay không gọi `Resolve<T>()` hoặc tìm service từ factory trong `Tick()`. Handler tạo một lần để xử lý nhiều item upgrade cùng kind; thêm item có cùng effect chỉ thêm dữ liệu. Factory track `IDisposable` mà nó sở hữu, dispose theo thứ tự ngược lúc khởi tạo; object do actor hoặc use case sở hữu được cleanup bởi owner đó. Không dispose scene object mà Bootstrap/Unity đang sở hữu.

Thêm class nội bộ không bắt buộc sửa Bootstrap. Thêm effect handler hợp lệ có thể được quét tự động. Thêm scene reference, config mới, implementation cần chọn giữa nhiều ứng viên, hoặc một root service cần khởi động mới có thể buộc cập nhật composition.

## Áp dụng vào upgrade, booster và tiền

- `IUpgradeEffect`: factory khám phá concrete handler theo `UpgradeEffectKind`, tạo và cấp dependency. `ProgressionService` dùng mapping để áp effect sau khi validate cost; nó không quét assembly trong mỗi lần mua.
- Booster: factory có thể khám phá **loại effect/handler**. Một booster đang chạy với `SourceId`, target và thời hạn là state runtime, do `BoosterService` tạo/giữ và gỡ modifier khi kết thúc. Không tạo lại một booster đang chạy chỉ vì factory resolve lại service.
- Economy: `Wallet` là service dùng chung theo scene và có thể do factory tạo từ initial balances. `Money(currency, amount)` là dữ liệu của một giá hoặc khoản thanh toán, được tạo trực tiếp. Nếu có quy tắc đặc thù cho từng currency trong tương lai, mới thêm contract/metadata để quét handler; enum currency một mình không kéo theo handler.

Feature ownership vẫn như [module boundaries](01-module-boundaries.md): Economy không biết upgrade/booster, Farming và Actors sở hữu modifier của mình, Simulation điều phối mua và vòng đời effect. Reflection factory không làm thay đổi chiều reference asmdef.

## Điều kiện cho Android IL2CPP

Type/constructor chỉ xuất hiện qua reflection có thể không được Unity giữ trong Player build. Với các type được quét, cần cơ chế preserve phù hợp (`[Preserve]`, `link.xml` hoặc tham chiếu tĩnh/generated registry) và kiểm thử **APK IL2CPP**, không chỉ Play Mode trong Editor. Không giả định quét được trong Editor thì Android cũng có đủ type. [Unity: managed code stripping](https://docs.unity3d.com/6000.0/Documentation/Manual/managed-code-stripping-configure.html).

Quét và kiểm tra graph ở startup, cache mapping, không scan assembly hoặc gọi reflection khi cập nhật từng frame. Test cần bao phủ: duplicate/missing key, constructor thiếu tham số, cycle, đúng lifetime, runtime parameter, cleanup và type tồn tại trong APK. Nếu việc preserve runtime quá khó kiểm soát, generated registry từ editor là phương án thay thế mà vẫn giữ API factory cho gameplay.

## Điểm cần chốt khi triển khai

- Factory chỉ tự quét các handler/effect theo contract, hay cả service dùng chung như Wallet và ProgressionService?
- Lifetime khai báo bằng metadata trên class hay bằng registration của composition root?
- Runtime parameters truyền trực tiếp qua factory chung hay qua factory nghiệp vụ của từng feature?
- Type được giữ cho IL2CPP bằng `[Preserve]`/`link.xml` hay generated registry?

Chưa cần thiết kế API hoặc attribute cụ thể trước khi chốt các câu trên. Quy tắc bất biến là dependency của logic phải thấy ở constructor, lỗi graph phải xuất hiện lúc startup, và state của scene/actor phải có owner rõ.
