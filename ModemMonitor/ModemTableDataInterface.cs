using HtmlAgilityPack;
using System;

namespace ModemMonitor
{
	interface ModemTableDataInterface
	{
		void ParseData(HtmlNodeCollection nodes, int rowNum);
		int Count { get; }
		String GetValueAtIndex(int index);
	}
}
