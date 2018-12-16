using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using MediaServer.Exceptions;
using MediaServer.Models;

namespace MediaServer.DAL
{
    public class FileContext
    {
        private static class Requests
        {
            public const string FilesFromPlaylist = "Select * from Files WHERE FileId=" +
                    "(SELECT * " +
                    "FROM FilesToPlaylists " +
                    "WHERE PlaylistId=@playlistId')";
            public const string Create = "INSERT INTO Files (" +
                    "FileId, FileName,Path, UserId,IsPublic,LoadDateTime, Size, Views, Likes, Dislikes) " +
                    "Values (" +
                    "@fileId, @fileName,@path, @userId, @isPublic, @loadDateTime, @size, @views, @likes, @dislikes)";
            public const string Delete = "DELETE FROM Files WHERE FileId=@fileId";
            public const string Update = "UPDATE Files SET " +
                        "FileId=@fileId, FileName=@fileName, Path=@path, UserId=@userId, IsPublic=@isPublic, " +
                        "Hash=@hash, LoadDateTime=@loadDateTime, Size=@size, " +
                        "Views=@views, Likes=@likes, Dislikes=@dislikes" +
                        " WHERE FileId=@fileId";
            public const string GetByFileName = "SELECT * FROM Files WHERE FileName=@fileName AND UserId=@userId";
            public const string GetByFileId = "SELECT * FROM Files WHERE FileId=@fileId";
            public const string GetByUserId = "SELECT * FROM Files WHERE UserId=@userId";
            public const string GetByUserIdAndPlaylistId =
                "SELECT TOP(10) * FROM Files AS f" +
                "JOIN FilesToPlaylists AS ftp" +
                "ON(f.FileId= ftp.FileId)" +
                "WHERE ftp.PlaylistId=@playlistId";


            public const string GetByHash = "SELECT * FROM Files WHERE Hash=@hash";
            public const string AddToPlaylist = "INSERT INTO FilesToPlaylists (Id,FileId,PlaylistId) VALUES (@id, @fileId, @playlistId)";

        }

        public FileContext()
        {

        }

        #region CRUD
        public IEnumerable<CFile> FilesFromPlaylist(Guid playlistId)
        {
            SqlConnection myConnection = new SqlConnection();
            using (myConnection)
            {

                SqlDataReader reader = null;
                myConnection.ConnectionString = ConnectionContext.GetConnectionString();

                SqlCommand sqlCmd = new SqlCommand();
                sqlCmd.CommandType = CommandType.StoredProcedure;
                dynamic procedure = "Files_FilesFromPlaylist";
                sqlCmd.CommandText = procedure;// Requests.FilesFromPlaylist;

                sqlCmd.Parameters.AddWithValue("@playlistId", playlistId);

                sqlCmd.Connection = myConnection;
                myConnection.Open();
                reader = sqlCmd.ExecuteReader();
                List<CFile> files = new List<CFile>();
                while (reader.Read())
                {
                    CFile file = new CFile();

                    file.Guid = (Guid)(reader.GetValue(0));
                    file.Name = (String)reader.GetValue(1);
                    file.Path = (String)reader.GetValue(2);
                    file.UserId = (Guid)(reader.GetValue(3));
                    file.IsPublic = (Boolean)reader.GetValue(4);
                    file.Hash = reader.IsDBNull(5) ? null : (byte[])reader.GetValue(5);
                    file.LoadDate = (DateTime)reader.GetValue(6);
                    file.Size = (Int64)reader.GetValue(7);
                    file.Views = (Int32)reader.GetValue(8);
                    file.Likes = (Int32)reader.GetValue(9);
                    file.Dislikes = (Int32)reader.GetValue(10);

                    files.Add(file);
                }
                return files;
            }
        }

