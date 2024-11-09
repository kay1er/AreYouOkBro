using System;
using System.Net.Sockets;
using System.Security.Cryptography;
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

            // Hash the password
            string passwordHash = ComputeSha256Hash(password);

            if (SendRegisterRequest(username, passwordHash))
            {
                MessageBox.Show("Registration successful!");
                this.Close();
            }
            else
            {
                MessageBox.Show("Registration failed. Username may already exist.");
            }
        }

        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private bool SendRegisterRequest(string username, string passwordHash)
        {
            try
            {
                using (TcpClient client = new TcpClient("192.168.1.153", 5000))
                {
                    NetworkStream stream = client.GetStream();
                    string message = $"REGISTER:{username}:{passwordHash}";
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
