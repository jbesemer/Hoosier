// #define TRACE_RECEIVE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace Hoosier
{
	public delegate void ReceiveMessageDelegate( Message message,IPEndPoint sender );

	public abstract class Common : IDisposable
	{
		#region Properties

		public const int HooserProtocolPort = 13003;

		// List of all foreign hosts encountered thus far.
		// Shared by both Monitor and Provider, with the assumption that
		// users will normally only implement one or the other half of the
		// protocol.
		public static Dictionary<IPAddress,string> KnownHosts
			= new Dictionary<IPAddress, string>();

		public int BroadcastPort { get; protected set; }
		public int ListenPort { get; protected set; }
		public string Name { get; protected set; }

		public bool IgnoreLoopback { get; set; }

		public ReceiveMessageDelegate ReceiveMessage;

		protected IPEndPoint BroadcastEndPoint;
		protected Socket Socket;

		protected IPAddress Address;

		protected UdpClient Listener;

		protected Thread MonitorThread;
		bool Done;

		#endregion

		#region Constructors and IDisposable

		public Common( int broadcastPort, int listenPort )
			: this(  broadcastPort, listenPort, "Unk" )
		{
		}

		public Common( int broadcastPort, int listenPort, string name )
		{
			BroadcastPort = broadcastPort;
			ListenPort = listenPort;
			Name = name;
			IgnoreLoopback = true;

			BroadcastEndPoint
				= new IPEndPoint(
					IPAddress.Broadcast,
					BroadcastPort );

			ReceiveMessage += new ReceiveMessageDelegate( ReceiveEventHandler );
		}

		public void Dispose()
		{
			Shutdown();
		}

		#endregion

		#region Startup & Shutdown

		public void StartServer()
		{
			Debug.WriteLine( "Hoosier.MonitorThreadBody StartServer" );

			Socket
				= new Socket(
					AddressFamily.InterNetwork,
					SocketType.Dgram,
					ProtocolType.Udp );
#if MASTER_CONTROLLER
			Socket.SetSocketOption(
				SocketOptionLevel.Socket,
				SocketOptionName.Broadcast,
				true );
			Socket.SetSocketOption(
				SocketOptionLevel.Socket,
				SocketOptionName.ReuseAddress,
				true );
			Socket.SetSocketOption(
				SocketOptionLevel.Socket,
				SocketOptionName.ExclusiveAddressUse,
				false );
#else
			Socket.EnableBroadcast = true;
			Socket.MulticastLoopback = true;
			Socket.ExclusiveAddressUse = false;
#endif
			string hostname = Dns.GetHostName();
			IPHostEntry iphe = Dns.GetHostEntry( hostname );
			Address = GetHostAddress();

			MonitorThread = new Thread( MonitorThreadBody );
			MonitorThread.IsBackground = true;
			MonitorThread.Name = Name + " Service Thread";
			MonitorThread.Start();
		}

		public virtual void Shutdown()
		{
			Debug.WriteLine( "Hoosier.MonitorThreadBody Shutdown" );
			
			Done = true;
			// MonitorThread.Abort();

			Socket.Shutdown( SocketShutdown.Both );
			Socket.Close();

			if( Listener != null )
			{
				Listener.Close();
				Listener = null;
			}

			Debug.WriteLine(
				"Hoosier.MonitorThreadBody Join: "
				+ MonitorThread.Join( 1000 ).ToString() );
		}

		#endregion

		#region Send and Receive

		public void SendTo( byte[] buffer, IPEndPoint endpoint )
		{
			Socket.SendTo( buffer, endpoint );
		}

		public void Broadcast( byte[] buffer )
		{
			Socket.SendTo( buffer, BroadcastEndPoint );
		}

		public abstract void SendStartupMessage();

		#endregion

		#region Thread Body

		protected void MonitorThreadBody()
		{
			// listen for more requests

			Debug.WriteLine( "Hoosier.MonitorThreadBody listening on " 
				+ Address.ToString() 
				+ ":" 
				+ ListenPort.ToString() );

			try
			{
				Listener = new UdpClient( ListenPort );

#if !MASTER_CONTROLLER
				Listener.EnableBroadcast = true;
				// listener.ExclusiveAddressUse = false;
				Listener.MulticastLoopback = false;
#endif
				Debug.WriteLine( "Hoosier.MonitorThreadBody sending StartupMessage" );

				SendStartupMessage();

				while( !Done )
				{
					IPEndPoint groupEP = new IPEndPoint( IPAddress.Any, ListenPort );

					Debug.WriteLine( "Hoosier.MonitorThreadBody Listener.Receive..." );
					byte[] bytes = Listener.Receive( ref groupEP );

					Debug.WriteLine( "Hoosier.MonitorThreadBody received " + bytes.Length.ToString() + " bytes" );

					if( IgnoreLoopback && Address.Equals( groupEP.Address ) )
					{
						//Debug.WriteLine( "\t\tIgnoring loopback" );
						continue;
					}

					if( ReceiveMessage != null )
					{
						Message msg = Message.Decode( bytes );
						ReceiveMessage( msg, groupEP );
					}
				}
			}
			catch( Exception e )
			{
				Debug.WriteLine( "Hoosier.MonitorThreadBody Exception: " + e.Message );
			}

			Debug.WriteLine( "Hoosier.MonitorThreadBody Exits " );
		}

		#endregion

		#region // Receive Event Handler

		protected void ReceiveEventHandler( Message msg, IPEndPoint sender )
		{
#if TRACE_RECEIVE
			Debug.WriteLine(
				string.Format(
					"\tReceived from {0}:{1}... {2} {3}",
					sender.Address.ToString(),
					sender.Port.ToString(),
					msg.Code,
					msg.ServerName ) );
#endif
			KnownHosts[ sender.Address ] = msg.ServerName;
		}

		#endregion

		#region GetHostAddress

		public static IPAddress GetHostAddress()	// IPv4 not v6
		{
			try
			{
				string hostname = Dns.GetHostName();
				IPHostEntry entry = Dns.GetHostEntry( hostname );
				IPAddress[] addrs = entry.AddressList;

				foreach( IPAddress addr in entry.AddressList )
				{
					if( addr.AddressFamily == AddressFamily.InterNetwork )
						return addr;
				}
			}
			catch( Exception e )
			{
				Debug.WriteLine( "GetHostAddress exception: " + e.Message );
			}

			return null;
		}

		#endregion
	}
}
