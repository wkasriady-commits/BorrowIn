using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BorrowIn
{
    public partial class Login_Form : Form 
    {
        private dbClient dbClient; // memanggil kelas config ke database
        public Login_Form()
        {
            InitializeComponent();
            dbClient = new dbClient(); // deklerasi sebagai objek
            this.AcceptButton = button1; // keyboard enter == klik button submit
        }

        // button untuk mengatur login
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                string email = textBox1.Text.Trim();
                string password = textBox2.Text.Trim();

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Please fill out Email and Password!");
                    return;
                }

                if (!dbClient.isEmailExists(email))
                {
                    MessageBox.Show("Unregistered Email!");
                    return;
                }

                Role user = dbClient.getUserLogin(email, password);

                if (user != null)
                {
                    // Menyimpan informasi pengguna ke dalam kelas Session statis
                    Session.UserID = user.Id;
                    Session.FullName = user.fullName; // Nama lengkap untuk sapaan di dashboard
                    Session.UserName = user.userName;
                    Session.UserEmail = user.email; // Menyimpan email secara global untuk dipakai di form lain

                    // Menentukan Role berdasarkan format email
                    if (email.EndsWith("@borrowinadmin"))
                    {
                        Session.UserRole = "admin";
                    }
                    else if (email.EndsWith("@borrowinstaff"))
                    {
                        Session.UserRole = "staff";
                    }
                    else
                    {
                        Session.UserRole = "user";
                    }

                    MessageBox.Show("Login Succeeded!");

                    // Memeriksa apakah Form Login ini dibuka di dalam container MDI parent (Cover)
                    if (this.MdiParent is Cover mainCover)
                    {
                        if (Session.UserRole == "admin")
                        {
                            // Jika email admin, buka AdminPage di dalam MDI parent
                            mainCover.OpenChildForm(new AdminPage());
                        }
                        else if (Session.UserRole == "staff")
                        {
                            // Jika email staf/pustakawan, buka LibrarianPage di dalam MDI parent
                            mainCover.OpenChildForm(new LibrarianPage());
                        }
                        else
                        {
                            // Jika email pengguna umum, buka Home di dalam MDI parent
                            Home home = new Home();
                            home.userEmail = email; // Fallback pengisian variabel lokal
                            mainCover.OpenChildForm(home);
                        }
                        
                        // Tutup form Login_Form agar tidak menumpuk di memori (akan mentrigger pelepasan event)
                        this.Close();
                    }
                    else
                    {
                        // Fallback jika MDI Parent tidak ditemukan (antisipasi jika dijalankan terpisah)
                        Home home = new Home();
                        home.userEmail = email;
                        home.Show();
                        this.Close(); // Langsung di-close daripada di-hide agar memori dibebaskan
                    }
                }
                else
                {
                    MessageBox.Show("Wrong Password!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("System Error: " + ex.Message);
            }
        }

        // button untuk pindah form ke forget password (update password nya)
        private void label3_Click(object sender, EventArgs e)
        {
            // Membuka form ForgotPassword dengan melewatkan referensi login form saat ini (this)
            ForgotPassword forgot = new ForgotPassword(this);
            
            if (this.MdiParent is Cover mainCover)
            {
                // Jika di dalam MDI, buka ForgotPassword sebagai MDI Child agar tetap rapi di dalam container
                mainCover.OpenChildForm(forgot);
            }
            else
            {
                forgot.Show();
            }
            this.Hide(); // Menyembunyikan form login sementara form forgot password aktif
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Tombol Batal/Kembali
            if (this.MdiParent is Cover mainCover)
            {
                // Kembalikan ke panel cover utama (splash screen) dan tutup login form
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
    }
}
