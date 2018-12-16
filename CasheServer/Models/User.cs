using DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;

namespace CasheServer.Models
{
    // to be used with ADO.NET & SQL later
    public class CUser
    {

        public Guid Guid { get; set; }
        public string Name { get; set; }// Name is like nickname - it should be UNIQUE
        public byte[] PasswordHash { get; set; }
        public byte[] SaltBytes { get; set; }
        public string Email { get; set; }

        #region CTORs
        public CUser()
        {

        }
        public CUser(CUserInfo user):this(Guid.NewGuid(), user.Name, user.Password, user.Email)
        {

        }
        public CUser(Guid guid, String name, string password, string email)
        {
            Guid = guid;
            Name = name;
            Email = email;
           
        }
        #endregion

      

        // Update fields from incoming DTO
        public void UpdateFields(CUserInfo userInfo)
        {
            try
            {
                // other fields are somewhat unchangeable: guid, name
                Email = userInfo.Email;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
        }
    }
}