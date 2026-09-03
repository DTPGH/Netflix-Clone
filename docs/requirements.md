# Đặc tả yêu cầu dự án Netflix Clone

> **Lưu ý:** Đây là dự án mô phỏng một số trải nghiệm cốt lõi của Netflix để vừa học và xây dựng portfolio, không nhằm sao chép toàn bộ Netflix.

---

## 1. Mục tiêu dự án

Xây dựng một ứng dụng web xem phim theo mô hình gần giống Netflix với các chức năng cốt lõi:

- Đăng ký, đăng nhập và quản lý tài khoản.
- Đăng ký gói cước sử dụng gần giống Netflix (giả lập thanh toán, không thanh toán thật).
- Một tài khoản có nhiều hồ sơ xem phim (Profile).
- Cá nhân hóa trải nghiệm theo từng Profile.
- Duyệt, tìm kiếm, xem chi tiết và phát video.
- My List (danh sách phim yêu thích), Rating, lịch sử xem và Continue Watching.
- Recommendation cơ bản theo Profile.
- Quản trị nội dung, người dùng, báo cáo lượt xem và ghi nhật ký hành động khóa/mở khóa tài khoản người dùng.

Mục tiêu học tập chính của dự án:

- Sử dụng ASP.NET Core Web API (Swagger xem nhanh các API) và Blazor trên .NET 10.
- Xây dựng dự án theo Clean Architecture đúng nguyên tắc; chỉ áp dụng Design Pattern, Unit of Work, SignalR, AutoMapper, Redis khi có use case phù hợp.
- Phân tích Use Case, Business Rule và Domain Model trước khi code.
- Authentication/Authorization và resource ownership.
- EF Core + SQL Server.
- Docker, deployment và tài liệu hóa.
- Quy trình phát triển có AI hỗ trợ nhưng vẫn giữ con người là người phân tích, quyết định và review.

---

## 2. Yêu cầu kỹ thuật

- **Backend:** ASP.NET Core Web API triển khai theo Clean Architecture.
- **Frontend:** ASP.NET Blazor.
- **Database:** SQL Server, sử dụng Entity Framework Core.
- **Authentication:** JWT + Authorization theo Role (`User`, `Admin`).
- **Password hashing:** Bcrypt hoặc thư viện tương đương.
- **Profile PIN:** Nếu được thiết lập thì phải lưu dưới dạng hash, không lưu plain text.
- **Media streaming:** Mô phỏng; chưa cần xây dựng hạ tầng streaming như Netflix, chỉ cần tích hợp video player.
- **Triển khai:** Docker hoặc Azure.

---

## 3. Yêu cầu bảo mật

- JWT Token lưu ở HTTP Only cookie hoặc local Storage; chiến lược lưu token cụ thể sẽ được chốt ở phase triển khai Authentication.
- Mã hóa mật khẩu bằng Bcrypt hoặc thư viện tương đương.
- Email confirmation token, password reset token và refresh token không lưu raw token trong database; chỉ lưu hash và thời hạn sử dụng khi phù hợp.
- Phân quyền rõ ràng giữa `User` và `Admin`.
- Input validation và chống SQL Injection bằng EF Core/parameterized query.
- Có cơ chế khóa tạm thời khi đăng nhập sai nhiều lần và khóa/mở khóa thủ công bởi Admin.

---

## 4. Yêu cầu đầu ra (Deliverables)

- Source code đầy đủ trên GitHub kèm `README.md` hướng dẫn cài đặt.
- Database mẫu: SQL script, seed data hoặc `.bacpac` nếu phù hợp.
- Video demo 5–10 phút.
- Tài liệu báo cáo PDF hoặc DOCX trình bày:
  - Mô tả hệ thống.
  - Thiết kế ERD.
  - Phân tích Use Case.
  - Kiến trúc tổng quan.
  - Phân chia công việc theo giai đoạn từng tuần.
  - Các quyết định thiết kế quan trọng.
  - Hướng phát triển tiếp theo.

---

## 5. Yêu cầu chức năng MVP đã chốt

### 5.1. Tài khoản, Authentication và Authorization

