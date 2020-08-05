using System.Collections.Generic;

namespace TCPTests.NetworkFileReceiver.Common.Models
{
	public class TransferMetadata
	{
		public int FilesCount { get; set; }

		public byte[] Delimiter = new byte[10];

		public List<FileMetadata> FileMetadata { get; set; }
	}
}
