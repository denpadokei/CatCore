using System;
using System.Collections.Generic;

namespace CatCore.Models.Twitch.EventSub.Responses
{
    /// <summary>
    /// EventSub: チャンネルで予想（Prediction）が開始されたときのイベントデータ
    /// 参考: https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelpredictionbegin
    /// </summary>
    public class ChannelPredictionBeginEvent
    {
        /// <summary>配信者のユーザーID</summary>
        public string? BroadcasterUserId { get; set; }

        /// <summary>配信者の表示名</summary>
        public string? BroadcasterUserName { get; set; }

        /// <summary>配信者のログイン名</summary>
        public string? BroadcasterUserLogin { get; set; }

        /// <summary>予想のID</summary>
        public string? Id { get; set; }

        /// <summary>予想のタイトル</summary>
        public string? Title { get; set; }

        /// <summary>予想の開始日時（ISO8601）</summary>
        public DateTime StartedAt { get; set; }

        /// <summary>予想のロック予定日時（ISO8601）</summary>
        public DateTime LocksAt { get; set; }

        /// <summary>選択肢リスト</summary>
        public List<PredictionChoice>? Outcomes { get; set; }

        /// <summary>
        /// 予想の選択肢
        /// </summary>
        public class PredictionChoice
        {
            /// <summary>選択肢ID</summary>
            public string? Id { get; set; }

            /// <summary>選択肢のタイトル</summary>
            public string? Title { get; set; }

            /// <summary>この選択肢に賭けられたポイント数</summary>
            public int ChannelPoints { get; set; }

            /// <summary>この選択肢を選んだユーザー数</summary>
            public int Users { get; set; }
        }
    }
}