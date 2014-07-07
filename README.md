Mount & Blade Warband Server GUI
===
Mount & Blade Warband Server GUI is just that a UI created to help start/run Mount & Blade Warband servers without having to create separate config files/bat files. It is very much still in development but should contain the following list of features when done.

- Launch any Module that is out there for Mount & Blade Warband
- Allow for any setting that uses the "Set" command via the .exe

##What Works Now

* Can Launch server with module and game mode selected.
* Settings are now working.
  * Basic Validation is set-up (checks if INT/BOOL/String)
  * Settings still need to be validated 
  
##Good to Know 
* there is no setting specific validation implemented yet. 


**Example**

>set_combat_speed takes an INT which is checked for however only 0,2,4 are valid settings. There is no validation yet to say only 0,2,4 are allowed you will have to validate values for now.


##Screen Shot


![alt tag](http://n00bworks.com/img/MBServerGUI.PNG)



##Running from source

C# WPF based application using the following from NuGet
- [SQL Lite](http://www.nuget.org/packages/System.Data.SQLite.Core/)
- [Entity Framework](http://www.nuget.org/packages/EntityFramework/)

You will also need a local copy of the dedicated server files from TaleWorlds
- [Dedicated Server Files](http://download2.taleworlds.com/mb_warband_dedicated_1158.zip)