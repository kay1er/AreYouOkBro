using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatServer
{
    public partial class ServerScreen : Form
    {
        private TcpListener listener;
        private bool isRunning;
        private Dictionary<string, TcpClient> connectedClients = new Dictionary<string, TcpClient>();

        public event Action<string> OnLogMessage;

        public ServerScreen()
        {
            InitializeComponent();
            OnLogMessage += LogMessage; // Log messages in the UI
        }

        private void ServerScreen_Load(object sender, EventArgs e)
        {
            StartServer();
        }

        private void ServerScreen_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopServer();
        }

        private void StartServer()
        {
            listener = new TcpListener(IPAddress.Any, 5000);
            listener.Start();
            isRunning = true;
            OnLogMessage?.Invoke("Server started...");

            Task.Run(() =>
            {
                while (isRunning)
                {
                    try
                    {
                        TcpClient client = listener.AcceptTcpClient();
                        Task.Run(() => HandleClient(client));
                    }
                    catch (Exception ex)
                    {
                        OnLogMessage?.Invoke($"Error accepting client: {ex.Message}");
                    }
                }
            });
        }

        private void StopServer()
        {
            isRunning = false;
            listener?.Stop();
            OnLogMessage?.Invoke("Server stopped.");
        }

        private void HandleClient(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int byteCount;

                while ((byteCount = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string data = Encoding.ASCII.GetString(buffer, 0, byteCount).Trim();
                    OnLogMessage?.Invoke($"Received: {data}");

                    if (data.StartsWith("LOGIN:"))
                    {
                        string username = data.Substring(6);
                        connectedClients[username] = client;
                        BroadcastUserList();
                        OnLogMessage?.Invoke($"User {username} logged in.");
                    }
                    else if (data.StartsWith("LOGOUT:"))
                    {
                        HandleLogout(client);
                    }
                    else if (data.StartsWith("PRIVATE:"))
                    {
                        HandlePrivateMessage(data, client);
                    }
                }
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"Error handling client: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }

        private void HandlePrivateMessage(string data, TcpClient sender)
        {
            var parts = data.Split(new[] { ':' }, 3);
            if (parts.Length == 3)
            {
                string recipient = parts[1];
                string message = parts[2];

                if (connectedClients.ContainsKey(recipient))
                {
                    SendMessage(connectedClients[recipient], $"{GetUsername(sender)}: {message}");
                    OnLogMessage?.Invoke($"Message from {GetUsername(sender)} to {recipient}: {message}");
                }
                else
                {
                    SendMessage(sender, "Recipient not available.");
                }
            }
        }

        private void HandleLogout(TcpClient client)
        {
            string username = GetUsername(client);
            if (username != null)
            {
                connectedClients.Remove(username);
                BroadcastUserList();
                OnLogMessage?.Invoke($"User {username} logged out.");
            }
        }

        private void SendMessage(TcpClient client, string message)
        {
            try
            {
                if (client.Connected)
                {
                    byte[] data = Encoding.ASCII.GetBytes(message);
                    client.GetStream().Write(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"Error sending message: {ex.Message}");
            }
        }

        private void BroadcastUserList()
        {
            string userList = "USERLIST:" + string.Join(",", connectedClients.Keys);
            foreach (var client in connectedClients.Values)
            {
                SendMessage(client, userList);
            }
        }

        private string GetUsername(TcpClient client)
        {
            foreach (var entry in connectedClients)
            {
                if (entry.Value == client) return entry.Key;
            }
            return null;
        }

        private void LogMessage(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(LogMessage), message);
            }
            else
            {
                txtMessageLog.AppendText($"{message}{Environment.NewLine}");
            }
        }
    }
}
