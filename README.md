Mount & Blade Warband Server GUI
===
Mount & Blade Warband Server GUI is just that a UI created to help start/run Mount & Blade Warband servers without having to create seperate config files/bat files. It is very much still in development but should contain the following list of features when done.

- Launch any Module that is out there for Mount & Blade Warband
- Allow for any setting that uses the "Set" command via the .exe

##What Works Now

- Can Launch server with module and game mode selected.
- No Settings are available at this point.

##Screen Shot


![alt tag](http://n00bworks.com/img/MBServerGUI.PNG)



##Running from source

C# WPF based application using the following from NuGet
- [SQL Lite](http://www.nuget.org/packages/System.Data.SQLite.Core/)
- [Entity Framework](http://www.nuget.org/packages/EntityFramework/)

You will also need a local copy of the dedicated server files from TaleWorlds
- [Dedicated Server Files](http://download2.taleworlds.com/mb_warband_dedicated_1158.zip)