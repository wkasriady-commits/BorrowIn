using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BorrowIn
{
    // Class ini menyimpan data pengguna yang sedang login secara global (Static)
    // tujuannya agar data user tidak perlu dioper secara manual antar Form
    internal class Session
    {
        // ID unik pengguna dari database
        public static int UserID { get; set; }

        // Nama lengkap pengguna (untuk sapaan di Dashboard)
        public static string FullName { get; set; }

        // Nama akun unik pengguna
        public static string UserName { get; set; }

        // Email utama pengguna (digunakan sebagai ID unik saat update/hapus akun)
        public static string UserEmail { get; set; }

        // Peran pengguna: admin (@borrowinadmin), staff (@borrowinstaff), atau user biasa
        public static string UserRole { get; set; }
    }
}
