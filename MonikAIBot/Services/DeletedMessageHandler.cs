using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MonikAIBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonikAIBot.Services
{
    class DeletedMessageHandler
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IServiceProvider _provider;
        private readonly MonikAIBotLogger _logger;

        public DeletedMessageHandler(
            DiscordSocketClient discord,
            CommandService commands,
            IServiceProvider provider,
            MonikAIBotLogger logger)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;
            _logger = logger;
            
            _discord.MessageDeleted += DeletedAsync;
            _discord.UserJoined += UserJoinedAsync;
            _discord.UserVoiceStateUpdated += UserVoiceStateAsync;
        }

        private async Task UserVoiceStateAsync(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
        {
            ulong ServerID;
            string ChannelName = "";
            bool swapped = false;
            bool joined = false;
            SocketVoiceChannel curChannel;
            SocketVoiceChannel prevChannel = null;
            if (arg3.VoiceChannel == null)
            {
                //They're no longer connected;
                ServerID = arg2.VoiceChannel.Guild.Id;
                ChannelName = arg2.VoiceChannel.Name;
                curChannel = arg2.VoiceChannel;
            }
            else if (arg2.VoiceChannel == null)
            {
                //They weren't connected but now are
                ServerID = arg3.VoiceChannel.Guild.Id;
                ChannelName = arg3.VoiceChannel.Name;
                curChannel = arg3.VoiceChannel;
                joined = true;
            }
            else
            {
                prevChannel = arg2.VoiceChannel;
                ServerID = arg3.VoiceChannel.Guild.Id;
                ChannelName = arg3.VoiceChannel.Name;
                curChannel = arg3.VoiceChannel;

                if (curChannel.Guild.Id == prevChannel.Guild.Id)
                    swapped = true;
                else
                    joined = true;

                return;
            }

            Guild G = null;
            Channels C = null;
            Channels PC = null;
            using (var uow = DBHandler.UnitOfWork())
            {
                //Lets get that guild
                G = uow.Guild.GetOrCreateGuild(ServerID);
                C = uow.Channels.GetOrCreateChannel(curChannel.Id, TimeSpan.FromMinutes(5));

                if (swapped)
                {
                    PC = uow.Channels.GetOrCreateChannel(prevChannel.Id, TimeSpan.FromMinutes(5));
                }
            }

            if (!G.VCNotifyEnable) return;
            if (C.VoiceChannelLink == 0) return;

            var channelToSend = (IMessageChannel)_discord.GetChannel(C.VoiceChannelLink);
            IMessageChannel prevChannelToSend = null;

            if (PC != null && PC.VoiceChannelLink != 0)
            {
                prevChannelToSend = (IMessageChannel)_discord.GetChannel(PC.VoiceChannelLink);
            }

            if (swapped)
            {
                if (prevChannelToSend != null)
                    await prevChannelToSend.SendMessageAsync($"📣 {arg1.Mention} has left {prevChannel.Name} (gone to {ChannelName}).");

                await channelToSend.SendMessageAsync($"📣 {arg1.Mention} has joined {ChannelName} (from {prevChannel.Name}).");
            }

            if (joined)
            {
                await channelToSend.SendMessageAsync($"📣 {arg1.Mention} has joined VC ({ChannelName}).");
                return;
            }

            await channelToSend.SendMessageAsync($"📣 {arg1.Mention} has left VC.");
        }

        private async Task UserJoinedAsync(SocketGuildUser user)
        {
            var GuildUser = (IGuildUser)user;
            if (user.Guild == null) return;

            GreetMessages GM = null;
            AutoBan AB = null;
            ulong ChannelID = 0;
            using (var uow = DBHandler.UnitOfWork())
            {
                if (!uow.Guild.IsGreeting(GuildUser.Guild.Id)) return;
                GM = uow.GreetMessages.GetRandomGreetMessage(GuildUser.Guild.Id);
                ChannelID = uow.Guild.GetOrCreateGuild(GuildUser.Guild.Id).GreetMessageChannel;
                AB = uow.AutoBan.GetAutoBan(GuildUser.Id);
            }

            if (AB != null)
            {
                await _discord.GetGuild(GuildUser.GuildId).AddBanAsync(user, reason: "Autobanned by Natsuki.");
                return;
            }

            if (GM == null || ChannelID == 0) return;

            var ChannelToSend = (IMessageChannel)_discord.GetChannel(ChannelID);

            string message = GM.Message.Replace("{user}", GuildUser.Mention);
            await ChannelToSend.SendMessageAsync(message);
        }

        private async Task DeletedAsync(Cacheable<IMessage, ulong> CacheableMessage, ISocketMessageChannel origChannel)
        {
            var CachedMessage = await CacheableMessage.GetOrDownloadAsync();
            var MessageChannel = (ITextChannel)origChannel;

            if (CachedMessage.Source != MessageSource.User) return;
            if (MessageChannel.Guild == null) return;
            if (CachedMessage == null) return;

            Guild G = null;
            List<BlockedLogs> BLs = new List<BlockedLogs>();
            using (var uow = DBHandler.UnitOfWork())
            {
                if (!uow.Guild.IsDeleteLoggingEnabled(MessageChannel.Guild.Id)) return;
                G = uow.Guild.GetOrCreateGuild(MessageChannel.Guild.Id);
                if (G.GuildID == 0) return;
                BLs = uow.BlockedLogs.GetServerBlockedLogs(MessageChannel.Guild.Id);
            }

            if (BLs != null && BLs.Count > 0)
            {
                if (BLs.Any(x => CachedMessage.Content.StartsWith(x.BlockedString))) return;
            }

            if (G == null) return;

            var ChannelToSend = (IMessageChannel) _discord.GetChannel(G.DeleteLogChannel);

            string content = CachedMessage.Content;
            if (content == "") content = "*original message was blank*";

            EmbedBuilder embed = new EmbedBuilder().WithAuthor(eab => eab.WithIconUrl(CachedMessage.Author.GetAvatarUrl()).WithName(CachedMessage.Author.Username)).WithOkColour()
                                                    .AddField(efb => efb.WithName("Channel").WithValue("#" + origChannel.Name).WithIsInline(true))
                                                    .AddField(efb => efb.WithName("MessageID").WithValue(CachedMessage.Id).WithIsInline(true))
                                                    .AddField(efb => efb.WithName("UserID").WithValue(CachedMessage.Author.Id).WithIsInline(true))
                                                    .AddField(efb => efb.WithName("Message").WithValue(content));

            string footerText = "Created: " + CachedMessage.CreatedAt.ToString();

            if (CachedMessage.EditedTimestamp != null) footerText += $" | Edited: " + CachedMessage.EditedTimestamp.ToString();

            EmbedFooterBuilder footer = new EmbedFooterBuilder().WithText(footerText);

            await ChannelToSend.BlankEmbedAsync(embed.WithFooter(footer));

            if (CachedMessage.Attachments.Count == 0) return;

            foreach(var attatchment in CachedMessage.Attachments)
            {
                await ChannelToSend.SendMessageAsync($"Message ID: {CachedMessage.Id} has attachment: {attatchment.Url}");
            }
        }
    }
}
