using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatServer
{
    public partial class ServerScreen : Form
    {
        private TcpListener listener; // Lắng nghe kết nối từ client
        private bool isRunning; // Trạng thái server
        private Dictionary<string, TcpClient> connectedClients = new Dictionary<string, TcpClient>(); // Danh sách người dùng đang kết nối

        public event Action<string> OnLogMessage; // Sự kiện để ghi log lên giao diện

        public ServerScreen()
        {
            InitializeComponent();
            OnLogMessage += LogMessage; // Đăng ký sự kiện ghi log vào giao diện
        }

        // Sự kiện khi form server được tải
        private void ServerScreen_Load(object sender, EventArgs e)
        {
            StartServer(); // Bắt đầu chạy server khi form được mở
        }

        // Sự kiện khi form server bị đóng
        private void ServerScreen_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopServer(); // Dừng server trước khi đóng form
        }

        // Bắt đầu server
        private void StartServer()
        {
            listener = new TcpListener(IPAddress.Any, 5000); // Lắng nghe trên cổng 5000
            listener.Start();
            isRunning = true;
            OnLogMessage?.Invoke("Server đã khởi động...");

            // Chạy một luồng mới để chấp nhận kết nối từ client
            Task.Run(() =>
            {
                while (isRunning)
                {
                    try
                    {
                        TcpClient client = listener.AcceptTcpClient(); // Chấp nhận kết nối từ client
                        Task.Run(() => HandleClient(client)); // Xử lý client trong luồng riêng
                    }
                    catch (Exception ex)
                    {
                        OnLogMessage?.Invoke($"Lỗi khi chấp nhận client: {ex.Message}");
                    }
                }
            });
        }

        // Dừng server
        private void StopServer()
        {
            isRunning = false; // Đặt trạng thái server là dừng
            listener?.Stop(); // Dừng lắng nghe kết nối
            OnLogMessage?.Invoke("Server đã dừng.");
        }

        // Xử lý client
        private void HandleClient(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream(); // Luồng dữ liệu từ client
                byte[] buffer = new byte[1024];
                int byteCount;

                while ((byteCount = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string data = Encoding.ASCII.GetString(buffer, 0, byteCount).Trim(); // Chuyển dữ liệu thành chuỗi
                    OnLogMessage?.Invoke($"Nhận được: {data}");

                    if (data.StartsWith("LOGIN:")) // Xử lý đăng nhập
                    {
                        string username = data.Substring(6); // Lấy tên người dùng từ thông điệp
                        connectedClients[username] = client; // Lưu client vào danh sách
                        BroadcastUserList(); // Gửi danh sách người dùng
                        OnLogMessage?.Invoke($"Người dùng {username} đã đăng nhập.");
                    }
                    else if (data.StartsWith("LOGOUT:")) // Xử lý đăng xuất
                    {
                        HandleLogout(client);
                    }
                    else if (data.StartsWith("PRIVATE:")) // Xử lý tin nhắn riêng tư
                    {
                        HandlePrivateMessage(data, client);
                    }
                }
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"Lỗi khi xử lý client: {ex.Message}");
            }
            finally
            {
                client.Close(); // Đóng kết nối client
            }
        }

        // Xử lý tin nhắn riêng tư
        private void HandlePrivateMessage(string data, TcpClient sender)
        {
            var parts = data.Split(new[] { ':' }, 3); // Tách thông điệp theo định dạng "PRIVATE:recipient:message"
            if (parts.Length == 3)
            {
                string recipient = parts[1]; // Người nhận
                string message = parts[2]; // Nội dung tin nhắn

                if (connectedClients.ContainsKey(recipient)) // Kiểm tra nếu người nhận trực tuyến
                {
                    SendMessage(connectedClients[recipient], $"{GetUsername(sender)}: {message}"); // Gửi tin nhắn đến người nhận
                    OnLogMessage?.Invoke($"Tin nhắn từ {GetUsername(sender)} đến {recipient}: {message}");
                }
                else
                {
                    SendMessage(sender, "Người nhận không trực tuyến."); // Thông báo nếu người nhận không trực tuyến
                }
            }
        }

        // Xử lý đăng xuất
        private void HandleLogout(TcpClient client)
        {
            string username = GetUsername(client); // Lấy tên người dùng từ client
            if (username != null)
            {
                connectedClients.Remove(username); // Xóa client khỏi danh sách
                BroadcastUserList(); // Gửi danh sách người dùng mới
                OnLogMessage?.Invoke($"Người dùng {username} đã đăng xuất.");
            }
        }

        // Gửi tin nhắn tới một client cụ thể
        private void SendMessage(TcpClient client, string message)
        {
            try
            {
                if (client.Connected) // Kiểm tra kết nối
                {
                    byte[] data = Encoding.ASCII.GetBytes(message); // Mã hóa thông điệp
                    client.GetStream().Write(data, 0, data.Length); // Gửi thông điệp qua luồng
                }
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"Lỗi khi gửi tin nhắn: {ex.Message}");
            }
        }

        // Gửi danh sách người dùng trực tuyến tới tất cả client
        private void BroadcastUserList()
        {
            string userList = "USERLIST:" + string.Join(",", connectedClients.Keys); // Tạo danh sách người dùng
            foreach (var client in connectedClients.Values)
            {
                SendMessage(client, userList); // Gửi danh sách tới từng client
            }
        }

        // Lấy tên người dùng tương ứng với một client
        private string GetUsername(TcpClient client)
        {
            foreach (var entry in connectedClients)
            {
                if (entry.Value == client) return entry.Key; // Tìm tên người dùng qua đối tượng TcpClient
            }
            return null;
        }

        // Ghi log lên giao diện
        private void LogMessage(string message)
        {
            if (InvokeRequired) // Nếu không thể cập nhật UI trực tiếp
            {
                Invoke(new Action<string>(LogMessage), message); // Thực hiện trên luồng giao diện
            }
            else
            {
                txtMessageLog.AppendText($"{message}{Environment.NewLine}"); // Hiển thị thông điệp trên ô log
            }
        }
    }
}
