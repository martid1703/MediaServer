using MediaServer.Exceptions;
using MediaServer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace MediaServer.DAL
{
    public class PlaylistContext
    {
        public Int32 Create(CPlaylist playlist)
        {
            try
            {
                SqlConnection myConnection = new SqlConnection();
                using (myConnection)
                {

                myConnection.ConnectionString = ConnectionContext.GetConnectionString();

                SqlCommand sqlCmd = new SqlCommand();
                sqlCmd.CommandType = CommandType.Text;
                sqlCmd.CommandText = "INSERT INTO Playlists (Id,Name,UserId,IsPublic) " +
                    $"Values (@Id, @Name,@UserId,@IsPublic)";
                sqlCmd.Connection = myConnection;

                sqlCmd.Parameters.AddWithValue("@Id", playlist.Guid);
                sqlCmd.Parameters.AddWithValue("@Name", playlist.Name);
                sqlCmd.Parameters.AddWithValue("@UserId", playlist.UserId);
                sqlCmd.Parameters.AddWithValue("@IsPublic", playlist.IsPublic);

                myConnection.Open();
                Int32 rowsInserted = sqlCmd.ExecuteNonQuery();
                myConnection.Close();

                return rowsInserted;
                }
            }
            catch (Exception e)
            {
                throw new ContextException("Cannot add given playlist!", e);
            }
        }

        public Int32 DeleteById(Guid playlistId)
        {
            try
            {
                SqlConnection myConnection = new SqlConnection();
                using (myConnection)
                {
                    myConnection.ConnectionString = ConnectionContext.GetConnectionString();

                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.CommandType = CommandType.Text;
                    sqlCmd.CommandText = $"DELETE FROM Playlists WHERE Id='{playlistId}'";
                    sqlCmd.Connection = myConnection;
                    myConnection.Open();
                    Int32 rowDeleted = sqlCmd.ExecuteNonQuery();
                    return rowDeleted;
                }
            }
            catch (Exception e)
            {
                throw new ContextException($"Cannot delete playlist! {playlistId}", e);
            }
        }

        public Int32 Update(CPlaylist playlist)
        {
            SqlConnection myConnection = new SqlConnection();
            try
            {
                using (myConnection)
                {
                    myConnection.ConnectionString = ConnectionContext.GetConnectionString();

                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.CommandType = CommandType.Text;
                    sqlCmd.CommandText =
                        $"UPDATE Playlists SET " +
                        $"Name=@playlistName, IsPublic=@isPublic"+
                        $" WHERE Id=@playlistId";
                    sqlCmd.Connection = myConnection;

                    sqlCmd.Parameters.AddWithValue("@playlistName", playlist.Name);
                    sqlCmd.Parameters.AddWithValue("@playlistId", playlist.Guid);
                    sqlCmd.Parameters.AddWithValue("@isPublic", playlist.IsPublic);


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

        // get all public playlists for given user (beside his own)
        public IEnumerable<CPlaylist> GetAllPublic(Guid userId)
        {
            SqlConnection myConnection = new SqlConnection();

            using (myConnection)
            {
                SqlDataReader reader = null;
                myConnection.ConnectionString = ConnectionContext.GetConnectionString();

                SqlCommand sqlCmd = new SqlCommand();
                sqlCmd.CommandType = CommandType.Text;
                sqlCmd.CommandText =
                    "SELECT * FROM Playlists WHERE IsPublic=@isPublic AND UserId!=@userId";
                sqlCmd.Connection = myConnection;

                sqlCmd.Parameters.AddWithValue("@isPublic", true);
                sqlCmd.Parameters.AddWithValue("@userId", userId);

                myConnection.Open();
                reader = sqlCmd.ExecuteReader();
                List<CPlaylist> playlists = new List<CPlaylist>();
                while (reader.Read())
                {
                    CPlaylist playlist = new CPlaylist();
                    playlist.Guid = (Guid)(reader.GetValue(0));
                    playlist.Name = reader.GetValue(1).ToString();
                    playlist.UserId = (Guid)reader.GetValue(2);
                    playlist.IsPublic = (bool)reader.GetValue(3);
                    playlists.Add(playlist);
                }
                //myConnection.Close();
                return playlists;
            }
        }

        public CPlaylist GetByName(string name, Guid userId)
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
                    sqlCmd.CommandText = $"SELECT * FROM Playlists WHERE Name=@name and UserId=@userId";
                    sqlCmd.Connection = myConnection;

                    sqlCmd.Parameters.AddWithValue("@name", name);
                    sqlCmd.Parameters.AddWithValue("@userId", userId);

                    myConnection.Open();
                    reader = sqlCmd.ExecuteReader();
                    CPlaylist playlist = new CPlaylist();
                    while (reader.Read())
                    {
                        playlist.Guid = (Guid)(reader.GetValue(0));
                        playlist.Name = reader.GetValue(1).ToString();
                        playlist.UserId = (Guid)reader.GetValue(2);
                        playlist.IsPublic = (bool)reader.GetValue(3);
                    }
                    return playlist;
                }
            }
            catch (Exception e)
            {
                throw new ContextException("Cannot find playlist!", e);
            }

        }

        public CPlaylist GetById(Guid playlistId)
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
                    sqlCmd.CommandText = $"SELECT * FROM Playlists WHERE Id=@playlistId";
                    sqlCmd.Connection = myConnection;

                    sqlCmd.Parameters.AddWithValue("@playlistId", playlistId);

                    myConnection.Open();
                    reader = sqlCmd.ExecuteReader();
                    CPlaylist playlist = new CPlaylist();
                    while (reader.Read())
                    {
                        playlist.Guid = (Guid)(reader.GetValue(0));
                        playlist.Name = reader.GetValue(1).ToString();
                        playlist.UserId = (Guid)reader.GetValue(2);
                        playlist.IsPublic = (bool)reader.GetValue(3);
                    }
                    return playlist;
                }
            }
            catch (Exception e)
            {
                throw new ContextException("Cannot find playlist!", e);
            }

        }

        public IEnumerable<CPlaylist> GetByUserId(Guid userId)
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
                    sqlCmd.CommandText = $"SELECT * FROM Playlists WHERE UserId=@userId";
                    sqlCmd.Connection = myConnection;

                    sqlCmd.Parameters.AddWithValue("@userId", userId);

                    myConnection.Open();
                    reader = sqlCmd.ExecuteReader();
                    List<CPlaylist> playlists = new List<CPlaylist>();
                    while (reader.Read())
                    {
                        CPlaylist playlist = new CPlaylist();
                        playlist.Guid = (Guid)(reader.GetValue(0));
                        playlist.Name = reader.GetValue(1).ToString();
                        playlist.UserId = (Guid)reader.GetValue(2);
                        playlist.IsPublic = (bool)reader.GetValue(3);
                        playlists.Add(playlist);

                    }
                    return playlists;
                }
            }
            catch (Exception e)
            {
                throw new ContextException("Cannot find playlist!", e);
            }

        }

    }
}