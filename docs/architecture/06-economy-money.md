# Economy, Money và Wallet

[← Mục lục](README.md)

**Trạng thái:** lõi Economy đã triển khai và kiểm tra bằng EditMode. DI thuần, Bootstrap Root, tiền chỉ có đơn vị nguyên và làm tròn xuống một lần khi chốt cả lô đã được chốt. Save adapter, giao dịch build/upgrade/sale và UI thuộc các phần sau.

## Bài toán

Requirement yêu cầu tính và lưu tiền lớn; ví còn là điểm chung cho build, upgrade và doanh thu.

## Quy tắc nghiệp vụ đã chốt

- Tiền chỉ có đơn vị nguyên; không có phần lẻ trong `Money`, `Wallet` hoặc dữ liệu lưu.
- Mỗi lượt thu hoạch tạo một lô hàng với **một `SaleValue`**. Mọi phần trăm lợi nhuận áp dụng lên giá trị lô bằng phép tính chính xác, rồi **làm tròn xuống đúng một lần khi chốt giá lô** tại thời điểm hoàn tất thu hoạch. Với 109 coin và hai hệ số 110/100, kết quả là `floor(109 × 110 × 110 / 10000) = 131` coin.
- Số quả nhìn thấy chỉ là biểu diễn hình ảnh, không chia `SaleValue` thành giá hoặc giao dịch cho từng quả. Cashout thành công ghi có toàn bộ `SaleValue` một lần.

## Thiết kế đang đề xuất

`Money` bất biến gồm `CurrencyId` và `BigInteger Amount`. CurrencyId hiện có Coin và Gem để kiểm chứng ranh giới đa tiền tệ; điều này chưa có nghĩa gameplay đã sử dụng gem. `Wallet` giữ số dư theo `CurrencyId`, nhận một hoặc nhiều số dư ban đầu, có `GetBalance(currency)`, `TrySpend(Money)` và `Credit(Money)`. Chi gem không làm thay đổi coin. Không cần class Wallet riêng cho mỗi currency.

`Money` không nạp chồng `+`/`-` như phiên bản đầu: công thức giá làm việc với lượng tiền theo quy tắc riêng, rồi tạo `Money(currency, amount)` ở boundary. Cách này ngăn vô tình cộng giá trị thuộc hai loại tiền. `MoneyMath.FloorAfterRatios` nhân các hệ số dạng phân số `MoneyRatio` bằng `BigInteger`, sau đó chia lấy phần nguyên **một lần**; Farming gọi nó khi chốt `SaleValue` sau khi đã xác định công thức modifier. Không chuyển tiền qua `float`/`double`. Cost/save cần lưu **cả currency lẫn số lượng**; `Wallet.GetBalances()` trả bản sao số dư để save adapter dùng, `CurrencyIdCodec` cấp ID ổn định `coin`/`gem`, còn amount dùng chuỗi thập phân. Chưa có adapter ghi file ở phần này. UI format K/M/B riêng cho từng currency ở phần Presentation.

Một cost hiện dùng một currency. Nếu sau này giao dịch yêu cầu đồng thời coin **và** gem, use case phải validate toàn bộ cost trước rồi mới trừ từng số dư; không gọi TrySpend tuần tự mà không có kiểm tra tổng thể.

Wallet không tự biết cây, upgrade hay sale. Use case điều phối transaction; `BalanceChanged` chỉ phát sau khi số dư thực sự thay đổi, không phát khi thiếu tiền hoặc giao dịch giá trị 0. Composition Root chỉ cấp `IWalletTransactions` cho use case giao dịch và cấp `IWalletReader` cho UI/consumer chỉ đọc. `Wallet` là implementation chung của hai contract; phương thức ghi được implement tường minh qua `IWalletTransactions`, actor/state không nhận contract ghi.

Reflection factory có thể tạo Wallet như một service theo scene từ initial balance được Bootstrap/load cung cấp. `Money(currency, amount)` là value object tạo trực tiếp từ dữ liệu của giao dịch, không quét assembly để tạo từng khoản tiền. Chỉ tạo handler theo `CurrencyId` nếu sau này có hành vi thực sự khác nhau giữa các currency; xem [quy tắc reflection factory](13-reflection-factory.md).

## Phương án và trade-off

BigInteger giữ chính xác nhưng không phải lựa chọn tối ưu mọi game idle. Mantissa/exponent gọn cho số cực lớn nhưng mất chính xác; decimal có giới hạn. Một Wallet dùng dictionary đơn giản cho ít loại tiền; chưa cần hệ thống exchange rate hay giao dịch chéo currency vì requirement chưa có.

## Điểm cần thảo luận

- Gem được kiếm trong gameplay hay chỉ nhận từ nguồn khác? Điều này quyết định module cấp gem, không thay đổi mô hình Wallet.

## Điều kiện cần giữ

Không balance âm; thiếu tiền không mutation. Không đưa số tiền qua float/double. Test vượt Int64, snapshot/parse lại chính xác và làm tròn đúng một lần cho nhiều hệ số. Save adapter ở phần 12 phải dùng `CurrencyIdCodec` cùng amount thập phân, không ghi số thứ tự enum.

## Liên quan

Xem [chủ đề liên quan](09-market-transactions.md). Đổi contract liên quan cần cập nhật cả hai tài liệu; số thứ tự là thứ tự đọc, không phải lệnh triển khai.
