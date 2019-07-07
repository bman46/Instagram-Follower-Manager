using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InstaSharper;
using InstaSharper.API;
using InstaSharper.API.Builder;
using InstaSharper.Classes;
using InstaSharper.Logger;

namespace Instagram_Follower_Manager
{
    class Program
    {
        private static IInstaApi _instaApi;

        static void Main(string[] args)
        {
            var result = Task.Run(MainAsync).GetAwaiter().GetResult();
            if (result)
                return;
            Console.ReadKey();
        }
        public static async Task<bool> MainAsync()
        {
            try
            {
                Console.WriteLine("Instagram Follower Manager");

                // create user session data and provide login details
                Console.WriteLine("Username then password on seperate lines.");
                var userSession = new UserSessionData
                {
                    UserName = Console.ReadLine(),
                    Password = Console.ReadLine()
                };
                Console.Clear();
                var delay = RequestDelay.FromSeconds(2, 2);

                // create new InstaApi instance using Builder

                _instaApi = InstaApiBuilder.CreateBuilder()

                    .SetUser(userSession)

                    .UseLogger(new DebugLogger(LogLevel.Exceptions)) // use logger for requests and debug messages

                    .SetRequestDelay(delay)

                    .Build();

                const string stateFile = "state.bin";

                try
                {
                    if (File.Exists(stateFile))
                    {
                        Console.WriteLine("Loading state from file");
                        using (var fs = File.OpenRead(stateFile))
                        {

                            _instaApi.LoadStateDataFromStream(fs);

                        }

                    }

                }
                catch (Exception e)
                {

                    Console.WriteLine(e);

                }

                if (!_instaApi.IsUserAuthenticated)
                {

                    // login

                    Console.WriteLine($"Logging in as {userSession.UserName}");

                    delay.Disable();

                    var logInResult = await _instaApi.LoginAsync();

                    delay.Enable();

                    if (!logInResult.Succeeded)

                    {

                        Console.WriteLine($"Unable to login: {logInResult.Info.Message}");

                        return false;

                    }

                }

                var state = _instaApi.GetStateDataAsStream();

                using (var fileStream = File.Create(stateFile))
                {

                    state.Seek(0, SeekOrigin.Begin);

                    state.CopyTo(fileStream);

                }
                Console.WriteLine("Getting followers and following...");
                //follower functions:
                var currentUser = await _instaApi.GetCurrentUserAsync();


                var result = await _instaApi.GetCurrentUserFollowersAsync(PaginationParameters.MaxPagesToLoad(5));
                var followers = result.Value;

                result = await _instaApi.GetUserFollowingAsync(currentUser.Value.UserName, PaginationParameters.MaxPagesToLoad(5));
                var following = result.Value;

                Console.WriteLine("\nPeople who dont follow you back :( \n");

                foreach (var x in following)
                {
                    if (!followers.Contains(x))
                    {
                        Console.WriteLine(x.UserName.ToString());
                    }                   
                }
                Console.WriteLine("\nType a Username to unfollow them. (put a space before typing the username)");
                Console.WriteLine("press ESC to exit.");
               do
                {
                    var resultnew = await _instaApi.GetUserAsync(Console.ReadLine());
                    await _instaApi.UnFollowUserAsync(resultnew.Value.Pk);
                    Console.WriteLine("Unfollowed.");
                } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex);

            }
            return false;

        }
    }
}
