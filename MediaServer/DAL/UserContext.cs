using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using DTO;
using MediaServer.Exceptions;
using MediaServer.Models;

namespace MediaServer.DAL
{
    public class UserContext
    {
        //private readonly object _lock = new object();
        //private Int32 _nextId = 2;

        //private static readonly Guid u0 = Guid.Parse("43e33024-c937-4d32-b2f5-0f7af9739453");
        //private static readonly Guid u1 = Guid.Parse("99f83a69-8372-484a-92f5-8b324dd4503f");

        ////data storage
        //readonly static Dictionary<Guid, CUser> _usersDictionary =
        //    new Dictionary<Guid, CUser>
        //    {
        //        {u0,new CUser (u0,"user1", "pass1", "mail1@abc.com")},
        //        {u1,new CUser (u1,"user2", "pass2", "mail2@abc.com")},

        //    };

        private static class Requests
        {
            public const string SelectAllUsers = "Select* from Users";
            public const string SelectUserById = "Select * from Users where UserId=@userId";
            public const string SelectUserByName = "Select * from Users where UserName=@userName";
            public const string SelectUserByEmail = "Select * from Users where Email=@Email";
            public const string CreateUser =
                "INSERT INTO Users (UserId,UserName,Email,PasswordHash,Salt) " +
                    "Values (@UserId, @UserName,@Email,@PasswordHash,@Salt)";
            public const string DeleteUser = "DELETE FROM Users where UserId=@userId";
            public const string UpdateUser =
                "UPDATE Users SET UserName=@UserName, Email=@Email, " +
                        "PasswordHash=@PasswordHash, Salt=@Salt" +
                        " WHERE UserId=@UserId";

        }

        public UserContext()
        {

        }

        #region CRUD
        public IEnumerable<CUser> GetAll()
        {
            SqlConnection myConnection = new SqlConnection();
            using (myConnection)
            {
                SqlDataReader reader = null;
                myConnection.ConnectionString = ConnectionContext.GetConnectionString();

                SqlCommand sqlCmd = new SqlCommand();
                sqlCmd.CommandType = CommandType.Text;
                sqlCmd.CommandText = Requests.SelectAllUsers;
                sqlCmd.Connection = myConnection;
                myConnection.Open();
                reader = sqlCmd.ExecuteReader();
                List<CUser> users = new List<CUser>();
                while (reader.Read())
                {
                    CUser user = new CUser();
                    user.Guid = (Guid)(reader.GetValue(0));
                    user.Name = reader.GetValue(1).ToString();
                    user.Email = reader.GetValue(2).ToString();
                    user.PasswordHash = (byte[])(reader.GetValue(3));
                    user.SaltBytes = (byte[])(reader.GetValue(4));
                    users.Add(user);
                }
                //myConnection.Close();
                return users;
            }
        }

        public CUser GetById(Guid userId)
        {
            try
            {
                SqlConnection myConnection = new SqlConnection();
                using (myConnection)
                {
                    SqlDataReader reader = null;
                    myConnection.ConnectionString = ConnectionContext.GetConnectionString();

                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.CommandType = CommandType.Text;
                    sqlCmd.CommandText = Requests.SelectUserById;
                    sqlCmd.Connection = myConnection;
                    sqlCmd.Parameters.AddWithValue("@userId", userId);
                    myConnection.Open();
                    reader = sqlCmd.ExecuteReader();
                    CUser user = new CUser();
                    while (reader.Read())
                    {
                        user.Guid = (Guid)(reader.GetValue(0));
                        user.Name = reader.GetValue(1).ToString();
                        user.Email = reader.GetValue(2).ToString();
                        user.PasswordHash = (byte[])(reader.GetValue(3));
                        user.SaltBytes = (byte[])(reader.GetValue(4));
                    }
                    myConnection.Close();
                    return user;
                }
            }
            catch (Exception e)
            {
                throw new ContextException("Cannot find user!", e);
            }

        }

