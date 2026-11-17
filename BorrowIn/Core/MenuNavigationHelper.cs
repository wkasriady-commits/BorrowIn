using System.Windows.Forms;

namespace BorrowIn
{
    // Helper class untuk menangani navigasi MenuStrip
    // agar setiap form memiliki fungsi menu yang konsisten
    public static class MenuNavigationHelper
    {
        // Menghubungkan event menu (Home, Logout, Exit)
        // ke form yang sedang aktif
        public static void SetupMenu(Form currentForm, ToolStripMenuItem homeItem, ToolStripMenuItem logoutItem, ToolStripMenuItem exitItem)
        {
            // Konfigurasi tombol Home
            if (homeItem != null)
            {
                homeItem.Click += (sender, e) =>
                {
                    // Mencegah membuka Home jika user sudah berada di halaman Home
                    if (!(currentForm is Home))
                    {
                        // Jika menggunakan MDI Parent (Cover),
                        // buka Home sebagai child form baru
                        if (currentForm.MdiParent is Cover mainCover)
                        {
                            Home home = new Home();
                            mainCover.OpenChildForm(home);

                            // Menutup form sebelumnya untuk menghemat memori
                            currentForm.Close();
                        }
                        else
                        {
                            // Fallback jika form tidak berada dalam MDI Parent
                            Home home = new Home();
                            home.Show();
                            currentForm.Hide();
                        }
                    }
                };
            }
        }

        // Menangani proses logout dengan konfirmasi dan navigasi kembali ke cover
        public static void Logout(Form currentForm)
        {
            // Tampilkan dialog konfirmasi untuk memastikan user ingin logout
            DialogResult result = MessageBox.Show(
                "Are you sure you want to log out?",
                "Logout Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            // Jika user memilih 'Yes', lakukan proses logout
            if (result == DialogResult.Yes)
            {
                // Jika form aktif berada di dalam MDI Parent (Cover)
                if (currentForm.MdiParent is Cover mainCover)
                {
                    // Tampilkan kembali panel splash utama dan tutup form anak saat ini
                    mainCover.ShowCoverPanel();
                    currentForm.Close();
                }
                else
                {
                    // Fallback jika form tidak berada dalam kontainer MDI Parent
                    Cover cover = new Cover();
                    cover.Show();
                    currentForm.Close();
                }
            }
        }
    }
}