using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TCPTests.Common.Models;

namespace TCP_Tests
{
	internal class Program
	{
		private static Socket _socketListener;

		private static void Main(string[] args)
		{
			Console.CancelKeyPress += Console_CancelKeyPress;

			var builder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", true, true);

			var confRoot = builder.Build();

			var hostConf = HostConfiguration.FromConfiguration(confRoot);


			_socketListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			_socketListener.Bind(new IPEndPoint(IPAddress.Parse(hostConf.Address), hostConf.Port));
			_socketListener.Listen(10);
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine(
				$"[Thread-{Thread.CurrentThread.ManagedThreadId}][System]: Entry point ThreadId: {Thread.CurrentThread.ManagedThreadId}");
			Console.WriteLine(
				$"[Thread-{Thread.CurrentThread.ManagedThreadId}][System]: Listening interface: {hostConf.Address}:{hostConf.Port}...");
			Console.ResetColor();

			try
			{
				while (true)
				{
					var clientSocket = _socketListener.Accept();
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
				_socketListener.Shutdown(SocketShutdown.Both);
			}
		}

		private static void HandleNewConnection(Socket clientSocket)
		{
			Task.Run(() =>
			{
				var epoint = (IPEndPoint)clientSocket.RemoteEndPoint;
				clientSocket.ReceiveBufferSize = 1024;

				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine(
					$"[Thread-{Thread.CurrentThread.ManagedThreadId}][System]: New client connected: {epoint.Address}:{epoint.Port}");
				Console.ResetColor();

				while (clientSocket.Connected)
				{
					HandleIncomingMessage(clientSocket);
				}

				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine(
					$"[Thread-{Thread.CurrentThread.ManagedThreadId}][System]: Client disconnected: {epoint.Address}:{epoint.Port}");
				Console.ResetColor();
			});

		}

		private static void HandleIncomingMessage(Socket clientSocket)
		{
			var buffer = new byte[clientSocket.ReceiveBufferSize];
			
			var bytesRead = 0;
			int counter;
			do
			{
				counter = clientSocket.Receive(buffer, SocketFlags.None);
				bytesRead += counter;
			} 
			while (clientSocket.Available > 0);

			if (counter == 0)
			{
				clientSocket.Shutdown(SocketShutdown.Send);
				clientSocket.Close();
				return;
			}

			var list = new List<byte>(buffer.Take(bytesRead));

			var message = Encoding.UTF8.GetString(list.ToArray());

			Console.WriteLine($"[Thread-{Thread.CurrentThread.ManagedThreadId}][Message]: {message}");

			list.Clear();
		}

		private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
		{
			_socketListener.Shutdown(SocketShutdown.Both);
			_socketListener.Close(1000);
		}
	}
}