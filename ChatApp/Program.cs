using System;
using System.Windows.Forms;
using ChatClient; // Add this if LoginForm is in ChatClient

namespace ChatApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ChatClient.LoginForm()); // Specify full namespace if needed
        }
    }
}
