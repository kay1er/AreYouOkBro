using System;
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

        public event Action<string> OnLogMessage; // Event to log messages to the UI

        public ServerScreen()
        {
            InitializeComponent();
            // Subscribe to the log message event to display logs in the TextBox
            OnLogMessage += LogMessage;
        }

        // This method starts the server when the form is loaded
        private void ServerScreen_Load(object sender, EventArgs e)
        {
            StartServer();
        }

        // This method stops the server when the form is closing
        private void ServerScreen_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopServer();
        }

        // Method to start the server
        private void StartServer()
        {
            listener = new TcpListener(IPAddress.Any, 5000);
            listener.Start();
            isRunning = true;
            OnLogMessage?.Invoke("Server started...");

            // Handle client connections asynchronously
            Task.Run(() =>
            {
                while (isRunning)
                {
                    try
                    {
                        TcpClient client = listener.AcceptTcpClient();
                        Task.Run(() => HandleClient(client));
                    }
                    catch (Exception ex)
                    {
                        OnLogMessage?.Invoke($"Error accepting client: {ex.Message}");
                    }
                }
            });
        }

        // Method to stop the server
        private void StopServer()
        {
            isRunning = false;
            listener?.Stop();
            OnLogMessage?.Invoke("Server stopped.");
        }

        // Handle the client request for login or register
        private void HandleClient(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int byteCount = stream.Read(buffer, 0, buffer.Length);
                string data = Encoding.ASCII.GetString(buffer, 0, byteCount);

                if (data.StartsWith("LOGIN:"))
                {
                    HandleLogin(client, data);
                }
                else if (data.StartsWith("REGISTER:"))
                {
                    HandleRegister(client, data);
                }

                client.Close();
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"Error handling client: {ex.Message}");
            }
        }

        // Handle login logic
        private void HandleLogin(TcpClient client, string data)
        {
            string[] parts = data.Split(':');
            string username = parts[1];
            string password = parts[2];

            if (AuthenticateUser(username, password))
            {
                SendMessage(client, "Login Success");
                OnLogMessage?.Invoke($"User {username} logged in successfully.");
            }
            else
            {
                SendMessage(client, "Login Failed");
                OnLogMessage?.Invoke($"Login failed for user {username}.");
            }
        }

        // Handle register logic
        private void HandleRegister(TcpClient client, string data)
        {
            string[] parts = data.Split(':');
            string username = parts[1];
            string password = parts[2]; // Mật khẩu thô (plaintext)

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

        // Đăng ký người dùng mà không mã hóa mật khẩu
        private bool RegisterUser(string username, string password)
        {
            string connectionString = "Server=kay1er;Database=UserData;Trusted_Connection=True";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "INSERT INTO Users (Username, Password) VALUES (@username, @password)";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@password", password); // Lưu mật khẩu thô vào cơ sở dữ liệu

                        int result = cmd.ExecuteNonQuery();
                        return result > 0;
                    }
                }
                catch (SqlException ex)
                {
                    OnLogMessage?.Invoke("Error: " + ex.Message);
                    return false;
                }
            }
        }

        // Xác thực người dùng với mật khẩu thô
        private bool AuthenticateUser(string username, string password)
        {
            string connectionString = "Server=kay1er;Database=UserData;Trusted_Connection=True";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT COUNT(1) FROM Users WHERE Username = @username AND Password = @password"; // So sánh mật khẩu thô
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@password", password); // Truyền mật khẩu thô vào câu lệnh SQL

                return (int)cmd.ExecuteScalar() == 1;
            }
        }

        // Gửi thông điệp cho client
        private void SendMessage(TcpClient client, string message)
        {
            NetworkStream stream = client.GetStream();
            byte[] data = Encoding.ASCII.GetBytes(message);
            stream.Write(data, 0, data.Length);
        }

        // Phương thức để ghi log ra UI
        private void LogMessage(string message)
        {
            // Đảm bảo thực thi trên UI thread
            if (InvokeRequired)
            {
                Invoke(new Action<string>(LogMessage), message);
            }
            else
            {
                // Thêm log vào TextBox
                txtMessageLog.AppendText($"{message}{Environment.NewLine}");
            }
        }
    }
}
