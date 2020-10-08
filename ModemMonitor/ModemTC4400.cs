using CsvHelper;
using HtmlAgilityPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ModemMonitor
{
	public class ModemTC4400
	{
		// passed parameters
		private HttpClient _client;
		private String username;
		private String password;

		// URLs/pages
		private String hostname = "http://192.168.100.1/";



		public ModemTC4400(HttpClient client, String username, String password)
		{
			_client = client;
			this.username = username;
			this.password = password;
		}

		public async Task GetAllDataAsync()
		{
			// get connection quality data; 2nd table
			await GetDataAsync("cmconnectionstatus.html", typeof(ModemTableDownstreamChannelStatus), "ChannelStatus-", 2, 1);
			//await GetDataAsync("cmeventlog.html", 2, "EventLog-", 1, typeof(ModemTableEventLog));
		}

		private async Task GetDataAsync(String pageUrl, Type classType, String filenamePrefix, int tableNum = 1, int skipTableRows = 0)
		{
			// get date variables and filename
			DateTime genDate = DateTime.Now;
			String strDateDay = genDate.ToString("yyyyMMdd");
			String strDateSec = genDate.ToString("yyyyMMdd-HH.mm.ss");
			String csvFilename = $"csv/{filenamePrefix}{strDateDay}.csv";
			String htmlFilename = $"html/{filenamePrefix}{strDateSec}.htm";

			// ensure directories exist
			new FileInfo(htmlFilename).Directory.Create();
			new FileInfo(csvFilename).Directory.Create();

			// get page
			String pageContents = await GetPageAsync(pageUrl);

			// debug
			//Console.WriteLine(pageContents);
			//Console.ReadLine();

			// save HTML file contents
			//if (!File.Exists(htmlFilename))
			//{
			// Create a new HTML file to write to.
			await File.WriteAllTextAsync(htmlFilename, pageContents, Encoding.UTF8);
			//}


			// extract data from table
			List<ModemTableDataInterface> data = extractTableData(pageContents, classType, tableNum, skipTableRows);

			// save data to CSV
			await WriteCSVAsync(csvFilename, data);
		}

		private async Task<String> GetPageAsync(String page)
		{
			string result = "";
			// setup basic auth
			var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
			_client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
			HttpResponseMessage response = await _client.GetAsync($"{hostname}{page}");
			HttpContent content = response.Content;

			// ... Check Status Code
			//Console.WriteLine("Response StatusCode: " + (int)response.StatusCode);

			// ... Read the string.
			if ((int)response.StatusCode == 200)    // OK
			{
				result = await content.ReadAsStringAsync();
			}

			return result;
		}

		private List<ModemTableDataInterface> extractTableData(String pageContents, Type className, int tableNum = 1, int skipRows = 0)
		{
			List<ModemTableDataInterface> results = new List<ModemTableDataInterface>();
			HtmlDocument htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(pageContents);

			HtmlNodeCollection nodesTR = htmlDoc.DocumentNode.SelectNodes("(//table)[" + tableNum + "]/tr");

			int numOfRows = 0;
			int numOfCols = 0;
			// get <TABLE><TD> nodes
			foreach (var nodeTR in nodesTR)
			{
				// skip rows
				if (numOfRows >= skipRows)
				{
					numOfCols = 0;
					//Console.WriteLine(nodeTR.InnerText);

					// get inner <TD> nodes
					HtmlNodeCollection nodesTD = nodeTR.SelectNodes("td");

					// go through each node
					/*					foreach (var nodeTD in nodesTD)
									{
										Console.WriteLine("Innernode #" + numOfCols + ": " + nodeTD.InnerText);
										numOfCols++;
									}
					*/
					// create a new object of the type that was passed to us
					ModemTableDataInterface data = (ModemTableDataInterface)Activator.CreateInstance(className);
					// store the nodes into the data
					data.ParseData(nodesTD, numOfRows);

					// add data to results
					results.Add(data);
				}
				numOfRows++;
			}

			/*			Console.WriteLine($"Number of rows:    {numOfRows}");
						Console.WriteLine($"Number of columns: {numOfCols}");
			*/
			return results;
		}

		private async Task WriteCSVAsync(String csvFilename, List<ModemTableDataInterface> data)
		{
			using (var writer = new StreamWriter(csvFilename, true))
			using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
			{
				//await csv.WriteRecordsAsync(row);
				//csv.WriteRecords(data);


				// add each row
				foreach (ModemTableDataInterface row in data)
				{
					//await csv.WriteRecordsAsync(row);
					csv.WriteRecords(row);

					/*					// add each column/data field
										for (int i = 0; i < row.Count; i++)
										{
											await csv.WriteRecordsAsync(row.GetValueAtIndex(i));
										}
					*/
				}
			}
		}
	}
}
