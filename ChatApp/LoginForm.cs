using ChatApp;
using System;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace ChatClient
{
    public partial class LoginForm : Form
    {
        // Hàm khởi tạo Form Login
        public LoginForm()
        {
            InitializeComponent(); // Khởi tạo các thành phần trên form
        }

        // Sự kiện khi nhấn nút Đăng Nhập
        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text; // Lấy tên người dùng từ ô nhập
            string password = txtPassword.Text; // Lấy mật khẩu từ ô nhập

            try
            {
                // Kết nối đến server (địa chỉ IP và cổng)
                using (TcpClient client = new TcpClient("172.20.10.2", 8888))
                {
                    NetworkStream stream = client.GetStream(); // Lấy luồng dữ liệu mạng từ client

                    // Tạo thông điệp đăng nhập với định dạng LOGIN:username:password
                    string message = $"LOGIN:{username}:{password}";
                    byte[] data = Encoding.ASCII.GetBytes(message); // Chuyển đổi thông điệp thành mảng byte
                    stream.Write(data, 0, data.Length); // Gửi thông điệp đến server

                    // Đọc phản hồi từ server
                    byte[] responseData = new byte[1024];
                    int bytes = stream.Read(responseData, 0, responseData.Length);
                    string response = Encoding.ASCII.GetString(responseData, 0, bytes); // Chuyển đổi dữ liệu nhận được thành chuỗi

                    // Kiểm tra phản hồi từ server để xác định đăng nhập thành công hay không
                    if (response == "Login Success")
                    {
                        MessageBox.Show("Login successful!"); // Hiển thị thông báo đăng nhập thành công
                        ChatForm chatForm = new ChatForm(username); // Tạo cửa sổ chat mới với tên người dùng
                        chatForm.Show(); // Hiển thị cửa sổ chat
                        this.Hide(); // Ẩn cửa sổ đăng nhập
                    }
                    else
                    {
                        MessageBox.Show("Login failed."); // Hiển thị thông báo đăng nhập thất bại
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}"); // Hiển thị thông báo lỗi nếu có sự cố khi kết nối tới server
            }
        }

        // Sự kiện khi nhấn nút Đăng ký
        private void button2_Click(object sender, EventArgs e)
        {
            RegisterScreen registerScreen = new RegisterScreen(); // Tạo form đăng ký mới
            registerScreen.Show(); // Hiển thị form đăng ký
        }
    }
}
