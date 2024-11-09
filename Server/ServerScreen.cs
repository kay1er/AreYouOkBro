using System.Data.SqlClient;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System;

namespace ChatServer
{
    class Server
    {
        private TcpListener listener;

        public void StartServer()
        {
            listener = new TcpListener(IPAddress.Any, 5000);
            listener.Start();
            Console.WriteLine("Server started...");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                HandleClient(client);
            }
        }

        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int byteCount = stream.Read(buffer, 0, buffer.Length);
            string data = Encoding.ASCII.GetString(buffer, 0, byteCount);

            if (data.StartsWith("LOGIN:"))
            {
                string[] parts = data.Split(':');
                string username = parts[1];
                string password = parts[2];

                if (AuthenticateUser(username, password))
                {
                    SendMessage(client, "Login Success");
                }
                else
                {
                    SendMessage(client, "Login Failed");
                }
            }
            else if (data.StartsWith("REGISTER:"))
            {
                string[] parts = data.Split(':');
                string username = parts[1];
                string passwordHash = parts[2];

                if (RegisterUser(username, passwordHash))
                {
                    SendMessage(client, "Register Success");
                }
                else
                {
                    SendMessage(client, "Register Failed");
                }
            }
        }

        private bool RegisterUser(string username, string passwordHash)
        {
            string connectionString = "Server=kay1er;Database=UserData;Trusted_Connection=True";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "INSERT INTO Users (Username, PasswordHash) VALUES (@username, @passwordHash)";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@passwordHash", passwordHash);

                        int result = cmd.ExecuteNonQuery();
                        return result > 0;
                    }
                }
                catch (SqlException)
                {
                    return false;
                }
            }
        }


        private bool AuthenticateUser(string username, string password)
        {
            string connectionString = "Server=kay1er;Database=UserData;Trusted_Connection=True";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT COUNT(1) FROM Users WHERE Username = @username AND PasswordHash = HASHBYTES('SHA2_256', @password)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@password", password);

                return (int)cmd.ExecuteScalar() == 1;
            }
        }

        private void SendMessage(TcpClient client, string message)
        {
            NetworkStream stream = client.GetStream();
            byte[] data = Encoding.ASCII.GetBytes(message);
            stream.Write(data, 0, data.Length);
        }
    }
}
