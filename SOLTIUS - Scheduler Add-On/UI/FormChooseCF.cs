using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace SOLTIUS_Scheduler_Add_On.UI
{
    public partial class FormChooseCF : Form
    {
        // Path file database semua profil
        private string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AllConfigurations.xml");

        // Path file penanda profil default/aktif
        private string defaultFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DefaultConfig.xml");

        // List memori untuk menampung data dari XML
        private List<AppConfig> loadedConfigs = new List<AppConfig>();

        /// <summary>
        /// Nama profil yang dipilih oleh user saat menekan Choose.
        /// </summary>
        public string SelectedProfileName { get; private set; }

        public FormChooseCF()
        {
            InitializeComponent();
            UITheme.ApplyForm(this); // default center screen; dipanggil sbg dialog => di-override CenterParent di pemanggil

            // Pastikan event handler tombol tidak terdaftar ganda
            this.button1.Click -= this.button1_Click;
            this.button1.Click += this.button1_Click; // Choose

            this.button2.Click -= this.button2_Click_1;
            this.button2.Click += this.button2_Click_1; // Manage

            this.button3.Click -= this.button3_Click;
            this.button3.Click += this.button3_Click; // Create

            // Event Load Form
            this.Load -= this.FormChooseCF_Load;
            this.Load += this.FormChooseCF_Load;
        }

        private void FormChooseCF_Load(object sender, EventArgs e)
        {
            // Terapkan theme konsisten
            UITheme.ApplyForm(this);
            UITheme.ApplyPrimary(button1);
            UITheme.ApplySecondary(button2);
            UITheme.ApplySecondary(button3);
            UITheme.ApplyCombo(comboBox1);
            UITheme.ApplyCheck(checkBox1);
            UITheme.ApplyLabel(label1);

            // 1. Matikan event SelectedIndexChanged sementara agar tidak konflik saat pengisian data
            this.comboBox1.SelectedIndexChanged -= this.comboBox1_SelectedIndexChanged;

            // 2. Ambil data terbaru dari XML Utama
            RefreshProfileList();

            // 3. Isi item ke ComboBox
            comboBox1.Items.Clear();
            foreach (var config in loadedConfigs)
            {
                comboBox1.Items.Add(config.ProfileName);
            }

            // 4. Pilih profil secara otomatis (Cari di DefaultConfig.xml)
            ApplyDefaultSelection();

            // 5. Aktifkan kembali event SelectedIndexChanged setelah UI siap
            this.comboBox1.SelectedIndexChanged += this.comboBox1_SelectedIndexChanged;

            // 6. Jalankan pembaruan label dan checkbox secara manual untuk inisialisasi pertama kali
            UpdateEnvironmentLabel();
            UpdateCheckboxBasedOnDefault();
        }

        private string GetCurrentDefaultProfile()
        {
            try
            {
                if (File.Exists(defaultFilePath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(DefaultSettings));
                    using (FileStream fs = new FileStream(defaultFilePath, FileMode.Open))
                    {
                        DefaultSettings def = (DefaultSettings)serializer.Deserialize(fs);
                        return def?.DefaultProfileName;
                    }
                }
            }
            catch { }
            return null;
        }

        private void RefreshProfileList()
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ConfigContainer));
                    using (FileStream fs = new FileStream(configFilePath, FileMode.Open))
                    {
                        ConfigContainer container = (ConfigContainer)serializer.Deserialize(fs);
                        if (container != null)
                        {
                            loadedConfigs = container.Configurations;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat XML Konfigurasi: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyDefaultSelection()
        {
            bool successLoadDefault = false;

            if (File.Exists(defaultFilePath))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(DefaultSettings));
                    using (FileStream fs = new FileStream(defaultFilePath, FileMode.Open))
                    {
                        DefaultSettings def = (DefaultSettings)serializer.Deserialize(fs);

                        // Cari index profil yang namanya sesuai dengan isi DefaultConfig.xml
                        int index = comboBox1.FindStringExact(def.DefaultProfileName);
                        if (index != -1)
                        {
                            comboBox1.SelectedIndex = index;
                            successLoadDefault = true;
                        }
                    }
                }
                catch { }
            }

            // Fallback: Jika tidak ada default, atau file tidak ditemukan, pilih item pertama
            if (!successLoadDefault && comboBox1.Items.Count > 0)
            {
                comboBox1.SelectedIndex = 0;
            }
        }

        private void UpdateEnvironmentLabel()
        {
            if (comboBox1.SelectedItem == null) return;

            string selectedName = comboBox1.Text;
            var config = loadedConfigs.FirstOrDefault(x => x.ProfileName == selectedName);

            if (config != null)
            {
                label1.Text = config.Environment;
            }
        }

        // FUNGSI UTAMA: Sinkronisasi Checkbox dengan file DefaultConfig.xml
        private void UpdateCheckboxBasedOnDefault()
        {
            try
            {
                string selectedProfile = comboBox1.Text;
                bool isCurrentlyDefault = false;

                if (File.Exists(defaultFilePath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(DefaultSettings));
                    using (FileStream fs = new FileStream(defaultFilePath, FileMode.Open))
                    {
                        DefaultSettings def = (DefaultSettings)serializer.Deserialize(fs);
                        // Cek apakah profil yang sedang dipilih di ComboBox adalah profil default
                        if (def != null && def.DefaultProfileName == selectedProfile)
                        {
                            isCurrentlyDefault = true;
                        }
                    }
                }
                // Set centang checkbox secara otomatis
                checkBox1.Checked = isCurrentlyDefault;
            }
            catch
            {
                checkBox1.Checked = false;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateEnvironmentLabel();
            UpdateCheckboxBasedOnDefault(); // Setiap kali ganti pilihan, checkbox akan cek ulang statusnya
        }

        // --- BUTTON 1: CHOOSE (Set Default & Close) ---
        private void button1_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem == null) return;

            try
            {
                string selectedProfile = comboBox1.Text;
                bool isAlreadyDefault = false;

                // 1. Cek dulu apakah profil yang dipilih saat ini memang sudah jadi default di file
                if (File.Exists(defaultFilePath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(DefaultSettings));
                    using (FileStream fs = new FileStream(defaultFilePath, FileMode.Open))
                    {
                        DefaultSettings currentDef = (DefaultSettings)serializer.Deserialize(fs);
                        if (currentDef != null && currentDef.DefaultProfileName == selectedProfile)
                        {
                            isAlreadyDefault = true;
                        }
                    }
                }

                // 2. LOGIKA BARU: Jika bukan default DAN checkbox tidak dicentang, maka blokir
                if (!isAlreadyDefault && !checkBox1.Checked)
                {
                    MessageBox.Show($"Profil '{selectedProfile}' belum menjadi profil aktif.\n\nSilakan ceklis 'Set As Default' terlebih dahulu untuk memilih profil ini.",
                                    "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    checkBox1.Focus();
                    return; // Berhenti di sini, jangan lanjut tutup form
                }

                // 3. Proses Simpan atau Hapus Default (Logika Normal)
                if (checkBox1.Checked)
                {
                    // SIMPAN: Menjadikan profil ini sebagai default permanen
                    DefaultSettings def = new DefaultSettings { DefaultProfileName = selectedProfile };
                    XmlSerializer serializer = new XmlSerializer(typeof(DefaultSettings));
                    using (StreamWriter writer = new StreamWriter(defaultFilePath))
                    {
                        serializer.Serialize(writer, def);
                    }
                }
                else
                {
                    // HAPUS: Jika user UNCHECK profil yang tadinya default, hapus file defaultnya
                    if (isAlreadyDefault)
                    {
                        File.Delete(defaultFilePath);
                    }
                }

                SelectedProfileName = selectedProfile;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memproses pilihan profil: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --- BUTTON 2: MANAGE (Edit) ---
        private void button2_Click_1(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem == null) return;

            var selected = loadedConfigs.FirstOrDefault(x => x.ProfileName == comboBox1.Text);
            if (selected != null)
            {
                // Gunakan 'using' agar memory FormManage langsung dibersihkan setelah Close
                using (ManageProfile frm = new ManageProfile(selected))
                {
                    frm.StartPosition = FormStartPosition.CenterParent;
                    frm.ShowDialog();
                }

                // JANGAN panggil FormChooseCF_Load(null, null);
                // Cukup panggil fungsi-fungsi pengupdate data saja
                RefreshDataSetelahEdit();
            }
        }

        // Buat fungsi baru ini untuk refresh data tanpa trigger event ganda
        private void RefreshDataSetelahEdit()
        {
            // Simpan nama yang sedang dipilih agar tidak reset ke index 0
            string currentSelected = comboBox1.Text;

            RefreshProfileList(); // Load ulang list dari XML

            comboBox1.Items.Clear();
            foreach (var config in loadedConfigs)
            {
                comboBox1.Items.Add(config.ProfileName);
            }

            // Kembalikan seleksi ke profil tadi (siapa tahu namanya berubah)
            int index = comboBox1.FindStringExact(currentSelected);
            if (index != -1) comboBox1.SelectedIndex = index;
            else if (comboBox1.Items.Count > 0) comboBox1.SelectedIndex = 0;

            UpdateEnvironmentLabel();
            UpdateCheckboxBasedOnDefault();
        }

        // --- BUTTON 3: CREATE (Add New) ---
        private void button3_Click(object sender, EventArgs e)
        {
            using (FormCreate frm = new FormCreate())
            {
                frm.StartPosition = FormStartPosition.CenterParent;
                frm.ShowDialog();
            }

            // Reload data profil tanpa memicu ulang event load
            RefreshDataSetelahEdit();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e) { }
        private void FormChooseCF_Load_1(object sender, EventArgs e) { }
    }

    // --- MODEL UNTUK XML DEFAULT ---
    [Serializable]
    public class DefaultSettings
    {
        public string DefaultProfileName { get; set; }
    }
}