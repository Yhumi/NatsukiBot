using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MonikAIBot.Services.Database.Models;
using System;
using System.Collections.Generic;
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
        }

        private async Task UserJoinedAsync(SocketGuildUser user)
        {
            var GuildUser = (IGuildUser)user;
            if (user.Guild == null) return;

            GreetMessages GM = null;
            ulong ChannelID = 0;
            using (var uow = DBHandler.UnitOfWork())
            {
                if (!uow.Guild.IsGreeting(GuildUser.Guild.Id)) return;
                GM = uow.GreetMessages.GetRandomGreetMessage(GuildUser.Guild.Id);
                ChannelID = uow.Guild.GetOrCreateGuild(GuildUser.Guild.Id).GreetMessageChannel;
            }

            if (GM == null || ChannelID == 0) return;

            var ChannelToSend = (IMessageChannel)_discord.GetChannel(ChannelID);

            string message = GM.Message.Replace("{user}", GuildUser.Username);
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
            using (var uow = DBHandler.UnitOfWork())
            {
                if (!uow.Guild.IsDeleteLoggingEnabled(MessageChannel.Guild.Id)) return;
                G = uow.Guild.GetOrCreateGuild(MessageChannel.Guild.Id);
                if (G.GuildID == 0) return;
            }

            if (G == null) return;

            var ChannelToSend = (IMessageChannel) _discord.GetChannel(G.DeleteLogChannel);

            string content = CachedMessage.Content;
            if (content == "") content = "*original message was blank*";

            EmbedBuilder embed = new EmbedBuilder().WithAuthor(eab => eab.WithIconUrl(CachedMessage.Author.GetAvatarUrl()).WithName(CachedMessage.Author.Username)).WithOkColour()
                                                    .AddField(efb => efb.WithName("Channel").WithValue("#" + origChannel.Name).WithIsInline(true))
                                                    .AddField(efb => efb.WithName("MessageID").WithValue(CachedMessage.Id).WithIsInline(true))
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
