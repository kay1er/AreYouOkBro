using System;
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

        public ChatForm()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            username = txtboxAccount.Text.Trim();

            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Please enter a username.");
                return;
            }

            try
            {
                // Kết nối tới server
                client = new TcpClient("192.168.1.153", 5000);
                stream = client.GetStream();

                // Gửi thông báo login tới server
                SendMessage($"LOGIN:{username}");

                // Thêm username vào ComboBox
                cmbUsers.Items.Add(username);
                cmbUsers.SelectedItem = username;

                // Kích hoạt chế độ chat
                Task.Run(() => ReceiveMessages());
                MessageBox.Show("Login successful!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect to server: {ex.Message}");
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            string message = txtChatInput.Text.Trim();
            if (!string.IsNullOrEmpty(message))
            {
                string selectedUser = cmbUsers.SelectedItem?.ToString();

                if (!string.IsNullOrEmpty(selectedUser))
                {
                    SendMessage($"PRIVATE:{selectedUser}:{message}");
                    txtChatDisplay.AppendText($"To {selectedUser}: {message}" + Environment.NewLine);
                }
                else
                {
                    MessageBox.Show("Please select a user to send a private message.");
                }

                txtChatInput.Clear();
            }
        }

        private void SendMessage(string message)
        {
            try
            {
                if (client != null && client.Connected)
                {
                    byte[] data = Encoding.ASCII.GetBytes(message);
                    stream.Write(data, 0, data.Length);
                }
                else
                {
                    MessageBox.Show("Disconnected from server.");
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private async Task ReceiveMessages()
        {
            byte[] buffer = new byte[1024];
            while (client != null && client.Connected)
            {
                try
                {
                    int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (byteCount == 0) break;

                    string message = Encoding.ASCII.GetString(buffer, 0, byteCount).Trim();

                    if (message.StartsWith("USERLIST:"))
                    {
                        UpdateUserList(message.Substring(9));
                    }
                    else
                    {
                        AppendMessage(message);
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Disconnected from server.");
                    break;
                }
            }
        }

        private void AppendMessage(string message)
        {
            Invoke((MethodInvoker)(() =>
            {
                txtChatDisplay.AppendText(message + Environment.NewLine);
            }));
        }

        private void UpdateUserList(string userList)
        {
            Invoke((MethodInvoker)(() =>
            {
                cmbUsers.Items.Clear();
                string[] users = userList.Split(',');
                foreach (string user in users)
                {
                    if (!string.IsNullOrEmpty(user) && user != username)
                    {
                        cmbUsers.Items.Add(user);
                    }
                }
            }));
        }

        private void ChatForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (client != null && client.Connected)
            {
                SendMessage($"LOGOUT:{username}");
                client.Close();
            }
        }
    }
}
