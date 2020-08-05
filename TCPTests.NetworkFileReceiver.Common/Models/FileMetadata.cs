using System;
using System.IO;

namespace TCPTests.NetworkFileReceiver.Common.Models
{
	public class FileMetadata
	{
		public FileMetadata(string fileName, int fileLength)
		{
			if (fileLength <= 0) throw new ArgumentOutOfRangeException(nameof(fileLength));
			FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
			FileLength = fileLength;
		}

		public string FileName { get; set; }
		public int FileLength { get; set; }
	}
}