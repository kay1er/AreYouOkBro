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
        private TcpClient client;
        private NetworkStream stream;
        private string username;

        public ChatForm(string username)
        {
            InitializeComponent();
            this.username = username;
            client = new TcpClient("172.20.10.2", 8888); // Kết nối tới server cục bộ tại cổng 5000
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
            string message = txtChatInput.Text.Trim();
            if (!string.IsNullOrEmpty(message))
            {
                string selectedUser = cmbUsers.SelectedItem?.ToString();

                if (!string.IsNullOrEmpty(selectedUser))
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
                if (client.Connected)
                {
                    // Mã hóa tin nhắn thành byte và gửi qua stream
                    byte[] data = Encoding.ASCII.GetBytes(message);
                    stream.Write(data, 0, data.Length);
                }
                else
                {
                    // Thông báo lỗi nếu kết nối đến server đã mất
                    MessageBox.Show("Connection to the server has been lost.");
                    this.Close();
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
            byte[] buffer = new byte[1024];
            while (client.Connected)
            {
                try
                {
                    int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (byteCount == 0)
                    {
                        // Không nhận được dữ liệu, có thể server đã đóng kết nối
                        MessageBox.Show("Disconnected from the server.");
                        break;
                    }

                    string message = Encoding.ASCII.GetString(buffer, 0, byteCount).Trim();
                    Console.WriteLine($"Received: {message}"); // Log thông điệp nhận được

                    if (message.StartsWith("USERLIST:"))
                    {
                        UpdateUserList(message.Substring(9));
                    }
                    else if (message == $"Login Success{username}")
                    {
                        // Skip over the login success confirmation message
                    }
                    else
                    {
                        AppendMessageToChatDisplay(message);
                    }
                }
                catch (IOException ex)
                {
                    MessageBox.Show("Disconnected from the server. IOException: " + ex.Message);
                    break;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}");
                    break;
                }
            }
        }


        private void AppendMessageToChatDisplay(string message)
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)(() =>
                {
                    txtChatDisplay.AppendText(message + Environment.NewLine);
                }));
            }
            else
            {
                txtChatDisplay.AppendText(message + Environment.NewLine);
            }
        }

        private void UpdateUserList(string userList)
        {
            Invoke((MethodInvoker)(() =>
            {
                cmbUsers.Items.Clear(); // Xóa danh sách người dùng cũ
                string[] users = userList.Split(',');
                foreach (string user in users)
                {
                    if (user != username) // Không thêm chính người dùng hiện tại
                        cmbUsers.Items.Add(user);
                }
            }));
        }

        private void ChatForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Gửi thông báo đăng xuất và đóng kết nối TcpClient
            SendMessage($"LOGOUT:{username}");
            client.Close();
        }
    }
}
