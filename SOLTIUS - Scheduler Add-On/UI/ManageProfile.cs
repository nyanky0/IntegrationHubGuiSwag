using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace SOLTIUS_Scheduler_Add_On.UI
{
    public partial class ManageProfile : Form
    {
        // Path file konfigurasi
        private string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AllConfigurations.xml");
        private string defaultFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DefaultConfig.xml");

        // Objek untuk menampung data profil asli yang dikirim dari FormChooseCF
        private AppConfig _originalConfig;

        public ManageProfile(AppConfig config)
        {
            InitializeComponent();
            UITheme.ApplyForm(this); // center screen — StartPosition harus di-set sebelum Show
            this._originalConfig = config;

            // Binding Event Handlers
            this.Load -= this.FormManage_Load;
            this.Load += this.FormManage_Load;
            this.button1.Click -= this.button1_Click;
            this.button1.Click += this.button1_Click; // Button Update
        }

        private void FormManage_Load(object sender, EventArgs e)
        {
            // Terapkan theme konsisten
            ApplyTheme();

            if (_originalConfig != null)
            {
                // Tampilkan data ke kontrol UI
                textBox1.Text = _originalConfig.ProfileName;

                // Set RadioButton sesuai Environment
                if (_originalConfig.Environment == "Production") radioButton1.Checked = true;
                else if (_originalConfig.Environment == "Development") radioButton2.Checked = true;
                else radioButton3.Checked = true;

                // SAP Database Group
                cboServerType.Text = _originalConfig.SAPServerType;
                txtSAPDBServer.Text = _originalConfig.SAPDBServer;
                txtSAPDBPort.Text = _originalConfig.SAPDBPort;
                txtSAPDBUser.Text = _originalConfig.SAPDBUser;
                txtSAPDBPass.Text = _originalConfig.SAPDBPass;

                // SAP B1 Group
                txtSAPDatabase.Text = _originalConfig.SAPDatabase;
                txtSAPUser.Text = _originalConfig.SAPUser;
                txtSAPPass.Text = _originalConfig.SAPPass;
                txtSAPLicenseServer.Text = _originalConfig.SAPLicenseServer;

                // Database Staging Group
                cboDBType.Text = _originalConfig.ExternalDBType;
                txtDBServer.Text = _originalConfig.ExternalDBServer;
                txtDBPort.Text = _originalConfig.ExternalDBPort;
                txtDBName.Text = _originalConfig.ExternalDBName;
                txtDBUser.Text = _originalConfig.ExternalDBUser;
                txtDBPass.Text = _originalConfig.ExternalDBPass;
                txtWebApiUrl.Text = _originalConfig.WebApiUrl ?? "";
            }
        }

        /// <summary>
        /// Terapkan theme SOLTIUS ke seluruh kontrol ManageProfile.
        /// </summary>
        private void ApplyTheme()
        {
            UITheme.ApplyForm(this);
            this.Text = "Manage Profile";

            UITheme.ApplyPrimary(button1);

            UITheme.ApplyCombo(cboServerType);
            UITheme.ApplyCombo(cboDBType);
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
            UITheme.ApplyTextBox(textBox1);

            UITheme.ApplyGroup(groupBox1);
            UITheme.ApplyGroup(groupBox2);
            UITheme.ApplyRadio(radioButton1);
            UITheme.ApplyRadio(radioButton2);
            UITheme.ApplyRadio(radioButton3);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. Validasi: Nama Profil tidak boleh kosong
                if (string.IsNullOrWhiteSpace(textBox1.Text))
                {
                    MessageBox.Show("Profile Name tidak boleh kosong!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox1.Focus();
                    return;
                }

                string newName = textBox1.Text.Trim();

                // 2. Load data dari XML Utama
                if (!File.Exists(configFilePath)) return;

                ConfigContainer container;
                XmlSerializer serializer = new XmlSerializer(typeof(ConfigContainer));

                using (FileStream fs = new FileStream(configFilePath, FileMode.Open))
                {
                    container = (ConfigContainer)serializer.Deserialize(fs);
                }

                // 3. LOGIKA CEK DUPLIKAT
                // Cek apakah ada profil LAIN (selain yang sedang diedit) yang memakai nama baru ini
                bool nameExists = container.Configurations.Any(x =>
                    x.ProfileName.Equals(newName, StringComparison.OrdinalIgnoreCase) &&
                    !x.ProfileName.Equals(_originalConfig.ProfileName, StringComparison.OrdinalIgnoreCase));

                if (nameExists)
                {
                    MessageBox.Show($"Nama profil '{newName}' sudah digunakan oleh profil lain.",
                                    "Gagal Update", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    textBox1.Focus();
                    return;
                }

                // 4. Cari data asli di dalam container untuk di-update
                var profileToUpdate = container.Configurations.FirstOrDefault(x =>
                    x.ProfileName.Equals(_originalConfig.ProfileName, StringComparison.OrdinalIgnoreCase));

                if (profileToUpdate != null)
                {
                    // 5. Update field dari UI ke objek
                    profileToUpdate.ProfileName = newName;
                    profileToUpdate.Environment = radioButton1.Checked ? "Production" : (radioButton2.Checked ? "Development" : "Testing");

                    // SAP DB Info
                    profileToUpdate.SAPServerType = cboServerType.Text;
                    profileToUpdate.SAPDBServer = txtSAPDBServer.Text;
                    profileToUpdate.SAPDBPort = txtSAPDBPort.Text;
                    profileToUpdate.SAPDBUser = txtSAPDBUser.Text;
                    profileToUpdate.SAPDBPass = txtSAPDBPass.Text;

                    // SAP B1 Credentials
                    profileToUpdate.SAPDatabase = txtSAPDatabase.Text;
                    profileToUpdate.SAPUser = txtSAPUser.Text;
                    profileToUpdate.SAPPass = txtSAPPass.Text;
                    profileToUpdate.SAPLicenseServer = txtSAPLicenseServer.Text;

                    // Database Staging
                    profileToUpdate.ExternalDBType = cboDBType.Text;
                    profileToUpdate.ExternalDBServer = txtDBServer.Text;
                    profileToUpdate.ExternalDBPort = txtDBPort.Text;
                    profileToUpdate.ExternalDBName = txtDBName.Text;
                    profileToUpdate.ExternalDBUser = txtDBUser.Text;
                    profileToUpdate.ExternalDBPass = txtDBPass.Text;

                    profileToUpdate.WebApiUrl = txtWebApiUrl.Text.Trim();

                    // 6. Simpan kembali ke AllConfigurations.xml
                    using (StreamWriter writer = new StreamWriter(configFilePath))
                    {
                        serializer.Serialize(writer, container);
                    }

                    // 7. SINKRONISASI: Update DefaultConfig.xml jika ini adalah profil aktif
                    SyncDefaultConfig(newName);

                    MessageBox.Show("Profil berhasil diperbarui!", "Update Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Beri tahu form pemanggil bahwa ada perubahan
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memperbarui profil: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SyncDefaultConfig(string newName)
        {
            try
            {
                if (File.Exists(defaultFilePath))
                {
                    DefaultSettings currentDefault;
                    XmlSerializer defSerializer = new XmlSerializer(typeof(DefaultSettings));

                    // Baca data default saat ini
                    using (FileStream fs = new FileStream(defaultFilePath, FileMode.Open))
                    {
                        currentDefault = (DefaultSettings)defSerializer.Deserialize(fs);
                    }

                    // Jika nama profil lama sama dengan yang ada di file aktif, ganti ke nama baru
                    if (currentDefault != null &&
                        currentDefault.DefaultProfileName.Equals(_originalConfig.ProfileName, StringComparison.OrdinalIgnoreCase))
                    {
                        currentDefault.DefaultProfileName = newName;

                        using (StreamWriter defWriter = new StreamWriter(defaultFilePath))
                        {
                            defSerializer.Serialize(defWriter, currentDefault);
                        }
                    }
                }
            }
            catch { /* Biarkan tetap tersimpan di master meski sinkronisasi default gagal */ }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}