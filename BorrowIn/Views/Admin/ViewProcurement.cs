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
    // Form Dashboard khusus Admin untuk mengelola data pengadaan (Procurement)
    public partial class ViewProcurement : Form
    {
        private dbClient db;
        private bool selectMode = false; // Flag untuk status mode seleksi checkbox
        private DataTable dtProcurements = null; // Cache data pengadaan untuk fitur filtering

        public ViewProcurement()
        {
            InitializeComponent();
            db = new dbClient();
        }

        // Inisialisasi halaman dan pengecekan otorisasi
        private void ViewProcurement_Load(object sender, EventArgs e)
        {
            dataGridView1.Columns["colSelect"].Visible = false;
            // Security Check: Hanya Admin yang boleh masuk ke menu detail pengadaan
            if (Session.UserRole != "admin")
            {
                MessageBox.Show("Unauthorized Access! Procurement details are restricted to Administrators.", "Security Alert", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                
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

            // Setup UI awal
            button5.BackColor = Color.Gray; // Tombol Switch Mode
            dataGridView1.AutoGenerateColumns = false;
            
            // Pemetaan manual kolom DataGridView ke field database
            ProcurementID.DataPropertyName = "ProcurementID";
            Title.DataPropertyName = "Title";
            Author.DataPropertyName = "Author";
            ISBN.DataPropertyName = "ISBN";
            PublishYear.DataPropertyName = "PublishYear";
            Vendor.DataPropertyName = "Vendor";
            Amount.DataPropertyName = "Ammount";
            InitialOffer.DataPropertyName = "InitialOffer";
            RequestDate.DataPropertyName = "RequestDate";
            DeliveryDate.DataPropertyName = "DeliveryDate";

            // Mengaktifkan fitur edit langsung di dalam grid
            dataGridView1.ReadOnly = false;
            ProcurementID.ReadOnly = true; // ID bersifat auto-increment, tidak boleh diedit
            RequestDate.ReadOnly = true;

            cmbStatusFilter.SelectedIndex = 0; // Filter status default: "All"
            LoadData(); // Tarik data dari database
        }

        // Mengambil data terbaru dari DB dan menampilkan di Grid
        private void LoadData()
        {
            try
            {
                dtProcurements = db.getAllProcurements();
                ApplyFilters(); // Terapkan pencarian/filter yang sedang aktif
                
                // Tambahkan kolom status secara dinamis jika belum ada di designer
                if (!dataGridView1.Columns.Contains("Status"))
                {
                    DataGridViewTextBoxColumn statusCol = new DataGridViewTextBoxColumn();
                    statusCol.Name = "Status";
                    statusCol.HeaderText = "Status";
                    statusCol.DataPropertyName = "Status";
                    statusCol.ReadOnly = true;
                    dataGridView1.Columns.Add(statusCol);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading data: " + ex.Message);
            }
        }

        // Logika pusat untuk memfilter data berdasarkan input pencarian dan dropdown status
        private void ApplyFilters()
        {
            if (dtProcurements == null) return;

            string searchText = txtSearch.Text.Trim().Replace("'", "''");
            string statusFilter = cmbStatusFilter.SelectedItem?.ToString() ?? "All";
            string filterExpression = "";

            // 1. Filter Pencarian Teks (Mencari di Judul, Penulis, ISBN, atau Vendor)
            if (!string.IsNullOrEmpty(searchText))
            {
                filterExpression = $"(Title LIKE '%{searchText}%' OR Author LIKE '%{searchText}%' OR ISBN LIKE '%{searchText}%' OR Vendor LIKE '%{searchText}%')";
            }

            // 2. Filter Status (Pending, Approved, Rejected)
            if (statusFilter != "All")
            {
                if (!string.IsNullOrEmpty(filterExpression)) filterExpression += " AND ";
                filterExpression += $"Status = '{statusFilter}'";
            }

            try
            {
                // Gunakan DataView untuk melakukan filtering di memori (sangat cepat)
                DataView dv = dtProcurements.DefaultView;
                dv.RowFilter = filterExpression;
                dataGridView1.DataSource = dv.ToTable(); // Tampilkan hasil filter di Grid
            }
            catch (Exception) { /* Handle filter error secara diam-diam */ }
        }

        // Trigger pencarian setiap kali ada teks baru yang diketik
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        // Trigger filter saat pilihan di dropdown status berubah
        private void cmbStatusFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        // Mengaktifkan/Menonaktifkan mode seleksi (Munculkan kolom Checkbox)
        private void button5_Click_1(object sender, EventArgs e)
        {
            selectMode = !selectMode;
            colSelect.Visible = selectMode;
            button5.BackColor = selectMode ? Color.Green : Color.Gray; // Ganti warna sebagai indikator
            
            if (!selectMode)
            {
                // Reset semua pilihan jika keluar dari mode seleksi
                foreach (DataGridViewRow row in dataGridView1.Rows)
                    row.Cells["colSelect"].Value = false;
            }
        }

        // Tombol untuk menyetujui request yang di-checklist (Otomatis restock buku)
        private void button6_Click(object sender, EventArgs e)
        {
            ProcessSelection(true); // true = Accept
        }

        // Tombol untuk menolak request yang di-checklist
        private void button7_Click(object sender, EventArgs e)
        {
            ProcessSelection(false); // false = Reject
        }

        // Logika utama untuk memproses baris yang dicentang
        private void ProcessSelection(bool isAccept)
        {
            dataGridView1.EndEdit(); // Pastikan perubahan centang terakhir tersimpan
            bool processed = false;
            int count = 0;

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                // Cek apakah baris ini dicentang (Select)
                if (row.Cells["colSelect"].Value != null && Convert.ToBoolean(row.Cells["colSelect"].Value))
                {
                    int id = Convert.ToInt32(row.Cells["ProcurementID"].Value);
                    string currentStatus = row.Cells["Status"].Value?.ToString();

                    // Hanya item 'Pending' yang bisa diproses Approve/Reject
                    if (currentStatus != "Pending") continue;

                    bool success = isAccept ? db.approveProcurement(id) : db.rejectProcurement(id);
                    if (success)
                    {
                        processed = true;
                        count++;
                    }
                }
            }

            if (processed)
            {
                string action = isAccept ? "Approved and Stocked" : "Rejected";
                MessageBox.Show($"{count} requests have been {action}.", "Success");
                LoadData(); // Refresh tampilan
            }
            else
            {
                MessageBox.Show("No pending items selected.", "Notice");
            }
        }

        // Tombol 'Update Record' untuk menyimpan perubahan manual yang diketik di dalam Grid
        private void button3_Click_1(object sender, EventArgs e)
        {
            dataGridView1.EndEdit();
            DataGridViewRow selectedRow = dataGridView1.CurrentRow; // Gunakan baris yang sedang aktif

            if (selectedRow == null) return;

            try
            {
                // Ambil semua value dari kolom Grid
                int procId = Convert.ToInt32(selectedRow.Cells["ProcurementID"].Value);
                string title = selectedRow.Cells["Title"].Value.ToString();
                string author = selectedRow.Cells["Author"].Value.ToString();
                int amount = Convert.ToInt32(selectedRow.Cells["Amount"].Value);
                decimal offer = Convert.ToDecimal(selectedRow.Cells["InitialOffer"].Value);
                int year = Convert.ToInt32(selectedRow.Cells["PublishYear"].Value);
                string vendor = selectedRow.Cells["Vendor"].Value.ToString();

                // Simpan ke Database
                db.updateProcurementStatus(procId, title, author, amount, offer, year, vendor);
                MessageBox.Show("Record updated successfully.", "Success");
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating record: " + ex.Message, "Error");
            }
        }

        // Tombol Refresh untuk menarik ulang data dari server
        private void button4_Click_1(object sender, EventArgs e)
        {
            LoadData();
        }

        // Menghapus data pengadaan (Bisa satu atau banyak sekaligus via mode seleksi)
        private void button2_Click(object sender, EventArgs e)
        {
            dataGridView1.EndEdit();
            bool deleted = false;
            
            // Loop semua baris untuk mencari yang dicentang
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["colSelect"].Value != null && Convert.ToBoolean(row.Cells["colSelect"].Value))
                {
                    db.deleteProcurement(Convert.ToInt32(row.Cells["ProcurementID"].Value));
                    deleted = true;
                }
            }

            if (deleted)
            {
                MessageBox.Show("Record(s) deleted successfully.");
                LoadData();
            }
        }

        // Klik logo kembali ke Dashboard Utama sesuai role user
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

        // --- RESTORED MISSING EVENT HANDLERS ---
        private void button1_Click(object sender, EventArgs e)
        {
            string msg = "HOW TO USE:\n\n" +
                         "1. EDIT: Click any cell to type new info, then click 'Update Record'.\n" +
                         "2. SEARCH: Use the search box to find specific books or vendors.\n" +
                         "3. FILTER: Use the dropdown to see only 'Pending' requests.\n" +
                         "4. PROCESS: Click 'Switch Mode', check boxes, then click 'Accept' or 'Reject'.";
            MessageBox.Show(msg, "Procurement Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) {
        }

        private void button8_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable dtProcure = db.getAllProcurements();
                dtProcure.TableName = "Procurement";

                ProcurementRequest ProcureReport = new ProcurementRequest();
                ProcureReport.SetDataSource(dtProcure);

                ReportViewerForm reportViewer = new ReportViewerForm(ProcureReport);
                reportViewer.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Loading Data Procurement: " + ex.Message,
                                "Reporting Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
