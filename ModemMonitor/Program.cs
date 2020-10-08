using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ModemMonitor
{
	class Program
	{
		//static String filename = "settings.cfg.txt";
		//static String hostname = "http://192.168.100.1/";
		static HttpClient _client = new HttpClient();

		static String username = "admin";
		static String password = "bEn2o#US9s";

		static int secondsToWait = 30;


		static async Task Main(string[] args)
		{
			bool keepGoing = true;
			int msToWait = secondsToWait * 1000;	// convert to milliseconds once
			ModemTC4400 connection = new ModemTC4400(_client, username, password);

			// Start loop
			while (keepGoing)
			{
				try
				{
					await connection.GetAllDataAsync();
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error: {ex.Message} ");
					if (ex.InnerException != null)
					{
						Console.WriteLine($"Error: {ex.InnerException} ");
					}
					//Console.WriteLine($"Error: {ex.StackTrace} ");
				}

				// wait for x seconds
				Thread.Sleep(msToWait);
			}

			Console.WriteLine("Done. Press Enter to stop program.");
			Console.ReadLine();
		}
	}
}
