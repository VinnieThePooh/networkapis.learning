using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace TCPTests.Common.Models
{
	public static class Configuration
	{
		public static IConfiguration FromJsonFile(string filePath)
		{
			if (string.IsNullOrEmpty(filePath)) 
				throw new ArgumentNullException(nameof(filePath));

			if (File.Exists(filePath))
				 throw new FileNotFoundException(nameof(filePath));

			var dir = Path.GetDirectoryName(filePath);
			var fname = Path.GetFileName(filePath);

			var builder = new ConfigurationBuilder()
				.SetBasePath(dir)
				.AddJsonFile(fname, optional: true, reloadOnChange: true);

			return builder.Build();
		}
	}
}
