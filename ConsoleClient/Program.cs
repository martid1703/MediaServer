using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using DTO;
using ConsoleClient.WebApi;
using ConsoleClient.ProgramActions;
using System.IO;
using System.Threading;
using ConsoleClient.Exceptions;
using System.Linq;

namespace ConsoleClient
{
    class Program
    {
        static UserWebApi clientWebApiConsumer = new UserWebApi();


        static void Main(string[] args)
        {
            // todo: get user settings from "UserSettings.xml" and write them to the UserSettings class
            UserSettings.ChunkSize = 500000;// 500 kB
            UserSettings.UserFolder = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..\\..\\" + "UserFiles\\"));

            CancellationToken ct = new CancellationToken();

            Console.WriteLine("Welcome to Media Server!");
            CUserInfo userInfo = new CUserInfo();
            userInfo = new CUserInfo("Dmitrii", "dmitrii@example.com", "123");// testing data


            String command;// command from user 

            Console.WriteLine("Existing users: Dmitrii, testUser, passwords: 123");

            do
            {
                Console.WriteLine("Please login(press 2 or type 'login') or register(press 0 or type 'register')!");
                command=Console.ReadLine();
                switch (command)
                {
                    case "0":
                    case "register":
                        userInfo = ActionsOnUser.GetUserCredentialsForRegister();
                        ActionsOnUser.Register(userInfo);
                        break;
                    case "2":
                    case "login":
                        userInfo = UserLogin(userInfo);
                        break;
                    default:break;
                }
            } while (userInfo.Id.Equals(Guid.Empty));


            Console.WriteLine("For list of commands press \"?\", to exit press \"q\"");

            do
            {
                command = Console.ReadLine();
                CFileInfo selectedFile;

                switch (command)
                {
                    case "?":
                        PrintListOfActions();
                        break;
                    // -------------USER ACTIONS-----------------------------
                    case "0":
                    case "register":
                        userInfo = ActionsOnUser.GetUserCredentialsForRegister();
                        ActionsOnUser.Register(userInfo);
                        break;
                    case "1":
                    case "unregister":
                        ActionsOnUser.Unregister(userInfo);
                        break;
                    case "2":
                    case "login":
                        userInfo = UserLogin(userInfo);
                        break;
                    case "3":
                    case "logout":
                        LogOut(userInfo);
                        break;
                    case "4":
                    case "userByName":
                        ActionsOnUser.UserByName(userInfo);
                        break;
                    case "5":
                    case "users":
                        ActionsOnUser.GetAllUsers(userInfo);
                        break;
                    // -------------FILE ACTIONS-----------------------------

                    case "6":
                    case "uploadFile":
                        FileInfo fileInfo = ActionsOnFiles.GetFileInfo();
                        if (fileInfo == null)
                        {
                            Console.WriteLine("Cannot load empty file");
                        }
                        Console.WriteLine("Upload file as public file? [Y/N]");
                        string answer = Console.ReadLine();
                        bool isPublic = false;
                        if (answer.Equals("y", StringComparison.InvariantCultureIgnoreCase))
                        {
                            isPublic = true;
                        }
                        ActionsOnFiles.UploadFileInChunksAsync(fileInfo, isPublic, userInfo, ct);
                        break;
                    case "7":
                    case "downloadFile":
                        selectedFile = SelectFile(userInfo, ct);
                        // downloading file in chunks
                        ActionsOnFiles.DownloadFileInChunksAsync(selectedFile, userInfo, ct);
                        break;
                    case "8":
                    case "renameFile":
                        selectedFile = SelectFile(userInfo, ct);
                        Console.WriteLine("Input new file name:");
                        string newName = Console.ReadLine();
                        ActionsOnFiles.RenameFileAsync(selectedFile, newName, userInfo);
                        break;
                    case "9":
                    case "deleteFile":
                        selectedFile = SelectFile(userInfo, ct);
                        ActionsOnFiles.DeleteFileAsync(selectedFile, userInfo);
                        break;
                    case "10":
                    case "addToPlaylist":

                        break;
                    case "11":
                    case "deleteFromPlaylist":

                        break;

                    case "12":
                    case "userFiles":
                        List<CFileInfo> userFiles = ActionsOnFiles.GetUserFiles(userInfo).Result;
                        //todo: orderby doesnt work?
                        userFiles.OrderByDescending(f => f.Size);
                        PrintUserFiles(userFiles);
                        break;
                    case "13":
                    case "deleteAllUserFiles":
                        List<CFileInfo> userFiles2 = ActionsOnFiles.GetUserFiles(userInfo).Result;
                        foreach (CFileInfo fi in userFiles2)
                        {
                            ActionsOnFiles.DeleteFileAsync(fi, userInfo);
                        }
                        Console.WriteLine("All user files deleted from Media Server!");
                        break;
                    // -------------PLAYLIST ACTIONS-----------------------------
                    case "20":
                    case "playlistsByUserId":

                        break;

                    case "21":
                    case "playlistById":

                        break;
                    case "22":
                    case "playlistByName":

                        break;
                    case "23":
                    case "createPlaylist":

                        break;
                    case "24":
                    case "deletePlaylist":

                        break;
                    case "25":
                    case "renamePlaylist":

                        break;
                    default:
                        break;
                }
            } while (command != "Q" && command != "q");
        }

