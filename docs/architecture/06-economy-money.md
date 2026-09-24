# Economy, Money và Wallet

[← Mục lục](README.md)

**Trạng thái:** đề xuất để phân tích và thảo luận; chưa phải quyết định triển khai. DI thuần và Bootstrap Root là ràng buộc đã chốt. Các lựa chọn khác bên dưới còn có thể sửa.

## Bài toán

Requirement yêu cầu tính và lưu tiền lớn; ví còn là điểm chung cho build, upgrade và doanh thu.

## Thiết kế đang đề xuất

Đề xuất `Money` bất biến gồm `CurrencyId` và `BigInteger Amount`. CurrencyId hiện có Coin và Gem để kiểm chứng ranh giới đa tiền tệ; điều này chưa có nghĩa gameplay đã sử dụng gem. `Wallet` giữ số dư theo `CurrencyId`, có `GetBalance(currency)`, `TrySpend(Money)` và `Credit(Money)`. Chi gem không làm thay đổi coin. Không cần class Wallet riêng cho mỗi currency.

`Money` không nạp chồng `+`/`-` như phiên bản đầu: công thức giá làm việc với lượng tiền theo quy tắc riêng, rồi tạo `Money(currency, amount)` ở boundary. Cách này ngăn vô tình cộng giá trị thuộc hai loại tiền. Cost/save cần lưu **cả currency lẫn số lượng**; lượng tiền lớn ở dạng chuỗi thập phân, currency ở dạng ID ổn định. UI format K/M/B riêng cho từng currency.

Một cost hiện dùng một currency. Nếu sau này giao dịch yêu cầu đồng thời coin **và** gem, use case phải validate toàn bộ cost trước rồi mới trừ từng số dư; không gọi TrySpend tuần tự mà không có kiểm tra tổng thể.

Wallet không tự biết cây, upgrade hay sale. Use case điều phối transaction; event BalanceChanged chỉ phát sau mutation.

Reflection factory có thể tạo Wallet như một service theo scene từ initial balance được Bootstrap/load cung cấp. `Money(currency, amount)` là value object tạo trực tiếp từ dữ liệu của giao dịch, không quét assembly để tạo từng khoản tiền. Chỉ tạo handler theo `CurrencyId` nếu sau này có hành vi thực sự khác nhau giữa các currency; xem [quy tắc reflection factory](13-reflection-factory.md).

## Phương án và trade-off

BigInteger giữ chính xác nhưng không phải lựa chọn tối ưu mọi game idle. Mantissa/exponent gọn cho số cực lớn nhưng mất chính xác; decimal có giới hạn. Một Wallet dùng dictionary đơn giản cho ít loại tiền; chưa cần hệ thống exchange rate hay giao dịch chéo currency vì requirement chưa có.

## Điểm cần thảo luận

- Tiền có đơn vị lẻ không?
- Làm tròn giá từng đơn vị hay một lần trên tổng lô?
- Wallet public Credit cho mọi service hay giới hạn quyền thay đổi theo use case?
- Gem được kiếm trong gameplay hay chỉ nhận từ nguồn khác? Điều này quyết định module cấp gem, không thay đổi mô hình Wallet.

## Điều kiện cần giữ

Không balance âm; thiếu tiền không mutation. Không đưa số tiền qua float/double. Test vượt Int64 và lưu/đọc lại chính xác.

## Liên quan

Xem [chủ đề liên quan](09-market-transactions.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.
