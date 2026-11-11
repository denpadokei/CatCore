using System.Threading.Tasks;
using CatCore.Helpers;

namespace CatCore.Services.Interfaces
{
	internal interface IKittenWebSocketProvider
	{
		bool IsConnected { get; }
		Task Connect(string uri);
		Task Disconnect(string? reason = null);

		/// <summary>
		/// Updates the URL that the provider's automatic reconnect logic uses after a connection loss.
		/// EventSub must not reuse a consumed reconnect URL for later reconnects.
		/// </summary>
		void UpdateAutoReconnectUrl(string uri);

		event AsyncEventHandlerDefinitions.AsyncEventHandler<WebSocketConnection>? ConnectHappened;
		event AsyncEventHandlerDefinitions.AsyncEventHandler? DisconnectHappened;
		event AsyncEventHandlerDefinitions.AsyncEventHandler<WebSocketConnection, string>? MessageReceived;
	}
}