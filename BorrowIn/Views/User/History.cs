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
    // Form untuk User melihat riwayat peminjaman pribadi (Buku yang sedang dipinjam vs sudah kembali)
    public partial class History : Form
    {
        public History()
        {
            InitializeComponent();
            // Setup menu navigasi standar (Home, Logout, Exit)
            MenuNavigationHelper.SetupMenu(this, homeToolStripMenuItem, logOutToolStripMenuItem, exitToolStripMenuItem);
            
            // Set deskripsi buku menjadi Read-Only agar user tidak bisa mengubah info buku
            richTextBox1.ReadOnly = true;

            // Mendaftarkan event click untuk kedua tabel riwayat
            dataGridView1.CellClick += DataGridView_CellClick;
            dataGridView2.CellClick += DataGridView_CellClick;

            // --- KONFIGURASI PEMETAAN DATA (Data Binding) ---
            // DataGridView1: Riwayat Peminjaman Selesai (Previously Borrowed)
            dataGridView1.AutoGenerateColumns = false; // Matikan auto-column agar menggunakan desain kita sendiri
            dataGridView1.Columns["Title"].DataPropertyName = "title";
            dataGridView1.Columns["Column1"].DataPropertyName = "borrow_date";
            dataGridView1.Columns["Column2"].DataPropertyName = "due_date";
            dataGridView1.Columns["Column3"].DataPropertyName = "return_date";
            dataGridView1.Columns["Column4"].DataPropertyName = "status";

            // DataGridView2: Peminjaman yang Sedang Berjalan (Currently Borrowed)
            dataGridView2.AutoGenerateColumns = false;
            dataGridView2.Columns["dataGridViewTextBoxColumn1"].DataPropertyName = "title";
            dataGridView2.Columns["dataGridViewTextBoxColumn2"].DataPropertyName = "borrow_date";
            dataGridView2.Columns["dataGridViewTextBoxColumn3"].DataPropertyName = "due_date";
            dataGridView2.Columns["dataGridViewTextBoxColumn4"].DataPropertyName = "return_date";
            dataGridView2.Columns["dataGridViewTextBoxColumn5"].DataPropertyName = "status";
        }

        // Event saat sel di salah satu tabel di-klik untuk melihat detail buku di panel samping
        private void DataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return; // Abaikan jika klik pada header

            DataGridView dgv = (DataGridView)sender;
            DataGridViewRow row = dgv.Rows[e.RowIndex];

            // Ambil data asli dari baris yang terikat (DataBoundItem)
            if (row.DataBoundItem != null)
            {
                DataRow dataRow = ((DataRowView)row.DataBoundItem).Row;
                ShowBookDetails(dataRow); // Tampilkan ke panel detail di sebelah kanan
            }
        }

        // Menampilkan informasi detail buku ke elemen Label dan PictureBox di sidebar kanan
        private void ShowBookDetails(DataRow row)
        {
            HistLabTitle.Text = row["title"].ToString();
            HistLabAuth.Text = row["author"].ToString();
            HistLabISBN13.Text = row["isbn_13"]?.ToString() ?? "-";
            HistLabISBN10.Text = row["isbn_10"]?.ToString() ?? "-";
            HistLabPubDate.Text = row["publish"] != DBNull.Value ? Convert.ToDateTime(row["publish"]).ToShortDateString() : "-";
            richTextBox1.Text = row["deskripsi"].ToString();

            // Meta-data buku tambahan
            HistLabPag.Text = row["author_pages"]?.ToString() ?? "-";
            HistLabLang.Text = row["language"]?.ToString() ?? "-";
            HistLabPublisher.Text = row["publisher"]?.ToString() ?? "-";
            HistLabDim.Text = row["dimensions"]?.ToString() ?? "-";

            // --- LOGIKA LOAD GAMBAR COVER ---
            string fullPath = row["file_path"].ToString();
            if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
            {
                try
                {
                    if (pictureBox5.Image != null) pictureBox5.Image.Dispose(); // Hapus memory gambar lama
                    pictureBox5.Image = Image.FromFile(fullPath); // Load gambar baru dari path absolut database
                    pictureBox5.SizeMode = PictureBoxSizeMode.Zoom;
                }
                catch { pictureBox5.Image = null; }
            }
            else { pictureBox5.Image = null; }
        }

        public string userEmail;

        // Navigasi Sidebar: Kembali ke Home
        private void label8_Click(object sender, EventArgs e)
        {
            if (this.MdiParent is Cover mainCover)
            {
                mainCover.OpenChildForm(new Home());
                this.Close();
            }
            else
            {
                Home Home = new Home();
                Home.Show();
                this.Close();
            }
        }

        // Tombol Menu Strip Home
        private void homeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            label8_Click(sender, e);
        }

        // Konfirmasi Keluar Aplikasi
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure to leave the BorrowIn App?", "Confirm Close", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes) Application.Exit();
        }

        // Navigasi Sidebar: Lihat Daftar Buku
        private void label7_Click(object sender, EventArgs e)
        {
            if (this.MdiParent is Cover mainCover)
            {
                mainCover.OpenChildForm(new BookLists());
                this.Close();
            }
            else
            {
                BookLists BookLists = new BookLists();
                BookLists.Show();
                this.Close();
            }
        }

        // Navigasi Sidebar: Pinjam Buku
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

        // Logout via Menu Strip
        private void logOutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            // Panggil helper navigasi menu untuk memproses logout dengan konfirmasi
            MenuNavigationHelper.Logout(this);
        }

        // Fitur Hapus Akun dengan Keamanan Captcha
        private void deleteAccountToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Session.UserEmail)) return;

            DialogResult result = MessageBox.Show("Are you sure to delete your account?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.No) return;

            // --- KEAMANAN TAMBAHAN ---
            // Meminta user mengetikkan kode random sebelum hapus permanen
            if (!SecurityHelper.VerifyDeletionCaptcha()) return;

            dbClient db = new dbClient();
            if (db.deleteUser(Session.UserEmail))
            {
                MessageBox.Show("Account deleted");
                if (this.MdiParent is Cover mainCover)
                {
                    mainCover.ShowCoverPanel();
                    this.Close();
                }
            }
        }

        // Event saat halaman Riwayat dimuat: Ambil data berdasarkan Email user yang login
        private void History_Load(object sender, EventArgs e)
        {
            try
            {
                dbClient db = new dbClient();
                // Mengambil data peminjaman global dari tabel Join via Session.UserEmail
                DataTable dtHistory = db.getHistoryByUser(Session.UserEmail);

                if (dtHistory != null && dtHistory.Rows.Count > 0)
                {
                    // Menggunakan LINQ untuk memisahkan dan mengurutkan data menjadi dua bagian
                    // 1. Yang masih berstatus dipinjam atau telat (active/overdue) - diurutkan berdasarkan tenggat waktu terdekat (due_date)
                    var currentlyBorrowed = dtHistory.AsEnumerable()
                        .Where(row => row.Field<string>("status") == "active" || row.Field<string>("status") == "overdue")
                        .OrderBy(row => row.Field<DateTime>("due_date"));

                    // 2. Yang sudah dikembalikan (returned) - diurutkan berdasarkan tanggal kembali terbaru (return_date)
                    var previouslyBorrowed = dtHistory.AsEnumerable()
                        .Where(row => row.Field<string>("status") == "returned")
                        .OrderByDescending(row => row.Field<DateTime>("return_date"));

                    // Bind hasil filter dan pengurutan ke masing-masing DataGridView
                    if (currentlyBorrowed.Any())
                        dataGridView2.DataSource = currentlyBorrowed.CopyToDataTable();
                    else
                        dataGridView2.DataSource = null;

                    if (previouslyBorrowed.Any())
                        dataGridView1.DataSource = previouslyBorrowed.CopyToDataTable();
                    else
                        dataGridView1.DataSource = null;
                }
            }
            catch (Exception ex) { MessageBox.Show("Error memuat riwayat: " + ex.Message); }
        }

        // --- RESTORED MISSING EVENT HANDLERS FOR DESIGNER ---
        private void label5_Click(object sender, EventArgs e) { /* Sidebar Link: History */ }
        private void groupBox1_Enter(object sender, EventArgs e) { }
        private void label15_Click(object sender, EventArgs e) { }
        private void richTextBox1_TextChanged(object sender, EventArgs e) { }
        private void label3_Click(object sender, EventArgs e) { }
        private void label1_Click(object sender, EventArgs e) { }
        private void pictureBox5_Click(object sender, EventArgs e) { }
        private void groupBox2_Enter(object sender, EventArgs e) { }
        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void pictureBox13_Click(object sender, EventArgs e) { }

        // Logo click redirect
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Form dashboard;
            if (Session.UserRole == "admin") dashboard = new AdminPage();
            else if (Session.UserRole == "staff") dashboard = new LibrarianPage();
            else dashboard = new Home();

            if (this.MdiParent is Cover mainCover)
            {
                mainCover.OpenChildForm(dashboard);
                this.Close();
            }
            else
            {
                dashboard.Show();
                this.Close();
            }
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }
    }
}