        public Int32 Create(CFile file, SqlConnection myConnection, SqlTransaction myTransaction)
        {
            try
            {
                Int32 result;
                SqlCommand sqlCmd = new SqlCommand();
                using (sqlCmd)
                {
                    sqlCmd.Transaction = myTransaction;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    dynamic procedure = "Files_Insert";
                    sqlCmd.CommandText = procedure;// Requests.Create;

                    // in such version SQLconnection is opened and closed from calling function
                    sqlCmd.Connection = myConnection;

                    sqlCmd.Parameters.AddWithValue("@fileId", Guid.NewGuid());
                    sqlCmd.Parameters.AddWithValue("@fileName", file.Name);
                    sqlCmd.Parameters.AddWithValue("@path", file.Path);
                    sqlCmd.Parameters.AddWithValue("@userId", file.UserId);
                    sqlCmd.Parameters.AddWithValue("@isPublic", file.IsPublic);
                    //sqlCmd.Parameters.AddWithValue("@hash", DBNull.Value);
                    sqlCmd.Parameters.AddWithValue("@loadDateTime", file.LoadDate);
                    sqlCmd.Parameters.AddWithValue("@size", file.Size);
                    sqlCmd.Parameters.AddWithValue("@views", file.Views);
                    sqlCmd.Parameters.AddWithValue("@likes", file.Likes);
                    sqlCmd.Parameters.AddWithValue("@dislikes", file.Dislikes);

                    result = sqlCmd.ExecuteNonQuery();
                }
                return result;
            }
            catch (Exception e)
            {
                throw new ContextException("Cannot add given file!", e);
            }
        }

        public Int32 Delete(Guid fileId)
        {
            try
            {
                SqlConnection myConnection = new SqlConnection();
                using (myConnection)
                {
                    myConnection.ConnectionString = ConnectionContext.GetConnectionString();

                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    // records from connection table will be deleted CASCADE
                    dynamic procedure = "Files_Delete";
                    sqlCmd.CommandText = procedure;// Requests.Delete;
                    sqlCmd.Connection = myConnection;

                    sqlCmd.Parameters.AddWithValue("@fileId", fileId);

                    myConnection.Open();
                    Int32 rowDeleted = sqlCmd.ExecuteNonQuery();
                    return rowDeleted;
                }
            }
            catch (Exception e)
            {
                throw new ContextException("Cannot delete file!", e);
            }
        }

        public Int32 Update(CFile file)
        {
            SqlConnection myConnection = new SqlConnection();
            try
            {
                using (myConnection)
                {
                    myConnection.ConnectionString = ConnectionContext.GetConnectionString();

                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.CommandType = CommandType.Text;

                    sqlCmd.CommandText = Requests.Update;

                    sqlCmd.Connection = myConnection;

                    sqlCmd.Parameters.AddWithValue("@fileId", file.Guid);
                    sqlCmd.Parameters.AddWithValue("@fileName", file.Name);
                    sqlCmd.Parameters.AddWithValue("@path", file.Path);
                    sqlCmd.Parameters.AddWithValue("@userId", file.UserId);
                    sqlCmd.Parameters.AddWithValue("@isPublic", file.IsPublic);

                    if (file.Hash == null)
                    {
                        sqlCmd.Parameters.AddWithValue("@hash", System.Data.SqlTypes.SqlBinary.Null);
                    }
                    else
                    {
                        sqlCmd.Parameters.AddWithValue("@hash", file.Hash);
                    }

                    sqlCmd.Parameters.AddWithValue("@loadDateTime", file.LoadDate);
                    sqlCmd.Parameters.AddWithValue("@size", file.Size);
                    sqlCmd.Parameters.AddWithValue("@views", file.Views);
                    sqlCmd.Parameters.AddWithValue("@likes", file.Likes);
                    sqlCmd.Parameters.AddWithValue("@dislikes", file.Dislikes);

                    myConnection.Open();
                    Int32 rowUpdated = sqlCmd.ExecuteNonQuery();
                    return rowUpdated;
                }
            }
            catch (Exception e)
            {
                throw new ContextException("Cannot update file!", e);
            }

        }

