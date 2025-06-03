using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.EventSub.Responses.VideoPlayback
{
    /// <summary>
    /// EventSub: channel.chat.message のイベントデータ
    /// 参考: https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelchatmessage
    /// </summary>
    public class ChannelChatMessageEvent
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; } = default!;

        [JsonPropertyName("broadcaster_user_id")]
        public string? BroadcasterUserId { get; set; } = default!;

        [JsonPropertyName("broadcaster_user_login")]
        public string? BroadcasterUserLogin { get; set; } = default!;

        [JsonPropertyName("broadcaster_user_name")]
        public string? BroadcasterUserName { get; set; } = default!;

        [JsonPropertyName("chatter_user_id")]
        public string? ChatterUserId { get; set; } = default!;

        [JsonPropertyName("chatter_user_login")]
        public string? ChatterUserLogin { get; set; } = default!;

        [JsonPropertyName("chatter_user_name")]
        public string? ChatterUserName { get; set; } = default!;

        [JsonPropertyName("message_id")]
        public string? MessageId { get; set; } = default!;

        [JsonPropertyName("message")]
        public ChannelChatMessageContent Message { get; set; } = default!;

        [JsonPropertyName("color")]
        public string? Color { get; set; }

        [JsonPropertyName("badges")]
        public List<ChannelChatBadge>? Badges { get; set; }

        [JsonPropertyName("cheer")]
        public ChannelChatCheer? Cheer { get; set; }

        [JsonPropertyName("reply")]
        public ChannelChatReply? Reply { get; set; }

        [JsonPropertyName("emotes")]
        public List<ChannelChatEmote>? Emotes { get; set; }

        [JsonPropertyName("is_action")]
        public bool IsAction { get; set; }

        [JsonPropertyName("message_type")]
        public string? MessageType { get; set; } = default!;

        [JsonPropertyName("sent_at")]
        public DateTimeOffset SentAt { get; set; }
    }

    public class ChannelChatBadge
    {
        [JsonPropertyName("set_id")]
        public string? SetId { get; set; } = default!;

        [JsonPropertyName("id")]
        public string? Id { get; set; } = default!;
    }

    public class ChannelChatCheer
    {
        [JsonPropertyName("bits")]
        public int Bits { get; set; }
    }

    public class ChannelChatReply
    {
        [JsonPropertyName("parent_message_id")]
        public string? ParentMessageId { get; set; } = default!;

        [JsonPropertyName("parent_message_body")]
        public string? ParentMessageBody { get; set; } = default!;

        [JsonPropertyName("parent_user_id")]
        public string? ParentUserId { get; set; } = default!;

        [JsonPropertyName("parent_user_login")]
        public string? ParentUserLogin { get; set; } = default!;

        [JsonPropertyName("parent_user_name")]
        public string? ParentUserName { get; set; } = default!;

        [JsonPropertyName("thread_message_id")]
        public string? ThreadMessageId { get; set; } = default!;

        [JsonPropertyName("thread_user_id")]
        public string? ThreadUserId { get; set; } = default!;

        [JsonPropertyName("thread_user_login")]
        public string? ThreadUserLogin { get; set; } = default!;

        [JsonPropertyName("thread_user_name")]
        public string? ThreadUserName { get; set; } = default!;
    }

    public class ChannelChatEmote
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; } = default!;

        [JsonPropertyName("start_index")]
        public int StartIndex { get; set; }

        [JsonPropertyName("end_index")]
        public int EndIndex { get; set; }
    }

	public class ChannelChatMessageContent
	{
		[JsonPropertyName("text")]
		public string? Text { get; set; } = default!;

		[JsonPropertyName("fragments")]
		public List<ChannelChatMessageFragment> Fragments { get; set; } = new();
	}

	public class ChannelChatMessageFragment
	{
		[JsonPropertyName("type")]
		public string? Type { get; set; } = default!;

		[JsonPropertyName("text")]
		public string? Text { get; set; } = default!;

		[JsonPropertyName("cheermote")]
		public object? Cheermote { get; set; }

		[JsonPropertyName("emote")]
		public object? Emote { get; set; }

		[JsonPropertyName("mention")]
		public object? Mention { get; set; }
	}
}