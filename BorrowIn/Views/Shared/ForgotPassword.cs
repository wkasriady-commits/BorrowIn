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
    public partial class ForgotPassword : Form 
    {
        private Login_Form loginForm; // membuat objek loginform
        public ForgotPassword(Login_Form login)
        {
            InitializeComponent();
            loginForm = login; // menyimpan objek login ke variabel loginForm
        }
        // event saat button reset password ditekan
        private void button1_Click(object sender, EventArgs e)
        {
            // mengambil input dari textbox1 jadi email dan textbox2 jadi password baru yg akan di update
            string email = textBox1.Text;
            string newPassword = textBox2.Text;

            // memanggil db client untuk koneksi dengan database
            dbClient db = new dbClient();

            // validasi kosong
            if (email == "" || newPassword == "")
            {
                MessageBox.Show("Fill All Field!"); // email/password kosong = keluar msg box notice
                return;
            }

            // cek email ada atau tidak
            if (!db.isEmailExists(email))
            {
                MessageBox.Show("No Email Registered"); // kalo gaada email nya, keluar notice msg box
                return;
            }

            // update password
            bool success = db.updatePassword(email, newPassword);

            if (success) // kalo berhasil update password
            {
                MessageBox.Show("Password Resetted!"); // keluar notice msg box
                
                // Cukup tutup Form ini. Event ForgotPassword_FormClosed akan secara otomatis
                // menampilkan kembali Form Login asli yang disembunyikan sebelumnya.
                this.Close();
            }
            else // gagal reset password 
            {
                MessageBox.Show("Fail to Reset Password"); // keluar notice msg box
            }
        }
        
        // Mengatur supaya saat ForgotPassword di-close, LoginForm asli kembali muncul
        private void ForgotPassword_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Memunculkan kembali Login_Form yang tadi disembunyikan
            loginForm.Show();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Tombol kembali ke Login: Cukup tutup Form ini agar memicu event FormClosed
            // yang akan memunculkan kembali Form Login yang asli.
            this.Close();
        }
    }
}
