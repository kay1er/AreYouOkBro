using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatServer
{
    public partial class ServerScreen : Form
    {
        private TcpListener listener; // Khai báo TcpListener để lắng nghe kết nối từ client
        private bool isRunning; // Biến kiểm tra trạng thái server (đang chạy hay không)
        private Dictionary<string, TcpClient> connectedClients = new Dictionary<string, TcpClient>(); // Lưu trữ các client đã kết nối

        public event Action<string> OnLogMessage; // Sự kiện ghi lại nhật ký hoạt động của server

        public ServerScreen()
        {
            InitializeComponent(); // Khởi tạo các thành phần trên giao diện
            OnLogMessage += LogMessage; // Đăng ký sự kiện ghi nhật ký
        }

        private void ServerScreen_Load(object sender, EventArgs e)
        {
            StartServer(); // Khi form được load, khởi động server
        }

        private void ServerScreen_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopServer(); // Dừng server khi form bị đóng
        }

        // Hàm khởi động server, bắt đầu lắng nghe kết nối từ các client
        private void StartServer()
        {
            listener = new TcpListener(IPAddress.Any, 8888); // Lắng nghe kết nối từ bất kỳ địa chỉ IP nào trên cổng 8888
            listener.Start(); // Bắt đầu quá trình lắng nghe
            isRunning = true; // Đánh dấu rằng server đang hoạt động
            OnLogMessage?.Invoke("Server started on local network..."); // Ghi nhật ký khi server khởi động thành công

            Task.Run(() =>
            {
                while (isRunning) // Tiếp tục lắng nghe kết nối nếu server vẫn còn chạy
                {
                    try
                    {
                        TcpClient client = listener.AcceptTcpClient(); // Chấp nhận kết nối từ client
                        Task.Run(() => HandleClient(client)); // Xử lý client trong một task riêng biệt
                    }
                    catch (Exception ex)
                    {
                        OnLogMessage?.Invoke($"Error accepting client: {ex.Message}"); // Ghi nhật ký khi có lỗi khi nhận kết nối
                    }
                }
            });
        }

        // Hàm dừng server
        private void StopServer()
        {
            isRunning = false; // Dừng server
            listener?.Stop(); // Dừng TcpListener
            OnLogMessage?.Invoke("Server stopped."); // Ghi nhật ký khi server dừng
        }

        // Xử lý client khi có kết nối
        private void HandleClient(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream(); // Lấy luồng dữ liệu mạng
                byte[] buffer = new byte[1024]; // Khai báo bộ đệm để nhận dữ liệu

                while (client.Connected) // Tiếp tục xử lý khi client vẫn còn kết nối
                {
                    int byteCount = stream.Read(buffer, 0, buffer.Length); // Đọc dữ liệu từ client
                    if (byteCount == 0)
                    {
                        // Nếu không nhận được dữ liệu (client ngắt kết nối)
                        break;
                    }

                    string data = Encoding.ASCII.GetString(buffer, 0, byteCount).Trim(); // Chuyển đổi dữ liệu byte thành chuỗi
                    OnLogMessage?.Invoke($"Received data from client: {data}"); // Ghi nhật ký khi nhận dữ liệu từ client

                    // Phân tích dữ liệu và thực hiện các hành động tương ứng
                    if (data.StartsWith("LOGIN:"))
                    {
                        HandleLogin(client, data); // Xử lý đăng nhập
                    }
                    else if (data.StartsWith("REGISTER:"))
                    {
                        HandleRegister(client, data); // Xử lý đăng ký
                    }
                    else if (data.StartsWith("LOGOUT:"))
                    {
                        HandleLogout(client); // Xử lý đăng xuất
                        break; // Dừng vòng lặp khi client đăng xuất
                    }
                    else
                    {
                        BroadcastMessage(data, client); // Phát tin nhắn cho tất cả client khác
                    }
                }
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"Error handling client: {ex.Message}"); // Ghi nhật ký khi có lỗi khi xử lý client
            }
            finally
            {
                client.Close(); // Đóng kết nối với client sau khi xử lý xong
            }
        }

        // Xử lý đăng nhập từ client
        private void HandleLogin(TcpClient client, string data)
        {
            string[] parts = data.Split(':'); // Tách dữ liệu theo dấu ':'

            if (parts.Length < 2 || parts.Length > 3)
            {
                SendMessage(client, "Invalid login format. Please use the format: LOGIN:username:password (password optional)");
                OnLogMessage?.Invoke($"Invalid login format: {data}"); // Ghi nhật ký khi định dạng đăng nhập không hợp lệ
                return;
            }

            string username = parts[1]; // Lấy tên người dùng
            string password = parts.Length == 3 ? parts[2] : null; // Lấy mật khẩu nếu có

            if (AuthenticateUser(username, password))
            {
                connectedClients[username] = client; // Thêm client vào danh sách kết nối
                SendMessage(client, "Login Success"); // Gửi thông báo đăng nhập thành công
                OnLogMessage?.Invoke($"User {username} logged in successfully."); // Ghi nhật ký khi đăng nhập thành công
                BroadcastUserList(); // Cập nhật danh sách người dùng online
            }
            else
            {
                SendMessage(client, "Login Failed"); // Gửi thông báo đăng nhập thất bại
                OnLogMessage?.Invoke($"Login failed for user {username}."); // Ghi nhật ký khi đăng nhập thất bại
            }
        }

        // Xử lý đăng ký người dùng
        private void HandleRegister(TcpClient client, string data)
        {
            string[] parts = data.Split(':');
            string username = parts[1]; // Lấy tên người dùng
            string password = parts[2]; // Lấy mật khẩu

            if (RegisterUser(username, password))
            {
                SendMessage(client, "Register Success"); // Gửi thông báo đăng ký thành công
                OnLogMessage?.Invoke($"User {username} registered successfully."); // Ghi nhật ký khi đăng ký thành công
            }
            else
            {
                SendMessage(client, "Register Failed"); // Gửi thông báo đăng ký thất bại
                OnLogMessage?.Invoke($"Registration failed for user {username}."); // Ghi nhật ký khi đăng ký thất bại
            }
        }

        // Xử lý đăng xuất người dùng
        private void HandleLogout(TcpClient client)
        {
            string username = GetUsername(client); // Lấy tên người dùng từ kết nối
            if (username != null)
            {
                connectedClients.Remove(username); // Xóa client khỏi danh sách kết nối
                BroadcastUserList(); // Cập nhật lại danh sách người dùng online
                OnLogMessage?.Invoke($"User {username} logged out."); // Ghi nhật ký khi người dùng đăng xuất
            }
        }

        // Xác thực người dùng trong cơ sở dữ liệu
        private bool AuthenticateUser(string username, string password)
        {
            string connectionString = "Server=kay1er;Database=UserData;Trusted_Connection=True"; // Chuỗi kết nối cơ sở dữ liệu
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open(); // Mở kết nối tới cơ sở dữ liệu
                OnLogMessage?.Invoke("Connected to database for authentication."); // Ghi nhật ký khi kết nối cơ sở dữ liệu thành công

                string query;
                SqlCommand cmd;

                // Kiểm tra nếu mật khẩu không có, chỉ kiểm tra tên người dùng
                if (string.IsNullOrEmpty(password))
                {
                    query = "SELECT COUNT(1) FROM Users WHERE Username = @username";
                    cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@username", username);
                }
                else
                {
                    query = "SELECT COUNT(1) FROM Users WHERE Username = @username AND Password = @password";
                    cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);
                }

                int result = (int)cmd.ExecuteScalar(); // Thực thi truy vấn và nhận kết quả
                OnLogMessage?.Invoke($"Authentication result: {result}"); // Ghi nhật ký kết quả kiểm tra đăng nhập

                return result == 1; // Nếu kết quả là 1, người dùng tồn tại và đăng nhập thành công
            }
        }

        // Đăng ký người dùng mới vào cơ sở dữ liệu
        private bool RegisterUser(string username, string password)
        {
            string connectionString = "Server=kay1er;Database=UserData;Trusted_Connection=True"; // Chuỗi kết nối cơ sở dữ liệu
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open(); // Mở kết nối
                string query = "INSERT INTO Users (Username, Password) VALUES (@username, @password)"; // Truy vấn thêm người dùng mới vào bảng
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username); // Thêm tên người dùng vào câu lệnh
                    cmd.Parameters.AddWithValue("@password", password); // Thêm mật khẩu vào câu lệnh
                    try
                    {
                        cmd.ExecuteNonQuery(); // Thực thi câu lệnh SQL để thêm người dùng mới
                        return true; // Nếu thêm thành công, trả về true
                    }
                    catch (Exception)
                    {
                        return false; // Nếu có lỗi, trả về false
                    }
                }
            }
        }

        // Phát tin nhắn cho tất cả client
        private void BroadcastMessage(string message, TcpClient senderClient)
        {
            foreach (var client in connectedClients)
            {
                if (client.Value != senderClient)
                {
                    SendMessage(client.Value, message); // Gửi tin nhắn cho tất cả client ngoại trừ người gửi
                }
            }
        }

        // Gửi tin nhắn tới client
        private void SendMessage(TcpClient client, string message)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = Encoding.ASCII.GetBytes(message);
                stream.Write(buffer, 0, buffer.Length); // Gửi dữ liệu
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"Error sending message: {ex.Message}"); // Ghi nhật ký khi có lỗi khi gửi tin nhắn
            }
        }

        // Ghi lại thông báo vào log
        private void LogMessage(string message)
        {
            // Kiểm tra xem có cần phải gọi Invoke để cập nhật trên UI thread hay không
            if (txtMessageLog.InvokeRequired)
            {
                // Nếu cần, sử dụng Invoke để cập nhật nội dung trên UI thread
                txtMessageLog.Invoke(new Action<string>((msg) =>
                {
                    txtMessageLog.AppendText($"{msg}{Environment.NewLine}");
                }), message);
            }
            else
            {
                // Nếu đang ở UI thread, thực hiện ngay
                txtMessageLog.AppendText($"{message}{Environment.NewLine}");
            }
        }


        // Cập nhật danh sách người dùng online
        private void BroadcastUserList()
        {
            string userList = "USERLIST:" + string.Join(",", connectedClients.Keys); // Tạo chuỗi danh sách người dùng
            foreach (var client in connectedClients.Values)
            {
                SendMessage(client, userList); // Gửi danh sách người dùng cho tất cả client
            }
        }

        // Lấy tên người dùng từ TcpClient
        private string GetUsername(TcpClient client)
        {
            foreach (var kvp in connectedClients)
            {
                if (kvp.Value == client)
                {
                    return kvp.Key; // Trả về tên người dùng tương ứng
                }
            }
            return null; // Nếu không tìm thấy, trả về null
        }
    }
}
