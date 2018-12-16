using DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using MediaServer.Exceptions;

namespace MediaServer.Models
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
            SaltBytes = SaltForNewUser();
            PasswordHash = PasswordHashNewUser(password, SaltBytes);// create new password hash for new user
        }
        #endregion

        /// <summary>
        /// Check if the received in HttpRequest password is correct, via calculating its hash+salting 
        /// and comparing to the stored in the Users tbl in DB
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool IsAuthentic(string password)
        {
            try
            {
                Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, this.SaltBytes);
                pbkdf2.IterationCount = 1000;
                byte[] computedPassword = pbkdf2.GetBytes(32);
                return this.PasswordHash.SequenceEqual(computedPassword);
            }
            catch (Exception)
            {

                throw new AuthorizationException("Couldn't check given user password!");
            }

        }


        //If new user is created, generate a password hash using salt, and later save this in DB
        public byte[] PasswordHashNewUser(string password, byte[] saltBytes)
        {
            // pbkdf2 - password-based key derivation functionality
            var pbkdf2=new Rfc2898DeriveBytes(password,saltBytes);
            pbkdf2.IterationCount = 1000;
            byte[] passwordHash = pbkdf2.GetBytes(32);
            return passwordHash;
        }

        // Generate salt bytes to hide password hash value
        private byte[] SaltForNewUser()
        {
            byte[] saltBytes = new byte[32];
            using (var provider = new RNGCryptoServiceProvider())
            {
                provider.GetBytes(saltBytes);// Generated salt
            }
            return saltBytes;
        }


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