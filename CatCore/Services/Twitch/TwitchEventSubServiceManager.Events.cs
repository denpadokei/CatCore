using System;
using System.Collections.Concurrent;
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
				_streamUpCallbackRegistrations.TryAdd(value, false);
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
				_streamDownCallbackRegistrations.TryAdd(value, false);
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
				_followCallbackRegistrations.TryAdd(value, false);
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
				_channelPointsRedeemCallbackRegistrations.TryAdd(value, false);
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
				_predictionBeginCallbackRegistrations.TryAdd(value, false);
			}
			remove
			{
				_predictionBeginCallbackRegistrations.TryRemove(value, out _);
			}
		}
	}
}