using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BorrowIn
{
    // Form Dialog (Pop-up) untuk proses Tambah dan Edit Buku
    public partial class BookDialogForm : Form
    {
        public Book Book { get; private set; } // Objek buku yang akan dikirim kembali ke form utama

        // Definisi elemen input secara manual (tanpa designer)
        private TextBox txtId, txtTitle, txtAuthor, txtStock, txtISBN13, txtISBN10, txtPages, txtLang, txtPub, txtDim, txtDesc, txtPath;

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // BookDialogForm
            // 
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Name = "BookDialogForm";
            this.Load += new System.EventHandler(this.BookDialogForm_Load);
            this.ResumeLayout(false);

        }

        private void BookDialogForm_Load(object sender, EventArgs e)
        {

        }

        private DateTimePicker dtPublish;
        private CheckBox chkAvailable;
        private PictureBox pbPreview;

        // Constructor: Menerima objek book jika ingin Edit, null jika ingin Tambah baru
        public BookDialogForm(Book book = null)
        {
            this.Text = book == null ? "Add New Book" : "Edit Book";
            this.Size = new Size(850, 750); // Ukuran lebar agar cukup untuk preview gambar
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;

            InitializeControls(); // Bangun UI
            
            txtId.ReadOnly = true; // ID selalu otomatis, tidak boleh diketik manual

            if (book != null) // Jika mode EDIT
            {
                Book = book;
                LoadBookData(); // Isi input dengan data lama
            }
            else // Jika mode ADD
            {
                Book = new Book();
                dbClient db = new dbClient();
                txtId.Text = db.generateNextBookId(); // Ambil ID B### otomatis dari database
            }
        }

        // Membangun elemen UI (Label, TextBox, Button) secara prosedural
        private void InitializeControls()
        {
            int y = 20;
            // Menambahkan baris demi baris field input
            txtId = AddField("Book ID:", ref y);
            txtTitle = AddField("Title:", ref y);
            txtAuthor = AddField("Author:", ref y);
            txtStock = AddField("Stock:", ref y);
            txtISBN13 = AddField("ISBN-13:", ref y);
            txtISBN10 = AddField("ISBN-10:", ref y);
            txtPages = AddField("Pages:", ref y);
            txtLang = AddField("Language:", ref y);
            txtPub = AddField("Publisher:", ref y);
            txtDim = AddField("Dimensions:", ref y);
            txtDesc = AddField("Description:", ref y, true);
            txtPath = AddField("Image Path:", ref y);
            txtPath.ReadOnly = true; // Path diisi otomatis via drag-drop

            Label lblDate = new Label() { Text = "Publish Date:", Left = 20, Top = y, Width = 100 };
            dtPublish = new DateTimePicker() { Left = 130, Top = y, Width = 300 };
            this.Controls.Add(lblDate);
            this.Controls.Add(dtPublish);
            y += 30;

            chkAvailable = new CheckBox() { Text = "Is Available", Left = 130, Top = y, Checked = true };
            this.Controls.Add(chkAvailable);
            y += 40;

            // Area Drop Zone untuk Gambar Cover
            Label lblDrop = new Label() { Text = "Drag & Drop Cover Image Here", Left = 460, Top = 20, Width = 350, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Arial", 10, FontStyle.Bold) };
            this.Controls.Add(lblDrop);

            pbPreview = new PictureBox()
            {
                Left = 460, Top = 50, Width = 350, Height = 500,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.LightGray,
                AllowDrop = true // Mengaktifkan fitur Drag & Drop
            };
            pbPreview.DragEnter += PbPreview_DragEnter;
            pbPreview.DragDrop += PbPreview_DragDrop;
            this.Controls.Add(pbPreview);

            // Tombol simpan
            Button btnSave = new Button() { Text = "Save", Left = 150, Top = 640, Width = 100, Height = 40 };
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            Button btnCancel = new Button() { Text = "Cancel", Left = 260, Top = 640, Width = 100, Height = 40, DialogResult = DialogResult.Cancel };
            this.Controls.Add(btnCancel);
        }

        // Mengizinkan kursor berubah saat file gambar ditarik ke atas PictureBox
        private void PbPreview_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        // Proses saat file gambar dilepas (Drop) ke PictureBox
        private void PbPreview_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                string sourceFile = files[0];
                string extension = Path.GetExtension(sourceFile).ToLower();
                string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".webp" };

                if (allowedExtensions.Contains(extension))
                {
                    try
                    {
                        // 1. Dapatkan folder Assets proyek
                        string projectRoot = GetProjectRoot();
                        string assetsPath = Path.Combine(projectRoot, "Assets");

                        if (!Directory.Exists(assetsPath)) Directory.CreateDirectory(assetsPath);

                        // 2. Tentukan nama file tujuan (hindari duplikat nama file)
                        string fileName = Path.GetFileName(sourceFile);
                        if (File.Exists(Path.Combine(assetsPath, fileName)))
                            fileName = Path.GetFileNameWithoutExtension(sourceFile) + "_" + DateTime.Now.Ticks + extension;
                        
                        string destPath = Path.Combine(assetsPath, fileName);

                        // 3. Salin file fisik ke dalam folder Assets proyek
                        File.Copy(sourceFile, destPath, true);

                        // 4. Update model dan tampilkan preview
                        txtPath.Text = destPath;
                        Book.CoverImg = fileName; // Simpan nama file
                        Book.FilePath = destPath; // Simpan path lengkap
                        
                        if (pbPreview.Image != null) pbPreview.Image.Dispose();
                        pbPreview.Image = Image.FromFile(destPath);
                    }
                    catch (Exception ex) { MessageBox.Show("Error processing image: " + ex.Message); }
                }
                else { MessageBox.Show("Please drop an image file (JPG, PNG, etc)."); }
            }
        }

        // Mencari lokasi root proyek agar bisa menemukan folder 'Assets' secara dinamis
        private string GetProjectRoot()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            DirectoryInfo dir = new DirectoryInfo(baseDir);
            string root = dir.Parent?.Parent?.Parent?.FullName;
            return root ?? baseDir;
        }

        // Helper untuk membuat Label dan TextBox secara otomatis
        private TextBox AddField(string label, ref int y, bool multiLine = false)
        {
            Label lbl = new Label() { Text = label, Left = 20, Top = y, Width = 100 };
            TextBox txt = new TextBox() { Left = 130, Top = y, Width = 300, Multiline = multiLine };
            if (multiLine) { txt.Height = 60; y += 40; }
            this.Controls.Add(lbl);
            this.Controls.Add(txt);
            y += 30;
            return txt;
        }

        // Mengisi input form dengan data objek Book (untuk mode Edit)
        private void LoadBookData()
        {
            txtId.Text = Book.BookId;
            txtTitle.Text = Book.Title;
            txtAuthor.Text = Book.Author;
            txtStock.Text = Book.Stock.ToString();
            txtISBN13.Text = Book.ISBN_13;
            txtISBN10.Text = Book.ISBN_10;
            txtPages.Text = Book.AuthorPages?.ToString() ?? "0";
            txtLang.Text = Book.Language;
            txtPub.Text = Book.Publisher;
            txtDim.Text = Book.Dimensions;
            txtDesc.Text = Book.Description;
            txtPath.Text = Book.FilePath;
            if (Book.PublishDate > DateTime.MinValue) dtPublish.Value = Book.PublishDate;
            chkAvailable.Checked = Book.IsAvailable;

            if (!string.IsNullOrEmpty(Book.FilePath) && File.Exists(Book.FilePath))
            {
                try { pbPreview.Image = Image.FromFile(Book.FilePath); } catch { }
            }
        }

        // Tombol simpan dengan validasi kelengkapan data
        private void BtnSave_Click(object sender, EventArgs e)
        {
            // --- VALIDASI MANDATORY FIELDS ---
            bool hasIsbn = !string.IsNullOrWhiteSpace(txtISBN13.Text) || !string.IsNullOrWhiteSpace(txtISBN10.Text);

            if (string.IsNullOrWhiteSpace(txtTitle.Text) || 
                string.IsNullOrWhiteSpace(txtAuthor.Text) ||
                string.IsNullOrWhiteSpace(txtStock.Text) ||
                !hasIsbn || // Librarian wajib isi minimal salah satu ISBN
                string.IsNullOrWhiteSpace(txtPages.Text) ||
                string.IsNullOrWhiteSpace(txtLang.Text) ||
                string.IsNullOrWhiteSpace(txtPub.Text) ||
                string.IsNullOrWhiteSpace(txtDim.Text) ||
                string.IsNullOrWhiteSpace(txtDesc.Text) ||
                string.IsNullOrWhiteSpace(txtPath.Text)) // Wajib ada gambar cover
            {
                string msg = !hasIsbn ? "At least one ISBN (13 or 10) must be provided!" : "All book information must be filled in, including the cover image!";
                MessageBox.Show(msg, "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Pindahkan data dari input ke objek Book
            Book.BookId = txtId.Text;
            Book.Title = txtTitle.Text;
            Book.Author = txtAuthor.Text;
            Book.Stock = int.TryParse(txtStock.Text, out int s) ? s : 0;
            Book.ISBN_13 = txtISBN13.Text;
            Book.ISBN_10 = txtISBN10.Text;
            Book.AuthorPages = int.TryParse(txtPages.Text, out int p) ? p : 0;
            Book.Language = txtLang.Text;
            Book.Publisher = txtPub.Text;
            Book.Dimensions = txtDim.Text;
            Book.Description = txtDesc.Text;
            Book.PublishDate = dtPublish.Value;
            Book.IsAvailable = chkAvailable.Checked;

            this.DialogResult = DialogResult.OK; // Menandakan sukses agar form utama bisa memproses
            this.Close();
        }
    }
}
