# CatCore
CatCore is a high-performance .NET Standard 2.0 library which provides a shared connection to mods and other applications for Twitch (and other future streaming platforms).


## Installation

The installation is fairly simple.

1) Grab the latest library from BeatMods/ModAssistant (once it's available) or from [the releases page](https://github.com/ErisApps/CatCore/releases/latest) (once there is actually a release)
2) Extract the zip in the main Beat Saber folder. This implies putting the DLL in the "Libs" folder and the manifest file in the "Plugins" folder of your Beat Saber installation.


### Setup

1) Boot up Beat Saber
2) If there's a mod installed that relies on CatCore, then the web portal will automatically pop up.
   If it does not, either make sure there's actually mod installed the depends on it and try navigating to http://localhost:8338.
3) Log in with your Twitch account (and possibly toggle some of the other settings as you see fit)
4) Press the save button
5) We're done, you should have set it up correctly now ^^


## Documentation

CatCore is documented using XML docs, which are provided in the release zip as well. However, documentation and information covering several subjects will also be added to [the CatCore wiki](https://github.com/ErisApps/CatCore/wiki).
It will cover several subjects, including but not limited to, performance benchmarks, configuration, et cetera. However, please keep in mind that this is still a work in progress.


### Twitch EventSub events

Besides the legacy chat events, CatCore subscribes to the EventSub subscription types of Twitch and exposes them through `ITwitchService.GetEventSubService()`.
The returned service is split into the following event families, each of which exposes its own events:

| Family | Subscription types |
| --- | --- |
| `HypeTrain` | `channel.hype_train.begin` / `.progress` / `.end` |
| `Goal` | `channel.goal.begin` / `.progress` / `.end` |
| `Charity` | `channel.charity_campaign.donate` / `.progress` / `.start` / `.stop` |
| `User` | `user.update`, `user.whisper.message` |
| `Subscriptions` | `channel.subscribe`, `channel.subscription.end` / `.gift` / `.message`, `channel.vip.add` / `.remove` |
| `ChannelActivity` | `channel.bits.use`, `channel.cheer`, `channel.custom_power_up_redemption.add`, `channel.raid`, `channel.update`, `channel.shoutout.create` / `.receive` |
| `Moderation` | `channel.ban`, `channel.unban`, `channel.moderate`, `channel.moderator.add` / `.remove`, `channel.shield_mode.begin` / `.end` |
| `ChannelPoints` | `channel.channel_points_automatic_reward_redemption.add`, `channel.channel_points_custom_reward_redemption.update`, `channel.channel_points_custom_reward.add` / `.remove` / `.update` |
| `AutoMod` | `automod.message.hold` / `.update`, `automod.settings.update`, `automod.terms.update`, `channel.chat.user_message_hold` / `.update` |
| `Safety` | `channel.suspicious_user.message` / `.update`, `channel.unban_request.create` / `.resolve`, `channel.warning.send` / `.acknowledge` |

Most of these subscription types require additional OAuth scopes. Those scopes are requested when you log in, but a token that is missing them stays valid: the matching subscriptions are then simply not created.
Because Twitch limits the total subscription cost of a WebSocket connection, the subscriptions that are billed are created last, so that the chat subscriptions always take priority.


## FAQ

### What does "subscription cost" mean? Does Twitch charge me for this?

No, Twitch does not bill you anything. EventSub uses a cost-based quota: every subscription has a cost that counts against `max_total_cost`, which is **10 for a WebSocket connection** (counted per client id and user id). Once the quota is used up, Twitch rejects further subscriptions with an HTTP 400, and CatCore logs a warning and skips them.

### Is the cost always 0 once my app is authorized?

Partly. The cost is evaluated per subscription, based on the user referenced by its condition:

- Subscriptions whose type requires that user to authorize your app cost **0** (without the grant they cannot be created at all).
- Subscriptions whose type does not require an authorization (e.g. `stream.online`, `channel.update`) cost **1**, unless that user has authorized your app with *any* OAuth scope, in which case they cost 0 as well.

So a single grant from a user makes every subscription that references that user free, but it does not make subscriptions that reference *other* users free.

### I'm the only user and I only watch my own channel. Do I have to care about the limit?

No. The user of the condition is always you, so every subscription costs 0 and the limit of 10 is never reached. All supported subscription types are created, including the ones that are otherwise billed.

### Why are `channel.update` or `channel.raid` missing on my additional channels?

Every additional channel whose broadcaster has not authorized CatCore consumes up to 4 cost (`stream.online`, `stream.offline`, `channel.update`, `channel.raid`), so the limit of 10 leaves room for roughly two such channels. CatCore creates the chat subscriptions of every channel first and the billed ones afterwards, so only the optional `channel.update` and `channel.raid` subscriptions of the later channels are skipped, together with a warning in the log. Chat and all authorization-gated families are unaffected.

### Do I need to log in again after updating CatCore?

Only to receive the new event families. The additional OAuth scopes are requested the next time you log in. An existing token stays valid and CatCore simply does not create the subscriptions whose scopes are missing, so nothing breaks until you re-authorize.

### Which EventSub subscription types are not supported?

Shared chat (`channel.shared_chat.*`), Conduit (`conduit.shard.disabled`), Drop (`drop.entitlement.grant`), Extensions (`extension.bits_transaction.create`), Guest Star (`channel.guest_star_*`) and `user.authorization.grant` / `user.authorization.revoke`.

### Why do moderator-scoped events only arrive when I'm a moderator of the channel?

Types such as `channel.moderate` or `channel.shield_mode.*` require the `moderator_user_id` of the condition to be the user of the token, and that user has to be a moderator (or the broadcaster) of the channel. CatCore subscribes with your logged-in account as the moderator, so these events only arrive for channels where that account has the required permissions. Twitch rejects the subscription otherwise, which CatCore logs and ignores.


## Origin and name

**CatCore** is the spiritual successor to ChatCore, a library made that is/was used by several Beat Saber mods in order to interact with Twitch (it did support Mixer as well, but only starting roughly 1 month before it got shut down.)

I took over maintaining ChatCore sometime around August/September 2020, during which I was often working on said library.
However, I noticed rather quickly that some things could be done in a different manner, leading to better maintainability, while also being able to gain more performance out of it, by leveraging newer techniques.
And thus... did I start on a journey on the 1st of March... by starting with the full-on rewrite of this library...

Now, you might be wondering about things like "**Why is this library actually called CatCore?**" or "**Why was the name actually changed?**".
There are a few reasons for it, some of them being sillier than others though.
1) As some might know, I'm terrible sometimes when it comes down to writing.
   While I was still working on ChatCore, I happened to typo its name _very_ often, so I would pretty much always end up accidentally sending "CatCore" instead.
   And... believe it or not, but the name kinda stuck.
2) The second reason is pretty much a continuation of the first one, but in case you're familiar with other languages, you might know that the english word "Cat" translates to "Chat" in french.
   So yeah... other than being a typo, it could also be a fun wordplay.
3) As the library is initially used in mods, we would often receive the question that the mod that displayed chat "wasn't working". In which the case often was that people had mistaken ChatCore for said mod.
   By slightly changing its name, I hope that it would actually get rid of some of said confusion.


## Developing

Compiling this library requires VS 2022 Preview or Rider 2021.3 EAP or newer and at least the .NET 6 RC2 SDK tooling.
CatCore leverages source generators to improve performance and throughput in some areas, which is why it relies on the latest compiler version, despite being a .NET Standard 2.0 library.
You also need to compile on Windows if you want to create a non-debug build, this because CatCore relies on ILRepack (to merge in all its dependencies) which currently, due to a bug in the underlying (older) version of Mono.Cecil, only works on Windows.
~~However, [a new maintainer recently joined the ILRepack project and plans to address a few of those issues as well.](https://github.com/gluck/il-repack/issues/304)~~