using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BorrowIn
{
    public partial class Signup : Form
    {
        private dbClient dbClient;
        public Signup()
        {
            InitializeComponent();
            dbClient = new dbClient(); // memanggil class dbclient untuk koneksi ke database
            this.AcceptButton = button1; // keyboard enter == klik button submit
        }
        // button yang mengatur pembuatan akun
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // ambil data + trim spasi
                string nama = textBox1.Text.Trim();
                string username = textBox2.Text.Trim();
                string email = textBox3.Text.Trim();
                string password = textBox4.Text.Trim();

                // validasi kosong
                if (nama == "" || username == "" || email == "" || password == "")
                {
                    MessageBox.Show("Fill all data!"); // kalo ada komponen yg masih kosong = keluar msg box 
                    return;
                }

                // validasi email
                if (dbClient.isEmailExists(email))
                {
                    MessageBox.Show("Email has already been registered!"); // kalo udah ada email terdaftar, gabisa buat lagi
                    return;
                }

                // memasukan user baru berdasarkan hasil signup
                Role user = new Role(
                    // mengambil input dan menyimpan data
                    textBox1.Text,
                    textBox2.Text,
                    textBox3.Text,
                    textBox4.Text
                );

                dbClient.addUser(user);

                MessageBox.Show("Account created!");

                // Kembali ke tampilan utama Cover (Splash Screen)
                if (this.MdiParent is Cover mainCover)
                {
                    // Meminta Cover Parent untuk memunculkan kembali splash panel-nya
                    mainCover.ShowCoverPanel();

                    // Tutup Form Signup saat ini agar memori dibebaskan dan tidak bertumpuk
                    this.Close();
                }
                else
                {
                    // Fallback jika tidak berada di dalam container MDI
                    Cover cover = new Cover();
                    cover.Show();
                    this.Close();
                }
            }
            catch (Exception ex) // error handling
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }   

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            //menunjukkan jalur file (berada di folder yang sama dengan file .exe)
            string filePath = AppDomain.CurrentDomain.BaseDirectory + "LibraryRules.txt";
            try
            {
                //file handling Read, Menggunakan StreamReader untuk membaca file teks
                if (File.Exists(filePath))
                {
                    using (StreamReader sr = new StreamReader(filePath))
                    {
                        string content = sr.ReadToEnd();

                        //menampilkan isi file dalam MessageBox 
                        MessageBox.Show(content, "Library Rules", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show("File 'LibraryRules.txt' is not found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error reading file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
