using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using CasheServer.Controllers;
using DTO;
using CasheServer.Exceptions;

namespace CasheServer
{
    /// <summary>
    /// This class uses 'ReaderWriterLockSlim' to implement concurrent access 
    /// to public static Dictionary<guid, List<CplaylistInfo>> UserFiles.
    /// </summary>
    public class SlimReaderWriter
    {
        static ReaderWriterLockSlim _rw = new ReaderWriterLockSlim();

        // Read files for given userId within given playlistId. Can return empty collection==null
        public static List<CFileInfo> Get(CPlaylistInfo playlistInfo)
        {
            try
            {
                    _rw.EnterReadLock();

                    List<CFileInfo> userFiles = CacheController.UserFiles[playlistInfo.UserId].
                        Where(p => p.Id.Equals(playlistInfo.Id)).FirstOrDefault().Files;
                    return userFiles;

            }
            catch (Exception ex)
            {
                throw new CacheServerException("Error on Cache Service trying to Get list of user files.", ex);
            }
            finally
            {
                    _rw.ExitReadLock();
            }
        }

        // Request from FileController to Write fileInfo into Dictionary<Guid, List<CPlaylistInfo>>
        public static bool AddOrUpdate(CFileInfo fileInfo)
        {
            try
            {
                _rw.EnterWriteLock();
                // if no user is present in UserFiles
                if (!CacheController.UserFiles.Keys.Contains(fileInfo.UserId))
                {
                    // insert user and empty List<CPlaylistInfo>
                    CacheController.UserFiles.Add(fileInfo.UserId, new List<CPlaylistInfo>());
                }

                // insert playlists in Dictionary for new user, or for existing if new playlists are present
                foreach (CPlaylistInfo playlistInfo in fileInfo.playlists)
                {
                    // Add playlist if it not present in Dictionary
                    CPlaylistInfo existingPlaylist = CacheController.UserFiles[fileInfo.UserId]
                        .Where(p => p.Id.Equals(playlistInfo.Id)).FirstOrDefault();

                    if (existingPlaylist == null)
                    {
                        CacheController.UserFiles[fileInfo.UserId].Add(playlistInfo);
                    }

                    // get the playlist - at this point it has to exist or be created
                    CPlaylistInfo insertToPlaylist = CacheController.UserFiles[fileInfo.UserId]
                        .Where(p => p.Id.Equals(playlistInfo.Id)).First();

                    // Add fileInfo if it is not already present in Dictionary
                    CFileInfo existingFileInfo = insertToPlaylist.Files
                        .Where(f => f.Guid.Equals(fileInfo.Guid)).FirstOrDefault();

                    if (existingFileInfo == null)
                    {
                        // if fileInfo doesn't exist - add new to the Playlist
                        insertToPlaylist.Files.Add(fileInfo);
                    }
                    else
                    {
                        // if fileInfo exist - update its data with new fileInfo
                        existingFileInfo = fileInfo;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new CacheServerException("Error on Cache Service trying to AddOrUpdate file", ex);
            }
            finally
            {
                _rw.ExitWriteLock();
            }

        }

        public static bool Delete(CFileInfo fileInfo)
        {
            try
            {
                _rw.EnterWriteLock();
                // if no user is present in UserFiles
                if (!CacheController.UserFiles.Keys.Contains(fileInfo.UserId))
                {
                    return false;
                }

                // find playlists for this user and delete files from them
                foreach (CPlaylistInfo playlistInfo in fileInfo.playlists)
                {
                    // existing playlist in Dictionary
                    CPlaylistInfo existingPlaylist = CacheController.UserFiles[fileInfo.UserId]
                        .Where(p => p.Id.Equals(playlistInfo.Id)).FirstOrDefault();

                    // if Dictionary actually contains playlist mentioned in fileInfo.Playlists
                    if (existingPlaylist != null)
                    {
                        // if file with such Id exists in playlist
                        CFileInfo existingFileInfo = existingPlaylist.Files
                            .Where(f => f.Guid.Equals(fileInfo.Guid)).FirstOrDefault();

                        if (existingFileInfo != null)
                        {
                            // delete file from playlist
                            existingPlaylist.Files.Remove(existingFileInfo);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new CacheServerException("Error on Cache Service trying to delete file", ex);
            }
            finally
            {
                _rw.ExitWriteLock();
            }
        }

        ~SlimReaderWriter()
        {
            if (_rw!=null)
            {
                _rw.Dispose();
            }
        }


    }
}
