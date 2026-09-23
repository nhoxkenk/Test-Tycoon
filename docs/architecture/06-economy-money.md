# Economy, Money và Wallet

[← Mục lục](README.md)

**Trạng thái:** đề xuất để phân tích và thảo luận; chưa phải quyết định triển khai. DI thuần và Bootstrap Root là ràng buộc đã chốt. Các lựa chọn khác bên dưới còn có thể sửa.

## Bài toán

Requirement yêu cầu tính và lưu tiền lớn; ví còn là điểm chung cho build, upgrade và doanh thu.

## Thiết kế đang đề xuất

Đề xuất Money bất biến dùng BigInteger theo coin nguyên; Wallet là owner balance với TrySpend/Credit. Cost và save dùng chuỗi thập phân. Tỷ lệ lợi nhuận dùng số nguyên/rational; UI format K/M/B riêng.

Wallet không tự biết cây, upgrade hay sale. Use case điều phối transaction; event BalanceChanged chỉ phát sau mutation.

## Phương án và trade-off

BigInteger giữ chính xác nhưng không phải lựa chọn tối ưu mọi game idle. Mantissa/exponent gọn cho số cực lớn nhưng mất chính xác; decimal có giới hạn. Chọn dựa trên quy mô và kỳ vọng hiển thị, không chỉ tên 'tiền lớn'.

## Điểm cần thảo luận

- Tiền có đơn vị lẻ không?
- Làm tròn giá từng đơn vị hay một lần trên tổng lô?
- Wallet public Credit cho mọi service hay giới hạn quyền thay đổi theo use case?

## Điều kiện cần giữ

Không balance âm; thiếu tiền không mutation. Không đưa số tiền qua float/double. Test vượt Int64 và lưu/đọc lại chính xác.

## Liên quan

Xem [chủ đề liên quan](09-market-transactions.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.

