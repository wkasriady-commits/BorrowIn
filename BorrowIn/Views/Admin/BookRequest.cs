using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace BorrowIn
{
    public partial class BookRequest : Form
    {
        private dbClient db;

        public BookRequest()
        {
            InitializeComponent();
            db = new dbClient();
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
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

        // Navigasi ke BookManager
        private void button1_Click(object sender, EventArgs e)
        {
            if (this.MdiParent is Cover mainCover)
            {
                mainCover.OpenChildForm(new BookManager());
                this.Close();
            }
            else
            {
                BookManager bookmanage = new BookManager();
                bookmanage.Show();
                this.Close();
            }
        }

        private void menusToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Validasi input: Memastikan seluruh kolom wajib diisi oleh pustakawan/staf
            if (string.IsNullOrWhiteSpace(TitleTxt.Text) ||
                string.IsNullOrWhiteSpace(AuthorTxt.Text) ||
                string.IsNullOrWhiteSpace(ISBNTxt.Text) ||
                string.IsNullOrWhiteSpace(PublishTxt.Text) ||
                string.IsNullOrWhiteSpace(VendorTxt.Text) ||
                string.IsNullOrWhiteSpace(AmmountTxt.Text) ||
                string.IsNullOrWhiteSpace(OfferTxt.Text))
            {
                MessageBox.Show("Please fill out all fields!", "Input Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validasi format angka: Mencegah timbulnya FormatException saat konversi nilai numerik
            if (!int.TryParse(PublishTxt.Text, out int publishYear) || publishYear <= 0)
            {
                MessageBox.Show("Publish Year must be a valid positive integer!", "Input Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(AmmountTxt.Text, out int amount) || amount <= 0)
            {
                MessageBox.Show("Amount must be a valid positive integer!", "Input Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(OfferTxt.Text, out decimal initialOffer) || initialOffer < 0)
            {
                MessageBox.Show("Initial Offer must be a valid non-negative number!", "Input Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Membuat data objek pengadaan baru dengan input yang sudah di-Trim
                BookProcure procurement = new BookProcure()
                {
                    Title = TitleTxt.Text.Trim(),
                    Author = AuthorTxt.Text.Trim(),
                    ISBN = ISBNTxt.Text.Trim(),
                    PublishYear = publishYear,
                    Vendor = VendorTxt.Text.Trim(),
                    Ammount = amount,
                    InitialOffer = initialOffer,
                    DeliveryDate = Delivery_dateTimePicker1.Value,
                    RequestDate = DateTime.Now
                };
                db.addProcurement(procurement);
                MessageBox.Show("Procurement request added successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                TitleTxt.Clear();
                AuthorTxt.Clear();
                ISBNTxt.Clear();
                PublishTxt.Clear();
                VendorTxt.Clear();
                AmmountTxt.Clear();
                OfferTxt.Clear();
                Delivery_dateTimePicker1.Value = DateTime.Now;
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                // Menangani error duplikasi ISBN (Constraint UNIQUE pada database) secara aman
                if (ex.Number == 2627) 
                {
                    MessageBox.Show("A procurement request with this ISBN already exists!", "Duplicate Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Database Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                // Menangani kesalahan sistem umum lainnya
                MessageBox.Show("System Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Navigasi ke ViewProcurement (Data Pengadaan Buku)
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

        // Navigasi ke ReturnManager
        private void button3_Click(object sender, EventArgs e)
        {
            if (this.MdiParent is Cover mainCover)
            {
                mainCover.OpenChildForm(new ReturnManager());
                this.Close();
            }
            else
            {
                ReturnManager returnmanager = new ReturnManager();
                returnmanager.Show();
                this.Close();
            }
        }

        // Navigasi ke Halaman Book Request (Refresh)
        private void button2_Click(object sender, EventArgs e)
        {
            // Already on Book Request - refresh could clear inputs or just do nothing
            // For consistency with other pages, we can just do nothing or clear inputs.
        }

        private void label8_Click(object sender, EventArgs e)
        {
            button3_Click(sender, e);
        }

        private void label3_Click_1(object sender, EventArgs e)
        {
            button3_Click(sender, e);
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            button3_Click(sender, e);
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            button1_Click(sender, e);
        }

        private void BookRequest_Load(object sender, EventArgs e)
        {
            // Security Check: Only Admin and Staff can manage procurement
            if (Session.UserRole != "admin" && Session.UserRole != "staff")
            {
                MessageBox.Show("Unauthorized Access!", "Security Alert", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                if (this.MdiParent is Cover mainCover)
                {
                    mainCover.OpenChildForm(new Home());
                }
                this.BeginInvoke(new MethodInvoker(this.Close));
                return;
            }
        }

        // Log Out
        private void logOutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            // Panggil helper navigasi menu untuk memproses logout dengan konfirmasi
            MenuNavigationHelper.Logout(this);
        }

        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            // Menampilkan pesan konfirmasi sebelum benar-benar keluar (Opsional)
            DialogResult result = MessageBox.Show(
                "Are you sure to leave the BorrowIn App?",
                "Confirm Close",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            // Jika user memilih Yes, matikan seluruh aplikasi secara total
            if (result == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void ViewProcurementButton_Click(object sender, EventArgs e)
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

        // Kembali ke LibrarianPage (Home-nya Staf)
        private void homeToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (this.MdiParent is Cover mainCover)
            {
                mainCover.OpenChildForm(new LibrarianPage());
                this.Close();
            }
            else
            {
                LibrarianPage librarian = new LibrarianPage();
                librarian.Show();
                this.Close();
            }
        }

        // Tombol ke LibrarianPage
        private void button1_Click_1(object sender, EventArgs e)
        {
            // This button is incorrectly mapped to LibrarianPage. 
            // Standardizing to BookManager.
            button1_Click(sender, e);
        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void label11_Click(object sender, EventArgs e)
        {

        }

        private void label12_Click(object sender, EventArgs e)
        {

        }
    }
}
