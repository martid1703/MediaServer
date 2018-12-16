using DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ConsoleClient.WebApi;
using ConsoleClient.Exceptions;

namespace ConsoleClient.ProgramActions
{
    static class ActionsOnUser
    {
        static UserWebApi userWebApi = new UserWebApi();

        // Get user credentials to be used in following actions
        internal static CUserInfo GetUserCredentials()
        {
            Console.WriteLine("Provide your credentials:");

            do
            {
                Console.WriteLine("Input user name:");
                string name = Console.ReadLine();
                Console.WriteLine("Input password:");
                string password = Console.ReadLine();
                CUserInfo user = new CUserInfo(name: name, password: password);

                if (user.Name != String.Empty && user.Password != String.Empty)
                {
                    Console.WriteLine("Thank you! Now you can continue to login. For list of commands press \"?\", to exit press \"q\"");
                    return user;
                }
                Console.WriteLine("Sorry! You must provide all requested fields.");
            } while (true);
        }


        // Get user credentials to be used in following actions
        internal static CUserInfo GetUserCredentialsForRegister()
        {
            Console.WriteLine("Provide your credentials:");

            do
            {
                Console.WriteLine("Input user name:");
                string name = Console.ReadLine();
                Console.WriteLine("Input password:");
                string password = Console.ReadLine();
                Console.WriteLine("Input email:");
                string email = Console.ReadLine();
                CUserInfo user = new CUserInfo(name: name, password: password, email: email);

                if (user.Name != String.Empty && user.Password != String.Empty && user.Email != String.Empty)
                {
                    Console.WriteLine("Thank you! Now you can continue. For list of commands press \"?\", to exit press \"q\"");
                    return user;
                }
                Console.WriteLine("Sorry! You must provide all requested fields.");
            } while (true);
        }

        // Get list of users from MediaServer
        internal static void GetAllUsers(CUserInfo user)
        {
            // Get all users from MediaServer
            Console.WriteLine("\nGetting list of all users...");
            HttpResponseMessage loginResponse = userWebApi.GetUsersAsync(user).Result;
            if (loginResponse.IsSuccessStatusCode)
            {
                List<CUserInfo> users = loginResponse.Content.ReadAsAsync<List<CUserInfo>>().Result;
                foreach (CUserInfo userInfo in users)
                {
                    Console.WriteLine($"User name: {userInfo.Name}, user email: {userInfo.Email}");
                }
            }
            else
            {
                Console.WriteLine($"User {user.Name} couldn't login to MediaServer!");
            }


        }

        // Check credentials of the user = Login to the MediaServer
        internal static Guid Login(CUserInfo user)
        {
            try
            {
                Console.WriteLine("\nLoggin in MediaServer...");
                HttpResponseMessage loginResponse = userWebApi.LoginAsync(user).Result;

                Guid userId = loginResponse.Content.ReadAsAsync<CUserInfo>().Result.Id;
                Console.WriteLine($"User {user.Name} successfully logged to MediaServer!");
                Console.WriteLine($"UserId: {userId}");
                return userId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"User {user.Name} couldn't login to Media Server!");
                throw new WebApiException($"User {user.Name} couldn't login to MediaServer!", ex);
            }

        }

        // Register user in MediaServer
        internal static void Register(CUserInfo user)
        {
            HttpResponseMessage response = userWebApi.RegisterAsync(user).Result;
            if (response.IsSuccessStatusCode)
            {
                user = response.Content.ReadAsAsync<CUserInfo>().Result;
                Console.WriteLine($"User {user.Name} is successfully registered in MediaServer!");
            }
            else
            {
                Console.WriteLine($"User {user.Name} failed to register in MediaServer!" +
                    $"\nServer reply: {response.Content.ReadAsStringAsync().Result}");
            }
        }

        // Register user in MediaServer
        internal static void Unregister(CUserInfo user)
        {

            HttpResponseMessage response = userWebApi.UnregisterAsync(user).Result;
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"User {user.Name} successfully unregistered in MediaServer!");
            }
            else
            {
                Console.WriteLine($"User {user.Name} failed to unregister in MediaServer!");
            }
        }

        // Edit user in MediaServer //todo: THIS IS BAD PRACTICE - BETTER DELETE AND CREATE NEW USER
        internal static void Edit(CUserInfo user)
        {
            Console.WriteLine("Please input new details for the user");
            CUserInfo updateUser = GetUserCredentials();

            HttpResponseMessage response = userWebApi.EditAsync(updateUser).Result;
            CUserInfo userMediaServer = response.Content.ReadAsAsync<CUserInfo>().Result;
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"User {updateUser.Name} is successfully edited in MediaServer!");
            }
            else
            {
                Console.WriteLine($"User {updateUser.Name} failed to be edit in MediaServer!");
            }
        }

        // Edit user in MediaServer
        internal static void UserByName(CUserInfo user)
        {
            Console.WriteLine("Input user name to find in MediaServer");
            string userToFind = Console.ReadLine();

            HttpResponseMessage response = userWebApi.UserByNameAsync(user, userToFind).Result;
            user = response.Content.ReadAsAsync<CUserInfo>().Result;
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"User {user.Name} found in MediaServer! Email: {user.Email}!");
            }
            else
            {
                Console.WriteLine($"User {user.Name} not found in MediaServer!");
            }
        }
    }
}
