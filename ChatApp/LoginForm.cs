using ChatApp;
using System;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace ChatClient
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Text;

            using (TcpClient client = new TcpClient("192.168.1.153", 5000))
            {
                NetworkStream stream = client.GetStream();

                string message = $"LOGIN:{username}:{password}";
                byte[] data = Encoding.ASCII.GetBytes(message);
                stream.Write(data, 0, data.Length);

                byte[] responseData = new byte[1024];
                int bytes = stream.Read(responseData, 0, responseData.Length);
                string response = Encoding.ASCII.GetString(responseData, 0, bytes);

                if (response == "Login Success")
                {
                    MessageBox.Show("Login successful!");
                    ChatForm chatForm = new ChatForm(username);
                    chatForm.Show();
                    this.Hide();
                }
                else
                {
                    MessageBox.Show("Login failed.");
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            RegisterScreen registerScreen = new RegisterScreen();
            registerScreen.Show();
        }
    }
}
