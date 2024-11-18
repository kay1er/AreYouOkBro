using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatClient
{
    public partial class ChatForm : Form
    {
        private TcpClient client; // Đối tượng TCP client để kết nối tới server
        private NetworkStream stream; // Luồng dữ liệu để gửi và nhận thông tin
        private string username; // Tên người dùng

        public ChatForm()
        {
            InitializeComponent(); // Khởi tạo giao diện form
        }

        // Xử lý sự kiện khi nhấn nút "Đăng nhập"
        private void btnLogin_Click(object sender, EventArgs e)
        {
            username = txtboxAccount.Text.Trim(); // Lấy tên người dùng từ ô nhập liệu

            // Kiểm tra nếu username trống
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Vui lòng nhập tên người dùng."); // Hiển thị thông báo lỗi
                return;
            }

            try
            {
                // Kết nối tới server
                client = new TcpClient("192.168.1.153", 5000);
                stream = client.GetStream(); // Lấy luồng dữ liệu từ client

                // Gửi thông báo đăng nhập tới server
                SendMessage($"LOGIN:{username}");

                // Thêm tên người dùng vào ComboBox
                cmbUsers.Items.Add(username);
                cmbUsers.SelectedItem = username;

                // Kích hoạt chế độ nhận tin nhắn
                Task.Run(() => ReceiveMessages()); // Chạy một luồng mới để nhận tin nhắn
                MessageBox.Show("Đăng nhập thành công!"); // Thông báo thành công
            }
            catch (Exception ex)
            {
                // Hiển thị thông báo lỗi nếu không kết nối được
                MessageBox.Show($"Kết nối tới server thất bại: {ex.Message}");
            }
        }

        // Xử lý sự kiện khi nhấn nút "Gửi"
        private void btnSend_Click(object sender, EventArgs e)
        {
            string message = txtChatInput.Text.Trim(); // Lấy nội dung tin nhắn từ ô nhập liệu
            if (!string.IsNullOrEmpty(message))
            {
                string selectedUser = cmbUsers.SelectedItem?.ToString(); // Lấy người dùng được chọn từ ComboBox

                if (!string.IsNullOrEmpty(selectedUser))
                {
                    // Gửi tin nhắn riêng tư tới người dùng được chọn
                    SendMessage($"PRIVATE:{selectedUser}:{message}");
                    // Hiển thị tin nhắn trong ô hiển thị
                    txtChatDisplay.AppendText($"To {selectedUser}: {message}" + Environment.NewLine);
                }
                else
                {
                    MessageBox.Show("Vui lòng chọn người dùng để gửi tin nhắn riêng."); // Thông báo nếu chưa chọn người dùng
                }

                txtChatInput.Clear(); // Xóa nội dung ô nhập liệu sau khi gửi tin nhắn
            }
        }

        // Hàm gửi tin nhắn tới server
        private void SendMessage(string message)
        {
            try
            {
                if (client != null && client.Connected) // Kiểm tra kết nối
                {
                    byte[] data = Encoding.ASCII.GetBytes(message); // Mã hóa tin nhắn thành byte
                    stream.Write(data, 0, data.Length); // Gửi dữ liệu qua luồng
                }
                else
                {
                    MessageBox.Show("Đã mất kết nối với server."); // Thông báo nếu kết nối bị gián đoạn
                    Close(); // Đóng form
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}"); // Hiển thị thông báo lỗi
            }
        }

        // Hàm nhận tin nhắn từ server
        private async Task ReceiveMessages()
        {
            byte[] buffer = new byte[1024]; // Bộ đệm để lưu dữ liệu nhận được
            while (client != null && client.Connected)
            {
                try
                {
                    int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length); // Đọc dữ liệu từ server
                    if (byteCount == 0) break; // Nếu không nhận được dữ liệu, thoát vòng lặp

                    string message = Encoding.ASCII.GetString(buffer, 0, byteCount).Trim(); // Chuyển đổi dữ liệu thành chuỗi

                    if (message.StartsWith("USERLIST:"))
                    {
                        // Cập nhật danh sách người dùng nếu nhận được thông báo USERLIST
                        UpdateUserList(message.Substring(9));
                    }
                    else
                    {
                        // Hiển thị tin nhắn trong ô chat
                        AppendMessage(message);
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Đã mất kết nối với server."); // Thông báo khi mất kết nối
                    break;
                }
            }
        }

        // Thêm tin nhắn vào ô hiển thị
        private void AppendMessage(string message)
        {
            Invoke((MethodInvoker)(() =>
            {
                txtChatDisplay.AppendText(message + Environment.NewLine); // Hiển thị tin nhắn trên giao diện
            }));
        }

        // Cập nhật danh sách người dùng
        private void UpdateUserList(string userList)
        {
            Invoke((MethodInvoker)(() =>
            {
                cmbUsers.Items.Clear(); // Xóa danh sách hiện tại
                string[] users = userList.Split(','); // Phân tách danh sách người dùng
                foreach (string user in users)
                {
                    // Thêm người dùng vào ComboBox nếu không trống và không trùng với username hiện tại
                    if (!string.IsNullOrEmpty(user) && user != username)
                    {
                        cmbUsers.Items.Add(user);
                    }
                }
            }));
        }

        // Xử lý sự kiện khi đóng form
        private void ChatForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (client != null && client.Connected)
            {
                SendMessage($"LOGOUT:{username}"); // Gửi thông báo đăng xuất tới server
                client.Close(); // Đóng kết nối
            }
        }
    }
}
