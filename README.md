Mount & Blade Warband Server Panel
===

Mount & Blade Warband Server Panel is just that a UI created to help start/run Mount & Blade Warband servers without having to create separate config files/bat files. It is very much still in development but should contain the following list of features when done.

- Installing via some other method than source build.
- Launch any Module that is out there for Mount & Blade Warband
- Allow for any setting that uses the "Set" command via the .exe
- Download and install server files from Taleworlds FTP.
- Save/Load server configs.

## What Works Now

* Can Launch server with module and game mode selected.
* Can Install server files from the taleworlds FTP
* Settings are now working.
  * Basic Validation is set-up (checks if INT/BOOL/String)
  * Settings still need to be validated (check for actual value as the server configs take some odd values.)
* Wix install package (*.msi) [Download Here](https://s3.amazonaws.com/mountandbladeserverpanel/installer/MBServerPanelInstall.msi)

## What Doesn't

* No saving of configs.
* Module tweaks for non native modules.

## Other Details

Check out the changelog.md for more specific details on the latest changes/issues with the development branch.

## Good to Know

* There is a minimum of settings needed to start a specific type of game mode.

**Example**

>you will need to add settings for password/name/steamport/port/maps/factions etc.

* There is no setting specific validation implemented yet.

**Example**

>set_combat_speed takes an INT which is checked for however only 0,2,4 are valid settings. There is no validation yet to say only 0,2,4 are allowed you will have to validate values for now.

## Running from source

This project should compile and run with no issues from source.

C# WPF based application using the following from NuGet
- [Mah Apps](http://mahapps.com/)
- [SQL Lite](http://www.nuget.org/packages/System.Data.SQLite.Core/)
