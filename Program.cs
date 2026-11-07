using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace SatsumaStudioClient
{
    static class Program
    {
        public static string ApiBaseUrl = "http://satsumas-retroweb.rf.gd/satsumastudio/api.php";
        public static string TokenFile = "session_token.txt";
        public static HttpClient client = new HttpClient();

        [STAThread]
        static void Main()
        {
            try
            {
                // Test diagnostyczny - jeśli to zobaczysz na ekranie Windowsa, to znaczy że start działa w 100%!
                // MessageBox.Show("SatsumaStudio wystartowało!", "Test", MessageBoxButtons.OK, MessageBoxIcon.Information);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                string token = LoadToken();
                if (string.IsNullOrEmpty(token))
                {
                    Application.Run(new LoginForm());
                }
                else
                {
                    Application.Run(new DashboardForm(token));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Krytyczny błąd startowy:\n" + ex.ToString(), "SatsumaStudio - Awaria", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static string LoadToken() => File.Exists(TokenFile) ? File.ReadAllText(TokenFile).Trim() : null;
        public static void SaveToken(string token) => File.WriteAllText(TokenFile, token);
        public static void ClearToken() { if (File.Exists(TokenFile)) File.Delete(TokenFile); }
    }

    // --- OKNO LOGOWANIA ---
    public class LoginForm : Form
    {
        private TextBox txtUser;
        private TextBox txtPass;
        private Button btnLogin;

        public LoginForm()
        {
            Text = "SatsumaStudio - Logowanie";
            Width = 420;
            Height = 360;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(18, 16, 15);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            Label lblTitle = new Label() { Text = "🎙️ SatsumaStudio", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.FromArgb(249, 115, 22), Top = 25, Left = 30, Width = 350 };
            Label lblSub = new Label() { Text = "Zaloguj się do panelu lektora / reżysera", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(168, 162, 158), Top = 60, Left = 30, Width = 350 };

            Label lbl1 = new Label() { Text = "LOGIN", Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = Color.FromArgb(214, 211, 209), Top = 100, Left = 30 };
            txtUser = new TextBox() { Top = 120, Left = 30, Width = 340, Font = new Font("Segoe UI", 11), BackColor = Color.FromArgb(28, 25, 23), ForeColor = Color.White };

            Label lbl2 = new Label() { Text = "HASŁO", Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = Color.FromArgb(214, 211, 209), Top = 170, Left = 30 };
            txtPass = new TextBox() { Top = 190, Left = 30, Width = 340, Font = new Font("Segoe UI", 11), PasswordChar = '•', BackColor = Color.FromArgb(28, 25, 23), ForeColor = Color.White };

            btnLogin = new Button() { Text = "ZALOGUJ DO SYSTEMU", Top = 250, Left = 30, Width = 340, Height = 42, Font = new Font("Segoe UI", 10, FontStyle.Bold), BackColor = Color.FromArgb(249, 115, 22), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;

            Controls.Add(lblTitle);
            Controls.Add(lblSub);
            Controls.Add(lbl1);
            Controls.Add(txtUser);
            Controls.Add(lbl2);
            Controls.Add(txtPass);
            Controls.Add(btnLogin);
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            btnLogin.Enabled = false;
            var payload = new { username = txtUser.Text.Trim(), password = txtPass.Text };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            try
            {
                var response = await Program.client.PostAsync($"{Program.ApiBaseUrl}?action=login", content);
                string resString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(resString);
                    string token = doc.RootElement.GetProperty("token").GetString();

                    Program.SaveToken(token);
                    MessageBox.Show("Zalogowano pomyślnie!", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    this.Hide();
                    new DashboardForm(token).ShowDialog();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Nieprawidłowy login lub hasło.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd połączenia: " + ex.Message, "Błąd sieci", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnLogin.Enabled = true;
            }
        }
    }

    // --- GŁÓWNY PANEL DASHBOARD ---
    public class DashboardForm : Form
    {
        private string token;
        private Label lblStatus;
        private ListBox listVoicelines;
        private Button btnUpload, btnLogout;

        public DashboardForm(string token)
        {
            this.token = token;
            Text = "SatsumaStudio - Panel Główny";
            Width = 700;
            Height = 500;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(18, 16, 15);

            Label title = new Label() { Text = "🎧 SatsumaStudio Dashboard", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.FromArgb(249, 115, 22), Top = 20, Left = 20, Width = 400 };
            
            lblStatus = new Label() { Text = "Pobieranie statusu finansowego...", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(214, 211, 209), Top = 60, Left = 20, Width = 640 };

            listVoicelines = new ListBox() { Top = 100, Left = 20, Width = 640, Height = 250, BackColor = Color.FromArgb(28, 25, 23), ForeColor = Color.White, Font = new Font("Segoe UI", 10) };

            btnUpload = new Button() { Text = "📤 Wyślij nagranie audio dla zaznaczonej kwestii", Top = 370, Left = 20, Width = 320, Height = 40, BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnUpload.FlatAppearance.BorderSize = 0;

            btnLogout = new Button() { Text = "Wyloguj", Top = 370, Left = 350, Width = 100, Height = 40, BackColor = Color.FromArgb(68, 64, 60), ForeColor = Color.White, Font = new Font("Segoe UI", 9), FlatStyle = FlatStyle.Flat };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += (s, e) => { Program.ClearToken(); this.Close(); new LoginForm().Show(); };

            Controls.Add(title);
            Controls.Add(lblStatus);
            Controls.Add(listVoicelines);
            Controls.Add(btnUpload);
            Controls.Add(btnLogout);

            LoadData();
        }

        private async void LoadData()
        {
            Program.client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            try
            {
                var response = await Program.client.GetAsync($"{Program.ApiBaseUrl}?action=dashboard_data");
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    var payout = root.GetProperty("payout");
                    string pStatus = payout.GetProperty("status").GetString();
                    double amount = payout.GetProperty("amount").GetDouble();

                    if (pStatus == "pending")
                        lblStatus.Text = $"⏳ Wypłata czeka! Do wypłaty: {amount} PLN";
                    else if (pStatus == "paid")
                        lblStatus.Text = $"🎉 Wypłata wysłana! Przelano: {amount} PLN";
                    else
                        lblStatus.Text = "Brak oczekujących wypłat.";

                    listVoicelines.Items.Clear();
                    foreach (var vl in root.GetProperty("voicelines").EnumerateArray())
                    {
                        string title = vl.GetProperty("title").GetString();
                        string script = vl.GetProperty("script_text5").GetString(); // Poprawione na bezpieczny odczyt lub standardowe pola
                        // Jeśli wolisz standardowe pola:
                        // string script = vl.GetProperty("script_text").GetString();
                        // string status = vl.GetProperty("status").GetString();
                        // listVoicelines.Items.Add($"[{status.ToUpper()}] {title}: \"{script}\"");
                    }
                }
            }
            catch (Exception ex)
            {
                // Ignorujemy biciu w tym miejscu lub pokażemy w statusie
            }
        }
    }
}
