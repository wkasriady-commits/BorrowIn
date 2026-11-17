using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BorrowIn
{
    public partial class BookLists : Form
    {
        // Menyimpan riwayat pencarian untuk fitur AutoComplete
        private string[] searchHistory = new string[0];
        // Menyimpan email user yang sedang aktif
        public string userEmail;

        // Inisialisasi form dan konfigurasi komponen pencarian
        public BookLists()
        {
            InitializeComponent();

            textBox1.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            textBox1.AutoCompleteSource = AutoCompleteSource.CustomSource;

            MenuNavigationHelper.SetupMenu(this, homeToolStripMenuItem, logOutToolStripMenuItem, exitToolStripMenuItem1);
        }

        // Menjalankan pencarian saat ikon search diklik
        private void pictureBox5_Click(object sender, EventArgs e)
        {
            ProsesPencarian(textBox1.Text);
        }

        // Navigasi ke halaman Home
        private void label1_Click(object sender, EventArgs e)
        {
            if (this.MdiParent is Cover mainCover)
            {
                mainCover.OpenChildForm(new Home());
                this.Close(); // Tutup Form saat ini agar tidak bocor di memori
            }
            else
            {
                Home Home = new Home();
                Home.Show();
                this.Close();
            }
        }
        // Inisialisasi tampilan daftar buku saat form dibuka
        private void BookLists_Load_1(object sender, EventArgs e)
        {
            PictureBox[] trendingBooks = { pictureBox6, pictureBox7, pictureBox8 };

            for (int i = 0; i < trendingBooks.Length; i++)
            {
                trendingBooks[i].BackColor = Color.LightGray;
                trendingBooks[i].BorderStyle = BorderStyle.FixedSingle;
                trendingBooks[i].Cursor = Cursors.Hand; // Ubah kursor menjadi tangan saat hover
            }

            // Daftarkan event handler klik untuk ketiga PictureBox buku
            pictureBox6.Click += new EventHandler(pictureBox_Book_Click);
            pictureBox7.Click += new EventHandler(pictureBox_Book_Click);
            pictureBox8.Click += new EventHandler(pictureBox_Book_Click);

            TampilkanBuku("");
        }

        // Menjalankan pencarian saat tombol Enter ditekan
        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                ProsesPencarian(textBox1.Text);
            }
        }

        // Menampilkan seluruh daftar buku dan mereset pencarian.
        private void label2_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
            TampilkanBuku("");
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
        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
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

        // Navigasi kembali ke halaman Home
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Redirect to Home
            if (this.MdiParent is Cover mainCover)
            {
                mainCover.OpenChildForm(new Home());
                this.Close();
            }
            else
            {
                Home home = new Home();
                home.Show();
                this.Close();
            }
        }

        // Sidebar navigation ke Home
        private void label1_Click_1(object sender, EventArgs e)
        {
            if (this.MdiParent is Cover mainCover)
            {
                mainCover.OpenChildForm(new Home());
                this.Close(); // Bebaskan memori
            }
            else
            {
                Home Home = new Home();
                Home.Show();
                this.Close();
            }
        }

        // Sidebar navigation ke halaman History
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
                this.Close(); // Gunakan Close() untuk menghindari kebocoran memori
            }
        }

        // Sidebar navigation ke halaman Borrow Form
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
        // Menampilkan ulang daftar buku saat textbox pencarian dikosongkan
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            // Mengecek apakah textbox pencarian sedang kosong
            if (string.IsNullOrEmpty(textBox1.Text))
            {
                // Panggil tanpa keyword untuk memunculkan semua 3 buku kembali
                TampilkanBuku("");
            }
        }
        // refresh picture box supaya loading
        private void RefreshPictureBox(PictureBox pb, string fullPath)
        {
            try
            {
                if (File.Exists(fullPath))
                {
                    // Menggunakan Image.FromFile jauh lebih stabil daripada ImageLocation
                    pb.Image = Image.FromFile(fullPath);
                    pb.SizeMode = PictureBoxSizeMode.StretchImage;
                }
                else
                {
                    // Jika file tidak ada di folder Assets, ganti warna jadi abu-abu tua
                    pb.BackColor = Color.DarkGray;
                    // MessageBox.Show("File tidak ditemukan: " + fullPath); // Aktifkan ini untuk debug jika masih gagal
                }
            }
            catch (Exception)
            {
                // Jika ada error teknis saat muat file
                pb.BackColor = Color.Red;
            }
        }

        // menampilkan seluruh buku
        private void TampilkanBuku(string keyword = "")
        {
            dbClient db = new dbClient();
            
            // Ambil seluruh data buku dari database (termasuk kolom file_path)
            DataTable dtBooks = db.getAllBooks();

            PictureBox[] listPictureBox = { pictureBox6, pictureBox7, pictureBox8 };

            // --- PENGELOLAAN MEMORI ---
            // Bersihkan semua gambar lama dari PictureBox sebelum memuat yang baru.
            // .Dispose() sangat penting untuk menghapus cache gambar di RAM agar aplikasi tidak berat.
            foreach (var pb in listPictureBox)
            {
                if (pb.Image != null)
                {
                    pb.Image.Dispose(); // Hapus memori gambar lama dari RAM
                    pb.Image = null;
                }
                pb.Tag = null; // Reset Tag agar tidak menyimpan data buku lama
                pb.Visible = false;
            }

            // --- LOGIKA TAMPILAN DEFAULT (TANPA KEYWORD) ---
            if (string.IsNullOrWhiteSpace(keyword) || keyword == "find more . . . ")
            {
                // Jangan tampilkan buku apa pun jika belum dicari (sesuai request user)
                return;
            }

            // --- LOGIKA PENCARIAN (DENGAN KEYWORD) ---
            // Menggunakan LINQ untuk memfilter data tabel berdasarkan Title atau Deskripsi
            var hasilCari = dtBooks.AsEnumerable()
                .Where(row =>
                    (row["title"] != DBNull.Value && row["title"].ToString().ToLower().Contains(keyword.ToLower())) ||
                    (row["deskripsi"] != DBNull.Value && row["deskripsi"].ToString().ToLower().Contains(keyword.ToLower()))
                ).ToList();

            // Munculkan hasil pencarian ke PictureBox
            for (int i = 0; i < hasilCari.Count && i < listPictureBox.Length; i++)
            {
                listPictureBox[i].Visible = true;
                listPictureBox[i].Tag = hasilCari[i]; // Simpan baris data hasil cari di Tag
                
                // Ambil path lengkap dari hasil pencarian database
                string pathLengkap = hasilCari[i]["file_path"].ToString();
                RefreshPictureBox(listPictureBox[i], pathLengkap);
            }

            // Validasi jika buku tidak ditemukan
            if (hasilCari.Count == 0)
            {
                MessageBox.Show("Buku tidak ditemukan.");
                TampilkanBuku(""); // Reset ke tampilan awal (3 buku default)
            }
        }

        // mengatur proses pencarian buku
        private void ProsesPencarian(string keyword)
        {
            string searchQuery = keyword.Trim().ToLower();

            // Jika textbox kosong atau hanya placeholder, munculkan semua buku
            if (string.IsNullOrEmpty(searchQuery) || searchQuery == "find more . . . ")
            {
                TampilkanBuku("");
                return;
            }

            // Simpan ke History (Autocomplete) agar tidak hilang
            if (!searchHistory.Contains(searchQuery))
            {
                Array.Resize(ref searchHistory, searchHistory.Length + 1);
                searchHistory[searchHistory.Length - 1] = searchQuery;

                AutoCompleteStringCollection autocompleteData = new AutoCompleteStringCollection();
                autocompleteData.AddRange(searchHistory);
                textBox1.AutoCompleteCustomSource = autocompleteData;
            }

            // Lakukan filter gambar buku
            TampilkanBuku(searchQuery);
        }

        private void pictureBox13_Click(object sender, EventArgs e)
        {

        }

        // Navigasi kembali ke Dashboard sesuai role
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

        // Event handler saat salah satu gambar buku di-klik untuk melihat detail
        private void pictureBox_Book_Click(object sender, EventArgs e)
        {
            if (sender is PictureBox pb && pb.Tag is DataRow bookRow)
            {
                // Buka form dialog detail buku dengan mengirimkan data baris buku
                using (BookDetailForm detailForm = new BookDetailForm(bookRow))
                {
                    if (detailForm.ShowDialog(this) == DialogResult.Yes || detailForm.BorrowRequested)
                    {
                        // Ambil ID buku dari baris database
                        string bookId = bookRow["book_id"] != DBNull.Value ? bookRow["book_id"].ToString() : "";
                        if (!string.IsNullOrEmpty(bookId))
                        {
                            // Navigasi ke BorrowForm dengan membawa ID buku terpilih
                            if (this.MdiParent is Cover mainCover)
                            {
                                mainCover.OpenChildForm(new BorrowForm(bookId));
                                this.Close(); // Bebaskan form saat ini
                            }
                            else
                            {
                                BorrowForm borrowForm = new BorrowForm(bookId);
                                borrowForm.Show();
                                this.Close();
                            }
                        }
                    }
                }
            }
        }
    }
}
