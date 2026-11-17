using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace BorrowIn.Models
{
    // class khusus role admin
    public class Admin : Role
    {
        public Admin(string fullName, string userName,
                     string email, string password)
            : base(fullName, userName, email, password) { }

        // constructor
        public Admin(int Id, string fullName, string userName,
                     string email, string password)
            : base(Id, fullName, userName, email, password) { }

        // mengambil informasi admin
        public override string GetRole() => "Admin";
        public override string GetInfo() => $"[Admin] {fullName} ({email})";
    }
}