- Người dùng có thể đăng ký tài khoản bằng email và mật khẩu.
- Sau khi đăng ký, hệ thống hỗ trợ xác thực email bằng token có thời hạn; token chỉ được lưu dưới dạng hash.
- Người dùng có thể đăng nhập bằng email và mật khẩu; ghi nhận thời điểm đăng nhập thành công gần nhất.
- Khi đăng nhập sai liên tiếp vượt ngưỡng cho phép, tài khoản bị khóa tạm thời đến `LockoutEnd`; cơ chế này độc lập với việc Admin khóa tài khoản thủ công.
- Người dùng có thể thực hiện quên mật khẩu và đặt lại mật khẩu bằng token một lần có thời hạn; token chỉ được lưu dưới dạng hash.
- Hệ thống sử dụng JWT Access Token + Refresh Token; Refresh Token được rotate khi refresh và có thể truy vết token thay thế.
- Hệ thống chỉ sử dụng Role-based Authorization với hai role MVP: `User` và `Admin`.

### 5.2. Manage Devices

- Khi đăng nhập, hệ thống nhận diện hoặc đăng ký thiết bị/browser hiện tại và gắn Refresh Token với thiết bị đó.
- Người dùng có thể xem danh sách các thiết bị đã đăng nhập tài khoản, bao gồm tên thiết bị và thời gian hoạt động gần nhất.
- Người dùng có thể đăng xuất một thiết bị từ xa; thiết bị được đánh dấu revoked và toàn bộ Refresh Token còn hiệu lực của thiết bị đó bị revoke.
- Trong MVP, Access Token đã phát trước khi thiết bị bị revoke có thể tiếp tục hợp lệ cho đến khi hết hạn; Access Token nên có thời gian sống ngắn.

### 5.3. Gói cước, Subscription và thanh toán giả lập

- Người dùng có thể xem các gói cước đang hoạt động và đăng ký một gói.
- Thanh toán chỉ được mô phỏng; mỗi lần thanh toán tạo một `PaymentTransaction` có mã giao dịch duy nhất và trạng thái `Pending`, `Succeeded` hoặc `Failed`.
- Thanh toán thành công sẽ kích hoạt hoặc gia hạn Subscription tương ứng.
- Một tài khoản có thể có nhiều Subscription trong lịch sử nhưng tại một thời điểm chỉ có tối đa một Subscription `Active`.
- Số Profile tối đa của tài khoản được xác định bởi `Plan.MaxProfiles`.
- Số lượt xem đồng thời tối đa được xác định bởi `Plan.MaxConcurrentStreams`.
- `Plan.MaxQuality` được lưu để phục vụ mở rộng về sau; MVP chưa cần nhiều video asset theo từng chất lượng.

### 5.4. Profile

- Một tài khoản có thể tạo, cập nhật, lựa chọn và xóa mềm các Profile trong giới hạn của gói cước đang hoạt động.
- Profile có thể là Kids Profile; nội dung được lọc theo `MaturityLevel` và `Movie.MinAge`.
- Profile có thể đặt PIN; PIN phải được lưu dưới dạng hash.
- Profile mới thực hiện onboarding bằng cách chọn một số Movie yêu thích để làm tín hiệu recommendation ban đầu.
- Profile đã bị xóa mềm không được sử dụng cho các thao tác xem phim hoặc cá nhân hóa mới, nhưng dữ liệu lịch sử vẫn được giữ lại.

### 5.5. Movie Catalog, Genre và Movie Detail

- Người dùng có thể duyệt danh sách Movie, tìm kiếm, lọc theo Genre và xem chi tiết Movie.
- Movie Detail hiển thị thông tin cơ bản, Genre, đạo diễn và diễn viên thông qua `People` + `MovieCredits`.
- `MovieCredits` hỗ trợ hai loại credit trong MVP: `Actor` và `Director`; Actor có thể có `CharacterName`.
- Movie có thể được đánh dấu `Featured`, `Available` hoặc soft-deleted.
- Movie soft-deleted không xuất hiện cho người dùng thông thường; dữ liệu lịch sử liên quan vẫn được giữ.
- `IsAvailable` dùng để tạm thời cho phép/không cho phép phát nội dung và có ý nghĩa khác với soft delete.

### 5.6. My List và Rating

- Mỗi Profile có My List riêng và có thể Add/Remove Movie.
- Một Movie chỉ xuất hiện tối đa một lần trong My List của cùng một Profile.
- Mỗi Profile chỉ có một Rating hiện tại cho một Movie; thay đổi Rating sẽ cập nhật record hiện có.
- Rating MVP gồm: `NotForMe`, `Like`, `Love`.

### 5.7. Watch History và Continue Watching

