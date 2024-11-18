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

        public ChatForm()
        {
            InitializeComponent();
        }

        private void ChatForm_Load(object sender, EventArgs e)
        {
            Task.Run(() => ReceiveMessages());
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            // Get the username from the TextBox
            username = txtboxAccount.Text.Trim();

            // Validate the username
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Please enter a username.");
                return;
            }

            try
            {
                // Connect to the server
                client = new TcpClient("192.168.1.153", 5000); // Update with the correct server IP
                stream = client.GetStream();

                // Send login message to server
                SendMessage($"LOGIN:{username}");

                // Add the username to the ComboBox (optional)
                cmbUsers.Items.Add(username);
                cmbUsers.SelectedItem = username;

                // Start receiving messages in a separate thread
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
                if (client.Connected)
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
            while (client.Connected)
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
            SendMessage($"LOGOUT:{username}");
            client.Close();
        }
    }
}
