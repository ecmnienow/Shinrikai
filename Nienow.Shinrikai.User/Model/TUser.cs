using Nienow.Shinrikai.Core;
using System;

namespace Nienow.Shinrikai.User.Model
{
    public class TUser
    {
        #region Attributes and Properties
            
        public int ID { get; set; }
        
        public DateTime Birthday { get; set; }
        public string Email { get; set; }
        public EJLPT JLPT { get; set; }
        public string Login { get; set; }
        public string Name { get; set; }
        public string OtherLanguages { get; set; }
        public string Password { get; set; }
        public string Profession { get; set; }
        public string RepeatPassword { get; set; }
        public ESex Sex { get; set; }
        public string Type { get; set; }

        #endregion
    }
}
