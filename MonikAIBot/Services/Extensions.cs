using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        public static Embed EmbedErrorAsync(this EmbedBuilder eb, string text)
            => eb.WithErrorColour().WithDescription(text).Build();

        public static Embed EmbedSuccessAsync(this EmbedBuilder eb, string text)
            => eb.WithOkColour().WithDescription(text).Build();

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

        public static Embed PictureEmbed(this EmbedBuilder eb, string title, string text, string pic, string url = null, string footer = null)
        {
            eb.WithOkColour().WithDescription(text).WithTitle(title);
            if (pic != null && Uri.IsWellFormedUriString(pic, UriKind.Absolute))
                eb.WithImageUrl(pic);
            if (url != null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
                eb.WithUrl(url);
            if (!string.IsNullOrWhiteSpace(footer))
                eb.WithFooter(efb => efb.WithText(footer));
            return eb.Build();
        }

        public static Task<IUserMessage> SendPictureAsync(this IMessageChannel ch, string pic)
        {
            var eb = new EmbedBuilder().WithOkColour();
            if (pic != null && Uri.IsWellFormedUriString(pic, UriKind.Absolute))
                eb.WithImageUrl(pic);
            return ch.SendMessageAsync("", embed: eb.Build());
        }

        public static string NicknameUsername(this IGuildUser user)
        {
            return user?.Nickname ?? user.Username;
        }

        public static Task<IUserMessage> SendSuccessAsync(this IMessageChannel ch, string text)
            => ch.SendMessageAsync("", embed: new EmbedBuilder().WithOkColour().WithDescription(text).Build());

        public static EmbedBuilder WithOkColour(this EmbedBuilder eb)
            => eb.WithColor(16729080);

        public static EmbedBuilder WithErrorColour(this EmbedBuilder eb)
            => eb.WithColor(16711731);

        public async static Task RemoveAllRoles(this IGuildUser user, IGuild guild)
        {
            //Collection of roles
            List<IRole> roles = new List<IRole>();

            //Sort out the roles now
            foreach (ulong roleID in user.RoleIds)
            {
                IRole role = guild.GetRole(roleID);
                if (!(role == guild.EveryoneRole))
                    roles.Add(role);
            }

            await user.RemoveRolesAsync(roles);
        }

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

        public static string ParseBooruTags(this string str)
        {
            //Remove whitespace around +
            str = Regex.Replace(str, @"\s*([+])\s*", "$1");

            //Fix up remaining whitespace if there's two or more
            str = Regex.Replace(str, "[ ]{2,}", " ");

            //Finally any single-spaces are now replaced by underscores
            return str.Replace(' ', '_');
        }

        public static string FirstCharToLower(this string input)
        {
            switch (input)
            {
                case null: throw new ArgumentNullException(nameof(input));
                case "": throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
                default: return input.First().ToString().ToLower() + input.Substring(1);
            }
        }
    }
}
