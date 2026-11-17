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
    // Form untuk mengelola daftar buku (Tambah, Edit, Hapus)
    public partial class BookManager : Form
    {
        private dbClient db;
        private string selectedBookId = null; // Menyimpan ID buku yang sedang dipilih user

        public BookManager()
        {
            InitializeComponent();
            db = new dbClient();
        }

        // Inisialisasi halaman saat pertama kali dibuka
        private void BookManager_Load(object sender, EventArgs e)
        {
            // Security Check: Memastikan hanya Admin dan Staff yang bisa mengelola buku
            if (Session.UserRole != "admin" && Session.UserRole != "staff")
            {
                MessageBox.Show("Unauthorized Access!", "Security Alert", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                if (this.MdiParent is Cover mainCover)
                    mainCover.OpenChildForm(new Home());
                
                this.BeginInvoke(new MethodInvoker(this.Close));
                return;
            }

            // Jika user login sebagai staff (Librarian), nonaktifkan tombol tambah, edit, dan hapus
            if (Session.UserRole == "staff")
            {
                AddButton.Enabled = false;
                UpdateButton.Enabled = false;
                DeleteButton.Enabled = false;
            }

            LoadBooks(); // Tampilkan data buku
        }

        // Mengambil data buku dari database dan menampilkan ke DataGridView
        private void LoadBooks()
        {
            try
            {
                DataTable dt = db.getAllBooks();
                dataGridView1.DataSource = dt;

                // Mengatur Header Teks agar lebih rapi (Bahasa Inggris sesuai UI)
                if (dataGridView1.Columns.Contains("book_id")) dataGridView1.Columns["book_id"].HeaderText = "ID";
                if (dataGridView1.Columns.Contains("title")) dataGridView1.Columns["title"].HeaderText = "Title";
                if (dataGridView1.Columns.Contains("author")) dataGridView1.Columns["author"].HeaderText = "Author";
                if (dataGridView1.Columns.Contains("stock")) dataGridView1.Columns["stock"].HeaderText = "Stock";
                if (dataGridView1.Columns.Contains("is_available")) dataGridView1.Columns["is_available"].HeaderText = "Available";

                // Menyembunyikan kolom teknis/metadata agar tabel tidak terlalu penuh
                string[] toHide = { "genre_id", "deskripsi", "cover_img", "file_path", "isbn_13", "isbn_10", "author_pages", "language", "publisher", "dimensions", "publish" };
                foreach (string col in toHide)
                {
                    if (dataGridView1.Columns.Contains(col)) dataGridView1.Columns[col].Visible = false;
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading books: " + ex.Message); }
        }

        // Event saat sel di dalam grid di-klik (Khusus untuk tombol 'Select')
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // Cek apakah kolom yang di-klik adalah kolom tombol 'Select' (colSelect)
            if (dataGridView1.Columns[e.ColumnIndex].Name == "colSelect")
            {
                DataGridViewRow row = dataGridView1.Rows[e.RowIndex];
                selectedBookId = row.Cells["book_id"].Value.ToString(); // Ambil ID buku
                
                // Memberikan feedback visual baris terpilih (warna Biru Muda)
                foreach (DataGridViewRow r in dataGridView1.Rows) r.DefaultCellStyle.BackColor = Color.White;
                row.DefaultCellStyle.BackColor = Color.LightBlue;
            }
        }

        // Membuka dialog untuk menambah buku baru
        private void AddButton_Click(object sender, EventArgs e)
        {
            using (BookDialogForm dialog = new BookDialogForm())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    // Validasi Duplikat (Judul atau ISBN sudah ada di DB)
                    if (db.isBookDuplicate(dialog.Book.Title, dialog.Book.ISBN_13, dialog.Book.ISBN_10))
                    {
                        MessageBox.Show("Book already exists! (Title or ISBN matched)", "Duplicate Entry", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Simpan data buku baru ke Database
                    if (db.addBook(dialog.Book))
                    {
                        MessageBox.Show("Book added successfully!");
                        LoadBooks(); // Refresh list
                    }
                }
            }
        }

        // Membuka dialog untuk mengedit buku yang sudah dipilih
        private void UpdateButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedBookId))
            {
                MessageBox.Show("Please select a book from the list first (click the Select button).");
                return;
            }

            // Ambil baris data asli dari DataSource untuk di-passing ke Dialog
            DataTable dt = (DataTable)dataGridView1.DataSource;
            DataRow row = dt.AsEnumerable().FirstOrDefault(r => r["book_id"].ToString() == selectedBookId);

            if (row != null)
            {
                // Konversi baris DataTable menjadi objek Book dengan pengecekan nilai NULL (DBNull)
                // untuk menghindari crash sistem (InvalidCastException/NullReferenceException)
                Book bookToEdit = new Book
                {
                    BookId = row["book_id"].ToString(),
                    Title = row["title"]?.ToString() ?? "",
                    Author = row["author"]?.ToString() ?? "",
                    Stock = row["stock"] != DBNull.Value ? Convert.ToInt32(row["stock"]) : 0,
                    ISBN_13 = row["isbn_13"]?.ToString() ?? "",
                    ISBN_10 = row["isbn_10"]?.ToString() ?? "",
                    AuthorPages = row["author_pages"] != DBNull.Value ? Convert.ToInt32(row["author_pages"]) : 0,
                    Language = row["language"]?.ToString() ?? "",
                    Publisher = row["publisher"]?.ToString() ?? "",
                    Dimensions = row["dimensions"]?.ToString() ?? "",
                    Description = row["deskripsi"]?.ToString() ?? "",
                    CoverImg = row["cover_img"]?.ToString() ?? "",
                    FilePath = row["file_path"]?.ToString() ?? "",
                    // Validasi khusus untuk tanggal terbit dan ketersediaan buku yang rentan bernilai NULL
                    PublishDate = row["publish"] != DBNull.Value ? Convert.ToDateTime(row["publish"]) : DateTime.MinValue,
                    IsAvailable = row["is_available"] != DBNull.Value ? Convert.ToBoolean(row["is_available"]) : false
                };

                using (BookDialogForm dialog = new BookDialogForm(bookToEdit))
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        // SMART VALIDATION: Only check fields that were actually modified
                        // This prevents blocking updates due to existing duplicates in the sample data
                        string checkTitle = (dialog.Book.Title != bookToEdit.Title) ? dialog.Book.Title : null;
                        string checkISBN13 = (dialog.Book.ISBN_13 != bookToEdit.ISBN_13) ? dialog.Book.ISBN_13 : null;
                        string checkISBN10 = (dialog.Book.ISBN_10 != bookToEdit.ISBN_10) ? dialog.Book.ISBN_10 : null;

                        if (checkTitle != null || checkISBN13 != null || checkISBN10 != null)
                        {
                            if (db.isBookDuplicate(checkTitle, checkISBN13, checkISBN10, selectedBookId))
                            {
                                MessageBox.Show("Another book with this Title or ISBN already exists.", "Duplicate Entry", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }

                        if (db.updateBook(dialog.Book))
                        {
                            MessageBox.Show("Book updated successfully!");
                            LoadBooks();
                        }
                    }
                }
            }
        }

        // Menghapus buku yang sedang dipilih dari database
        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedBookId))
            {
                MessageBox.Show("Please select a book first.");
                return;
            }

            DialogResult confirm = MessageBox.Show($"Are you sure to delete Book ID {selectedBookId}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            
            if (confirm == DialogResult.Yes)
            {
                if (db.deleteBook(selectedBookId))
                {
                    MessageBox.Show("Book deleted successfully.");
                    selectedBookId = null;
                    LoadBooks();
                }
                else { MessageBox.Show("Failed to delete. Book may have active borrow records."); }
            }
        }

        // Sidebar Navigation: Pindah ke halaman Return
        private void button3_Click(object sender, EventArgs e)
        {
            if (this.MdiParent is Cover mainCover)
                mainCover.OpenChildForm(new ReturnManager());
            else
                new ReturnManager().Show();
            this.Close();
        }

        // Klik logo kembali ke dashboard
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Form dashboard;
            if (Session.UserRole == "admin") dashboard = new AdminPage();
            else if (Session.UserRole == "staff") dashboard = new LibrarianPage();
            else dashboard = new Home();

            if (this.MdiParent is Cover mainCover)
                mainCover.OpenChildForm(dashboard);
            else
                dashboard.Show();
            this.Close();
        }

        // --- RESTORED MISSING EVENT HANDLERS ---
        private void pictureBox4_Click(object sender, EventArgs e) { button3_Click(sender, e); }
        private void pictureBox2_Click(object sender, EventArgs e) { LoadBooks(); } // Refresh data

        private void button1_Click(object sender, EventArgs e)
        {
            LoadBooks(); // Refresh data
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            if (this.MdiParent is Cover mainCover)
                mainCover.OpenChildForm(new BookRequest());
            else
                new BookRequest().Show();
            this.Close();
        }

        // Navigasi ke Halaman ViewProcurement (Data Pengadaan Buku)
        private void viewProcurementDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.MdiParent is Cover mainCover)
            {
                mainCover.OpenChildForm(new ViewProcurement());
                this.Close();
            }
            else
            {
                ViewProcurement viewForm = new ViewProcurement();
                viewForm.Show();
                this.Close();
            }
        }

        // Logout dari akun admin dengan konfirmasi
        private void logOutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            MenuNavigationHelper.Logout(this);
        }

        // Keluar dari aplikasi dengan konfirmasi
        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure to leave the BorrowIn App?", "Confirm Close", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes) Application.Exit();
        }
    }
}
