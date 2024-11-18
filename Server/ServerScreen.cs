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
        private TcpListener listener; // Đối tượng TcpListener để lắng nghe kết nối từ client
        private bool isRunning; // Biến kiểm soát trạng thái server
        private Dictionary<string, TcpClient> connectedClients = new Dictionary<string, TcpClient>(); // Danh sách người dùng và kết nối của họ

        public event Action<string> OnLogMessage; // Sự kiện ghi lại log cho giao diện

        public ServerScreen()
        {
            InitializeComponent();
            OnLogMessage += LogMessage; // Gắn sự kiện log để ghi vào giao diện
        }

        private void ServerScreen_Load(object sender, EventArgs e)
        {
            StartServer(); // Bắt đầu server khi form được tải
        }

        private void ServerScreen_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopServer(); // Dừng server khi form đóng
        }

        private void StartServer()
        {
            // Khởi tạo server chỉ lắng nghe trên localhost tại cổng 5000
            listener = new TcpListener(IPAddress.Any, 5000);
            listener.Start(); // Bắt đầu lắng nghe
            isRunning = true; // Đặt trạng thái server là đang chạy
            OnLogMessage?.Invoke("Đã khởi động server...");

            // Chạy nhiệm vụ để chấp nhận các kết nối từ client
            Task.Run(() =>
            {
                while (isRunning)
                {
                    try
                    {
                        // Chấp nhận kết nối từ client
                        TcpClient client = listener.AcceptTcpClient();
                        // Xử lý client trong một nhiệm vụ riêng biệt
                        Task.Run(() => HandleClient(client));
                    }
                    catch (Exception ex)
                    {
                        OnLogMessage?.Invoke($"Lỗi khi chấp nhận client: {ex.Message}");
                    }
                }
            });
        }

        private void StopServer()
        {
            isRunning = false; // Đặt trạng thái server là dừng
            listener?.Stop(); // Dừng listener
            OnLogMessage?.Invoke("Server đã ngừng hoạt động.");
        }

        private void HandleClient(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream(); // Lấy luồng mạng từ client
                byte[] buffer = new byte[1024]; // Bộ đệm để lưu dữ liệu nhận được
                int byteCount;

                while ((byteCount = stream.Read(buffer, 0, buffer.Length)) > 0) // Đọc dữ liệu từ client
                {
                    string data = Encoding.ASCII.GetString(buffer, 0, byteCount).Trim(); // Chuyển đổi dữ liệu thành chuỗi
                    OnLogMessage?.Invoke($"Tin nhắn nhận được: {data}");

                    if (data.StartsWith("LOGIN:")) // Xử lý đăng nhập
                    {
                        string username = data.Substring(6); // Lấy tên người dùng
                        connectedClients[username] = client; // Thêm vào danh sách client đã kết nối
                        BroadcastUserList(); // Gửi danh sách người dùng cho tất cả client
                        OnLogMessage?.Invoke($"Người dùng {username} đã đăng nhập.");
                    }
                    else if (data.StartsWith("LOGOUT:")) // Xử lý logout
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
                client.Close(); // Đóng kết nối khi xử lý xong
            }
        }

        private void HandlePrivateMessage(string data, TcpClient sender)
        {
            // Tách dữ liệu thành các phần để xử lý tin nhắn riêng
            var parts = data.Split(new[] { ':' }, 3);
            if (parts.Length == 3)
            {
                string recipient = parts[1]; // Người nhận
                string message = parts[2]; // Nội dung tin nhắn

                if (connectedClients.ContainsKey(recipient)) // Kiểm tra nếu người nhận đang online
                {
                    // Gửi tin nhắn tới người nhận
                    SendMessage(connectedClients[recipient], $"{GetUsername(sender)}: {message}");
                    OnLogMessage?.Invoke($"Tin nhắn từ {GetUsername(sender)} đến {recipient}: {message}");
                }
                else
                {
                    // Thông báo nếu người nhận không khả dụng
                    SendMessage(sender, "Người nhận không khả dụng.");
                }
            }
        }

        private void HandleLogout(TcpClient client)
        {
            // Lấy tên người dùng từ client
            string username = GetUsername(client);
            if (username != null)
            {
                // Xóa khỏi danh sách client đã kết nối
                connectedClients.Remove(username);
                BroadcastUserList(); // Cập nhật danh sách người dùng
                OnLogMessage?.Invoke($"Người dùng {username} đã thoát.");
            }
        }

        private void SendMessage(TcpClient client, string message)
        {
            try
            {
                if (client.Connected) // Kiểm tra kết nối có hoạt động không
                {
                    byte[] data = Encoding.ASCII.GetBytes(message); // Chuyển đổi tin nhắn thành byte
                    client.GetStream().Write(data, 0, data.Length); // Gửi tin nhắn qua luồng mạng
                }
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"Lỗi khi gửi tin nhắn: {ex.Message}");
            }
        }

        private void BroadcastUserList()
        {
            // Tạo danh sách người dùng dưới dạng chuỗi
            string userList = "USERLIST:" + string.Join(",", connectedClients.Keys);
            foreach (var client in connectedClients.Values) // Gửi danh sách tới tất cả client
            {
                SendMessage(client, userList);
            }
        }

        private string GetUsername(TcpClient client)
        {
            // Tìm tên người dùng dựa trên TcpClient
            foreach (var entry in connectedClients)
            {
                if (entry.Value == client) return entry.Key;
            }
            return null;
        }

        private void LogMessage(string message)
        {
            // Ghi log tin nhắn vào giao diện
            if (InvokeRequired)
            {
                Invoke(new Action<string>(LogMessage), message); // Đảm bảo thực hiện trong luồng giao diện chính
            }
            else
            {
                txtMessageLog.AppendText($"{message}{Environment.NewLine}");
            }
        }
    }
}
