# Architecture — mục lục thảo luận

Mỗi file tập trung vào một chủ đề để đọc, phân tích và sửa riêng. Đây không phải checklist implementation; chưa tự động chốt các đề xuất trong file.

**Đã chốt:** không VContainer; Bootstrap Root cấp đầu vào từ Unity/scene và dùng DI thuần. Reflection factory ở phần 13 là phương án tùy chọn khi xuất hiện nhiều handler có hành vi riêng, không phải yêu cầu cho mỗi service hay resource. Không sử dụng workflow Superpowers.

| File | Chủ đề |
|---|---|
| [01-module-boundaries.md](01-module-boundaries.md) | Ranh giới module và asmdef |
| [02-bootstrap-di-lifecycle.md](02-bootstrap-di-lifecycle.md) | Bootstrap Root và lifecycle |
| [03-actor-model-factory.md](03-actor-model-factory.md) | Actor model và Factory |
| [04-actor-state-machines.md](04-actor-state-machines.md) | State Pattern cho nhân viên và khách |
| [05-navigation-world.md](05-navigation-world.md) | Navigation và world queries |
| [06-economy-money.md](06-economy-money.md) | Economy, Money và Wallet |
| [07-crops-construction.md](07-crops-construction.md) | Cây trồng và xây dựng |
| [08-worker-harvest-logistics.md](08-worker-harvest-logistics.md) | Nhân viên, phân việc và snapshot thu hoạch |
| [09-market-transactions.md](09-market-transactions.md) | Khách hàng, hàng đợi và thanh toán |
| [10-upgrades-modifiers.md](10-upgrades-modifiers.md) | Upgrade Strategy và buff nhiều nguồn |
| [11-presentation-config.md](11-presentation-config.md) | UI, presentation và cấu hình |
| [12-persistence-validation.md](12-persistence-validation.md) | Save/load, lifecycle mobile và kiểm chứng |
| [13-reflection-factory.md](13-reflection-factory.md) | Reflection Factory, Activator và ranh giới DI |
| [14-scene-layout-portrait.md](14-scene-layout-portrait.md) | Bố cục Farm, camera dọc và các mốc scene 07–09 |
| [15-final-implementation-review.md](15-final-implementation-review.md) | Phạm vi còn lại 10–14 và các quyết định cần xác nhận trước khi code |

Nên bắt đầu từ 01 (module boundaries) và 02 (DI/lifecycle), sau đó chọn từng chủ đề cần bàn. Mỗi file ghi bài toán, phương án đề xuất, trade-off, câu hỏi mở và điều kiện cần giữ.

[Bản thiết kế tổng ban đầu](../superpowers/specs/2026-09-23-farm-architecture-design.md) giữ làm tài liệu tham chiếu. Các đề xuất trong bản đó chưa được xem là đã chốt chỉ vì đã viết ra. Khi thảo luận, cập nhật file chủ đề trước; không phải gom lại toàn bộ tài liệu để sửa một quyết định.
