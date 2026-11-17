using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BorrowIn
{
    public partial class BookDetailForm : Form
    {
        public BookDetailForm(DataRow bookRow)
        {
            InitializeComponent();
            PopulateDetails(bookRow);
        }

        // Mengisi detail buku dari data database ke komponen UI secara dinamis
        private void PopulateDetails(DataRow row)
        {
            if (row == null) return;

            // Ambil data dari kolom tabel database Book
            lblTitle.Text = row["title"] != DBNull.Value ? row["title"].ToString() : "N/A";
            lblAuthor.Text = "Penulis: " + (row["author"] != DBNull.Value ? row["author"].ToString() : "N/A");
            lblISBN.Text = "ISBN: " + (row["isbn_13"] != DBNull.Value ? row["isbn_13"].ToString() : "N/A");
            
            if (row["publish"] != DBNull.Value)
            {
                DateTime publishDate = Convert.ToDateTime(row["publish"]);
                lblPublish.Text = "Tahun Terbit: " + publishDate.ToString("yyyy-MM-dd");
            }
            else
            {
                lblPublish.Text = "Tahun Terbit: N/A";
            }

            txtDescription.Text = row["deskripsi"] != DBNull.Value ? row["deskripsi"].ToString() : "Tidak ada deskripsi.";
            int stock = row["stock"] != DBNull.Value ? Convert.ToInt32(row["stock"]) : 0;
            lblStock.Text = "Stok Tersisa: " + stock;

            // Tentukan warna label stok dan status tombol pinjam berdasarkan ketersediaan
            if (stock <= 0)
            {
                lblStock.Text = "Stok Tersisa: Habis";
                lblStock.ForeColor = Color.Red;
                btnBorrow.Enabled = false;
                btnBorrow.BackColor = Color.LightGray; // Nonaktifkan tombol jika stok habis
            }
            else
            {
                lblStock.ForeColor = Color.Green;
                btnBorrow.Enabled = true;
                btnBorrow.BackColor = Color.FromArgb(46, 204, 113); // Warna hijau aktif
            }

            // Muat gambar cover buku dari alamat file_path yang tersimpan di DB
            string pathLengkap = row["file_path"] != DBNull.Value ? row["file_path"].ToString() : "";
            if (File.Exists(pathLengkap))
            {
                try
                {
                    pbCover.Image = Image.FromFile(pathLengkap);
                    pbCover.SizeMode = PictureBoxSizeMode.StretchImage;
                }
                catch (Exception)
                {
                    pbCover.BackColor = Color.DarkGray;
                }
            }
            else
            {
                pbCover.BackColor = Color.DarkGray;
            }
        }

        // Menyimpan status apakah user memilih untuk meminjam buku
        public bool BorrowRequested { get; private set; } = false;

        // Handler saat tombol Pinjam di-klik
        private void btnBorrow_Click(object sender, EventArgs e)
        {
            BorrowRequested = true;
            this.DialogResult = DialogResult.Yes; // Menandai tindakan peminjaman buku
            this.Close();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            // Menutup dialog detail buku
            this.Close();
        }
    }
}
