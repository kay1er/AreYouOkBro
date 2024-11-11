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
        private TcpListener listener;
        private bool isRunning;
        private Dictionary<string, TcpClient> connectedClients = new Dictionary<string, TcpClient>();

        public event Action<string> OnLogMessage;

        public ServerScreen()
        {
            InitializeComponent();
            OnLogMessage += LogMessage; // Đăng ký sự kiện ghi nhật ký
        }

        private void ServerScreen_Load(object sender, EventArgs e)
        {
            StartServer(); // Khởi động server khi form load
        }

        private void ServerScreen_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopServer(); // Dừng server khi form đóng
        }

        private void StartServer()
        {
            listener = new TcpListener(IPAddress.Any, 5000);
            listener.Start();
            isRunning = true;
            OnLogMessage?.Invoke("Server started...");

            Task.Run(() =>
            {
                while (isRunning)
                {
                    try
                    {
                        TcpClient client = listener.AcceptTcpClient();
                        Task.Run(() => HandleClient(client)); // Xử lý kết nối client
                    }
                    catch (Exception ex)
                    {
                        OnLogMessage?.Invoke($"Error accepting client: {ex.Message}");
                    }
                }
            });
        }

        private void StopServer()
        {
            isRunning = false;
            listener?.Stop();
            OnLogMessage?.Invoke("Server stopped.");
        }

        private void HandleClient(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int byteCount;

                while ((byteCount = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string data = Encoding.ASCII.GetString(buffer, 0, byteCount);
                    OnLogMessage?.Invoke($"Received data from client: {data}"); // Log incoming data

                    if (data.StartsWith("LOGIN:"))
                    {
                        HandleLogin(client, data);
                    }
                    else if (data.StartsWith("REGISTER:"))
                    {
                        HandleRegister(client, data);
                    }
                    else if (data.StartsWith("LOGOUT:"))
                    {
                        HandleLogout(client);
                    }
                    else
                    {
                        BroadcastMessage(data, client); // Broadcast the message to other clients
                    }
                }
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"Error handling client: {ex.Message}");
            }
            finally
            {
                client.Close(); // Close the client connection after handling
            }
        }


        // Xử lý đăng nhập
        private void HandleLogin(TcpClient client, string data)
        {
            string[] parts = data.Split(':');

            // Kiểm tra nếu có ít nhất 2 phần (LOGIN và username), mật khẩu có thể không có
            if (parts.Length < 2 || parts.Length > 3)
            {
                SendMessage(client, "Invalid login format. Please use the format: LOGIN:username:password (password optional)");
                OnLogMessage?.Invoke($"Invalid login format: {data}");
                return;
            }

            string username = parts[1];
            string password = parts.Length == 3 ? parts[2] : null; // Nếu có mật khẩu, lấy mật khẩu, nếu không thì null

            if (AuthenticateUser(username, password))
            {
                connectedClients[username] = client;
                SendMessage(client, "Login Success");
                OnLogMessage?.Invoke($"User {username} logged in successfully.");
                BroadcastUserList();
            }
            else
            {
                SendMessage(client, "Login Failed");
                OnLogMessage?.Invoke($"Login failed for user {username}.");
            }
        }




        // Xử lý đăng ký
        private void HandleRegister(TcpClient client, string data)
        {
            string[] parts = data.Split(':');
            string username = parts[1];
            string password = parts[2];

            if (RegisterUser(username, password))
            {
                SendMessage(client, "Register Success");
                OnLogMessage?.Invoke($"User {username} registered successfully.");
            }
            else
            {
                SendMessage(client, "Register Failed");
                OnLogMessage?.Invoke($"Registration failed for user {username}.");
            }
        }

        // Xử lý đăng xuất
        private void HandleLogout(TcpClient client)
        {
            string username = GetUsername(client);
            if (username != null)
            {
                connectedClients.Remove(username);
                BroadcastUserList(); // Cập nhật danh sách người dùng
                OnLogMessage?.Invoke($"User {username} logged out.");
            }
        }

        // Kiểm tra đăng nhập người dùng
        private bool AuthenticateUser(string username, string password)
        {
            string connectionString = "Server=kay1er;Database=UserData;Trusted_Connection=True";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                OnLogMessage?.Invoke("Connected to database for authentication."); // Kiểm tra kết nối

                string query;
                SqlCommand cmd;

                if (string.IsNullOrEmpty(password)) // Nếu không có mật khẩu, chỉ kiểm tra username
                {
                    query = "SELECT COUNT(1) FROM Users WHERE Username = @username";
                    cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@username", username);
                }
                else // Nếu có mật khẩu, kiểm tra cả username và password
                {
                    query = "SELECT COUNT(1) FROM Users WHERE Username = @username AND Password = @password";
                    cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);
                }

                int result = (int)cmd.ExecuteScalar();
                OnLogMessage?.Invoke($"Authentication result: {result}"); // Kiểm tra kết quả

                return result == 1;
            }
        }


        // Xử lý đăng ký người dùng mới
        private bool RegisterUser(string username, string password)
        {
            string connectionString = "Server=kay1er;Database=UserData;Trusted_Connection=True";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO Users (Username, Password) VALUES (@username, @password)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);

                    int result = cmd.ExecuteNonQuery();
                    return result > 0;
                }
            }
        }

        // Gửi tin nhắn tới client
        private void SendMessage(TcpClient client, string message)
        {
            try
            {
                if (client.Connected)  // Kiểm tra xem client còn kết nối không
                {
                    NetworkStream stream = client.GetStream();
                    byte[] data = Encoding.ASCII.GetBytes(message);
                    stream.Write(data, 0, data.Length);
                }
                else
                {
                    OnLogMessage?.Invoke($"Client {client.Client.RemoteEndPoint} disconnected.");
                }
            }
            catch (ObjectDisposedException)
            {
                OnLogMessage?.Invoke("Tried to send message to a disposed client.");
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"Error sending message: {ex.Message}");
            }
        }


        // Phát tin nhắn tới tất cả các client ngoại trừ client gửi
        // Broadcast a message to all clients except the sender
        // Modify the BroadcastMessage method to handle both private and public messages.
        private void BroadcastMessage(string message, TcpClient sender)
        {
            // Check if the message is a private message by checking the prefix.
            if (message.StartsWith("PRIVATE:"))
            {
                string[] parts = message.Split(new[] { ':' }, 3);
                if (parts.Length == 3)
                {
                    string recipient = parts[1];
                    string privateMessage = parts[2];
                    SendPrivateMessage(privateMessage, sender, recipient);
                    return;
                }
            }

            // Handle public message by broadcasting to all clients except the sender.
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            foreach (var client in connectedClients.Values)
            {
                if (client != sender && client.Connected)
                {
                    try
                    {
                        NetworkStream stream = client.GetStream();
                        stream.Write(buffer, 0, buffer.Length);
                    }
                    catch (Exception ex)
                    {
                        OnLogMessage?.Invoke($"Error sending broadcast message: {ex.Message}");
                    }
                }
            }
            OnLogMessage?.Invoke("Broadcasting message: " + message);
        }

        // Method to send a private message to a specific recipient
        private void SendPrivateMessage(string message, TcpClient sender, string recipient)
        {
            if (connectedClients.TryGetValue(recipient, out TcpClient recipientClient) && recipientClient.Connected)
            {
                string senderName = GetUsername(sender);
                string formattedMessage = $"PRIVATE FROM {senderName}: {message}";
                byte[] buffer = Encoding.UTF8.GetBytes(formattedMessage);

                try
                {
                    NetworkStream stream = recipientClient.GetStream();
                    stream.Write(buffer, 0, buffer.Length);
                    OnLogMessage?.Invoke($"Private message from {senderName} to {recipient}: {message}");
                }
                catch (Exception ex)
                {
                    OnLogMessage?.Invoke($"Error sending private message: {ex.Message}");
                }
            }
            else
            {
                // Notify sender if the recipient is not found or disconnected
                SendMessage(sender, $"User {recipient} is not available.");
                OnLogMessage?.Invoke($"User {recipient} is not available for private messages.");
            }
        }



        // Cập nhật danh sách người dùng online
        private void BroadcastUserList()
        {
            string userList = "USERLIST:" + string.Join(",", connectedClients.Keys);
            List<TcpClient> clientsToRemove = new List<TcpClient>(); // Danh sách các client sẽ bị xóa

            foreach (var client in connectedClients.Values)
            {
                try
                {
                    // Kiểm tra kết nối client trước khi gửi tin nhắn
                    if (client.Connected)
                    {
                        SendMessage(client, userList);
                    }
                    else
                    {
                        clientsToRemove.Add(client); // Thêm client vào danh sách cần xóa
                    }
                }
                catch (Exception ex)
                {
                    OnLogMessage?.Invoke($"Error sending message to client: {ex.Message}");
                }
            }

            // Xóa những client không còn kết nối
            foreach (var client in clientsToRemove)
            {
                string username = GetUsername(client);
                if (username != null)
                {
                    connectedClients.Remove(username);
                }
            }

            OnLogMessage?.Invoke("Updated user list broadcasted.");
        }


        // Lấy tên người dùng từ kết nối
        private string GetUsername(TcpClient client)
        {
            foreach (var entry in connectedClients)
            {
                if (entry.Value == client) return entry.Key;
            }
            return null;
        }

        // Ghi nhật ký vào giao diện
        private void LogMessage(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(LogMessage), message);
            }
            else
            {
                txtMessageLog.AppendText($"{message}{Environment.NewLine}");
            }
        }
    }
}
