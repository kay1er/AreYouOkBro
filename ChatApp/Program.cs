using ChatClient;
using System;
using System.Windows.Forms;

namespace ChatApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Mở ChatForm trực tiếp
            Application.Run(new ChatForm());
        }
    }
}