        public CUser GetByName(string userName)
        {
            try
            {

                SqlConnection myConnection = new SqlConnection();

                using (myConnection)
                {

                    SqlDataReader reader = null;
                    myConnection.ConnectionString = ConnectionContext.GetConnectionString();

                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.CommandType = CommandType.Text;
                    sqlCmd.CommandText = Requests.SelectUserByName;
                    sqlCmd.Connection = myConnection;
                    sqlCmd.Parameters.AddWithValue("@userName", userName);
                    myConnection.Open();
                    reader = sqlCmd.ExecuteReader();
                    CUser user = new CUser();
                    while (reader.Read())
                    {
                        user.Guid = (Guid)(reader.GetValue(0));
                        user.Name = reader.GetValue(1).ToString();
                        user.Email = reader.GetValue(2).ToString();
                        user.PasswordHash = (byte[])(reader.GetValue(3));
                        user.SaltBytes = (byte[])(reader.GetValue(4));
                    }
                    //myConnection.Close();
                    return user;
                }
            }
            catch (Exception e)
            {
                throw new ContextException("Cannot find user!", e);
            }

        }

        public CUser GetByEmail(string email)
        {
            try
            {
                SqlConnection myConnection = new SqlConnection();

                using (myConnection)
                {
                    SqlDataReader reader = null;
                    myConnection.ConnectionString = ConnectionContext.GetConnectionString();

                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.CommandType = CommandType.Text;
                    sqlCmd.CommandText = Requests.CreateUser;
                    sqlCmd.Connection = myConnection;
                    sqlCmd.Parameters.AddWithValue("@Email", email);
                    myConnection.Open();
                    reader = sqlCmd.ExecuteReader();
                    CUser user = new CUser();
                    while (reader.Read())
                    {
                        user.Guid = (Guid)(reader.GetValue(0));
                        user.Name = reader.GetValue(1).ToString();
                        user.Email = reader.GetValue(2).ToString();
                        user.PasswordHash = (byte[])(reader.GetValue(3));
                        user.SaltBytes = (byte[])(reader.GetValue(4));
                    }
                    //myConnection.Close();
                    return user;
                }
            }
            catch (Exception e)
            {
                throw new ContextException("Cannot find user!", e);
            }

        }

        public Int32 Create(CUser user)
        {
            try
            {
                SqlConnection myConnection = new SqlConnection();

                using (myConnection)
                {
                    myConnection.ConnectionString = ConnectionContext.GetConnectionString();

                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.CommandType = CommandType.Text;
                    sqlCmd.CommandText = Requests.CreateUser;
                    sqlCmd.Connection = myConnection;

                    sqlCmd.Parameters.AddWithValue("@UserId", user.Guid);
                    sqlCmd.Parameters.AddWithValue("@UserName", user.Name);
                    sqlCmd.Parameters.AddWithValue("@Email", user.Email);
                    sqlCmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                    sqlCmd.Parameters.AddWithValue("@Salt", user.SaltBytes);

                    myConnection.Open();
                    Int32 rowsInserted = sqlCmd.ExecuteNonQuery();
                    myConnection.Close();

                    return rowsInserted;
                }
            }
            catch (Exception e)
            {
                throw new ContextException("Cannot add given user!", e);
            }
        }

        public Int32 Delete(Guid userId)
        {
            try
            {

                SqlConnection myConnection = new SqlConnection();
                using (myConnection)
                {
                    myConnection.ConnectionString = ConnectionContext.GetConnectionString();

                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.CommandType = CommandType.Text;
                    sqlCmd.CommandText = Requests.DeleteUser;
                    sqlCmd.Connection = myConnection;
                    sqlCmd.Parameters.AddWithValue("@userId", userId);
                    myConnection.Open();
                    Int32 rowDeleted = sqlCmd.ExecuteNonQuery();
                    return rowDeleted;
                }
            }
            catch (Exception e)
            {
                throw new ContextException("Cannot find user!", e);
            }
        }

        public Int32 Update(CUser user)
        {
            SqlConnection myConnection = new SqlConnection();
            try
            {
                using (myConnection)
                {
                    myConnection.ConnectionString = ConnectionContext.GetConnectionString();

                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.CommandType = CommandType.Text;
                    sqlCmd.CommandText = Requests.UpdateUser;
                    sqlCmd.Connection = myConnection;

                    sqlCmd.Parameters.AddWithValue("@UserId", user.Guid);
                    sqlCmd.Parameters.AddWithValue("@UserName", user.Name);
                    sqlCmd.Parameters.AddWithValue("@Email", user.Email);
                    sqlCmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                    sqlCmd.Parameters.AddWithValue("@Salt", user.SaltBytes);

                    myConnection.Open();
                    Int32 rowUpdated = sqlCmd.ExecuteNonQuery();
                    return rowUpdated;
                }
            }
            catch (Exception)
            {

                throw;
            }

        }


        #endregion
    }
}