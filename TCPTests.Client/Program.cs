using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;
using TCPTests.Common.Models;

namespace TcpClient
{
	class Program
	{
		static void Main(string[] args)
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

			var confRoot = builder.Build();

			var hostConfig = HostConfiguration.FromConfiguration(confRoot);

			var socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			Console.WriteLine($"Entry point ThreadId: {Thread.CurrentThread.ManagedThreadId}");

			Console.Write($"Waiting for an accepting connection...");
			socketClient.Connect(new IPEndPoint(IPAddress.Parse(hostConfig.Address), hostConfig.Port));
			Console.WriteLine($"connected");


			while (true)
			{
				Console.Write("Say something: ");
				var data = Console.ReadLine();
				var bytes = Encoding.UTF8.GetBytes(data);
				var sArgs = new SocketAsyncEventArgs();
				sArgs.SetBuffer(bytes);
				socketClient.SendAsync(sArgs);
				var k = 0;
			}
		}


		static void HandleSendingResult(object sender, SocketAsyncEventArgs args)
		{
			Console.WriteLine($"Sent {args.Buffer.Length} bytes");
		}
	}
}
