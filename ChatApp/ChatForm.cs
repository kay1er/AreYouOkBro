using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatClient
{
    public partial class ChatForm : Form
    {
        private TcpClient client; // Khai báo đối tượng TcpClient
        private NetworkStream stream; // Khai báo đối tượng NetworkStream để truyền tải dữ liệu
        private string username; // Lưu tên người dùng

        public ChatForm(string username)
        {
            InitializeComponent(); // Khởi tạo các thành phần trên giao diện
            this.username = username; // Gán tên người dùng vào biến
            client = new TcpClient("172.20.10.2", 8888);  // Kết nối đến server qua địa chỉ IP và cổng
            stream = client.GetStream(); // Tạo luồng stream từ TcpClient

            // Thông báo cho server biết người dùng đã đăng nhập
            SendMessage($"LOGIN:{username}");
        }

        private void ChatForm_Load(object sender, EventArgs e)
        {
            // Tạo một task mới để xử lý nhận tin nhắn từ server
            Task.Run(() => ReceiveMessages());
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            string message = txtChatInput.Text.Trim(); // Lấy nội dung tin nhắn nhập vào và loại bỏ khoảng trắng thừa
            if (!string.IsNullOrEmpty(message)) // Kiểm tra nếu tin nhắn không rỗng
            {
                string selectedUser = cmbUsers.SelectedItem?.ToString(); // Lấy tên người dùng được chọn từ ComboBox

                if (!string.IsNullOrEmpty(selectedUser)) // Kiểm tra nếu người dùng đã chọn
                {
                    // Gửi tin nhắn riêng tư đến người dùng đã chọn
                    SendMessage($"PRIVATE:{selectedUser}:{message}");
                    txtChatDisplay.AppendText($"To {selectedUser}: {message}" + Environment.NewLine);
                }
                else
                {
                    // Gửi tin nhắn công khai nếu không có người dùng nào được chọn
                    SendMessage($"{username}: {message}");
                    txtChatDisplay.AppendText($"{username}: {message}" + Environment.NewLine);
                }

                txtChatInput.Clear(); // Xóa nội dung hộp nhập sau khi gửi tin nhắn
            }
        }

        private void SendMessage(string message)
        {
            try
            {
                if (client.Connected) // Kiểm tra xem kết nối có còn sống không
                {
                    // Mã hóa tin nhắn thành byte và gửi qua stream
                    byte[] data = Encoding.ASCII.GetBytes(message);
                    stream.Write(data, 0, data.Length);
                }
                else
                {
                    // Thông báo lỗi nếu kết nối đến server đã mất
                    MessageBox.Show("Connection to the server has been lost.");
                    this.Close(); // Đóng cửa sổ nếu không kết nối được
                }
            }
            catch (IOException ex)
            {
                // Hiển thị thông báo lỗi khi gặp sự cố gửi tin nhắn
                MessageBox.Show($"Error sending message: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}");
            }
        }

        private async Task ReceiveMessages()
        {
            byte[] buffer = new byte[1024]; // Tạo bộ đệm để nhận dữ liệu
            while (client.Connected) // Vòng lặp chạy khi kết nối còn sống
            {
                try
                {
                    int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length); // Đọc dữ liệu từ server
                    if (byteCount == 0)
                    {
                        // Không nhận được dữ liệu, có thể server đã đóng kết nối
                        MessageBox.Show("Disconnected from the server.");
                        break; // Thoát khỏi vòng lặp
                    }

                    string message = Encoding.ASCII.GetString(buffer, 0, byteCount).Trim(); // Giải mã dữ liệu nhận được thành chuỗi
                    Console.WriteLine($"Received: {message}"); // In ra thông điệp nhận được trong console

                    if (message.StartsWith("USERLIST:")) // Nếu tin nhắn là danh sách người dùng
                    {
                        UpdateUserList(message.Substring(9)); // Cập nhật danh sách người dùng
                    }
                    else if (message == $"Login Success{username}") // Nếu nhận được thông báo đăng nhập thành công
                    {
                        // Bỏ qua thông báo đăng nhập thành công
                    }
                    else
                    {
                        AppendMessageToChatDisplay(message); // Hiển thị tin nhắn vào cửa sổ chat
                    }
                }
                catch (IOException ex)
                {
                    MessageBox.Show("Disconnected from the server. IOException: " + ex.Message);
                    break; // Thoát khỏi vòng lặp khi gặp lỗi
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}");
                    break; // Thoát khỏi vòng lặp khi có lỗi không mong muốn
                }
            }
        }

        private void AppendMessageToChatDisplay(string message)
        {
            if (InvokeRequired) // Kiểm tra nếu cần gọi phương thức trên luồng giao diện
            {
                Invoke((MethodInvoker)(() =>
                {
                    txtChatDisplay.AppendText(message + Environment.NewLine); // Thêm tin nhắn vào hộp hiển thị chat
                }));
            }
            else
            {
                txtChatDisplay.AppendText(message + Environment.NewLine); // Thêm tin nhắn vào hộp hiển thị chat
            }
        }

        private void UpdateUserList(string userList)
        {
            Invoke((MethodInvoker)(() =>
            {
                cmbUsers.Items.Clear(); // Xóa danh sách người dùng cũ
                string[] users = userList.Split(','); // Tách danh sách người dùng từ chuỗi
                foreach (string user in users)
                {
                    if (user != username) // Không thêm chính người dùng hiện tại
                        cmbUsers.Items.Add(user); // Thêm người dùng vào ComboBox
                }
            }));
        }

        private void ChatForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Gửi thông báo đăng xuất và đóng kết nối TcpClient
            SendMessage($"LOGOUT:{username}");
            client.Close(); // Đóng kết nối với server
        }
    }
}
