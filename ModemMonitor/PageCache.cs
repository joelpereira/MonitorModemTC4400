using System;
using System.Collections.Generic;
using System.Text;

namespace ModemMonitor
{
	public class PageCache
	{
		public String PageName { get; private set; }
		public String PageContents { get; set; }
		public DateTime DateTimeCached { get; private set; }

		public PageCache(String pageName, String pageContents, DateTime dateTimeCached)
		{
			PageName = pageName;
			PageContents = pageContents;
			DateTimeCached = dateTimeCached;
		}
	}
}
