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
            client = new TcpClient("192.168.1.99", 5000);
            stream = client.GetStream();

            // Notify server of login
            SendMessage($"LOGIN:{username}");
        }

        private void ChatForm_Load(object sender, EventArgs e)
        {
            Task.Run(() => ReceiveMessages());
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            string message = txtChatInput.Text;
            SendMessage($"{username}: {message}");
            txtChatDisplay.AppendText($"{username}: {message}\n");
            txtChatInput.Clear();
        }

        private void SendMessage(string message)
        {
            try
            {
                if (client.Connected) // Kiểm tra kết nối trước khi gửi
                {
                    byte[] data = Encoding.ASCII.GetBytes(message);
                    stream.Write(data, 0, data.Length);
                }
                else
                {
                    MessageBox.Show("Connection to the server has been lost.");
                    this.Close(); // Đóng ứng dụng nếu không thể kết nối
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show($"Error sending message: {ex.Message}");
                // Thực hiện các hành động phục hồi ở đây, ví dụ: đóng kết nối và thoát ứng dụng
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}");
            }
        }


        private async Task ReceiveMessages()
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                string message = Encoding.ASCII.GetString(buffer, 0, byteCount);

                if (message.StartsWith("USERLIST:"))
                {
                    UpdateUserList(message.Substring(9));
                }
                else
                {
                    Invoke((MethodInvoker)(() => txtChatDisplay.AppendText(message + "\n")));
                }
            }
        }

        private void UpdateUserList(string userList)
        {
            Invoke((MethodInvoker)(() =>
            {
                cmbUsers.Items.Clear();
                string[] users = userList.Split(',');
                foreach (string user in users)
                {
                    if (user != username) // Don't show the current user
                        cmbUsers.Items.Add(user);
                }
            }));
        }

        private void ChatForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SendMessage($"LOGOUT:{username}");
            client.Close();
        }
    }
}
