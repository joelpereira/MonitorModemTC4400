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
			int recordsAdded = 0;
			// get connection quality data; 2nd table
			recordsAdded += await GetDataAsync("cmconnectionstatus.html", "ChannelStatus-", 2, 1);
			// get event log data
			recordsAdded += await GetDataAsync("cmeventlog.html", "EventLog-", 1, 1);
			// get modem info
			recordsAdded += await GetDataAsync("info.html", "Info-", 1, 0);


			Console.WriteLine($"Stored {recordsAdded} records at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
		}

		/// <summary>
		/// Get's data from a table on the modem website/page
		/// </summary>
		/// <param name="pageUrl"></param>
		/// <param name="filenamePrefix"></param>
		/// <param name="tableNum"></param>
		/// <param name="skipTableRows"></param>
		/// <returns>The number of records stored.</returns>
		private async Task<int> GetDataAsync(String pageUrl, String filenamePrefix, int tableNum = 1, int skipTableRows = 0)
		{
			// get date variables and filename
			DateTime genDate = DateTime.Now;
			String strDateDay = genDate.ToString("yyyyMMdd");
			String strDateSec = genDate.ToString("yyyyMMdd-HH.mm.ss");
			String csvFilename = $"csv/{filenamePrefix}{strDateDay}.csv";
			String htmlFilename = $"html/{strDateDay}/{filenamePrefix}{strDateSec}.htm";

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

			// fix pages if needed!
			// Event Log
			pageContents = FixEventLogPage(pageUrl, pageContents);

			// extract data from table
			List<List<String>> data = extractTableData(pageContents, genDate, tableNum, skipTableRows);

			// save data to CSV
			return WriteCSV(csvFilename, data);
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

		private List<List<String>> extractTableData(String pageContents, DateTime genDate, int tableNum = 1, int skipRows = 0)
		{
			List<List<String>> results = new List<List<String>>();
			HtmlDocument htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(pageContents);

			HtmlNodeCollection nodesTR = htmlDoc.DocumentNode.SelectNodes("(//table)[" + tableNum + "]/tr");

			int curRow = 0;
			int curCol = 0;
			// get <TABLE><TR> nodes
			foreach (var nodeTR in nodesTR)
			{
				// skip rows
				if (curRow >= skipRows)
				{
					curCol = 0;
					//Console.WriteLine(nodeTR.InnerText);

					// get inner <TD> nodes
					HtmlNodeCollection nodesTD = nodeTR.SelectNodes("td");

					List<String> data = new List<string>();

					if (curRow - skipRows == 0)
					{
						// add the standard headings
						data.Add("Date");
						data.Add("Time");
						data.Add("Row No");
					}
					else
					{
						String strDateSec = genDate.ToString("yyyyMMdd-HH.mm.ss");

						// add the standard items
						data.Add(genDate.ToString("yyyyMMdd"));
						data.Add(genDate.ToString("HH:mm:ss"));
						data.Add((curRow - skipRows).ToString());
					}

					// go through each node
					foreach (var nodeTD in nodesTD)
					{
						//Console.WriteLine("Innernode #" + numOfCols + ": " + nodeTD.InnerText);
						// store the nodes into the data
						data.Add(nodeTD.InnerText);
						curCol++;
					}



					// add data to results
					results.Add(data);
				}
				curRow++;
			}

			/*			Console.WriteLine($"Number of rows:    {numOfRows}");
						Console.WriteLine($"Number of columns: {numOfCols}");
			*/
			return results;
		}

		private int WriteCSV(String csvFilename, List<List<String>> data)
		{
			int recordsAdded = 0;
			bool skipFirstRow = false;
			// if the file doesn't exist, write all rows
			// if the file exists already, append and skip the first row
			if (File.Exists(csvFilename))
			{
				skipFirstRow = true;
			}

			using (var writer = new StreamWriter(csvFilename, true))
			using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
			{
				// add each row
				int curRow = 0;

				foreach (List<String> row in data)
				{
					// skip first/heading row?
					if (curRow == 0 && skipFirstRow)
					{
						curRow++;
						continue;
					}
					else
					{
						// write all the fields for the current record
						foreach (String field in row)
						{
							csv.WriteField(field);
						}
						// complete the record
						csv.NextRecord();
						recordsAdded++;
					}
					curRow++;
				}

				writer.Flush();
			}
			return recordsAdded;
		}

		private String FixEventLogPage(String pageUrl, String pageContents)
		{
			if (pageUrl.Contains("cmeventlog.html"))
			{
				// minor data cleanup
				pageContents = pageContents.Replace("&nbsp;", " ");

				// FIX
				// split string on each newline
				List<String> splitStr = pageContents.Split("\n").ToList<String>();
				// determine where the table is; between line 23/24 and again at where the </table> isand determine where the table is; between line 23/24 and again at where the </table> is			
				int startIndex = 22;
				for (int i = startIndex; i < splitStr.Count; i++)
				{
					if (splitStr[i].Contains("</TABLE>", StringComparison.InvariantCultureIgnoreCase))
					{
						// stop now
						i = splitStr.Count;
					}
					else
					{
						// just replace all </tr> with </tr><tr>
						splitStr[i] = splitStr[i].Replace("</tr>", "</tr>\n<tr>", StringComparison.InvariantCultureIgnoreCase);
					}
				}

				// stitch the string back together
				pageContents = String.Join("\n", splitStr);

			}
			return pageContents;
		}
	}
}
