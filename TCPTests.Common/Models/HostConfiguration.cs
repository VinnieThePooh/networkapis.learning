using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace TCPTests.Common.Models
{
	public class HostConfiguration
	{
		public static string ConfigurationKey = "HostConfiguration";

		public string Address { get; set; }

		public ushort Port { get; set; }

		public static HostConfiguration FromConfiguration(IConfiguration configuration)
		{
			var con = new HostConfiguration();
			configuration.GetSection(ConfigurationKey).Bind(con);
			return con;
		}
	}
}
