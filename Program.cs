using System;
using System.Windows.Forms;
using Crestkey.Core;
using Crestkey.Forms;

namespace Crestkey
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var unlock = new UnlockForm();
            if (unlock.ShowDialog() != DialogResult.OK)
                return;

            Application.Run(new MainForm(unlock.UnlockedVault));
        }
    }
}