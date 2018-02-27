using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MonikAIBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MonikAIBot.Services
{
    class ImageRateLimitHandler
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IServiceProvider _provider;
        private readonly MonikAIBotLogger _logger;

        private TimeSpan DefaultCD = TimeSpan.FromMinutes(5);

        //Create a 

        public ImageRateLimitHandler(
            DiscordSocketClient discord,
            CommandService commands,
            IServiceProvider provider,
            MonikAIBotLogger logger)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;
            _logger = logger;

            _discord.MessageReceived += OnMessageAsync;
        }

        private async Task OnMessageAsync(SocketMessage s)
        {
            var msg = s as SocketUserMessage;
            if (msg == null) return;
            if (msg.Author.Id == _discord.CurrentUser.Id) return;

            var context = new SocketCommandContext(_discord, msg);

            //Get URL in string
            MatchCollection Matches = Regex.Matches(context.Message.Content, @"((http|ftp|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?)");

            //Right first step
            //Check if the message has an image. If it doesn't and doesn't have a URL, we don't care.
            if (context.Message.Attachments.Count == 0 && Matches.Count == 0) return;

            //Now we fetch each part
            User U = null;
            Channels C = null;
            using (var uow = DBHandler.UnitOfWork())
            {
                U = uow.User.GetOrCreateUser(context.User.Id);
                C = uow.Channels.GetOrCreateChannel(context.Channel.Id, DefaultCD);

                //Create just to make sure it exists so more doesn't fail down there
                uow.UserRate.GetOrCreateUserRate(C.ID, U.ID);
            }

            //Is the user exempt?

            if (U == null || U.IsExempt) return;

            //Now we need to check if the channel is even part of it all

            if (C == null || !C.State) return;

            //Alright we need to limit shit now. 
            //Lets check if the user is allowed to post
            bool canPost = true;
            using (var uow = DBHandler.UnitOfWork())
            {
                canPost = uow.UserRate.CanUserPostImages(C.ID, U.ID, C.CooldownTime, C.MaxPosts);
            }

            int amountToAdd = context.Message.Attachments.Count + Matches.Count;

            //If they can post we need to add one to their posts and then we're done here
            if (canPost)
            {
                using (var uow = DBHandler.UnitOfWork())
                {
                    uow.UserRate.AddUserPost(C.ID, U.ID, amountToAdd);
                }

                return;
            }

            //If they can't post we have a lot to deal with here.
            //Make a list of the message (cause apparently we can't just delete one)
            List<IMessage> messageList = new List<IMessage>()
            {
                context.Message
            };

            //First we have to actually delete the message
            await context.Channel.DeleteMessagesAsync(messageList);

            //Then we need to tell the user not to post so quick
            var tellingOff = await context.Channel.SendErrorAsync("Slow down dummy!");

            //Remove after 2 seconds
            tellingOff.DeleteAfter(5);
        }
    }
}
