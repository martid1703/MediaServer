using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DTO
{
    public class CUserInfo
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }


        public CUserInfo()
        {

        }
        public CUserInfo(string name, string password): this(Guid.Empty, name, "", password)
        {
        }
        public CUserInfo(string name, string email, string password) : this(Guid.Empty, name, email, password)
        {
        }

        public CUserInfo(Guid id, string name, string email, string password)
        {
            Id = id;
            Name = name;
            Email = email;
            Password = password;
        }

        public override String ToString()
        {
            return $"Name: {Name}, Id: {Id}";
        }
    }
}
