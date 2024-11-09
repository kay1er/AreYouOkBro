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

        public ChatForm(string username)
        {
            InitializeComponent();
            this.username = username;
            client = new TcpClient("192.168.1.99", 5000);
            stream = client.GetStream();
        }

        private void ChatForm_Load(object sender, EventArgs e)
        {
            Task.Run(() => ReceiveMessages());
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            string message = txtChatInput.Text;
            SendMessage($"{username}: {message}");
            txtChatInput.Clear();
        }

        private void SendMessage(string message)
        {
            byte[] data = Encoding.ASCII.GetBytes(message);
            stream.Write(data, 0, data.Length);
            txtChatDisplay.AppendText($"{message}{Environment.NewLine}");
        }

        private async Task ReceiveMessages()
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (byteCount == 0) continue; // No data received, skip
                string message = Encoding.ASCII.GetString(buffer, 0, byteCount);

                Invoke((MethodInvoker)(() => txtChatDisplay.AppendText($"{message}{Environment.NewLine}")));
            }
        }
    }
}
