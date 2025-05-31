namespace CatCore.Models.Twitch.EventSub
{
    /// <summary>
    /// EventSubのサブスクリプションタイプを定義します。
    /// </summary>
    internal static class EventSubTypes
    {
        /// <summary>配信が開始されたとき</summary>
        public const string STREAM_ONLINE = "stream.online";
        /// <summary>配信が終了したとき</summary>
        public const string STREAM_OFFLINE = "stream.offline";
        /// <summary>チャンネルが新たにフォローされたとき</summary>
        public const string CHANNEL_FOLLOW = "channel.follow";
        /// <summary>チャンネルにサブスクライブされたとき</summary>
        public const string CHANNEL_SUBSCRIBE = "channel.subscribe";
        /// <summary>チャンネルポイント報酬がリワードされたとき</summary>
        public const string CHANNEL_POINTS_REDEEM = "channel.channel_points_custom_reward_redemption.add";

        // 追加 EventSub タイプ
        /// <summary>チャンネル情報（タイトルやカテゴリなど）が更新されたとき</summary>
        public const string CHANNEL_UPDATE = "channel.update";
        /// <summary>他のチャンネルからレイドを受けた/送ったとき</summary>
        public const string CHANNEL_RAID = "channel.raid";
        /// <summary>ユーザーがBANされたとき</summary>
        public const string CHANNEL_BAN = "channel.ban";
        /// <summary>ユーザーのBANが解除されたとき</summary>
        public const string CHANNEL_UNBAN = "channel.unban";
        /// <summary>モデレーターが追加されたとき</summary>
        public const string CHANNEL_MODERATOR_ADD = "channel.moderator.add";
        /// <summary>モデレーターが解除されたとき</summary>
        public const string CHANNEL_MODERATOR_REMOVE = "channel.moderator.remove";
        /// <summary>チアー（ビッツ）が送信されたとき</summary>
        public const string CHANNEL_CHEER = "channel.cheer";
        /// <summary>サブスクギフトが贈られたとき</summary>
        public const string CHANNEL_SUBSCRIPTION_GIFT = "channel.subscription.gift";
        /// <summary>サブスクメッセージが送信されたとき</summary>
        public const string CHANNEL_SUBSCRIPTION_MESSAGE = "channel.subscription.message";
        /// <summary>投票（Poll）が開始されたとき</summary>
        public const string CHANNEL_POLL_BEGIN = "channel.poll.begin";
        /// <summary>投票（Poll）の途中経過</summary>
        public const string CHANNEL_POLL_PROGRESS = "channel.poll.progress";
        /// <summary>投票（Poll）が終了したとき</summary>
        public const string CHANNEL_POLL_END = "channel.poll.end";
        /// <summary>予想（Prediction）が開始されたとき</summary>
        public const string CHANNEL_PREDICTION_BEGIN = "channel.prediction.begin";
        /// <summary>予想（Prediction）の途中経過</summary>
        public const string CHANNEL_PREDICTION_PROGRESS = "channel.prediction.progress";
        /// <summary>予想（Prediction）がロックされたとき</summary>
        public const string CHANNEL_PREDICTION_LOCK = "channel.prediction.lock";
        /// <summary>予想（Prediction）が終了したとき</summary>
        public const string CHANNEL_PREDICTION_END = "channel.prediction.end";
        /// <summary>目標（Goal）が開始されたとき</summary>
        public const string CHANNEL_GOAL_BEGIN = "channel.goal.begin";
        /// <summary>目標（Goal）の途中経過</summary>
        public const string CHANNEL_GOAL_PROGRESS = "channel.goal.progress";
        /// <summary>目標（Goal）が終了したとき</summary>
        public const string CHANNEL_GOAL_END = "channel.goal.end";
        /// <summary>ハイプトレインが開始されたとき</summary>
        public const string CHANNEL_HYPE_TRAIN_BEGIN = "channel.hype_train.begin";
        /// <summary>ハイプトレインの途中経過</summary>
        public const string CHANNEL_HYPE_TRAIN_PROGRESS = "channel.hype_train.progress";
        /// <summary>ハイプトレインが終了したとき</summary>
        public const string CHANNEL_HYPE_TRAIN_END = "channel.hype_train.end";
        /// <summary>ユーザーがアプリの認可を許可したとき</summary>
        public const string USER_AUTHORIZATION_GRANT = "user.authorization.grant";
        /// <summary>ユーザーがアプリの認可を取り消したとき</summary>
        public const string USER_AUTHORIZATION_REVOKE = "user.authorization.revoke";
        /// <summary>ユーザー情報が更新されたとき</summary>
        public const string USER_UPDATE = "user.update";
    }

    /// <summary>
    /// EventSubのサブスクリプション条件を生成するヘルパーです。
    /// </summary>
    internal static class EventSubConditions
    {
        public static object BroadcasterUserId(string channelId) => new { broadcaster_user_id = channelId };
        public static object ModeratorUserId(string channelId, string userId) => new { broadcaster_user_id = channelId, moderator_user_id = userId };
        // 必要に応じて他の条件も追加
    }
}