using System;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace ChatApp
{
    public partial class RegisterScreen : Form
    {
        public RegisterScreen()
        {
            InitializeComponent();
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Text;

            // Không mã hóa mật khẩu nữa, chỉ sử dụng mật khẩu thô
            string passwordPlaintext = password;

            if (SendRegisterRequest(username, passwordPlaintext))
            {
                MessageBox.Show("Registration successful!");
                this.Close();
            }
            else
            {
                MessageBox.Show("Registration failed. Username may already exist.");
            }
        }

        private bool SendRegisterRequest(string username, string password)
        {
            try
            {
                using (TcpClient client = new TcpClient("192.168.1.99", 5000))
                {
                    NetworkStream stream = client.GetStream();
                    string message = $"REGISTER:{username}:{password}"; // Sử dụng mật khẩu thô
                    byte[] data = Encoding.ASCII.GetBytes(message);
                    stream.Write(data, 0, data.Length);

                    byte[] responseData = new byte[1024];
                    int bytes = stream.Read(responseData, 0, responseData.Length);
                    string response = Encoding.ASCII.GetString(responseData, 0, bytes);

                    return response == "Register Success";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error connecting to server: " + ex.Message);
                return false;
            }
        }
    }
}
