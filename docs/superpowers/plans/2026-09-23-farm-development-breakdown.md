# Architecture — mục lục các tài liệu riêng

Nội dung đã được tách sang [docs/architecture/README.md](../../architecture/README.md) để phân tích và thảo luận từng chủ đề độc lập.

| Tài liệu | Nội dung |
|---|---|
| [01 — Module boundaries](../../architecture/01-module-boundaries.md) | Module, asmdef, hướng dependency |
| [02 — Bootstrap và DI](../../architecture/02-bootstrap-di-lifecycle.md) | DI thuần, wiring, ownership, lifecycle |
| [03 — Actor và Factory](../../architecture/03-actor-model-factory.md) | Model nhân viên/khách và cách tạo actor |
| [04 — State machines](../../architecture/04-actor-state-machines.md) | FSM, transitions và cleanup |
| [05 — Navigation](../../architecture/05-navigation-world.md) | Di chuyển, world queries, vật cản |
| [06 — Economy](../../architecture/06-economy-money.md) | Tiền lớn và Wallet |
| [07 — Cây và xây dựng](../../architecture/07-crops-construction.md) | Plot/Crop, build và trạng thái cây |
| [08 — Thu hoạch](../../architecture/08-worker-harvest-logistics.md) | Phân việc, reservation, snapshot giá |
| [09 — Chợ và khách](../../architecture/09-market-transactions.md) | Queue, population, thanh toán |
| [10 — Nâng cấp và buff](../../architecture/10-upgrades-modifiers.md) | Strategy, stacking và modifiers |
| [11 — UI và config](../../architecture/11-presentation-config.md) | Presentation, events, cấu hình |
| [12 — Save và kiểm chứng](../../architecture/12-persistence-validation.md) | Lưu tiến trình, mobile lifecycle, acceptance |

Đây là mục lục thảo luận architecture, thay thế breakdown triển khai gom chung trước đó. Mỗi file có đề xuất, trade-off và câu hỏi mở; không mặc định coi các đề xuất là quyết định đã được chấp nhận.
