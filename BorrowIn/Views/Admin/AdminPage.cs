using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Data.SqlClient;

namespace BorrowIn
{

    public partial class AdminPage : Form
    {
        // koneksi database
        private dbClient db;
        private DataTable dtVendorSummary;
        // String koneksi database
        private string connectionString = "Data Source=.\\SQLEXPRESS;Initial Catalog=BorrowIn;Integrated Security=true;TrustServerCertificate=True;";

        public AdminPage()
        {
            InitializeComponent();
            cmbFilterAvailable.SelectedIndex = 0; //mengatur agar default combobox menjadi "Semua buku"
            db = new dbClient();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            // Silently navigate or refresh dashboard
            if (Session.UserRole == "admin")
            {
                LoadDashboardData();
            }
            else
            {
                Form dashboard = Session.UserRole == "staff" ? (Form)new LibrarianPage() : new Home();
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
        }

        private void AdminPage_Load_1(object sender, EventArgs e)
        {
            // Security Check: Only Admin can access this dashboard
            if (Session.UserRole != "admin")
            {
                MessageBox.Show("Unauthorized Access! Redirecting to your dashboard.", "Security Alert", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                
                if (this.MdiParent is Cover mainCover)
                {
                    if (Session.UserRole == "staff")
                        mainCover.OpenChildForm(new LibrarianPage());
                    else
                        mainCover.OpenChildForm(new Home());
                }
                this.BeginInvoke(new MethodInvoker(this.Close));
                return;
            }

            LoadDashboardData();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            button1_Click(sender, e);
        }

        private void LoadDashboardData()
        {
            try
            {
                dtVendorSummary = db.getProcurementVendorSummary();
                histogramPanel.Invalidate(); // Trigger paint event for histogram
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading dashboard data: " + ex.Message);
            }
        }

        private void histogramPanel_Paint(object sender, PaintEventArgs e)
        {
            if (dtVendorSummary == null || dtVendorSummary.Rows.Count == 0)
            {
                e.Graphics.DrawString("No data available for histogram.", this.Font, Brushes.Black, 10, 10);
                return;
            }

            Graphics g = e.Graphics;
            int margin = 50;
            int barWidth = 40;
            int gap = 20;
            int maxBarHeight = histogramPanel.Height - (2 * margin);
            
            // Find max value for scaling
            int maxValue = 0;
            foreach (DataRow row in dtVendorSummary.Rows)
            {
                int val = Convert.ToInt32(row["TotalAmount"]);
                if (val > maxValue) maxValue = val;
            }

            if (maxValue == 0) maxValue = 1; // Avoid division by zero

            int x = margin;
            foreach (DataRow row in dtVendorSummary.Rows)
            {
                string vendor = row["Vendor"].ToString();
                int amount = Convert.ToInt32(row["TotalAmount"]);
                int barHeight = (int)((float)amount / maxValue * maxBarHeight);

                // Draw bar
                Rectangle barRect = new Rectangle(x, histogramPanel.Height - margin - barHeight, barWidth, barHeight);
                g.FillRectangle(Brushes.RoyalBlue, barRect);
                g.DrawRectangle(Pens.Black, barRect);

                // Draw vendor label (rotated for space)
                g.TranslateTransform(x + (barWidth / 2), histogramPanel.Height - margin + 5);
                g.RotateTransform(45);
                g.DrawString(vendor, this.Font, Brushes.Black, 0, 0);
                g.ResetTransform();

                // Draw amount label
                string amountStr = amount.ToString();
                SizeF amountSize = g.MeasureString(amountStr, this.Font);
                g.DrawString(amountStr, this.Font, Brushes.DarkBlue, x + (barWidth / 2) - (amountSize.Width / 2), histogramPanel.Height - margin - barHeight - 20);

                x += barWidth + gap;
            }

            // Draw Axes
            g.DrawLine(Pens.Black, margin, histogramPanel.Height - margin, x, histogramPanel.Height - margin); // X-Axis
            g.DrawLine(Pens.Black, margin, margin, margin, histogramPanel.Height - margin); // Y-Axis
        }

        // Navigasi ke BookManager (Manajer Buku)
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

        // Navigasi ke BookRequest (Permintaan Buku Baru/Pengadaan)
        private void button2_Click(object sender, EventArgs e)
        {
            if (this.MdiParent is Cover mainCover)
            {
                mainCover.OpenChildForm(new BookRequest());
                this.Close();
            }
            else
            {
                BookRequest bookrq = new BookRequest();
                bookrq.Show();
                this.Close();
            }
        }

        // Navigasi ke ReturnManager (Manajer Pengembalian Buku)
        private void button3_Click(object sender, EventArgs e)
        {
            if (this.MdiParent is Cover mainCover)
            {
                mainCover.OpenChildForm(new ReturnManager());
                this.Close();
            }
            else
            {
                ReturnManager returnmanage = new ReturnManager();
                returnmanage.Show();
                this.Close();
            }
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            button3_Click(sender, e);
        }

        private void cmbFilterAvailable_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void btnCetakBuku_Click(object sender, EventArgs e)
        {
            // 1. Ambil pilihan filter dari ComboBox
            if (cmbFilterAvailable.SelectedItem == null) return;
            // Validasi pilihan filter, trim untuk menghindari spasi yang tidak diinginkan
            string filterTerpilih = cmbFilterAvailable.SelectedItem.ToString().Trim();
            string query = "SELECT * FROM Book";

            // 2. Sesuaikan query database berdasarkan pilihan filter
            if (filterTerpilih == "Available")
            {
                query += " WHERE is_available = 1 AND stock > 0";
            }
            else if (filterTerpilih == "Empty")
            {
                query += " WHERE stock = 0 OR is_available = 0";
            }

            // 3. Ambil data dari database ke dalam DataTable
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable("Book");
                
                try
                {
                    adapter.Fill(dt);

                    // Validasi jika data kosong
                    if (dt.Rows.Count == 0)
                    {
                        MessageBox.Show("Tidak ada data buku yang sesuai dengan filter ini.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    // 4. Siapkan Crystal Report dan masukkan data yang sudah di-filter
                    BookInventoryReport report = new BookInventoryReport();
                    
                    // Membersihkan data source lama dan memasukkan data baru yang sudah di-filter
                    report.DataSourceConnections.Clear();
                    report.Database.Tables["Book"].SetDataSource(dt);
                    report.Refresh();

                    // 5. Buka ReportViewerForm untuk menampilkan laporan
                    ReportViewerForm viewer = new ReportViewerForm(report);
                    viewer.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Terjadi kesalahan saat memproses laporan: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
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
