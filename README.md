# optimize-bot
A Telegram written in C# .NET 5, to provide useful services to Mobile money clients in Cameroon.

### PREREQUISITES
This program was written using Visual studio 2019 on a Windows 10 station. Thus you can directly import this project in your environment provided you have both software installed, along with the following dependencies:
- .NET Core 5

It is possible to import the project in Visual Studio Code. How to do so is not covered by this readme.

You will also need the following packages from Nuget. You can either use the nuget package manager, or the Package console. Both can be found under Tools > Nuget package Manager.
- log4Net v2.0.14
- Microsoft.EntityFrameworkCore.Design v5.0.5
- Microsoft.EntityFrameworkCore.Sqlite v5.0.5
- Stateless v5.11.0
- Telegram.Bot v17.0.0
- Telegram.Bot.Extensions.Polling v1.0.2

### Important:
- This program uses sqlite as backend database. Make sure you add **Microsoft.EntityFrameworkCore.Sqlite v5.0.5** to your project.
- This program makes use of the log4net library, especially the `RollingFileAppender` and the `ConsoleAppender`. Both of them are configured in the `app.config` file. For the RollingFileAppender a default folder, default file name and default file size are already specified. If case you need to customize these settings, head to line 30.

### About telegram: The Bot ID
In order for this app to issue responses to client requests through the Telegram Bot API, you need to install the Telegram App for Andoid or iOS, then invoke @BotFather to create a bot. Instructions on how to create a bot can be found on the [Bot Page](https://core.telegram.org/bots#3-how-do-i-create-a-bot).
Once you are in prossesion of your Bot ID, simply paste that value in the `constants.cs` file. Replace `YOUR_BOT_TOKEN` by your Bot ID in the following code:
`public const string BOT_TOKEN = "YOUR_BOT_TOKEN";`.

You can also edit the `public const string DEV_LINK_TELEGRAM = @"https://t.me/YOUR_TELEGRAM_USERNAME";` constant and replace `YOUR_TELEGRAM_USERNAME` by your own Telegram username, e.g `public const string DEV_LINK_TELEGRAM = @"https://t.me/kyo-kusanagi";`

Last but not the least, the app can also notify designated admins when a fatal error occurs in the app lifecycle, provided the following requirements are met:
- Edit `public static readonly string[] ADMINS = { "TELEGRAM_ID_01","TELEGRAM_ID_02" };` and replace `TELEGRAM_ID_01` and `TELEGRAM_ID_02` (We had two admins at the time of readction of the README) by the actual telegram Ids of your admins. You can add as many IDs as you wish, provided each value is separated by a comma.
- As an admin, send a `/start` command to the bot. This will trigger the admin registration in the datatabase. Only then, whenever an issue occurs with the app, the database will be queried for admins and all of them will receive message notifications about the incident.

### The database : How to generate one?
Before running the app, you need to generate a database. Currently, sqlite and Entity Framework are used for this purpose. We've already prepared the code for the database migrations. Please refer to the `OptimizeBotContext.cs` file in the Context folder for more information. Follow the steps below to  create the database:
- Open the Package manager console (In Visual Studio 2019, Menu Tools > Nuget package Manager > Package Manager Console.
- Type the `dotnet ef migrations add InitialMigration` command to create the initial migration file. Running this command will create a `Migrations` folder in your solution directory.
- Type the `dotnet ef database update` command to create database file. The created file will be named `optimize.db` by default. You can customize the file name by editing the `OptimizeBotContext.cs`,  line 10: 

`protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder
            .UseSqlite("Data Source=optimizebot.db")`
            
- In the solution explorer window pane, right-click the default db file, select Properties sub-menu. In the Properties pane, edit the *Copy in output folder* property and set its value to *Copy if recent*. This will instruct the compiler to copy your database to the output folder the first time the app is compiled, as well as replace any copy if the file in the solutions directory is recent.

### Hit compile, run the app and there you go!
You've reached down here. Great! All you have to do now is cmpile the app, run it, sit back and realx while the message keep flowing in.

### App services available
Currently the app serves three services to its cusotmers
- Cash in
- Cash out
- Receipt download

You can add as many services as you wish. Please refer to the Architecture section for details.

### Application Architecture
The app revolves around a simple architecture. TLDR, this is what happens when an end user client submits a request to our bot.
- The end client sends a service request from his device to the telegam API
- The message (Telegram calls that an Update, so we do in the next lines) is transmitted to our bot by Telegram through polling
- Upon reception, a handler manages the Update. It first sends it through a Memory Cache, where the the user details are cached. The Cache strategy used is pass-through, meaning the user will be persisted in the database if it was not before, without hitting the Cache. Subsequent queries will Hit the Cache > Poll Database > Return User Entity > Put in cache > Serve.
- The handler then sends the Update to a Process Manager for processing. Depending on the requested service the process manager will select the correct Process object to instanciate and pass the data received for further processing. The Process manager is also sole resposible for sending requests/ responses back to the end-user.
- Once the data is processed, the result is sent back to the end-user and the app returns to Idle state.

To learn more about the different components of the app, please refer to:
- `CacheMgr.cs` and `MemoryCacheWithPolicy` in the Cache folder
- `Handlers.cs` int the Helpers folder
- `About.cs`, `CashIn.cs`, `CashOut.cs`, `Start.cs`, `Receipt.cs`, `Conversation.cs` in the Processes Folder.







