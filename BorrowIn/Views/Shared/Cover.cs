using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BorrowIn
{
    public partial class Cover : Form
    {
        public Cover()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized; // auto max window
            this.IsMdiContainer = true; // Set sebagai MDI Parent
        }

        // Menampilkan kembali panel cover (splash screen)
        public void ShowCoverPanel()
        {
            panel1.Visible = true;
            panel1.BringToFront();
        }

        // Method pusat untuk membuka Form Anak (MDI Child)
        // Ini memastikan semua form dibuka dengan standar yang sama (tengah & terpantau)
        public void OpenChildForm(Form childForm)
        {
            // Sembunyikan cover panel agar tidak menghalangi form yang baru dibuka
            panel1.Visible = false;

            // Daftarkan form ini sebagai anak dari Cover (MDI Parent)
            childForm.MdiParent = this;
            
            // Pasang event handler agar kita tahu kapan form ini ditutup
            // Jika form terakhir ditutup, kita harus menampilkan kembali splash screen
            childForm.FormClosed += MdiChild_FormClosed;

            // Atur agar form anak tanpa border (borderless), memenuhi seluruh client area (Dock = Fill),
            // dan mengaktifkan auto-scroll jika konten melebihi area layar.
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill;
            childForm.AutoScroll = true;

            childForm.Show();
        }

        // Handler global saat ada Form Anak yang ditutup
        private void MdiChild_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Gunakan BeginInvoke agar pengecekan dilakukan SETELAH form benar-benar selesai tertutup.
            // Jika kita cek langsung, form yang sedang menutup masih dianggap "ada" di MdiChildren.
            this.BeginInvoke(new Action(() => {
                bool hasVisibleChildren = false;
                foreach (Form child in this.MdiChildren)
                {
                    // Cek apakah masih ada form lain yang terbuka dan terlihat
                    if (child != sender && child.Visible)
                    {
                        hasVisibleChildren = true;
                        break;
                    }
                }

                // Jika tidak ada form anak yang tersisa, kembalikan tampilan ke Splash Screen (Cover)
                if (!hasVisibleChildren)
                {
                    ShowCoverPanel();
                }
            }));
        }

        private void Cover_Load(object sender, EventArgs e)
        {
        }

        // TOMBOL LOGIN 
        private void button1_Click(object sender, EventArgs e)
        {
            OpenChildForm(new Login_Form());
        }

        // TOMBOL SIGNUP 
        private void button2_Click(object sender, EventArgs e)
        {
            OpenChildForm(new Signup());
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}