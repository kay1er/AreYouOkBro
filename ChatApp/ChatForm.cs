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
            client = new TcpClient("192.168.1.153", 5000);
            stream = client.GetStream();
        }

        private void ChatForm_Load(object sender, EventArgs e)
        {
            Task.Run(() => ReceiveMessages());
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            string message = txtChatInput.Text;
            byte[] data = Encoding.ASCII.GetBytes($"{username}: {message}");
            stream.Write(data, 0, data.Length);
            txtChatDisplay.AppendText($"{username}: {message}\n");
            txtChatInput.Clear();
        }

        private async Task ReceiveMessages()
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                string message = Encoding.ASCII.GetString(buffer, 0, byteCount);
                Invoke((MethodInvoker)(() => txtChatDisplay.AppendText(message + "\n")));
            }
        }
    }
}