        public Int32 Update(CFile file, SqlConnection myConnection, SqlTransaction myTransaction)
        {
            try
            {
                SqlCommand sqlCmd = new SqlCommand();
                sqlCmd.Transaction = myTransaction;
                sqlCmd.CommandType = CommandType.Text;

                sqlCmd.CommandText = Requests.Update;

                sqlCmd.Connection = myConnection;

                sqlCmd.Parameters.AddWithValue("@fileId", file.Guid);
                sqlCmd.Parameters.AddWithValue("@fileName", file.Name);
                sqlCmd.Parameters.AddWithValue("@path", file.Path);
                sqlCmd.Parameters.AddWithValue("@userId", file.UserId);
                sqlCmd.Parameters.AddWithValue("@isPublic", file.IsPublic);

                if (file.Hash == null)
                {
                    sqlCmd.Parameters.AddWithValue("@hash", System.Data.SqlTypes.SqlBinary.Null);
                }
                else
                {
                    sqlCmd.Parameters.AddWithValue("@hash", file.Hash);
                }

                sqlCmd.Parameters.AddWithValue("@loadDateTime", file.LoadDate);
                sqlCmd.Parameters.AddWithValue("@size", file.Size);
                sqlCmd.Parameters.AddWithValue("@views", file.Views);
                sqlCmd.Parameters.AddWithValue("@likes", file.Likes);
                sqlCmd.Parameters.AddWithValue("@dislikes", file.Dislikes);

                Int32 rowUpdated = sqlCmd.ExecuteNonQuery();
                return rowUpdated;
            }
            catch (Exception e)
            {
                throw new ContextException("Cannot update file!", e);
            }

        }

        // in such version SQLconnection is opened and closed from calling function
        public CFile GetByFileName(string fileName, Guid userId, SqlConnection myConnection, SqlTransaction myTransaction)
        {
            try
            {
                CFile file = new CFile();

                SqlDataReader reader = null;

                SqlCommand sqlCmd = new SqlCommand();
                using (sqlCmd)
                {
                    sqlCmd.Transaction = myTransaction;
                    sqlCmd.Connection = myConnection;

                    sqlCmd.CommandType = CommandType.Text;
                    sqlCmd.CommandText = Requests.GetByFileName;


                    sqlCmd.Parameters.AddWithValue("@fileName", fileName);
                    sqlCmd.Parameters.AddWithValue("@userId", userId);

                    reader = sqlCmd.ExecuteReader();
                    while (reader.Read())
                    {
                        file.Guid = (Guid)(reader.GetValue(0));
                        file.Name = (String)reader.GetValue(1);
                        file.Path = (String)reader.GetValue(2);
                        file.UserId = (Guid)(reader.GetValue(3));
                        file.IsPublic = (Boolean)reader.GetValue(4);
                        file.Hash = reader.IsDBNull(5) ? null : (byte[])reader.GetValue(5);
                        file.LoadDate = (DateTime)reader.GetValue(6);
                        file.Size = (Int64)reader.GetValue(7);
                        file.Views = (Int32)reader.GetValue(8);
                        file.Likes = (Int32)reader.GetValue(9);
                        file.Dislikes = (Int32)reader.GetValue(10);
                    }
                    reader.Close();
                }
                return file;
            }
            catch (Exception e)
            {
                throw new ContextException("Cannot find file!", e);
            }

        }

        public CFile GetByFileName(string fileName, Guid userId)
        {
            CFile cFile = new CFile();
            using (SqlConnection myConnection = new SqlConnection())
            {
                myConnection.ConnectionString = ConnectionContext.GetConnectionString();
                myConnection.Open();
                SqlTransaction myTransaction = myConnection.BeginTransaction("transactionFileUpload");
                cFile = GetByFileName(fileName, userId, myConnection, myTransaction);
                myTransaction.Commit();
            }
            return cFile;
        }

        public CFile GetByFileId(Guid fileId)
        {
            try
            {
                SqlConnection myConnection = new SqlConnection();
                using (myConnection)
                {
                    SqlDataReader reader = null;
                    myConnection.ConnectionString = ConnectionContext.GetConnectionString();

                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    dynamic procedure = "Files_GetByFileId";
                    sqlCmd.CommandText = procedure;// Requests.GetByFileId;
                    sqlCmd.Connection = myConnection;

                    sqlCmd.Parameters.AddWithValue("@fileId", fileId);

                    myConnection.Open();
                    reader = sqlCmd.ExecuteReader();
                    CFile file = new CFile();
                    while (reader.Read())
                    {
                        file.Guid = (Guid)(reader.GetValue(0));
                        file.Name = (String)reader.GetValue(1);
                        file.Path = (String)reader.GetValue(2);
                        file.UserId = (Guid)(reader.GetValue(3));
                        file.IsPublic = (Boolean)reader.GetValue(4);
                        file.Hash = reader.IsDBNull(5) ? null : (byte[])reader.GetValue(5);
                        file.LoadDate = (DateTime)reader.GetValue(6);
                        file.Size = (Int64)reader.GetValue(7);
                        file.Views = (Int32)reader.GetValue(8);
                        file.Likes = (Int32)reader.GetValue(9);
                        file.Dislikes = (Int32)reader.GetValue(10);
                    }
                    return file;
                }
            }
            catch (Exception e)
            {
                throw new ContextException("Cannot find file!", e);
            }


        }

