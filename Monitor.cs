// #define TRACE_BROADCAST
#define ENABLE_RECEIVE_HANDLER

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
	public class Monitor : Common
	{
		#region // Vars & Constructor

		byte[] HoosierMessageBytes;

		public Monitor( string name )
			: base( HooserProtocolPort, HooserProtocolPort, name )
		{
			Debug.WriteLine(
				"ServiceMonitor listening on port "
				+ ListenPort.ToString() );

			HoosierMessageBytes = new HoosierMessage( Name ).GetBytes();

#if ENABLE_RECEIVE_HANDLER
			ReceiveMessage += new ReceiveMessageDelegate( ReceiveEventHandler );
#endif
		}

		public override void SendStartupMessage()
		{
			BroadcastHoosierMessage();
		}

		#endregion

		#region // User actions

		public void BroadcastHoosierMessage()
		{
#if TRACE_BROADCAST
			Debug.WriteLine( "\tBroadcasting Hoosier..." );
#endif
			Broadcast( HoosierMessageBytes );
		}

		#endregion

		#region // Receive Event Handler

#if ENABLE_RECEIVE_HANDLER
		// all requirements handled by Common or by end user
		protected new void ReceiveEventHandler( Message msg, IPEndPoint sender )
		{
		}
#endif
		#endregion
	}
}