- Hệ thống duy trì một trạng thái xem hiện tại cho mỗi cặp `Profile + Movie`.
- `LastPositionSeconds` và `LastWatchedAt` được cập nhật trong quá trình xem để hỗ trợ Continue Watching.
- MVP đánh dấu Movie là hoàn thành khi tiến độ đạt tối thiểu 90% thời lượng; khi hoàn thành lưu `CompletedAt`.
- Người dùng có thể ẩn Movie khỏi Continue Watching mà không xóa lịch sử xem; trạng thái này dùng `IsHidden`.
- Continue Watching chỉ hiển thị các Movie chưa hoàn thành, chưa bị ẩn và vẫn hợp lệ đối với Profile hiện tại.

### 5.8. Playback và Viewing Sessions

- Khi bắt đầu phát Movie, hệ thống tạo một `ViewingSession` gắn với Profile, Movie và Device hiện tại.
- Pause/Resume trong cùng một lần xem không tạo session mới.
- `EndedAt = NULL` biểu thị session đang active.
- Khi kết thúc session, `EndReason` thuộc một trong các giá trị MVP: `Finished`, `Closed`, `Timeout`.
- Hệ thống ghi nhận `WatchedSeconds`; một lượt xem được tính là `Qualified View` khi xem thực tế tối thiểu 120 giây.
- `ViewingSession.DeviceId` chỉ dùng để biết playback xảy ra trên thiết bị nào; giới hạn concurrent stream được tính bằng số `ViewingSessions` đang active, không phải số Devices.
- Trước khi bắt đầu playback, hệ thống đếm các active Viewing Sessions của toàn bộ Profile thuộc tài khoản và so sánh với `Plan.MaxConcurrentStreams`.
- Backend phải đảm bảo Device và Profile của một ViewingSession thuộc cùng một UserAccount.
- Khi browser crash, mất mạng hoặc không gửi được request kết thúc session, module Playback phải có cơ chế xác định stale session và đóng session bằng `EndReason = Timeout`.
- `Quality` có thể được lưu như thông tin mặc định trong MVP nhưng chưa đại diện cho việc chuyển đổi video asset thật sự.

### 5.9. Recommendation

- Recommendation được cá nhân hóa theo từng Profile.
- MVP có thể sử dụng các tín hiệu hiện có: movie đã chọn khi onboarding, Rating và dữ liệu xem.
- Recommendation phải tôn trọng Kids/Maturity filtering và không đề xuất Movie đã soft-delete hoặc không phù hợp để hiển thị.
- Thuật toán recommendation nâng cao không nằm trong phạm vi MVP.

### 5.10. Admin

- Admin có thể quản lý Movie, Genre và dữ liệu Actor/Director phục vụ Movie Detail.
- Admin có thể xem danh sách tài khoản và khóa/mở khóa tài khoản thủ công.
- Mỗi hành động `AccountLocked` hoặc `AccountUnlocked` phải được ghi vào `AdminActionLogs`, bao gồm Admin thực hiện, tài khoản bị tác động, hành động, lý do và thời gian.
- Admin có thể xem báo cáo lượt xem cơ bản dựa trên `ViewingSessions`, trong đó Qualified View được xác định theo rule của MVP.
- `AdminActionLogs` trong MVP chỉ audit hành động khóa/mở khóa tài khoản; hệ thống audit tổng quát cho Movie, Plan, Role... được để dành cho giai đoạn mở rộng.

---

## 6. Ngoài phạm vi MVP / hướng mở rộng

Các chức năng sau được ghi nhận nhưng chưa triển khai trong MVP để kiểm soát phạm vi:

- Permission-based Authorization (`Permissions`, `RolePermissions`).
- Multi-quality Video Assets và đổi chất lượng video thực tế.
- Subtitle đa ngôn ngữ và chuyển subtitle thực tế.
- Collections/CollectionItems để Admin tự thiết kế các hàng nội dung trên Home.
- Search History và gợi ý tìm kiếm nâng cao.
- Notifications.
- People/MovieCredits mở rộng thêm các loại credit ngoài Actor/Director.
- Audit log tổng quát cho mọi thay đổi quản trị.
- Recommendation nâng cao, Trending Score hoặc cache thống kê bằng Redis khi có nhu cầu thực tế.
- Series / Season / Episode.
- Hạ tầng media streaming thực tế như adaptive bitrate streaming/CDN/DRM.
