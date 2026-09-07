using System;
using System.Collections.Generic; // Diperlukan untuk List
using System.IO;
using System.Linq; // Diperlukan untuk mengecek duplikat profil
using System.Windows.Forms;
using System.Xml.Serialization;

namespace SOLTIUS_Scheduler_Add_On.UI
{
    public partial class FormCreate : Form
    {
        // Tentukan satu path file utama untuk menampung semua konfigurasi
        private string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AllConfigurations.xml");

        public FormCreate()
        {
            InitializeComponent();
            UITheme.ApplyForm(this); // center screen — StartPosition harus di-set sebelum Show
            this.button2.Click -= this.button2_Click;
            this.button2.Click += this.button2_Click;
        }

        private void FormCreate_Load(object sender, EventArgs e)
        {
            // Terapkan theme konsisten
            UITheme.ApplyForm(this);
            UITheme.ApplyPrimary(button2);
            UITheme.ApplyCombo(cboServerType);
            UITheme.ApplyCombo(cboDBType);
            UITheme.ApplyTextBox(textBox1);
            UITheme.ApplyTextBox(txtSAPDBServer);
            UITheme.ApplyTextBox(txtSAPDBPort);
            UITheme.ApplyTextBox(txtSAPDBUser);
            UITheme.ApplyTextBox(txtSAPDBPass);
            UITheme.ApplyTextBox(txtSAPDatabase);
            UITheme.ApplyTextBox(txtSAPUser);
            UITheme.ApplyTextBox(txtSAPPass);
            UITheme.ApplyTextBox(txtSAPLicenseServer);
            UITheme.ApplyTextBox(txtDBServer);
            UITheme.ApplyTextBox(txtDBPort);
            UITheme.ApplyTextBox(txtDBName);
            UITheme.ApplyTextBox(txtDBUser);
            UITheme.ApplyTextBox(txtDBPass);
            UITheme.ApplyTextBox(txtWebApiUrl);
            UITheme.ApplyGroup(groupBox1);
            UITheme.ApplyGroup(groupBox2);
            UITheme.ApplyRadio(radioButton1);
            UITheme.ApplyRadio(radioButton2);
            UITheme.ApplyRadio(radioButton3);

            if (cboServerType.Items.Count > 0) cboServerType.SelectedIndex = 0;
            if (cboDBType.Items.Count > 0) cboDBType.SelectedIndex = 0;
            radioButton2.Checked = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. Validasi: Pastikan nama profile diisi
                if (string.IsNullOrWhiteSpace(textBox1.Text))
                {
                    MessageBox.Show("Silakan isi nama 'Profile Config' terlebih dahulu!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox1.Focus();
                    return;
                }

                string inputProfileName = textBox1.Text.Trim();

                // 2. Load data lama (jika ada file-nya) untuk pengecekan
                ConfigContainer container = new ConfigContainer();
                XmlSerializer serializer = new XmlSerializer(typeof(ConfigContainer));

                if (File.Exists(configFilePath))
                {
                    using (FileStream fs = new FileStream(configFilePath, FileMode.Open))
                    {
                        try
                        {
                            container = (ConfigContainer)serializer.Deserialize(fs);
                        }
                        catch
                        {
                            container = new ConfigContainer(); // Reset jika file XML corrupt
                        }
                    }
                }

                // 3. LOGIKA STRICT NO-DUPLICATE
                // Mengecek apakah ada nama profil yang sama (Case-Insensitive)
                bool isDuplicate = container.Configurations.Any(x =>
                    x.ProfileName.Equals(inputProfileName, StringComparison.OrdinalIgnoreCase));

                if (isDuplicate)
                {
                    MessageBox.Show($"Nama profil '{inputProfileName}' sudah digunakan.\nSilakan gunakan nama lain.",
                                    "Nama Profil Duplikat",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                    textBox1.Focus();
                    return; // Berhenti di sini, tidak lanjut simpan
                }

                // 4. Jika tidak duplikat, buat object AppConfig baru
                AppConfig newConfig = new AppConfig
                {
                    ProfileName = inputProfileName,
                    Environment = GetSelectedEnvironment(),

                    // SAP Database Section
                    SAPServerType = cboServerType.Text,
                    SAPDBServer = txtSAPDBServer.Text,
                    SAPDBPort = txtSAPDBPort.Text,
                    SAPDBUser = txtSAPDBUser.Text,
                    SAPDBPass = txtSAPDBPass.Text,

                    // SAP B1 Section
                    SAPDatabase = txtSAPDatabase.Text,
                    SAPUser = txtSAPUser.Text,
                    SAPPass = txtSAPPass.Text,
                    SAPLicenseServer = txtSAPLicenseServer.Text,

                    // External Database Section
                    ExternalDBType = cboDBType.Text,
                    ExternalDBServer = txtDBServer.Text,
                    ExternalDBPort = txtDBPort.Text,
                    ExternalDBName = txtDBName.Text,
                    ExternalDBUser = txtDBUser.Text,
                    ExternalDBPass = txtDBPass.Text,

                    WebApiUrl = txtWebApiUrl.Text.Trim()
                };

                // 5. Tambahkan ke List dan Simpan
                container.Configurations.Add(newConfig);

                using (StreamWriter writer = new StreamWriter(configFilePath))
                {
                    serializer.Serialize(writer, container);
                }

                MessageBox.Show($"Konfigurasi '{newConfig.ProfileName}' berhasil ditambahkan ke daftar!",
                                "Berhasil", MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.Close(); // Tutup form setelah berhasil save
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetSelectedEnvironment()
        {
            if (radioButton1.Checked) return "Production";
            if (radioButton2.Checked) return "Development";
            if (radioButton3.Checked) return "Testing";
            return "Unknown";
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e) { }
        private void label25_Click(object sender, EventArgs e) { }

        private void cboDBType_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }

    /// <summary>
    /// Class Wrapper agar XML memiliki root tunggal untuk banyak item
    /// </summary>
    [Serializable]
    [XmlRoot("ConfigurationSettings")]
    public class ConfigContainer
    {
        [XmlElement("Config")]
        public List<AppConfig> Configurations { get; set; } = new List<AppConfig>();
    }

    [Serializable]
    public class AppConfig
    {
        public string ProfileName { get; set; }
        public string Environment { get; set; }
        public string SAPServerType { get; set; }
        public string SAPDBServer { get; set; }
        public string SAPDBPort { get; set; }
        public string SAPDBUser { get; set; }
        public string SAPDBPass { get; set; }
        public string SAPDatabase { get; set; }
        public string SAPUser { get; set; }
        public string SAPPass { get; set; }
        public string SAPLicenseServer { get; set; }
        public string ExternalDBType { get; set; }
        public string ExternalDBServer { get; set; }
        public string ExternalDBPort { get; set; }
        public string ExternalDBName { get; set; }
        public string ExternalDBUser { get; set; }
        public string ExternalDBPass { get; set; }
        public string WebApiUrl { get; set; }
    }
}