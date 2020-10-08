using System;
using System.Collections.Generic;
using System.Text;

namespace ModemMonitor
{
	class Settings
	{
		public String Filename { get; set; }
		public String Hostname { get; set; }

		public Settings(String filename, String hostname)
		{
			Filename = filename;
			Hostname = hostname;

			Load();
		}

		public bool Save()
		{
			return true;
		}

		public void Load()
		{

		}
	}
}
