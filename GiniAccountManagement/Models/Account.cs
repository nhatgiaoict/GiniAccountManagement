using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GiniAccountManagement.Models
{
    public class Account
    {
        public bool Chon { get; set; }
        public int STT { get; set; }
        public string UID { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }
        public string Email { get; set; }
        public string EmailPassword { get; set; }
        public string Ten { get; set; }
        public int BanBe { get; set; }
        public int Nhom { get; set; }
        public string NgaySinh { get; set; }
        public string GioiTinh { get; set; }
       
        public string EmailRecovery { get; set; }
        public string PasswordEmailRecovery { get; set; }
        public string TwoFA { get; set; }
        public string Proxy { get; set; }
        public string Cookie { get; set; }
        public string Note { get; set; }
        public string ThuMuc { get; set; }
        public string ProfilePath { get; set; }
        public string UserAgent { get; set; }
        public string LanTuongTacCuoi { get; set; }
    }
}
