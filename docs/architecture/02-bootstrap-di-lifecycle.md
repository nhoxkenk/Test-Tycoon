# Bootstrap Root, Reflection Factory và lifecycle

[← Mục lục](README.md)

**Trạng thái:** đề xuất để phân tích và thảo luận. Bootstrap Root và không dùng VContainer đã chốt; cơ chế reflection factory xem [phần 13](13-reflection-factory.md).

## Bài toán

Không dùng VContainer. Bootstrap nhận các reference Unity/scene; factory dùng reflection để tạo và nối class C# thường. Dependency của logic vẫn được cấp từ bên ngoài qua constructor.

## Thiết kế đang đề xuất

GameBootstrap giữ scene/config/view references và cấp chúng cho factory. Factory khám phá type hợp lệ, tạo graph C# bằng constructor, giữ ownership; Bootstrap bind MonoBehaviour bằng Initialize tường minh rồi mới bật simulation.

```text
Validate config → Load state → Quét/kiểm tra graph → Tạo services bằng factory
→ Bind views → Spawn actors → Start tick
```
Teardown dừng tick trước, dispose presenter/actor rồi để factory giải phóng service do nó sở hữu. Factory nghiệp vụ chịu trách nhiệm actor mà nó spawn. Không truyền Bootstrap hoặc reflection factory vào gameplay để gọi Resolve tùy ý.

### Khi nào phải sửa Bootstrap?

Bootstrap là entry point và cấp dependency từ scene; factory tạo các service C# đã được khám phá. **Thêm một class không đồng nghĩa phải sửa Bootstrap**:

| Thay đổi | Có sửa Bootstrap? | Lý do |
|---|---|---|
| Thêm một loại tiền mới vào `CurrencyId` | Không, nếu chỉ là số dư trong Wallet hiện có | Wallet xử lý số dư theo currency |
| Thêm giá upgrade bằng gem | Thường không | ProgressionService nhận cost data và Wallet đã có |
| Thêm state của worker | Không | WorkerFactory/Controller tạo state của actor |
| Thêm class nội bộ cho thuật toán profit | Không | Được class sở hữu nó tạo/sử dụng |
| Thêm service scene mới | Tùy cấu hình factory | Nếu type được quét và lifetime rõ thì factory tự tạo; nếu phải chọn implementation hoặc cấp dependency từ scene thì composition cập nhật |
| Thêm giá trị gem khởi đầu trong Inspector | Có ở config/binding | Đây là dữ liệu khởi tạo mới, không phải dependency mới |

Bootstrap chỉ thay đổi khi cần cấp reference/config mới từ Unity hoặc thay đổi lựa chọn implementation. Factory chịu trách nhiệm mapping type, constructor và lifetime; đây là một DI container nhỏ do dự án sở hữu, không nên giấu việc chọn implementation bằng thứ tự quét. Chi tiết ở [phần 13](13-reflection-factory.md).

## Phương án và trade-off

Một MonoBehaviour root đủ cho một scene. Factory có thể đặt ở Bootstrap assembly để feature module không phụ thuộc nó. AppBootstrap xuyên scene chỉ hữu ích khi thật sự có nhiều scene và service sống lâu hơn scene.

## Điểm cần thảo luận

- Bootstrap có trực tiếp giữ tất cả service hay giữ một session object?
- Tick tập trung qua runner hay actor tự Update?
- Scene reload có reset hoàn toàn session không?

## Điều kiện cần giữ

Awake/OnEnable không dùng dependency chưa initialize. Cleanup gọi lặp an toàn; startup lỗi giữa chừng dọn được phần đã tạo. Không static Instance/GetService.

## Liên quan

Xem [chủ đề liên quan](01-module-boundaries.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.
