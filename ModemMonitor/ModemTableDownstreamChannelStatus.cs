using HtmlAgilityPack;
using System;
using System.Text;


namespace ModemMonitor
{
	class ModemTableDownstreamChannelStatus : ModemTableDataInterface
	{
		public int RowNum { get; private set; }
		public String ChannelIndex { get; set; }
		public String ChannelID { get; set; }
		public String LockStatus { get; set; }
		public String ChannelType { get; set; }
		public String BondingStatus { get; set; }
		public String CenterFrequency { get; set; }
		public String ChannelWidth { get; set; }
		public String SNR_MER_ThresholdValue { get; set; }
		public String ReceivedLevel { get; set; }
		public String ModulationProfileID { get; set; }
		public String UnerroredCodewords { get; set; }
		public String CorrectedCodewords { get; set; }
		public String UncorrectableCodewords { get; set; }

		public int Count
		{
			get { return 14; }
		}

		public string GetValueAtIndex(int index)
		{
			switch (index)
			{
				case 0:
					return this.RowNum.ToString();
				case 1:
					return this.ChannelIndex;
				case 2:
					return this.ChannelID;
				case 3:
					return this.LockStatus;
				case 4:
					return this.ChannelType;
				case 5:
					return this.BondingStatus;
				case 6:
					return this.CenterFrequency;
				case 7:
					return this.ChannelWidth;
				case 8:
					return this.SNR_MER_ThresholdValue;
				case 9:
					return this.ReceivedLevel;
				case 10:
					return this.ModulationProfileID;
				case 11:
					return this.UnerroredCodewords;
				case 12:
					return this.CorrectedCodewords;
				case 13:
					return this.UncorrectableCodewords;
				case 14:
					return ChannelIndex;
				default:
					throw new NullReferenceException();
			}
		}

		public void ParseData(HtmlNodeCollection nodes, int rowNum)
		{
			RowNum = rowNum;
			ChannelIndex = nodes[0].InnerText;
			ChannelID = nodes[1].InnerText;
			LockStatus = nodes[2].InnerText;
			ChannelType = nodes[3].InnerText;
			BondingStatus = nodes[4].InnerText;
			CenterFrequency = nodes[5].InnerText;
			ChannelWidth = nodes[6].InnerText;
			SNR_MER_ThresholdValue = nodes[7].InnerText;
			ReceivedLevel = nodes[8].InnerText;
			ModulationProfileID = nodes[9].InnerText;
			UnerroredCodewords = nodes[10].InnerText;
			CorrectedCodewords = nodes[11].InnerText;
			UncorrectableCodewords = nodes[12].InnerText;
		}
	}
}
