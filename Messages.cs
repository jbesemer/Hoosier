using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace Hoosier
{
	public class Message
	{
		public enum Codes {
			Unk = 0,
			Howdy = 2,
			Hoosier = 1,
			Goodbye = 3,
		}

		public Codes Code;		// unique code per message type
		public string ServerName;

		public Message( Codes code, string serverName )
		{
			Code = code;
			ServerName = serverName;
		}

		public Message( byte[] data )
		{
			Code = (Codes)BitConverter.ToInt32( data, 0 );
			ServerName = Encoding.ASCII.GetString( data, 4, data.Length - 4 );
		}

		public byte[] GetBytes()
		{
			byte[] name = Encoding.ASCII.GetBytes( ServerName );
			byte[] code = BitConverter.GetBytes( (int)Code );
			byte[] data = new byte[ code.Length + name.Length ];

			Buffer.BlockCopy( code, 0, data, 0, code.Length );
			Buffer.BlockCopy( name, 0, data, code.Length, name.Length );

			return data;
		}

		public static Message Decode( byte[] data )
		{
			Codes code = (Codes)BitConverter.ToInt32( data, 0 );
			string name = Encoding.ASCII.GetString( data, 4, data.Length - 4 );

			switch( code )
			{
			case Codes.Howdy: return new HowdyMessage( name );
			case Codes.Hoosier: return new HoosierMessage( name );
			case Codes.Goodbye: return new GoodbyeMessage( name );
			default: return new Message( Codes.Unk, name );
			}
		}

		public string ToString( IPEndPoint sender )
		{
			return string.Format(
				"{0} {1} from {2}.{3}",
				Code,
				ServerName,
				sender.Address.ToString(),
				sender.Port );
		}

		public override string ToString()
		{
			return string.Format(
				"{0} {1} from ?",
				Code,
				ServerName );
		}
	}

	public class HowdyMessage : Message
	{
		public HowdyMessage( string serverName )
			: base( Codes.Howdy, serverName )
		{
		}
	}

	public class HoosierMessage : Message
	{
		public HoosierMessage( string serverName )
			: base( Codes.Hoosier, serverName )
		{
		}
	}

	public class GoodbyeMessage : Message
	{
		public GoodbyeMessage( string serverName )
			: base( Codes.Goodbye, serverName )
		{
		}
	}
}