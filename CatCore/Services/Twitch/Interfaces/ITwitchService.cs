using CatCore.Models.Twitch;
using CatCore.Models.Twitch.IRC;
using CatCore.Services.Interfaces;
using JetBrains.Annotations;

namespace CatCore.Services.Twitch.Interfaces
{
	public interface ITwitchService : IPlatformService<ITwitchService, TwitchChannel, TwitchMessage>
	{
		/// <summary>
		 /// PubSubサービスマネージャーを返します。さまざまなイベントの購読が可能です。
		 /// </summary>
		 /// <returns>PubSubサービスマネージャー</returns>
		[PublicAPI]
		ITwitchPubSubServiceManager GetPubSubService();

		/// <summary>
		 /// Helix APIサービスを返します。Twitch Helix APIとのやり取りが可能です。
		 /// </summary>
		 /// <returns></returns>
		[PublicAPI]
		ITwitchHelixApiService GetHelixApiService();

		/// <summary>
		 /// RoomStateトラッカーサービスを返します。現在購読中のチャンネルの状態を管理します。
		 /// </summary>
		 /// <returns>RoomStateトラッカーサービス</returns>
		[PublicAPI]
		ITwitchRoomStateTrackerService GetRoomStateTrackerService();

		/// <summary>
		 /// UserStateトラッカーサービスを返します。ユーザーのグローバル・チャンネル別状態を管理します。
		 /// </summary>
		 /// <returns>UserStateトラッカーサービス</returns>
		[PublicAPI]
		ITwitchUserStateTrackerService GetUserStateTrackerService();

		/// <summary>
		 /// チャンネル管理サービスを返します。Webポータル経由で登録された全チャンネルを管理します。
		 /// </summary>
		 /// <returns>チャンネル管理サービス</returns>
		[PublicAPI]
		ITwitchChannelManagementService GetChannelManagementService();

		/// <summary>
		 /// EventSubサービスマネージャーを返します。EventSubイベントの購読が可能です。
		 /// </summary>
		 /// <returns>EventSubサービスマネージャー</returns>
		[PublicAPI]
		ITwitchEventSubServiceManager GetEventSubServiceManager();
	}
}