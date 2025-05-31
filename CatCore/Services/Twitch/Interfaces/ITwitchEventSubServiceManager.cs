using System;
using System.Threading.Tasks;
using CatCore.Models.Twitch.EventSub.Responses;
using CatCore.Models.Twitch.EventSub.Responses.VideoPlayback;
using CatCore.Models.Twitch.PubSub.Responses;
using CatCore.Models.Twitch.PubSub.Responses.ChannelPointsChannelV1;
using CatCore.Models.Twitch.PubSub.Responses.VideoPlayback;

namespace CatCore.Services.Twitch.Interfaces
{
	public interface ITwitchEventSubServiceManager
	{
		internal Task Start();
		internal Task Stop();

		/// <summary>
		 /// チャンネルが配信を開始したときに発火します。
		 /// コールバックの第1引数はイベントが発生したチャンネルIDです。
		 /// 第2引数は配信開始イベントに関する追加データです。
		 /// </summary>
		event Action<string, StreamOnlineEvent> OnStreamUp;

		/// <summary>
		 /// チャンネルが配信を終了したときに発火します。
		 /// コールバックの第1引数はイベントが発生したチャンネルIDです。
		 /// 第2引数は配信終了イベントに関する追加データです。
		 /// </summary>
		event Action<string, StreamOfflineEvent> OnStreamDown;

		/// <summary>
		 /// チャンネルが新たにフォローされたときに発火します。
		 /// コールバックの第1引数はイベントが発生したチャンネルIDです。
		 /// 第2引数は新しいフォロワーに関する追加データです。
		 /// </summary>
		event Action<string, ChannelFollowEvent> OnFollow;

		/// <summary>
		 /// 特定のチャンネルで視聴者がリワードを獲得したときに発火します。
		 /// コールバックの第1引数はイベントが発生したチャンネルIDです。
		 /// 第2引数はリワードと獲得者に関する追加データです。
		 /// </summary>
		event Action<string, ChannelPointsRedeemEvent> OnChannelPointsRedeem;

		/// <summary>
		 /// チャンネルで予想（Prediction）が開始されたときに発火します。
		 /// コールバックの第1引数はイベントが発生したチャンネルIDです。
		 /// 第2引数は予想開始イベントに関する追加データです。
		 /// </summary>
		event Action<string, ChannelPredictionBeginEvent> OnPredictionBegin;
	}
}