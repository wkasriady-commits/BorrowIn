using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO; // Diperlukan untuk File & Stream logic

namespace BorrowIn
{
    // Form Dashboard khusus untuk Pustakawan / Staff
    public partial class LibrarianPage : Form
    {
        private dbClient dbClient;
        
        public LibrarianPage()
        {
            InitializeComponent();
            dbClient = new dbClient();
        }

        // Event saat form dimuat pertama kali
        private void LibrarianPage_Load(object sender, EventArgs e)
        {
            // Security Check: Memastikan hanya user dengan role 'staff' yang bisa masuk
            if (Session.UserRole != "staff")
            {
                MessageBox.Show("Unauthorized Access! Redirecting to your dashboard.", "Security Alert", MessageBoxButtons.OK, MessageBoxIcon.Stop);

                if (this.MdiParent is Cover mainCover)
                {
                    // Redirect otomatis sesuai role aslinya jika salah masuk
                    if (Session.UserRole == "admin")
                        mainCover.OpenChildForm(new AdminPage());
                    else
                        mainCover.OpenChildForm(new Home());
                }
                this.BeginInvoke(new MethodInvoker(this.Close)); // Tutup form secara aman
                return;
            }

            // Memuat angka statistik di dashboard (Total Buku, User, dan Peminjaman)
            label3.Text = dbClient.GetTotalBooks().ToString();
            label4.Text = dbClient.GetTotalUsers().ToString();
            label6.Text = dbClient.GetTotalBorrowedBooks().ToString();
        }

        // Keluar dari aplikasi secara total
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
                Application.Exit(); // Matikan seluruh proses aplikasi
            }
        }
        
        // Logout dan kembali ke layar splash screen
        private void logOutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Panggil helper navigasi menu untuk memproses logout dengan konfirmasi
            MenuNavigationHelper.Logout(this);
        }

        // Navigasi ke Halaman Return Manager (Pengembalian)
        private void button3_Click(object sender, EventArgs e)
        {
            ReturnManager returnManager = new ReturnManager();
            if (this.MdiParent is Cover mainCover)
                mainCover.OpenChildForm(returnManager);
            else
            {
                returnManager.Show();
                this.Close();
            }
        }

        // Klik logo BorrowIn kembali ke dashboard (Tanpa konfirmasi sesuai request)
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (Session.UserRole == "staff")
            {
                LibrarianPage_Load(sender, e); // Hanya refresh data dashboard
            }
            else
            {
                Form dashboard = Session.UserRole == "admin" ? (Form)new AdminPage() : new Home();
                if (this.MdiParent is Cover mainCover)
                    mainCover.OpenChildForm(dashboard);
                else
                    dashboard.Show();
                this.Close();
            }
        }

        // Navigasi ke Halaman Book Manager (Kelola Buku)
        private void button1_Click(object sender, EventArgs e)
        {
            BookManager bookManager = new BookManager();
            if (this.MdiParent is Cover mainCover)
                mainCover.OpenChildForm(bookManager);
            else
            {
                bookManager.Show();
                this.Close();
            }
        }

        // Mencetak Laporan Timeline Peminjaman menggunakan Crystal Report
        private void btnGenerateReport_Click(object sender, EventArgs e)
        {
            try
            {
                // Ambil data lengkap dari DB
                DataTable dtHistory = dbClient.getAllBorrowHistory();

                if (dtHistory == null || dtHistory.Rows.Count == 0)
                {
                    MessageBox.Show("No borrowing history found.", "Empty Data", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Inisialisasi file Crystal Report yang sudah didesain
                BorrowHistoryReport report = new BorrowHistoryReport();
                report.SetDataSource(dtHistory); // Masukkan data hasil query ke report

                // Tampilkan report di dalam form viewer khusus
                ReportViewerForm viewer = new ReportViewerForm(report);
                if (this.MdiParent is Cover mainCover)
                    mainCover.OpenChildForm(viewer);
                else
                    viewer.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generating report: " + ex.Message, "Report Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Mengekspor data riwayat ke file CSV menggunakan konsep File & Stream
        private void btnExportCSV_Click(object sender, EventArgs e)
        {
            try
            {
                // mengambil data riwayat peminjaman dari database, DataTable digunakan untuk menyimpan data dalam bentuk tabel
                DataTable dt = dbClient.getAllBorrowHistory();
                // kalau data kosong, keluar dari fungsi
                if (dt == null || dt.Rows.Count == 0) return;

                // Tampilkan dialog simpan file untuk memilih lokasi dan nama file CSV
                SaveFileDialog sfd = new SaveFileDialog() { Filter = "CSV Files (*.csv)|*.csv" };
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    // IMPLEMENTASI FILE & STREAM: Menggunakan FileStream dan StreamWriter
                    using (FileStream fs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(fs))
                        {
                            // 1. Tulis nama kolom (Header)
                            string header = string.Join(",", dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
                            sw.WriteLine(header);

                            // 2. Tulis isi data per baris
                            foreach (DataRow row in dt.Rows)
                            {
                                string line = string.Join(",", row.ItemArray.Select(f => "\"" + f.ToString().Replace("\"", "\"\"") + "\""));
                                sw.WriteLine(line);
                            }
                        }
                    }
                    MessageBox.Show("Export CSV Success!", "Info");
                }
            } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        // Mengekspor tampilan report langsung ke format PDF menggunakan FileStream dan Crystal Reports Engine
        private void btnExportPDF_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable dt = dbClient.getAllBorrowHistory();
                if (dt == null || dt.Rows.Count == 0) return;

                BorrowHistoryReport report = new BorrowHistoryReport();
                report.SetDataSource(dt);

                // Isi nilai parameter report untuk menghindari error "missing parameter value"
                report.SetParameterValue("ReportTitle", "Borrow History Report");
                report.SetParameterValue("PrintedBy", Session.FullName ?? "Librarian");
                report.SetParameterValue("PrintedDate", DateTime.Now.ToString("dd-MM-yyyy HH:mm"));

                SaveFileDialog sfd = new SaveFileDialog() { Filter = "PDF Files (*.pdf)|*.pdf" };
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    // Ekspor report ke bentuk Stream data PDF
                    using (System.IO.Stream stream = report.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat))
                    {
                        // FileStream membuat file baru dan menulis bytes data PDF ke disk
                        using (System.IO.FileStream fs = new System.IO.FileStream(sfd.FileName, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                        {
                            // Salin isi stream dari Crystal Report ke dalam FileStream
                            stream.CopyTo(fs);
                        }
                    }
                    MessageBox.Show("Export PDF Success!", "Info");
                }
                report.Dispose();
            } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        // Navigasi ke Halaman Book Request (Pengadaan)
        private void button2_Click(object sender, EventArgs e)
        {
            BookRequest request = new BookRequest();
            if (this.MdiParent is Cover mainCover)
                mainCover.OpenChildForm(request);
            else
                request.Show();
            this.Close();
        }

        // --- RESTORED MISSING EVENT HANDLERS ---
        private void label10_Click(object sender, EventArgs e) { button3_Click(sender, e); }
        private void pictureBox4_Click(object sender, EventArgs e) { button3_Click(sender, e); }
        private void label8_Click(object sender, EventArgs e) { button3_Click(sender, e); }
        private void pictureBox2_Click(object sender, EventArgs e) { button1_Click(sender, e); }
        private void groupBox1_Enter(object sender, EventArgs e) { }
    }
}
