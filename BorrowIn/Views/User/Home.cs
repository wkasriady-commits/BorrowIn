using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace BorrowIn
{
    // Form Dashboard Utama untuk User (Peminjam)
    public partial class Home : Form
    {
        private dbClient db;
        // variable yg dikirim dari login form untuk mencatat data email user (Legacy/Fallback)
        public string userEmail;
        
        // List untuk menampung buku rekomendasi yang akan ditampilkan bergantian
        private List<Book> recommendedBooks = new List<Book>();
        private int currentBookIndex = 0;

        public Home()
        {
            InitializeComponent();
            db = new dbClient();

            // Atur label sambutan agar transparan di atas banner
            label15.Parent = pictureBox8;
            label15.BackColor = Color.Transparent;
            label15.Location = new Point(20, 20);
            label15.BringToFront();

            // Konfigurasi Timer Banner (5 detik)
            bannerTimer.Interval = 5000;
            bannerTimer.Tick += BannerTimer_Tick;
        }

        // ======================================================
        // DASHBOARD DATA
        // ======================================================

        // Memuat data statistik user saat dashboard dibuka
        private void Home_Load(object sender, EventArgs e)
        {
            // Update Tanggal Hari Ini
            label6.Text = DateTime.Now.ToString("dddd, dd/MM/yyyy");

            // Update Nama User di Dashboard
            if (!string.IsNullOrEmpty(Session.FullName))
            {
                label5.Text = "Welcome Back, " + Session.FullName + "!";
            }

            // 1. Load statistik berdasarkan data yang ada di database
            LoadUserStats();

            // 2. Loading buku rekomendasi
            LoadRecommendedBooks();

            // 3. Loading buku yang segera habis masa peminjaman nya
            LoadDueSoon();
        }
        // menampilkan stats user
        private void LoadUserStats()
        {
            try
            {
                DataTable dtStats = db.getUserStats(Session.UserEmail);
                if (dtStats != null && dtStats.Rows.Count > 0)
                {
                    DataRow row = dtStats.Rows[0];
                    label9.Text = row["Active"].ToString();
                    label16.Text = row["Active"].ToString();
                    label11.Text = row["Overdue"].ToString();
                    label13.Text = row["TotalRead"].ToString();
                }
            }
            catch { /* Default values in designer are 0 */ }
        }

        // Mengambil daftar buku terbaru untuk dijadikan slideshow di banner
        private void LoadRecommendedBooks()
        {
            try
            {
                recommendedBooks = db.getRecommendedBooks();
                if (recommendedBooks.Count > 0)
                {
                    currentBookIndex = 0;
                    DisplayBookInBanner(recommendedBooks[0]);

                    // Mulai timer jika buku lebih dari 1
                    if (recommendedBooks.Count > 1)
                        bannerTimer.Start();
                }
                else
                {
                    // Fallback jika database kosong
                    label15.Text = "Welcome to BorrowIn!\r\nBrowse our books in the catalog.";
                }
            }
            catch (Exception ex) { Console.WriteLine("Error loading recommendations: " + ex.Message); }
        }


        // Logika pergantian buku setiap 5 detik
        private void BannerTimer_Tick(object sender, EventArgs e)
        {
            if (recommendedBooks.Count == 0) return;

            currentBookIndex++;
            if (currentBookIndex >= recommendedBooks.Count)
                currentBookIndex = 0;

            DisplayBookInBanner(recommendedBooks[currentBookIndex]);
        }

        // Menampilkan data buku tertentu ke dalam elemen banner
        private void DisplayBookInBanner(Book book)
        {
            label15.Text = $"Recommended: {book.Title}\r\nBy {book.Author} - Borrow Now!";
        }

        private void LoadDueSoon()
        {
            try
            {
                DataTable dtDue = db.getDueSoon(Session.UserEmail);
                dgvDueSoon.DataSource = dtDue;

                // UI adjustments
                if (dtDue != null && dtDue.Rows.Count > 0)
                {
                    labelDueSoon.Visible = true;
                    dgvDueSoon.Visible = true;
                    labelDueSoon.Text = "Due Soon (Next 3 Days)";
                    labelDueSoon.ForeColor = Color.OrangeRed;
                }
                else
                {
                    labelDueSoon.Text = "No Upcoming Deadlines";
                    labelDueSoon.ForeColor = Color.SeaGreen;
                    dgvDueSoon.Visible = false;
                }
            }
            catch { dgvDueSoon.Visible = false; }
        }

        // ======================================================
        // NAVIGATION
        // ======================================================

        private void StatLabel_Click(object sender, EventArgs e)
        {
            // Semua stat klik mengarah ke History page
            label3_Click(sender, e);
        }

        // Pindah ke halaman BookList (Daftar Buku)
        private void label2_Click(object sender, EventArgs e)
        {
            if (this.MdiParent is Cover mainCover)
            {
                mainCover.OpenChildForm(new BookLists());
                this.Close();
            }
            else
            {
                BookLists frmBookLists = new BookLists();
                frmBookLists.Show();
                this.Close();
            }
        }

        // Pindah ke halaman History (Riwayat Pinjam)
        private void label3_Click(object sender, EventArgs e)
        {
            if (this.MdiParent is Cover mainCover)
            {
                mainCover.OpenChildForm(new History());
                this.Close();
            }
            else
            {
                History historyForm = new History();
                historyForm.Show();
                this.Close();
            }
        }

        // Pindah ke halaman BorrowForm (Pinjam Buku)
        private void label18_Click(object sender, EventArgs e)
        {
            if (this.MdiParent is Cover mainCover)
            {
                mainCover.OpenChildForm(new BorrowForm());
                this.Close();
            }
            else
            {
                BorrowForm borrowForm = new BorrowForm();
                borrowForm.Show();
                this.Close();
            }
        }

        // Logout dan kembali ke layar splash
        private void logOutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            // Panggil helper navigasi menu untuk memproses logout dengan konfirmasi
            MenuNavigationHelper.Logout(this);
        }

        // Hapus akun dengan konfirmasi dan captcha
        private void deleteAccountToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Session.UserEmail)) return;

            DialogResult result = MessageBox.Show("Are you sure to delete your account?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.No) return;

            if (!SecurityHelper.VerifyDeletionCaptcha()) return;

            if (db.deleteUser(Session.UserEmail))
            {
                MessageBox.Show("Account deleted");
                if (this.MdiParent is Cover mainCover)
                {
                    mainCover.ShowCoverPanel();
                    this.Close();
                }
                else
                {
                    new Cover().Show();
                    this.Close();
                }
            }
        }

        private void label1_Click(object sender, EventArgs e) { /* Home - Do Nothing */ }
        private void homeToolStripMenuItem_Click(object sender, EventArgs e) { /* Home - Do Nothing */ }
        private void logOutToolStripMenuItem_Click(object sender, EventArgs e) { }

        // Konfirmasi Keluar Aplikasi
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure to leave the BorrowIn App?", "Confirm Close", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes) Application.Exit();
        }

        // Klik Hero Banner bawa ke Book List
        private void pictureBox8_Click(object sender, EventArgs e)
        {
            label2_Click(sender, e);
        }

        private void pictureBox2_Click(object sender, EventArgs e) { }
        private void pictureBox1_Click(object sender, EventArgs e) { }
        private void pictureBox13_Click(object sender, EventArgs e) { }
    }
}
