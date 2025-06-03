using System;
using System.Collections.Concurrent;
using CatCore.Models.Twitch.EventSub;
using CatCore.Models.Twitch.EventSub.Responses;
using CatCore.Models.Twitch.EventSub.Responses.VideoPlayback;

namespace CatCore.Services.Twitch
{
	internal sealed partial class TwitchEventSubServiceManager
	{
		// 配信開始
		private readonly ConcurrentDictionary<Action<string, StreamOnlineEvent>, bool> _streamUpCallbackRegistrations = new();
		private void NotifyOnStreamUp(string channelId, StreamOnlineEvent data)
		{
			foreach (var action in _streamUpCallbackRegistrations.Keys)
			{
				action(channelId, data);
			}
		}
		public event Action<string, StreamOnlineEvent> OnStreamUp
		{
			add
			{
				if (_streamUpCallbackRegistrations.TryAdd(value, false))
				{
					RegisterTopicWhenNeeded(EventSubTypes.STREAM_ONLINE);
				}
			}
			remove
			{
				_streamUpCallbackRegistrations.TryRemove(value, out _);
			}
		}

		// 配信終了
		private readonly ConcurrentDictionary<Action<string, StreamOfflineEvent>, bool> _streamDownCallbackRegistrations = new();
		private void NotifyOnStreamDown(string channelId, StreamOfflineEvent data)
		{
			foreach (var action in _streamDownCallbackRegistrations.Keys)
			{
				action(channelId, data);
			}
		}
		public event Action<string, StreamOfflineEvent> OnStreamDown
		{
			add
			{
				if (_streamDownCallbackRegistrations.TryAdd(value, false))
				{
					RegisterTopicWhenNeeded(EventSubTypes.STREAM_OFFLINE);
				}
			}
			remove
			{
				_streamDownCallbackRegistrations.TryRemove(value, out _);
			}
		}

		// フォロー
		private readonly ConcurrentDictionary<Action<string, ChannelFollowEvent>, bool> _followCallbackRegistrations = new();
		private void NotifyOnFollow(string channelId, ChannelFollowEvent data)
		{
			foreach (var action in _followCallbackRegistrations.Keys)
			{
				action(channelId, data);
			}
		}
		public event Action<string, ChannelFollowEvent> OnFollow
		{
			add
			{
				if (_followCallbackRegistrations.TryAdd(value, false))
				{
					RegisterTopicWhenNeeded(EventSubTypes.CHANNEL_FOLLOW);
				}
			}
			remove
			{
				_followCallbackRegistrations.TryRemove(value, out _);
			}
		}

		// チャンネルポイント
		private readonly ConcurrentDictionary<Action<string, ChannelPointsRedeemEvent>, bool> _channelPointsRedeemCallbackRegistrations = new();
		private void NotifyOnChannelPointsRedeem(string channelId, ChannelPointsRedeemEvent data)
		{
			foreach (var action in _channelPointsRedeemCallbackRegistrations.Keys)
			{
				action(channelId, data);
			}
		}
		public event Action<string, ChannelPointsRedeemEvent> OnChannelPointsRedeem
		{
			add
			{
				if (_channelPointsRedeemCallbackRegistrations.TryAdd(value, false))
				{
					RegisterTopicWhenNeeded(EventSubTypes.CHANNEL_POINTS_REDEEM);
				}
			}
			remove
			{
				_channelPointsRedeemCallbackRegistrations.TryRemove(value, out _);
			}
		}

		// 予想開始
		private readonly ConcurrentDictionary<Action<string, ChannelPredictionBeginEvent>, bool> _predictionBeginCallbackRegistrations = new();
		private void NotifyOnPredictionBegin(string channelId, ChannelPredictionBeginEvent data)
		{
			foreach (var action in _predictionBeginCallbackRegistrations.Keys)
			{
				action(channelId, data);
			}
		}
		public event Action<string, ChannelPredictionBeginEvent> OnPredictionBegin
		{
			add
			{
				if (_predictionBeginCallbackRegistrations.TryAdd(value, false))
				{
					RegisterTopicWhenNeeded(EventSubTypes.CHANNEL_PREDICTION_BEGIN);
				}
			}
			remove
			{
				_predictionBeginCallbackRegistrations.TryRemove(value, out _);
			}
		}

