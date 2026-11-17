using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BorrowIn
{
    public partial class BorrowForm : Form
    {
        // Koneksi ke database
        private dbClient dbClient;

        // Menyimpan ID buku yang dipilih dari halaman sebelumnya
        private string preSelectedBookId = null;

        // Inisialisasi form peminjaman buku
        public BorrowForm(string bookId = null)
        {
            InitializeComponent();

            lblStatus.Visible = false;

            dbClient = new dbClient();

            // Batasi durasi peminjaman maksimal 14 hari sesuai aturan perpustakaan
            numericUpDown1.Minimum = 1;
            numericUpDown1.Maximum = 14;

            preSelectedBookId = bookId; // Simpan ID buku yang akan dipinjam

            MenuNavigationHelper.SetupMenu(
                this,
                homeToolStripMenuItem,
                logOutToolStripMenuItem,
                exitToolStripMenuItem
            );
        }

        // Memuat daftar buku dan data user saat form dibuka
        private void BorrowForm_Load(object sender, EventArgs e)
        {
            List<Book> books = dbClient.getBooks();

            comboBox1.DataSource = books;
            comboBox1.DisplayMember = "Title";
            comboBox1.ValueMember = "BookId";
            label3.Text = Session.UserName;

            // Jika form dipanggil dengan ID buku tertentu, pilih buku tersebut secara otomatis
            if (!string.IsNullOrEmpty(preSelectedBookId))
            {
                comboBox1.SelectedValue = preSelectedBookId;
            }
        }

        // Memproses transaksi peminjaman buku
        private void button1_Click(object sender, EventArgs e)
        {
            // Validasi buku telah dipilih
            if (comboBox1.SelectedItem == null)
            {
                MessageBox.Show("Please select a book!");
                return;
            }

            Book selectedBook = (Book)comboBox1.SelectedItem;

            // Validasi ketersediaan stok buku
            if (selectedBook.Stock <= 0)
            {
                MessageBox.Show("Book is out of stock!");
                return;
            }

            // Cek apakah user sudah meminjam maksimal 3 buku yang belum dikembalikan (active atau overdue)
            int activeCount = dbClient.getActiveBorrowCount(Session.UserID.ToString());
            if (activeCount >= 3)
            {
                MessageBox.Show("Sorry, you have borrowed a maximum of 3 books that have not been returned..\n" +
                                "Please return the book first before borrowing it again according to library rules.",
                                "Loan Limit Reached", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return; // Batalkan proses peminjaman
            }

            selectedBook.Stock--;

            // Menentukan lama peminjaman berdasarkan input user
            int borrowTime = (int)numericUpDown1.Value;

            DateTime borrowDate = DateTime.Now;
            DateTime deadlineDate = borrowDate.AddDays(borrowTime);

            // Membuat kode pickup unik untuk pengambilan buku
            string pickupCode =
                Guid.NewGuid().ToString().Substring(0, 6).ToUpper();

            // Membuat ID peminjaman otomatis
            string borrowId = dbClient.generateNextBorrowId();

            // Membentuk data peminjaman baru
            Borrower record = new Borrower
            {
                BorrowId = borrowId,
                UserID = Session.UserID,
                BookId = selectedBook.BookId,
                BorrowDate = borrowDate,
                DeadlineDate = deadlineDate,
                PickupCode = pickupCode,
                Status = "active"
            };

            // Menyimpan transaksi peminjaman ke database
            dbClient.addBorrow(record);

            MessageBox.Show("Book borrowed successfully!");

            lblStatus.Visible = true;

            // Menampilkan informasi peminjaman yang berhasil dibuat
            lblStatus.Text =
                $"{Session.UserName} borrowed '{selectedBook.Title}' " +
                $"for {borrowTime} days.\n" +
                $"Pickup Code: {pickupCode}";

            lblStatus.ForeColor = Color.Green;
        }

        // Logout dari akun yang sedang aktif
        private void logOutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            // Panggil helper navigasi menu untuk memproses logout dengan konfirmasi
            MenuNavigationHelper.Logout(this);
        }

        // Menghapus akun user yang sedang login
        private void deleteAccountToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Session.UserEmail))
            {
                MessageBox.Show("User unknown");
                return;
            }

            DialogResult result = MessageBox.Show(
                "Are you sure to delete your account?",
                "Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.No) return;

            // --- TAMBAHAN KEAMANAN: CAPTCHA ---
            if (!SecurityHelper.VerifyDeletionCaptcha())
            {
                return; // Batalkan jika captcha salah
            }

            dbClient db = new dbClient();
            bool success = db.deleteUser(Session.UserEmail);

            if (success)
            {
                MessageBox.Show("Account deleted");
                if (this.MdiParent is Cover mainCover)
                {
                    mainCover.ShowCoverPanel();
                    this.Close();
                }
                else
                {
                    Cover cover = new Cover();
                    cover.Show();
                    this.Close();
                }
            }
            else
            {
                MessageBox.Show("Failed to delete account");
            }
        }
        // Menutup aplikasi BorrowIn
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Are you sure to leave the BorrowIn App?",
                "Confirm Close",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        // Navigasi kembali ke dashboard sesuai role user
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

        // Sidebar navigation ke halaman Home
        private void label8_Click(object sender, EventArgs e)
        {
            if (this.MdiParent is Cover mainCover)
            {
                mainCover.OpenChildForm(new Home());
                this.Close(); // Tutup form saat ini untuk menghemat memori
            }
            else
            {
                Home home = new Home();
                home.Show();
                this.Close();
            }
        }
        // Sidebar navigation ke halaman Book Lists
        private void label7_Click(object sender, EventArgs e)
        {
            if (this.MdiParent is Cover mainCover)
            {
                mainCover.OpenChildForm(new BookLists());
                this.Close();
            }
            else
            {
                BookLists bookLists = new BookLists();
                bookLists.Show();
                this.Close();
            }
        }

        // Sidebar navigation ke halaman History
        private void label5_Click(object sender, EventArgs e)
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

        // Halaman yang sedang aktif (Borrow Form)
        private void label18_Click(object sender, EventArgs e)
        {
            // Sudah berada di halaman BorrowForm, jadi ga ada kemana mana
        }
    }
}