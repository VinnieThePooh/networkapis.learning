using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;
using TCPTests.Common.Models;

namespace TcpClient
{
	class Program
	{
		private static Socket _socketClient;

		static void Main(string[] args)
		{
			Console.CancelKeyPress += Console_CancelKeyPress;
			AssemblyLoadContext.Default.Unloading += Default_Unloading;

			var builder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

			var confRoot = builder.Build();

			var hostConfig = HostConfiguration.FromConfiguration(confRoot);

			_socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			Console.WriteLine($"Entry point ThreadId: {Thread.CurrentThread.ManagedThreadId}");

			Console.Write($"Waiting for an accepting connection...");
			_socketClient.Connect(new IPEndPoint(IPAddress.Parse(hostConfig.Address), hostConfig.Port));
			Console.WriteLine($"connected");


			string data = null;
			byte[] bytes;

			while (true)
			{
				Console.Write("Say something: ");
				try
				{
					data = Console.ReadLine();
					bytes = Encoding.UTF8.GetBytes(data);

					using (var sArgs = new SocketAsyncEventArgs())
					{
						sArgs.SetBuffer(bytes);
						_socketClient.SendAsync(sArgs);
					}
				}
				catch (ArgumentNullException e)
				{
					Console.WriteLine("Ctrl + C pressed.");
					return;
				}
			}
		}

		private static void Default_Unloading(AssemblyLoadContext obj) => DisconnectListener();
		private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e) => DisconnectListener();

		private static void DisconnectListener()
		{
			lock (_socketClient)
			{
				_socketClient.Shutdown(SocketShutdown.Both);
				_socketClient.Close();
			}
		}
	}
}
