using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonikAIBot.Services
{
    public static class Extensions
    {
        private static Random _random = new Random();

        public static T RandomItem<T>(this IEnumerable<T> list)
        {
            return list.ElementAt(_random.Next(0, list.Count()));
        }

        public static Task<IUserMessage> SendErrorAsync(this IMessageChannel ch, string title, string text, string url = null, string footer = null)
        {
            var eb = new EmbedBuilder().WithErrorColour().WithDescription(text).WithTitle(title);
            if (url != null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
                eb.WithUrl(url);
            if (!string.IsNullOrWhiteSpace(footer))
                eb.WithFooter(efb => efb.WithText(footer));
            return ch.SendMessageAsync("", embed: eb.Build());
        }

        public static Task<IUserMessage> SendErrorAsync(this IMessageChannel ch, string text)
            => ch.SendMessageAsync("", embed: new EmbedBuilder().WithErrorColour().WithDescription(text).Build());

        public static Task<IUserMessage> SendSuccessAsync(this IMessageChannel ch, string title, string text, string url = null, string footer = null)
        {
            var eb = new EmbedBuilder().WithOkColour().WithDescription(text).WithTitle(title);
            if (url != null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
                eb.WithUrl(url);
            if (!string.IsNullOrWhiteSpace(footer))
                eb.WithFooter(efb => efb.WithText(footer));
            return ch.SendMessageAsync("", embed: eb.Build());
        }

        public static Task<IUserMessage> SendPictureAsync(this IMessageChannel ch, string title, string text, string pic, string url = null, string footer = null)
        {
            var eb = new EmbedBuilder().WithOkColour().WithDescription(text).WithTitle(title);
            if (pic != null && Uri.IsWellFormedUriString(pic, UriKind.Absolute))
                eb.WithImageUrl(pic);
            if (url != null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
                eb.WithUrl(url);
            if (!string.IsNullOrWhiteSpace(footer))
                eb.WithFooter(efb => efb.WithText(footer));
            return ch.SendMessageAsync("", embed: eb.Build());
        }

        public static Task<IUserMessage> SendPictureAsync(this IMessageChannel ch, string pic)
        {
            var eb = new EmbedBuilder().WithOkColour();
            if (pic != null && Uri.IsWellFormedUriString(pic, UriKind.Absolute))
                eb.WithImageUrl(pic);
            return ch.SendMessageAsync("", embed: eb.Build());
        }

        public static Task<IUserMessage> SendSuccessAsync(this IMessageChannel ch, string text)
            => ch.SendMessageAsync("", embed: new EmbedBuilder().WithOkColour().WithDescription(text).Build());

        public static EmbedBuilder WithOkColour(this EmbedBuilder eb)
            => eb.WithColor(16729080);

        public static EmbedBuilder WithErrorColour(this EmbedBuilder eb)
            => eb.WithColor(16711731);

        public static IMessage DeleteAfter(this IUserMessage msg, int seconds)
        {
            Task.Run(async () =>
            {
                await Task.Delay(seconds * 1000);
                try { await msg.DeleteAsync().ConfigureAwait(false); }
                catch { }
            });
            return msg;
        }

        public static Task<IUserMessage> BlankEmbedAsync(this IMessageChannel ch, Embed embed)
            => ch.SendMessageAsync("", false, embed);
    }
}
