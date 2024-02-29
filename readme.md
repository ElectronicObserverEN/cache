# Cache

## Updating

Using `GadgetUpdateCheck.Desktop.exe` with a Japanese IP address:

![image](https://github.com/ElectronicObserverEN/cache/assets/40002167/8178afd9-0f0b-4442-98ee-f3e283873c9c)

- Start/Stop - toggle the periodic update check - check is performed every 5 minutes
- Update - instantly makes an update check without pushing
- Push - push updates to GitHub
- Get IP - fetches your current IP and writes it to log
- Proxy - for proxying the update check (I can't get split tunneling working with the exe, so I proxy it through EO)

## Building the update check tool

Right click on `GadgetUpdateCheck.Desktop` in Visual Studio and click `Publish...`. The existing folder profile will override the existing exe.  
I got a case where publish didn't work via the UI, so running this command in the package manager console can be used as an alternative.
```
dotnet publish .\GadgetUpdateCheck.Desktop\GadgetUpdateCheck.Desktop.csproj /p:PublishProfile=GadgetUpdateCheck.Desktop\Properties\PublishProfiles\FolderProfile.pubxml
```