        public IEnumerable<CFile> GetByUserId(Guid userId)
        {
            try
            {
                SqlConnection myConnection = new SqlConnection();
                using (myConnection)
                {
                    SqlDataReader reader = null;
                    myConnection.ConnectionString = ConnectionContext.GetConnectionString();

                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    dynamic procedure = "Files_GetByUserId";
                    sqlCmd.CommandText = procedure;// Requests.GetByUserId;
                    sqlCmd.Connection = myConnection;

                    sqlCmd.Parameters.AddWithValue("@userId", userId);

                    myConnection.Open();
                    reader = sqlCmd.ExecuteReader();
                    List<CFile> files = new List<CFile>();
                    while (reader.Read())
                    {
                        CFile file = new CFile();

                        file.Guid = (Guid)(reader.GetValue(0));
                        file.Name = (String)reader.GetValue(1);
                        file.Path = (String)reader.GetValue(2);
                        file.UserId = (Guid)(reader.GetValue(3));
                        file.IsPublic = (Boolean)reader.GetValue(4);
                        file.Hash = reader.IsDBNull(5) ? null : (byte[])reader.GetValue(5);
                        file.LoadDate = (DateTime)reader.GetValue(6);
                        file.Size = (Int64)reader.GetValue(7);
                        file.Views = (Int32)reader.GetValue(8);
                        file.Likes = (Int32)reader.GetValue(9);
                        file.Dislikes = (Int32)reader.GetValue(10);

                        files.Add(file);

                    }
                    return files;
                }
            }
            catch (Exception e)
            {
                throw new ContextException("Cannot find file!", e);
            }

        }

        public IEnumerable<CFile> GetByPlaylistId(Guid playlistId)
        {
            try
            {
                SqlConnection myConnection = new SqlConnection();
                using (myConnection)
                {
                    SqlDataReader reader = null;
                    myConnection.ConnectionString = ConnectionContext.GetConnectionString();

                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    dynamic procedure = "Files_FilesFromPlaylist";
                    sqlCmd.CommandText = procedure;// Requests.GetByUserIdAndPlaylistId;
                    sqlCmd.Connection = myConnection;

                    sqlCmd.Parameters.AddWithValue("@playlistId", playlistId);

                    myConnection.Open();
                    reader = sqlCmd.ExecuteReader();
                    List<CFile> files = new List<CFile>();
                    while (reader.Read())
                    {
                        CFile file = new CFile();

                        file.Guid = (Guid)(reader.GetValue(0));
                        file.Name = (String)reader.GetValue(1);
                        file.Path = (String)reader.GetValue(2);
                        file.UserId = (Guid)(reader.GetValue(3));
                        file.IsPublic = (Boolean)reader.GetValue(4);
                        file.Hash = reader.IsDBNull(5) ? null : (byte[])reader.GetValue(5);
                        file.LoadDate = (DateTime)reader.GetValue(6);
                        file.Size = (Int64)reader.GetValue(7);
                        file.Views = (Int32)reader.GetValue(8);
                        file.Likes = (Int32)reader.GetValue(9);
                        file.Dislikes = (Int32)reader.GetValue(10);

                        files.Add(file);

                    }
                    return files;
                }
            }
            catch (Exception e)
            {
                throw new ContextException("Cannot find file!", e);
            }

        }

        public CFile GetByHash(byte[] hash)
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
                    sqlCmd.CommandText = Requests.GetByHash;
                    sqlCmd.Connection = myConnection;

                    sqlCmd.Parameters.AddWithValue("@hash", hash);

