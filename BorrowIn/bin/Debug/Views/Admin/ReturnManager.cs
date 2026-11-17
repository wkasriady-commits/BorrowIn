using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BorrowIn
{
    // Form untuk memproses pengembalian buku dan denda keterlambatan
    public partial class ReturnManager : Form
    {
        private dbClient dbClient;
        private string connectionString = "Data Source=.\\SQLEXPRESS;Initial Catalog=BorrowIn;Integrated Security=true;TrustServerCertificate=True;";
        private string selectedBorrowId = null; // Menyimpan ID transaksi yang dipilih
        private DataTable dtReturns = null; // Cache data peminjaman aktif untuk fitur pencarian

        public ReturnManager()
        {
            InitializeComponent();
            dbClient = new dbClient();
        }

        // Inisialisasi halaman saat form dibuka
        private void ReturnManager_Load(object sender, EventArgs e)
        {
            // Security Check: Memastikan hanya Admin dan Staff yang bisa mengakses Return Manager
            if (Session.UserRole != "admin" && Session.UserRole != "staff")
            {
                MessageBox.Show("Unauthorized Access!", "Security Alert", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                if (this.MdiParent is Cover mainCover)
                    mainCover.OpenChildForm(new Home());
                
                this.BeginInvoke(new MethodInvoker(this.Close));
                return;
            }

            LoadPendingReturns(); // Memuat daftar peminjaman yang belum dikembalikan
        }

        // Mengambil data peminjaman aktif dari database
        private void LoadPendingReturns()
        {
            try
            {
                dtReturns = dbClient.getPendingReturns();
                dataGridView1.DataSource = dtReturns;

                // Menyembunyikan kolom ID internal agar tabel lebih bersih
                if (dataGridView1.Columns.Contains("user_id")) dataGridView1.Columns["user_id"].Visible = false;
                if (dataGridView1.Columns.Contains("book_id")) dataGridView1.Columns["book_id"].Visible = false;
                if (dataGridView1.Columns.Contains("file_path")) dataGridView1.Columns["file_path"].Visible = false;
                if (dataGridView1.Columns.Contains("author")) dataGridView1.Columns["author"].Visible = false;

                // Mengubah nama header agar lebih ramah user (Bahasa Inggris sesuai UI)
                if (dataGridView1.Columns.Contains("borrow_id")) dataGridView1.Columns["borrow_id"].HeaderText = "ID";
                if (dataGridView1.Columns.Contains("userName")) dataGridView1.Columns["userName"].HeaderText = "Borrower";
                if (dataGridView1.Columns.Contains("title")) dataGridView1.Columns["title"].HeaderText = "Book";
                if (dataGridView1.Columns.Contains("borrow_date")) dataGridView1.Columns["borrow_date"].HeaderText = "Date";
                if (dataGridView1.Columns.Contains("due_date")) dataGridView1.Columns["due_date"].HeaderText = "Due";
                if (dataGridView1.Columns.Contains("status")) dataGridView1.Columns["status"].HeaderText = "Status";
            }
            catch (Exception ex) { MessageBox.Show("Error loading returns: " + ex.Message); }
        }

        // Event saat salah satu baris di tabel di-klik
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = dataGridView1.Rows[e.RowIndex];
            selectedBorrowId = row.Cells["borrow_id"].Value.ToString();

            // Tampilkan informasi detail di panel samping
            lblBorrowIDValue.Text = selectedBorrowId;
            lblBookTitleValue.Text = row.Cells["title"].Value.ToString();
            lblBorrowerValue.Text = row.Cells["userName"].Value.ToString();
            
            DateTime borrowDate = Convert.ToDateTime(row.Cells["borrow_date"].Value);
            DateTime dueDate = Convert.ToDateTime(row.Cells["due_date"].Value);

            lblBorrowDateValue.Text = borrowDate.ToShortDateString();
            lblDueDateValue.Text = dueDate.ToShortDateString();

            // --- LOGIKA HITUNG SISA WAKTU & DENDA ---
            TimeSpan diff = dueDate.Date - DateTime.Today; // Selisih tanggal hari ini dengan jatuh tempo
            int days = (int)diff.TotalDays;

            if (days < 0) // Jika nilai negatif, artinya sudah lewat jatuh tempo (TERLAMBAT)
            {
                int lateDays = Math.Abs(days);
                lblTimeLeftValue.Text = $"({lateDays} days late)";
                lblTimeLeftValue.ForeColor = Color.Red; // Warna merah untuk peringatan terlambat

                // Hitung denda: Jumlah hari telat * Rp 2.500
                int fee = lateDays * 2500;
                lblOverdueFeeValue.Text = "Rp " + fee.ToString("N0"); // Format mata uang (N0)
                lblOverdueFeeValue.Visible = true;
                lblOverdueFee.Visible = true;
            }
            else // Belum terlambat
            {
                lblTimeLeftValue.Text = $"({days} days left)";
                lblTimeLeftValue.ForeColor = Color.Blue; // Warna biru untuk sisa waktu aman
                lblOverdueFeeValue.Text = "Rp 0";
                lblOverdueFeeValue.Visible = false; // Sembunyikan label denda jika tidak ada
                lblOverdueFee.Visible = false;
            }

            // Tampilkan gambar cover buku jika filenya ada
            string fullPath = row.Cells["file_path"].Value.ToString();
            if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
            {
                if (pictureBoxBook.Image != null) pictureBoxBook.Image.Dispose();
                pictureBoxBook.Image = Image.FromFile(fullPath);
            }
            else { pictureBoxBook.Image = null; }
        }

        // Tombol Submit untuk memproses pengembalian secara resmi
        private void UpdateButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedBorrowId))
            {
                MessageBox.Show("Please select a borrow record first.");
                return;
            }

            // Tampilkan konfirmasi agar tidak salah klik
            DialogResult confirm = MessageBox.Show($"Mark Borrow ID {selectedBorrowId} as returned?", "Confirm Return", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (confirm == DialogResult.Yes)
            {
                // Update status di DB menjadi 'returned' (Pustakawan menyetujui pengembalian fisik buku)
                if (dbClient.updateBorrowStatus(selectedBorrowId, "returned"))
                {
                    MessageBox.Show("Return submitted successfully.");
                    ClearDetails(); // Bersihkan panel detail
                    LoadPendingReturns(); // Refresh list agar item yang sudah dikembalikan hilang
                }
            }
        }

        // Mengosongkan tampilan detail
        private void ClearDetails()
        {
            selectedBorrowId = null;
            lblBorrowIDValue.Text = "-";
            lblBookTitleValue.Text = "-";
            lblBorrowerValue.Text = "-";
            lblBorrowDateValue.Text = "-";
            lblDueDateValue.Text = "-";
            lblTimeLeftValue.Text = "-";
            lblOverdueFeeValue.Text = "-";
            pictureBoxBook.Image = null;
        }

        // Fitur Pencarian Real-time berdasarkan Nama Peminjam atau Judul Buku
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            if (dtReturns == null) return;

            string filter = txtSearch.Text.Trim().Replace("'", "''"); // Handle karakter tanda kutip (') agar query filter tidak error
            
            if (string.IsNullOrEmpty(filter))
            {
                dataGridView1.DataSource = dtReturns; // Jika kosong, tampilkan semua
            }
            else
            {
                try
                {
                    DataView dv = dtReturns.DefaultView;
                    // Filter menggunakan userName (Peminjam) atau title (Judul Buku)
                    dv.RowFilter = $"userName LIKE '%{filter}%' OR title LIKE '%{filter}%'";
                    dataGridView1.DataSource = dv.ToTable();
                }
                catch (Exception) { /* Abaikan jika input filter tidak valid formatnya */ }
            }
        }

        // Klik logo BorrowIn kembali ke dashboard utama sesuai role
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

        // Sidebar Navigation: Ke halaman Book Manager
        private void pictureBox2_Click(object sender, EventArgs e)
        {
            button1_Click(sender, e);
        }

        // Sidebar Navigation: Ke halaman Book Request
        private void button2_Click(object sender, EventArgs e)
        {
            if (this.MdiParent is Cover mainCover)
                mainCover.OpenChildForm(new BookRequest());
            else
                new BookRequest().Show();
            this.Close();
        }

        // Sidebar Navigation: Ke halaman Book Manager
        private void button1_Click(object sender, EventArgs e)
        {
            if (this.MdiParent is Cover mainCover)
                mainCover.OpenChildForm(new BookManager());
            else
                new BookManager().Show();
            this.Close();
        }

        // Sidebar Navigation: Refresh Halaman Return Manager
        private void button3_Click(object sender, EventArgs e)
        {
            LoadPendingReturns(); // Refresh data
        }

        private void btnCetakOverdue_Click(object sender, EventArgs e)
        {
            if (cmbFilterOverdue.SelectedItem == null) return;

            string filterTerpilih = cmbFilterOverdue.SelectedItem.ToString().Trim();

            // Query untuk mendapatkan data Borrow yang di-filter
            string queryBorrow = "SELECT * FROM Borrow WHERE 1=1";
            if (filterTerpilih == "Overdue")
            {
                queryBorrow += " AND status = 'overdue'";
            }
            else if (filterTerpilih == "Active")
            {
                queryBorrow += " AND status = 'active'";
            }

            string queryUsers = "SELECT * FROM users";
            string queryBook = "SELECT * FROM Book";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    DataSet ds = new DataSet();

                    // 1. Ambil data Borrow
                    SqlDataAdapter adapterBorrow = new SqlDataAdapter(queryBorrow, conn);
                    DataTable dtBorrow = new DataTable("Borrow");
                    adapterBorrow.Fill(dtBorrow);
                    ds.Tables.Add(dtBorrow);

                    // 2. Ambil data Users
                    SqlDataAdapter adapterUsers = new SqlDataAdapter(queryUsers, conn);
                    DataTable dtUsers = new DataTable("users");
                    adapterUsers.Fill(dtUsers);
                    ds.Tables.Add(dtUsers);

                    // 3. Ambil data Book
                    SqlDataAdapter adapterBook = new SqlDataAdapter(queryBook, conn);
                    DataTable dtBook = new DataTable("Book");
                    adapterBook.Fill(dtBook);
                    ds.Tables.Add(dtBook);

                    if (dtBorrow.Rows.Count == 0)
                    {
                        MessageBox.Show("No Data for Borrowed", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    // Panggil report Overdue
                    OverdueReport report = new OverdueReport();

                    // Bersihkan saved data dan koneksi lama
                    report.DataSourceConnections.Clear();

                    // Set data source untuk masing-masing tabel secara eksplisit
                    report.Database.Tables["Borrow"].SetDataSource(dtBorrow);
                    report.Database.Tables["users"].SetDataSource(dtUsers);
                    report.Database.Tables["Book"].SetDataSource(dtBook);

                    report.Refresh();

                    // Menampilkan
                    ReportViewerForm viewer = new ReportViewerForm(report);
                    viewer.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error generating report: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            } // End using SqlConnection
        } // End btnCetakOverdue_Click

        private void cmbFilterOverdue_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    } // End partial class ReturnManager
} // End namespace BorrowIn