        private static void PrintUserFiles(List<CFileInfo> userFiles)
        {
            if (userFiles.Count == 0)
            {
                Console.WriteLine("No user files!");
            }
            for (int i = 0; i < userFiles.Count; i++)
            {
                Console.WriteLine(
                    $"{i}) {userFiles[i].Name}, " +
                    $"Size: {userFiles[i].Size / 1024f / 1024f:F2} MB, " +
                    $"Load date: {userFiles[i].LoadDate}");
            }
        }

        private static CUserInfo UserLogin(CUserInfo userInfo)
        {
            try
            {
                if (String.IsNullOrEmpty(userInfo.Name) || String.IsNullOrEmpty(userInfo.Password))
                {
                    userInfo = ActionsOnUser.GetUserCredentials();
                }
                userInfo.Id = ActionsOnUser.Login(userInfo);
            }
            catch (WebApiException ex)
            {
                Console.WriteLine("Login exception. Try again? [Y/N]", ex);
                string answer = Console.ReadLine();
                if (answer.Equals("y", StringComparison.InvariantCultureIgnoreCase))
                {
                    userInfo = ActionsOnUser.GetUserCredentials();
                }
            }
            return userInfo;
        }

        private static void LogOut(CUserInfo userInfo)
        {
            try
            {
                userInfo.Id = Guid.Empty;
                userInfo.Name = "";
                userInfo.Email = "";
                Console.WriteLine("User has logged out!");
            }
            catch (WebApiException ex)
            {
                Console.WriteLine("Error on user unlogin", ex);
            }
            return;
        }

        private static CFileInfo SelectFile(CUserInfo userInfo, CancellationToken ct)
        {
            // retreiving list of user files from Media Server
            List<CFileInfo> userFiles = ActionsOnFiles.GetUserFiles(userInfo).Result;

            // printing the file names
            Console.WriteLine("Enter the number of the file:");
            PrintUserFiles(userFiles);

            int fileNumber;
            if (Int32.TryParse(Console.ReadLine(), out fileNumber))
            {
                // searching for file index
                for (int i = 0; i < userFiles.Count; i++)
                {
                    if (fileNumber == i)
                    {
                        return userFiles[i];
                    }
                }
            };

            return null;
        }

        private static void PrintListOfActions()
        {
            Console.WriteLine("-------------USER ACTIONS-----------------------------");
            Console.WriteLine($"0. For register type \"register\" or command number.");
            Console.WriteLine($"1. For unregister type \"unregister\"  or command number.");
            Console.WriteLine($"2. For login type \"login\"  or command number.");
            Console.WriteLine($"3. For LOGOUT type \"logout\"  or command number.");
            //Console.WriteLine($"4. To get user by id type \"userById\"  or command number.");
            Console.WriteLine($"5. To get all users type \"users\"  or command number.");

            Console.WriteLine("-------------FILE ACTIONS-----------------------------");
            Console.WriteLine($"6. To upload a file type \"uploadFile\"  or command number.");
            Console.WriteLine($"7. To download a file type \"downloadFile\"  or command number.");
            Console.WriteLine($"8. To rename a file type \"renameFile\"  or command number.");
            Console.WriteLine($"9. To delete file from Media Server type \"deleteFromPlaylist\"  or command number.");
            //Console.WriteLine($"10. Add file to playlist type \"AddtoPlaylist\"  or command number.");
            //Console.WriteLine($"11. To delete file from playlist type \"deleteFromPlaylist\"  or command number.");
            Console.WriteLine($"12. To list user files type \"userFiles\"  or command number.");
            Console.WriteLine($"13. To delete ALL user files type \"deleteAllUserFiles\"  or command number.");


            Console.WriteLine("-------------PLAYLIST ACTIONS--(NOT YET IMPLEMENTED - ALL FILES ARE IN THE 'default' playlist)--------------");
            //Console.WriteLine($"13. To get playlists by user id type \"playlistsByUserId\"  or command number.");
            //Console.WriteLine($"14. To get playlist by id type \"playlistById\"  or command number.");
            //Console.WriteLine($"15. To get playlist by name type \"playlistByName\"  or command number.");
            //Console.WriteLine($"16. To create playlist type \"createPlaylist\"  or command number.");
            //Console.WriteLine($"17. To delete playlist type \"deletePlaylist\"  or command number.");
            //Console.WriteLine($"18. To rename playlist type \"renamePlaylist\"  or command number.");

        }


    }
}