                    myConnection.Open();
                    reader = sqlCmd.ExecuteReader();
                    CFile file = new CFile();
                    while (reader.Read())
                    {
                        file.Guid = (Guid)(reader.GetValue(0));
                        file.Name = (String)reader.GetValue(1);
                        file.Path = (String)reader.GetValue(2);
                        file.UserId = (Guid)(reader.GetValue(3));
                        file.IsPublic = (Boolean)reader.GetValue(4);
                        file.Hash = reader.IsDBNull(5) ? null : (byte[])reader.GetValue(5);
                        file.LoadDate = (DateTime)reader.GetValue(6);
                        file.Size = (Int64)reader.GetValue(7);
                        file.Views = (Int32)reader.GetValue(8);
                        file.Likes = (Int32)reader.GetValue(9);
                        file.Dislikes = (Int32)reader.GetValue(10);
                    }
                    return file;
                }
            }
            catch (Exception e)
            {
                throw new ContextException("Cannot find file!", e);
            }

        }


        // in such version SQLconnection is opened and closed from calling function
        public CFile GetByHash(byte[] hash, SqlConnection myConnection, SqlTransaction myTransaction)
        {
            try
            {
                CFile file = new CFile();
                SqlDataReader reader = null;

                SqlCommand sqlCmd = new SqlCommand();
                using (sqlCmd)
                {
                    sqlCmd.Transaction = myTransaction;
                    sqlCmd.Connection = myConnection;

                    sqlCmd.CommandType = CommandType.Text;
                    sqlCmd.CommandText = Requests.GetByHash;

                    sqlCmd.Parameters.AddWithValue("@hash", hash);

                    reader = sqlCmd.ExecuteReader();
                    while (reader.Read())
                    {
                        file.Guid = (Guid)(reader.GetValue(0));
                        file.Name = (String)reader.GetValue(1);
                        file.Path = (String)reader.GetValue(2);
                        file.UserId = (Guid)(reader.GetValue(3));
                        file.IsPublic = (Boolean)reader.GetValue(4);
                        file.Hash = reader.IsDBNull(5) ? null : (byte[])reader.GetValue(5);
                        file.LoadDate = (DateTime)reader.GetValue(6);
                        file.Size = (Int64)reader.GetValue(7);
                        file.Views = (Int32)reader.GetValue(8);
                        file.Likes = (Int32)reader.GetValue(9);
                        file.Dislikes = (Int32)reader.GetValue(10);
                    }
                    reader.Close();
                }
                return file;
            }
            catch (Exception e)
            {
                throw new ContextException("Cannot find file!", e);
            }

        }


        public Int32 AddToPlaylist(Guid fileId, Guid playListId)
        {
            try
            {
                SqlConnection myConnection = new SqlConnection();
                using (myConnection)
                {
                    myConnection.ConnectionString = ConnectionContext.GetConnectionString();

                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.CommandType = CommandType.Text;
                    // records from connection table will be deleted CASCADE
                    sqlCmd.CommandText = Requests.AddToPlaylist;
                    sqlCmd.Connection = myConnection;

                    sqlCmd.Parameters.AddWithValue("@id", Guid.NewGuid());
                    sqlCmd.Parameters.AddWithValue("@fileId", fileId);
                    sqlCmd.Parameters.AddWithValue("@playlistId", playListId);

                    myConnection.Open();
                    Int32 rowsInserted = sqlCmd.ExecuteNonQuery();
                    return rowsInserted;
                }
            }
            catch (Exception e)
            {
                throw new ContextException("Cannot add file to file!", e);
            }
        }

        public Int32 AddToPlaylist(Guid fileId, Guid playListId, SqlConnection myConnection, SqlTransaction myTransaction)
        {
            try
            {

                SqlCommand sqlCmd = new SqlCommand();
                sqlCmd.Transaction = myTransaction;
                sqlCmd.CommandType = CommandType.Text;
                // records from connection table will be deleted CASCADE
                sqlCmd.CommandText = Requests.AddToPlaylist;
                sqlCmd.Connection = myConnection;

                sqlCmd.Parameters.AddWithValue("@id", Guid.NewGuid());
                sqlCmd.Parameters.AddWithValue("@fileId", fileId);
                sqlCmd.Parameters.AddWithValue("@playlistId", playListId);

                Int32 rowsInserted = sqlCmd.ExecuteNonQuery();
                return rowsInserted;
            }
            catch (Exception e)
            {
                throw new ContextException("Cannot add file to file!", e);
            }
        }

        #endregion
    }

}