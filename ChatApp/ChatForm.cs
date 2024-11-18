using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatClient
{
    public partial class ChatForm : Form
    {
        private TcpClient client; // Đối tượng TcpClient để kết nối tới server
        private NetworkStream stream; // Luồng mạng để gửi và nhận dữ liệu
        private string username; // Tên người dùng hiện tại

        public ChatForm()
        {
            InitializeComponent(); // Khởi tạo các thành phần giao diện
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            username = txtboxAccount.Text.Trim(); // Lấy tên người dùng từ ô nhập liệu

            if (string.IsNullOrEmpty(username)) // Kiểm tra tên người dùng có trống không
            {
                MessageBox.Show("Hãy nhập tên người dùng"); // Hiển thị thông báo nếu tên trống
                return;
            }

            try
            {
                // Kết nối tới server cục bộ (localhost) tại cổng 5000
                client = new TcpClient("192.168.1.99", 5000);
                stream = client.GetStream(); // Lấy luồng mạng để giao tiếp với server

                // Gửi thông báo login tới server
                SendMessage($"LOGIN:{username}");

                // Thêm username của người dùng vào ComboBox để hiển thị danh sách người dùng
                cmbUsers.Items.Add(username);
                cmbUsers.SelectedItem = username;

                // Kích hoạt chế độ nhận tin nhắn
                Task.Run(() => ReceiveMessages());
                MessageBox.Show("Đăng nhập thành công!"); // Hiển thị thông báo đăng nhập thành công
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối tới server: {ex.Message}"); // Hiển thị lỗi nếu kết nối thất bại
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            string message = txtChatInput.Text.Trim(); // Lấy nội dung tin nhắn từ ô nhập liệu

            if (!string.IsNullOrEmpty(message)) // Kiểm tra tin nhắn có trống không
            {
                string selectedUser = cmbUsers.SelectedItem?.ToString(); // Lấy người dùng được chọn trong ComboBox

                if (!string.IsNullOrEmpty(selectedUser)) // Kiểm tra người dùng được chọn có hợp lệ không
                {
                    // Gửi tin nhắn riêng tư tới người dùng được chọn
                    SendMessage($"PRIVATE:{selectedUser}:{message}");
                    txtChatDisplay.AppendText($"To {selectedUser}: {message}" + Environment.NewLine); // Hiển thị tin nhắn đã gửi
                }
                else
                {
                    MessageBox.Show("Hãy chọn người dùng để nhắn tin"); // Yêu cầu chọn người dùng nếu chưa chọn
                }

                txtChatInput.Clear(); // Xóa nội dung ô nhập liệu sau khi gửi
            }
        }

        private void SendMessage(string message)
        {
            try
            {
                if (client != null && client.Connected) // Kiểm tra kết nối có hoạt động không
                {
                    byte[] data = Encoding.ASCII.GetBytes(message); // Mã hóa tin nhắn thành byte
                    stream.Write(data, 0, data.Length); // Gửi dữ liệu qua luồng mạng
                }
                else
                {
                    MessageBox.Show("Đã ngắt kết nối khỏi server"); // Thông báo nếu mất kết nối
                    Close(); // Đóng form nếu mất kết nối
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}"); // Hiển thị lỗi nếu xảy ra
            }
        }

        private async Task ReceiveMessages()
        {
            byte[] buffer = new byte[1024]; // Bộ đệm để lưu dữ liệu nhận được
            while (client != null && client.Connected) // Lặp lại khi kết nối còn hoạt động
            {
                try
                {
                    // Đọc dữ liệu từ server
                    int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (byteCount == 0) break; // Ngắt nếu không nhận được dữ liệu

                    string message = Encoding.ASCII.GetString(buffer, 0, byteCount).Trim(); // Chuyển đổi dữ liệu thành chuỗi

                    if (message.StartsWith("Danh sách người dùng:")) // Kiểm tra nếu tin nhắn là danh sách người dùng
                    {
                        UpdateUserList(message.Substring(9)); // Cập nhật danh sách người dùng
                    }
                    else
                    {
                        AppendMessage(message); // Thêm tin nhắn vào khung hiển thị
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Đã ngắt kết nối khỏi server."); // Hiển thị thông báo nếu mất kết nối
                    break;
                }
            }
        }

        private void AppendMessage(string message)
        {
            // Thêm tin nhắn vào khung hiển thị với giao diện chính
            Invoke((MethodInvoker)(() =>
            {
                txtChatDisplay.AppendText(message + Environment.NewLine);
            }));
        }

        private void UpdateUserList(string userList)
        {
            // Cập nhật danh sách người dùng từ chuỗi nhận được
            Invoke((MethodInvoker)(() =>
            {
                cmbUsers.Items.Clear(); // Xóa danh sách cũ
                string[] users = userList.Split(','); // Phân tách người dùng
                foreach (string user in users)
                {
                    if (!string.IsNullOrEmpty(user) && user != username) // Không thêm chính mình vào danh sách
                    {
                        cmbUsers.Items.Add(user);
                    }
                }
            }));
        }

        private void ChatForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (client != null && client.Connected) // Kiểm tra kết nối có hoạt động không
            {
                SendMessage($"LOGOUT:{username}"); // Gửi thông báo logout tới server
                client.Close(); // Đóng kết nối
            }
        }
    }
}
