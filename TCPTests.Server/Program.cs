using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TCPTests.Common.Models;

namespace TCP_Tests
{
	class Program
	{
		private static ConcurrentDictionary<Socket, ManualResetEvent> ResetEvents { get;  } = new ConcurrentDictionary<Socket, ManualResetEvent>();

		static void Main(string[] args)
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

			var confRoot = builder.Build();

			var hostConf = HostConfiguration.FromConfiguration(confRoot);


			var socketListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socketListener.Bind(new IPEndPoint(IPAddress.Parse(hostConf.Address),  hostConf.Port));
			socketListener.Listen(10);
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine($"[Thread-{Thread.CurrentThread.ManagedThreadId}][System]: Entry point ThreadId: {Thread.CurrentThread.ManagedThreadId}");
			Console.WriteLine($"[Thread-{Thread.CurrentThread.ManagedThreadId}][System]: Listening interface: {hostConf.Address}:{hostConf.Port}...");
			Console.ResetColor();

			try
			{
				while (true)
				{
					var clientSocket = socketListener.Accept();
					HandleNewConnection(clientSocket);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}
			finally
			{
				Console.WriteLine("Server ends");
				socketListener.Shutdown(SocketShutdown.Both);
			}
		}


		static Task HandleNewConnection(Socket client)
		{
			return Task.Run(() =>
			{
				var resetEvent = ResetEvents.GetOrAdd(client, (s) => new ManualResetEvent(false));

				var epoint = (IPEndPoint)client.RemoteEndPoint;
				client.ReceiveBufferSize = 1024;

				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine($"[Thread-{Thread.CurrentThread.ManagedThreadId}][System]: New client connected: {epoint.Address}:{epoint.Port}");
				Console.ResetColor();

				while (client.Connected)
				{
					var args = new SocketAsyncEventArgs();
					args.Completed += HandleIncomingMessage;
					client.ReceiveAsync(args);
					resetEvent.WaitOne();
				}

				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine($"[Thread-{Thread.CurrentThread.ManagedThreadId}][System]: Client disconnected: {epoint.Address}:{epoint.Port}");
				Console.ResetColor();
			});
		}

		static void HandleIncomingMessage(object sender, SocketAsyncEventArgs args)
		{
			var socket = (Socket) sender;
			var buffer = new byte[socket.ReceiveBufferSize];

			int bytesRead = 0;
			int counter;
			do
			{
				counter= socket.Receive(buffer, SocketFlags.None);
				bytesRead += counter;
			} 
			while (socket.Available > 0 && counter > 0);

			var list = new List<byte>(buffer.Take(bytesRead));

			var message = Encoding.UTF8.GetString(list.ToArray());

			Console.WriteLine($"[Thread-{Thread.CurrentThread.ManagedThreadId}][Message]: {message}");

			list.Clear();
			args.Completed -= HandleIncomingMessage;
			args.Dispose();

			ResetEvents[socket].Set();
		}
	}
}
