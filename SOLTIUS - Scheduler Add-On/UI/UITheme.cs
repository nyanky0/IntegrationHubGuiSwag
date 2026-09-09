using System.Drawing;
using System.Windows.Forms;

namespace SOLTIUS_Scheduler_Add_On.UI
{
    /// <summary>
    /// Theme konsisten untuk seluruh form SOLTIUS Scheduler.
    /// Palet: biru SAP, abu netral, putih bersih. Font: Segoe UI.
    /// </summary>
    public static class UITheme
    {
        // Palet
        public static readonly Color Primary = Color.FromArgb(0, 104, 168);      // biru SAP
        public static readonly Color PrimaryDark = Color.FromArgb(0, 74, 125);
        public static readonly Color PrimaryLight = Color.FromArgb(222, 240, 250); // biru muda (selected/hover)
        public static readonly Color Background = Color.FromArgb(244, 246, 248);  // abu sangat muda
        public static readonly Color Panel = Color.White;
        public static readonly Color Border = Color.FromArgb(204, 210, 216);
        public static readonly Color Text = Color.FromArgb(33, 37, 41);
        public static readonly Color TextMuted = Color.FromArgb(110, 118, 126);
        public static readonly Color Danger = Color.FromArgb(200, 60, 60);
        public static readonly Color Success = Color.FromArgb(40, 130, 70);

        public static readonly Font FontBase = new Font("Segoe UI", 9F);
        public static readonly Font FontTitle = new Font("Segoe UI", 15F, FontStyle.Bold);
        public static readonly Font FontSubtitle = new Font("Segoe UI", 9F, FontStyle.Regular);
        public static readonly Font FontSmall = new Font("Segoe UI", 8.25F);
        public static readonly Font FontButton = new Font("Segoe UI", 9F, FontStyle.Regular);

        /// <summary>Terapkan background + font dasar ke form, dan tampilkan di tengah layar.</summary>
        public static void ApplyForm(Form form)
        {
            form.BackColor = Background;
            form.Font = FontBase;
            form.ForeColor = Text;
            form.StartPosition = FormStartPosition.CenterScreen;
        }

        /// <summary>Tombol aksi utama (biru solid, teks putih).</summary>
        public static Button ApplyPrimary(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Primary;
            btn.ForeColor = Color.White;
            btn.Font = FontButton;
            btn.Cursor = Cursors.Hand;
            btn.FlatAppearance.MouseOverBackColor = PrimaryDark;
            return btn;
        }

        /// <summary>Tombol sekunder (putih, border abu).</summary>
        public static Button ApplySecondary(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Border;
            btn.BackColor = Panel;
            btn.ForeColor = Text;
            btn.Font = FontButton;
            btn.Cursor = Cursors.Hand;
            btn.FlatAppearance.MouseOverBackColor = PrimaryLight;
            return btn;
        }

        /// <summary>Tombol aksi berbahaya / destruktif (merah solid, teks putih).</summary>
        public static Button ApplyDanger(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Danger;
            btn.ForeColor = Color.White;
            btn.Font = FontButton;
            btn.Cursor = Cursors.Hand;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(160, 40, 40);
            return btn;
        }

        /// <summary>TextBox standar: putih, border abu, padding nyaman.</summary>
        public static TextBox ApplyTextBox(TextBox txt)
        {
            txt.BackColor = Color.White;
            txt.ForeColor = Text;
            txt.Font = FontBase;
            txt.BorderStyle = BorderStyle.FixedSingle;
            return txt;
        }

        /// <summary>ComboBox standar.</summary>
        public static ComboBox ApplyCombo(ComboBox cb)
        {
            cb.BackColor = Color.White;
            cb.ForeColor = Text;
            cb.Font = FontBase;
            cb.FlatStyle = FlatStyle.Flat;
            return cb;
        }

        /// <summary>Label normal.</summary>
        public static Label ApplyLabel(Label lbl, bool muted = false)
        {
            lbl.Font = FontBase;
            lbl.ForeColor = muted ? TextMuted : Text;
            return lbl;
        }

        /// <summary>GroupBox: border abu, judul berwarna primary.</summary>
        public static GroupBox ApplyGroup(GroupBox gb)
        {
            gb.ForeColor = Text;
            gb.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            gb.BackColor = Panel;
            return gb;
        }

        /// <summary>TabControl: tab header putih, selected berwarna primary.</summary>
        public static TabControl ApplyTab(TabControl tc)
        {
            tc.Font = FontBase;
            tc.BackColor = Panel;
            return tc;
        }

        /// <summary>DataGridView: header biru, zebra rows, selection biru muda.</summary>
        public static DataGridView ApplyGrid(DataGridView grid)
        {
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.GridColor = Border;
            grid.EnableHeadersVisualStyles = false;

            grid.ColumnHeadersDefaultCellStyle.BackColor = Primary;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Primary;
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.ColumnHeadersHeight = 34;

            grid.DefaultCellStyle.Font = FontBase;
            grid.DefaultCellStyle.ForeColor = Text;
            grid.DefaultCellStyle.BackColor = Color.White;
            grid.DefaultCellStyle.SelectionBackColor = PrimaryLight;
            grid.DefaultCellStyle.SelectionForeColor = Text;
            grid.DefaultCellStyle.Padding = new Padding(4, 2, 4, 2);

            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.RowTemplate.Height = 28;

            return grid;
        }

        /// <summary>ProgressBar: warna primary.</summary>
        public static ProgressBar ApplyProgress(ProgressBar pBar)
        {
            pBar.ForeColor = Primary;
            return pBar;
        }

        /// <summary>CheckBox: font konsisten.</summary>
        public static CheckBox ApplyCheck(CheckBox chk)
        {
            chk.Font = FontBase;
            chk.ForeColor = Text;
            return chk;
        }

        /// <summary>RadioButton: font konsisten.</summary>
        public static RadioButton ApplyRadio(RadioButton rb)
        {
            rb.Font = FontBase;
            rb.ForeColor = Text;
            return rb;
        }
    }
}
