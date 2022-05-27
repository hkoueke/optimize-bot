# optimize-bot
A Telegram written in C# .NET 5, to provide useful services to Mobile money clients in Cameroon.

# PREREQUISITES
This program was written using Visual studio 2019 on a Windows 10 station. Thus you can directly import this project in your environment provided you have the following dependencies installed:
- .NET Core 5
You will also need the following packages from Nuget. You can either use the nuget package manager, or the Package console. Both can be found under Tools > Nuget package Manager.
- log4Net v2.0.14
- Microsoft.EntityFrameworkCore.Design v5.0.5
- Microsoft.EntityFrameworkCore.Sqlite v5.0.5
- Stateless v5.11.0
- Telegram.Bot v17.0.0
- Telegram.Bot.Extensions.Polling v1.0.2

# Important:
- This program uses sqlite as backend database. Make sure you add **Microsoft.EntityFrameworkCore.Sqlite v5.0.5** to your project.
- This program makes use of the log4net library, especially the RollingFileAppender and the ConsoleAppender. Both of them are configured in the `app.config` file. For the RollingFileAppender a default folder, default file name and default file size are already specified. If case you need to customize these settings, head to line 30.

# About telegram: The Bot ID
In order for this app to issue responses to client requests through the Telegram Bot API, you need to install the Telegram App for Andoid or iOS, then invoke @BotFather to create a bot. Instructions on how to create a bot can be found on the [Bot Page](https://core.telegram.org/bots#3-how-do-i-create-a-bot)
