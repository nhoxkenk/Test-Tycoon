# Navigation và world queries

[← Mục lục](README.md)

**Trạng thái:** đề xuất để phân tích và thảo luận; chưa phải quyết định triển khai. DI thuần và Bootstrap Root là ràng buộc đã chốt. Các lựa chọn khác bên dưới còn có thể sửa.

## Bài toán

Actor phải tìm đường, tránh/vượt chướng ngại và tới đúng điểm thu hoạch/quầy. AI Navigation đã có trong project.

## Thiết kế đang đề xuất

UnityAdapters quản lý NavMesh, world anchors và ánh xạ destination ID sang vị trí. Simulation gọi IActorNavigation theo ID, nhận Pending/Moving/Arrived/Unreachable. Actors có thể dùng Vector3 ở motor port; Simulation không dùng Unity types.

IWorldQuery trả chi phí đường khả dụng để chọn cây. Tính khi nhận job/replan, không mỗi frame. Đích nằm ở interaction anchor, không tại tâm collider.

## Phương án và trade-off

ID gateway giữ Simulation thuần nhưng thêm bước lookup. Cho gameplay dùng Vector3 trực tiếp đơn giản hơn, song phải thay quyết định noEngineReferences trong kiến trúc. NavMesh phù hợp layout này; chỉ thêm thuật toán khác nếu demo có yêu cầu không đáp ứng được.

## Điểm cần thảo luận

- “Vượt chướng ngại” là đi vòng hay cần nhảy/link traversal?
- Xây cây có làm thay đổi đường đi không?
- Chọn cây gần nhất theo độ dài đường hay thời gian di chuyển?
- Có cần tách motor port và navigation gateway hay một port đã đủ?

## Điều kiện cần giữ

Chưa có path không được coi là Arrived. Target mất/đường không hoàn chỉnh trả lỗi có xử lý. Không khẳng định đường ngắn nhất tuyệt đối trong mọi hình học.

## Liên quan

Xem [chủ đề liên quan](08-worker-harvest-logistics.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.

