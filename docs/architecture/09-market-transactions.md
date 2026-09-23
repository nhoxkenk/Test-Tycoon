# Khách hàng, hàng đợi và thanh toán

[← Mục lục](README.md)

**Trạng thái:** đề xuất để phân tích và thảo luận; chưa phải quyết định triển khai. DI thuần và Bootstrap Root là ràng buộc đã chốt. Các lựa chọn khác bên dưới còn có thể sửa.

## Bài toán

Ban đầu có một khách; khách chờ tại quầy, nhận hàng rồi trả tiền. Requirement chưa xác định rõ chu kỳ thay khách và số hàng mỗi lượt mua.

## Thiết kế đang đề xuất

MarketService sở hữu queue/dock reservation và ghép khách với hàng. CustomerPopulation giữ target count; PopulationController dùng CustomerFactory để bù số thiếu, không để ProgressionService gọi factory.

Sale validate batch ownership, dock và customer; consume batch + đánh dấu khách nhận + credit ví trong một đoạn đồng bộ, rồi phát notification.

## Phương án và trade-off

FIFO dễ hiểu và công bằng; ghép theo loại hàng chỉ cần nếu khách có yêu cầu loại quả. Bán cả lô đơn giản; bán từng phần cần remaining quantity/value và quy tắc rounding khác. Tránh quyết định chi tiết này chỉ từ suy đoán.

## Điểm cần thảo luận

- +2 khách nghĩa là thêm hai lượt khách hay tăng population lâu dài?
- Một khách mua cả lô hay số lượng cố định?
- Quầy có bao nhiêu dock giao đồng thời?
- Khách có patience/timeout không?

## Điều kiện cần giữ

Một batch chỉ được credit một lần. Khách chưa tới quầy không nhận hàng. Queue đầy không spawn chồng vô hạn. Target count không tăng lại khi chỉ bù khách đã rời.

## Liên quan

Xem [chủ đề liên quan](10-upgrades-modifiers.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.

