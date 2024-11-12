using System;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace ChatApp
{
    public partial class RegisterScreen : Form
    {
        public RegisterScreen()
        {
            InitializeComponent(); // Khởi tạo giao diện và các thành phần trên form
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text; // Lấy tên người dùng từ ô nhập
            string password = txtPassword.Text; // Lấy mật khẩu từ ô nhập

            // Không mã hóa mật khẩu nữa, chỉ sử dụng mật khẩu thô
            string passwordPlaintext = password; // Mật khẩu thô được sử dụng trực tiếp

            // Gửi yêu cầu đăng ký tới server và nhận kết quả
            if (SendRegisterRequest(username, passwordPlaintext))
            {
                MessageBox.Show("Registration successful!"); // Hiển thị thông báo thành công
                this.Close(); // Đóng form đăng ký
            }
            else
            {
                MessageBox.Show("Registration failed. Username may already exist."); // Thông báo lỗi nếu đăng ký thất bại
            }
        }

        private bool SendRegisterRequest(string username, string password)
        {
            try
            {
                // Tạo kết nối TCP tới server
                using (TcpClient client = new TcpClient("172.20.10.2", 8888))
                {
                    NetworkStream stream = client.GetStream(); // Lấy luồng dữ liệu mạng để gửi/nhận thông tin

                    // Tạo thông điệp đăng ký với định dạng "REGISTER:username:password"
                    string message = $"REGISTER:{username}:{password}";
                    byte[] data = Encoding.ASCII.GetBytes(message); // Mã hóa thông điệp thành byte
                    stream.Write(data, 0, data.Length); // Gửi dữ liệu đến server

                    byte[] responseData = new byte[1024]; // Tạo bộ đệm để nhận phản hồi từ server
                    int bytes = stream.Read(responseData, 0, responseData.Length); // Đọc phản hồi từ server
                    string response = Encoding.ASCII.GetString(responseData, 0, bytes); // Chuyển đổi dữ liệu byte thành chuỗi

                    return response == "Register Success"; // Kiểm tra nếu server phản hồi "Register Success"
                }
            }
            catch (Exception ex)
            {
                // Hiển thị thông báo lỗi nếu có sự cố khi kết nối hoặc giao tiếp với server
                MessageBox.Show("Error connecting to server: " + ex.Message);
                return false; // Trả về false nếu gặp lỗi
            }
        }
    }
}
