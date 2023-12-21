using AIMLbot;
using AIMLbot.AIMLTagHandlers;
using AIMLbot.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace AIMLTGBot
{
    public class AIMLService
    {
        readonly Bot bot;
        readonly Dictionary<long, User> users = new Dictionary<long, User>();
        readonly Dictionary<long, bool> isInTrap = new Dictionary<long, bool>();

        public AIMLService()
        {
            string root = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
            bot = new Bot();
            bot.loadSettings($"{root}\\config\\Settings.xml");
            bot.isAcceptingUserInput = false;
            AIMLLoader loader = new AIMLLoader(bot);
            loader.loadAIML($"{root}\\aiml");
            bot.isAcceptingUserInput = true;
        }

        public string Talk(long userId, string userName, string phrase)
        {
            var result = "";
            User user;
            bool isUserInTrap;
            if (!users.ContainsKey(userId))
            {
                user = new User(userId.ToString(), bot);
                users.Add(userId, user);
                isInTrap.Add(userId, false);
                Request r = new Request($"Меня зовут {userName}", user, bot);
                result += bot.Chat(r).Output + System.Environment.NewLine;
            }
            else
            {
                user = users[userId];
            }
            if (phrase.Contains("анекдот"))
            {
                isInTrap[userId] = true;          
            } else if (phrase == "заплакать")
            {
                isInTrap[userId] = false;
            }
            isUserInTrap = isInTrap[userId];
            if (isUserInTrap)
            {
                phrase = "анекдот";
            }
            result += bot.Chat(new Request(phrase, user, bot)).Output;
            return result;
        }
    }
}
