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

		// local cache
		private List<PageCache> localPageCache = new List<PageCache>();
		private long previousRecordsAdded = 0;
		private long counterTimesExecuted = 0;


		public ModemTC4400(HttpClient client, String username, String password)
		{
			_client = client;
			this.username = username;
			this.password = password;
		}

		public async Task GetAllDataAsync()
		{
			// clear the local cache
			localPageCache.Clear();

			int recordsAdded = 0;
			// get startup procedure data; 1st table
			recordsAdded += await GetDataAsync(ModemTC4400Pages.ConnectionStatusPage, "ConnectionStatus-", 1, 1, true);
			// get downstream connection quality data; 2nd table
			recordsAdded += await GetDataAsync(ModemTC4400Pages.ConnectionStatusPage, "DownstreamChannelStatus-", 2, 1, false);
			// get upstream connection quality data; 3rd table
			recordsAdded += await GetDataAsync(ModemTC4400Pages.ConnectionStatusPage, "UpstreamChannelStatus-", 3, 1, false);
			// get event log data
			recordsAdded += await GetDataAsync(ModemTC4400Pages.EventLogPage, "EventLog-", 1, 1);
			// get modem info
			recordsAdded += await GetDataAsync(ModemTC4400Pages.InfoPage, "Info-", 1, 0);

			// check data for known issues
			CheckDataConsistency(recordsAdded);

			// increase counter(s)
			counterTimesExecuted++;
			previousRecordsAdded = recordsAdded;

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
		private async Task<int> GetDataAsync(String pageUrl, String filenamePrefix, int tableNum = 1, int skipTableRows = 0, bool saveHTMLPage = true)
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
			// Create a new HTML file to write to, but only if we haven't already retrieved this from local cache.
			if (saveHTMLPage)
			{
				// save the html page
				await File.WriteAllTextAsync(htmlFilename, pageContents, Encoding.UTF8);
			}

			// fix pages if needed!
			// Event Log
			pageContents = FixEventLogPage(pageUrl, pageContents);

			// extract data from table
			List<List<String>> data = extractTableData(pageContents, genDate, tableNum, skipTableRows);

			// save data to CSV
			return WriteToCSV(csvFilename, data);
		}

		private async Task<String> GetPageAsync(String pageURL)
		{
			string result = "";

			// check local cache first
			if (PageInCache(pageURL) >= 0)
			{
				return localPageCache[PageInCache(pageURL)].PageContents;
			}
			else
			{
				// setup basic auth
				var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
				_client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
				HttpResponseMessage response = await _client.GetAsync($"{hostname}{pageURL}");
				HttpContent content = response.Content;

				// ... Check Status Code
				//Console.WriteLine("Response StatusCode: " + (int)response.StatusCode);

				// ... Read the string.
				if ((int)response.StatusCode == 200)    // OK
				{
					result = await content.ReadAsStringAsync();

					// add to local page cache
					localPageCache.Add(new PageCache(pageURL, result));
				}
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

					// make sure we found a <td>
					if (nodesTD == null)
					{
						throw new NullReferenceException($"NULL error: Could not find a <td> tag in the table.");
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

		private int WriteToCSV(String csvFilename, List<List<String>> data)
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

		/// <summary>
		/// Fix issues with Event Log page
		/// </summary>
		/// <param name="pageUrl"></param>
		/// <param name="pageContents"></param>
		/// <returns></returns>
		private String FixEventLogPage(String pageUrl, String pageContents)
		{
			if (pageUrl.Contains(ModemTC4400Pages.EventLogPage))
			{
				// FIX 1 - minor data cleanup
				pageContents = pageContents.Replace("&nbsp;", " ");

				// FIX 2 - event log has no values (no </table>
				if (!pageContents.Contains("</table>", StringComparison.InvariantCultureIgnoreCase))
				{
					//event log page is messed up; don't run other fixes
				}
				else
				{
					// FIX 3
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
			}
			return pageContents;
		}

		/// <summary>
		/// Check if page name is in local cache (retrieved already)
		/// </summary>
		/// <param name="pageName"></param>
		/// <returns></returns>
		private int PageInCache(String pageName)
		{
			for (int i = 0; i < localPageCache?.Count; i++)
			{
				if (localPageCache[i].PageName.Contains(pageName))
				{
					return i;
				}
			}
			return -1;
		}

		private void CheckDataConsistency(long recordsAdded)
		{
			// Check if the number of records is drastically different from the previous run time
			if (recordsAdded != previousRecordsAdded && counterTimesExecuted > 1)
			{
				Console.WriteLine($"Difference in number of records added: {(recordsAdded - previousRecordsAdded)}");
			}
		}
	}
}
