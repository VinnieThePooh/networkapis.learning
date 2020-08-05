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
using NLog.LayoutRenderers;
using TCPTests.Common.Models;

namespace TCP_Tests
{
	internal class Program
	{
		private static ConcurrentDictionary<Socket, ManualResetEvent> ResetEvents { get; } =
			new ConcurrentDictionary<Socket, ManualResetEvent>();

		private static void Main(string[] args)
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", true, true);

			var confRoot = builder.Build();

			var hostConf = HostConfiguration.FromConfiguration(confRoot);


			var socketListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socketListener.Bind(new IPEndPoint(IPAddress.Parse(hostConf.Address), hostConf.Port));
			socketListener.Listen(10);
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
					var acceptArgs = new SocketAsyncEventArgs();
					acceptArgs.Completed += HandleNewConnection;

					var isAsync = socketListener.AcceptAsync(acceptArgs);
					if (!isAsync)
						HandleNewConnection(socketListener, acceptArgs);
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


		private static void HandleNewConnection(object sender, SocketAsyncEventArgs args)
		{
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine(
				$"[Thread-{Thread.CurrentThread.ManagedThreadId}][System]: Completed event fired");
			Console.ResetColor();

			var clientSocket = args.AcceptSocket;


			Console.WriteLine(
				$"[Thread-{Thread.CurrentThread.ManagedThreadId}][System]: AcceptSocket connected: {clientSocket.Connected}");

			var resetEvent = ResetEvents.GetOrAdd(clientSocket, (s) => new ManualResetEvent(false));

			var epoint = (IPEndPoint) clientSocket.RemoteEndPoint;
			clientSocket.ReceiveBufferSize = 1024;

			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine(
				$"[Thread-{Thread.CurrentThread.ManagedThreadId}][System]: New client connected: {epoint.Address}:{epoint.Port}");
			Console.ResetColor();

			args.Completed -= HandleNewConnection;
			args.Dispose();
			args = null;

			while (clientSocket.Connected)
			{
				var receiveArgs = new SocketAsyncEventArgs();
				receiveArgs.Completed += HandleIncomingMessage;
				var isAsync = clientSocket.ReceiveAsync(receiveArgs);
				if (!isAsync)
					HandleIncomingMessage(clientSocket, receiveArgs);
				else
					resetEvent.WaitOne();
			}

			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine(
				$"[Thread-{Thread.CurrentThread.ManagedThreadId}][System]: Client disconnected: {epoint.Address}:{epoint.Port}");
			Console.ResetColor();
		}

		private static void HandleIncomingMessage(object sender, SocketAsyncEventArgs args)
		{
			var socket = (Socket) sender;
			var buffer = new byte[socket.ReceiveBufferSize];

			var bytesRead = 0;
			int counter;
			do
			{
				counter = socket.Receive(buffer, SocketFlags.None);
				bytesRead += counter;
			} while (socket.Available > 0 && counter > 0);

			var list = new List<byte>(buffer.Take(bytesRead));

			var message = Encoding.UTF8.GetString(list.ToArray());

			Console.WriteLine($"[Thread-{Thread.CurrentThread.ManagedThreadId}][Message]: {message}");

			list.Clear();
			args.Completed -= HandleIncomingMessage;
			args.Dispose();
			args = null;

			ResetEvents[socket].Set();
		}
	}
}