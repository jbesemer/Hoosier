using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Hoosier
{
	public class Provider : Common
	{
		#region // vars & Constructor

		byte[] HowdyMessageBytes;
		byte[] GoodbyeMessageBytes;

		public Provider( string name )
			: base( HooserProtocolPort, HooserProtocolPort, name )
		{
			Debug.WriteLine(
				"Provider "
				+ name
				+ " listening on port "
				+ ListenPort.ToString() );

			HowdyMessageBytes = new HowdyMessage( Name ).GetBytes();
			GoodbyeMessageBytes= new GoodbyeMessage( Name ).GetBytes();

			ReceiveMessage += new ReceiveMessageDelegate( ReceiveEventHandler );
		}

		public override void SendStartupMessage()
		{
			BroadcastHowdyMessage();
		}

		public override void Shutdown()
		{
			BroadcastGoodbyeMessage();
			base.Shutdown();
		}

		#endregion

		#region // User actions

		public void BroadcastHowdyMessage()
		{
			Debug.WriteLine( "Broadcasting Howdy..." );

			Broadcast( HowdyMessageBytes );
		}

		public void SendHowdyMessage( IPEndPoint sender )
		{
			sender.Port = ListenPort;
			Debug.WriteLine( 
				"Sending Howdy To " 
				+ sender.ToString() 
				+ " ..." );

			SendTo( HowdyMessageBytes, sender );
		}

		public void BroadcastGoodbyeMessage()
		{
			Debug.WriteLine( "Broadcasting Goodbye..." );

			Broadcast( GoodbyeMessageBytes );
		}
		
		#endregion
		
		#region // Receive Event Handler

		protected new void ReceiveEventHandler( Message msg, IPEndPoint sender )
		{
			if( msg.Code == Message.Codes.Hoosier )
			{
				SendHowdyMessage( sender );
			}
		}

		#endregion
	}
}
