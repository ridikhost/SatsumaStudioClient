using System;
using System.Windows.Forms;

namespace SatsumaStudioClient
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                MessageBox.Show("Aplikacja wystartowała poprawnie!", "Test Startu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new LoginForm()); // Tutaj na razie testujemy samo okno logowania
            }
            catch (Exception ex)
            {
                MessageBox.Show("Awaria: " + ex.ToString(), "Błąd Krytyczny", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