		// チャットメッセージ送信
		private readonly ConcurrentDictionary<Action<string, ChannelChatMessageEvent>, bool> _chatMessageCallbackRegistrations = new();
		private void NotifyOnChatMessage(string channelId, ChannelChatMessageEvent data)
		{
			foreach (var action in _chatMessageCallbackRegistrations.Keys)
			{
				action(channelId, data);
			}
		}
		public event Action<string, ChannelChatMessageEvent> OnChatMessage
		{
			add
			{
				if (_chatMessageCallbackRegistrations.TryAdd(value, false))
				{
					RegisterTopicWhenNeeded(EventSubTypes.CHANNEL_CHAT_MESSAGE);
				}
			}
			remove
			{
				_chatMessageCallbackRegistrations.TryRemove(value, out _);
			}
		}

		// チャットメッセージ削除
		private readonly ConcurrentDictionary<Action<string, ChannelChatMessageDeleteEvent>, bool> _chatMessageDeleteCallbackRegistrations = new();
		private void NotifyOnChatMessageDelete(string channelId, ChannelChatMessageDeleteEvent data)
		{
			foreach (var action in _chatMessageDeleteCallbackRegistrations.Keys)
			{
				action(channelId, data);
			}
		}
		public event Action<string, ChannelChatMessageDeleteEvent> OnChatMessageDelete
		{
			add
			{
				if (_chatMessageDeleteCallbackRegistrations.TryAdd(value, false))
				{
					RegisterTopicWhenNeeded(EventSubTypes.CHANNEL_CHAT_MESSAGE_DELETE);
				}
			}
			remove
			{
				_chatMessageDeleteCallbackRegistrations.TryRemove(value, out _);
			}
		}

		// シャウトアウト作成
		private readonly ConcurrentDictionary<Action<string, ChannelShoutoutCreateEvent>, bool> _shoutoutCreateCallbackRegistrations = new();
		private void NotifyOnShoutoutCreate(string channelId, ChannelShoutoutCreateEvent data)
		{
			foreach (var action in _shoutoutCreateCallbackRegistrations.Keys)
			{
				action(channelId, data);
			}
		}
		public event Action<string, ChannelShoutoutCreateEvent> OnShoutoutCreate
		{
			add
			{
				if (_shoutoutCreateCallbackRegistrations.TryAdd(value, false))
				{
					RegisterTopicWhenNeeded(EventSubTypes.CHANNEL_SHOUTOUT_CREATE);
				}
			}
			remove
			{
				_shoutoutCreateCallbackRegistrations.TryRemove(value, out _);
			}
		}

		// シャウトアウト受信
		private readonly ConcurrentDictionary<Action<string, ChannelShoutoutReceiveEvent>, bool> _shoutoutReceiveCallbackRegistrations = new();
		private void NotifyOnShoutoutReceive(string channelId, ChannelShoutoutReceiveEvent data)
		{
			foreach (var action in _shoutoutReceiveCallbackRegistrations.Keys)
			{
				action(channelId, data);
			}
		}
		public event Action<string, ChannelShoutoutReceiveEvent> OnShoutoutReceive
		{
			add
			{
				if (_shoutoutReceiveCallbackRegistrations.TryAdd(value, false))
				{
					RegisterTopicWhenNeeded(EventSubTypes.CHANNEL_SHOUTOUT_RECEIVE);
				}
			}
			remove
			{
				_shoutoutReceiveCallbackRegistrations.TryRemove(value, out _);
			}
		}

		private static bool CanRegisterTopicOnAllChannels(string topic)
		{
			return topic switch
			{
				EventSubTypes.CHANNEL_POINTS_REDEEM => false,
				_ => true
			};
		}
	}
}