﻿using System.Collections.ObjectModel;
using CatCore.Models.Shared;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.IRC
{
	public class TwitchMessage : IChatMessage
	{
		/// <inheritdoc cref="IChatMessage.Id"/>
		[PublicAPI]
		public string Id { get; internal set;  }

		/// <inheritdoc cref="IChatMessage.IsSystemMessage"/>
		[PublicAPI]
		public bool IsSystemMessage { get; internal set;  }

		/// <inheritdoc cref="IChatMessage.IsActionMessage"/>
		[PublicAPI]
		public bool IsActionMessage { get; internal set; }

		/// <inheritdoc cref="IChatMessage.IsHighlighted"/>
		[PublicAPI]
		public bool IsHighlighted { get; internal set;  }

		/// <inheritdoc cref="IChatMessage.IsPing"/>
		[PublicAPI]
		public bool IsPing { get; internal set; }

		/// <inheritdoc cref="IChatMessage.Message"/>
		[PublicAPI]
		public string Message { get; internal set; }

		/// <inheritdoc cref="IChatMessage.Sender"/>
		[PublicAPI]
		public IChatUser Sender { get; internal set; }

		/// <inheritdoc cref="IChatMessage.Channel"/>
		[PublicAPI]
		public IChatChannel Channel { get; internal set; }

		/// <inheritdoc cref="IChatMessage.Metadata"/>
		[PublicAPI]
		public ReadOnlyDictionary<string, string> Metadata { get; internal set; }

		/// <summary>
		/// The IRC message type for this TwitchMessage
		/// </summary>
		[PublicAPI]
		public string Type { get; internal set; }

		/// <summary>
		/// The number of bits in this message, if any.
		/// </summary>
		[PublicAPI]
		public int Bits { get; internal set; }

		public TwitchMessage(string id, bool isSystemMessage, bool isActionMessage, bool isHighlighted, bool isPing, string message, IChatUser sender, IChatChannel channel,
			ReadOnlyDictionary<string, string> metadata, string type, int bits)
		{
			Id = id;
			IsSystemMessage = isSystemMessage;
			IsActionMessage = isActionMessage;
			IsHighlighted = isHighlighted;
			IsPing = isPing;
			Message = message;
			Sender = sender;
			Channel = channel;
			Metadata = metadata;
			Type = type;
			Bits = bits;
		}
	}
}