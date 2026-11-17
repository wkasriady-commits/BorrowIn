using System;
using System.Drawing;
using System.Windows.Forms;

namespace BorrowIn
{
    internal static class SecurityHelper
    {
        public static bool VerifyDeletionCaptcha()
        {
            // 1. Generate random captcha
            string captcha = Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
            
            // 2. Create a dynamic dialog
            Form prompt = new Form()
            {
                Width = 400,
                Height = 220,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Security Verification",
                StartPosition = FormStartPosition.CenterScreen,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label textLabel = new Label() { Left = 20, Top = 20, Width = 350, Height = 40, Text = "To delete your account, please type the following code:" };
            Label captchaLabel = new Label() { Left = 20, Top = 60, Width = 350, Font = new Font("Arial", 14, FontStyle.Bold), ForeColor = Color.Red, Text = captcha, TextAlign = ContentAlignment.MiddleCenter };
            TextBox inputBox = new TextBox() { Left = 100, Top = 100, Width = 200 };
            Button confirmation = new Button() { Text = "Confirm Deletion", Left = 150, Width = 100, Top = 140, DialogResult = DialogResult.OK };
            
            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(captchaLabel);
            prompt.Controls.Add(inputBox);
            prompt.Controls.Add(confirmation);
            prompt.AcceptButton = confirmation;

            // 3. Show dialog and check result
            if (prompt.ShowDialog() == DialogResult.OK)
            {
                if (inputBox.Text.Trim().ToUpper() == captcha)
                {
                    return true;
                }
                else
                {
                    MessageBox.Show("Incorrect captcha code. Deletion cancelled.", "Security Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            return false;
        }
    }
}
