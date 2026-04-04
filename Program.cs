using System;
using System.IO;
using System.Windows.Forms;

namespace Klip;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        try
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new PetForm());
        }
        catch (Exception ex)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "crash.txt");
            File.WriteAllText(path, ex.ToString());
            MessageBox.Show(
                ex.ToString(),
                "Klip crashed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }
}
