# Introduction #

The GPFUpdateChecker library allows .NET Framework Windows applications (i.e. applications that use `System.Windows.Forms`) to check for updates from an XML file hosted somewhere on the Internet. It is not intended for use with console applications or Web applications (`System.Web`). It _may_ work with non-Microsoft .NET implementations such as [Mono](http://www.mono-project.com/), but this is untested and considered not supported.

# Project Structure #

The project/solution is a fairly standard Microsoft Visual Studio 2010 solution. We use the Express edition of Visual Studio, so the solution should be supported by all versions of Visual Studio, from Express all the way to Ultimate, without any problems.

For our projects, we use the .NET Framework 2.0 rather than later implementations. Version 2.0 is widely supported, fairly feature rich, and is widely implemented by non-Microsoft implementations such as Mono. Many of the later versions of the Framework are not so widely supported. As far as I know, you should be able to use the library in your projects even if you are using a different version of the Framework. The only requirement is that the user must have both versions of the Framework installed, which is usually the case for most installations later than version 2.0.

There are two projects within the overall solution:

  * The GPFUpdateChecker project is the heart of the solution and holds the base library code. If you only need the library and don't need the reference implementation, compiling this code will be sufficient.
  * The UpdateCheckTester project is a simple `Windows.Forms` application the provides a reference implementation of the library. This application opens a small window, then initiates the update check in such a way that it is guaranteed to always find an updated file. The reference implementation searches for the [Cryptnos](http://www.cryptnos.com/) update XML file by default. You may modify this application to test the library's many features, or to test your own XML update feed.

# XML Feed Syntax #

The XML update feed syntax will be covered in a [separate document](XMLSyntax.md). There is an XSD schema file included as part of the project, and this schema file **must** be included along with the compiled library when you distribute your final application. (Failure to include this file in your project will result in your update check always failing with XML parsing and validation errors.)

# Including the Library into Your Project #

To include the library as part of your project, in the Visual Studio Solution Explorer, right-click the References folder and select Add Reference. Use the Add Reference dialog to locate the `GPFUpdateChecker.dll` assembly file and click OK to include the library as part of your project.

As a general rule, we do not recommend that you put the GPFUpdateChecker assembly into the Global Assembly Cache (GAC). Although this library does not get updated often, placing it in the GAC could interfere with other applications that reference specific versions. This library is small enough that it can be easily included as part of your application's main distribution. This should help ensure that you don't get the wrong version whenever you deploy or upgrade your application.

Once you've added the library reference, you need to decide when you want to check for updates. This usually happens during the start-up sequence, when the application is first opened. In Cryptnos, we initiate the check as the last step of initializing the main form. When you decide when you want to perform the check, identify the corresponding code file and include the reference. For example, in C# you would use the `using` directive at the top of the file:

```c#

using com.gpfcomics.UpdateChecker;
```

# Initiating the Update Check #

The most likely place to perform the update check will be somewhere in your main form's constructor. Here's a C# example:

```c#

private UpdateChecker updateChecker = null;

public MyAppsConstructor()
{
try
{
// Your initialization code begins here...
InitializeComponent();
// ...
// Create the update checker instance:
updateChecker = new UpdateChecker.UpdateChecker(feedUri, appName,
currentVersion, this, lastUpdate, updateInterval, debug);
// Launch the update process:
updateChecker.CheckForNewVersion();
}
catch (Exception ex)
{
// The UpdateChecker may throw exceptions, so handle any errors here.
}
}
```

The `UpdateChecker` constructor takes several parameters:

  * A string or `System.Uri` object pointing to the location of your application's XML update feed.
  * A string that uniquely identifies your application in the XML feed in case the feed represents multiple applications. This must match the `<name>` entry in the `<app>` element of the feed.
  * The `System.Version` of the application. This is usually obtained from `System.Reflection.Assembly.GetExecutingAssembly().GetName().Version`.
  * A reference to the `IUpdateCheckListener` implementer who will be listening for updates from the update checker. Many times this will be the calling form itself (thus the use of `this` in the example above), but it doesn't have to be.
  * A `System.DateTime` specifying the last time an update check was performed. It is the main application's responsibility to store and keep track of this value. You can force an update check every time by setting this to some value far in the past, such as `DateTime.MinValue`.
  * An `Int32` value specifying the update check interval in days. This must be a positive integer greater than zero. Thus, if you properly keep track of the last update check date, subsequent update checks should occur only after this many days have passed. Seven days (one week) is a good default interval. Applications with frequent updates (like security applications) may wish to check daily, while those that change very infrequently may wish to check only once per month (every 30 days).
  * A Boolean flag indicating whether the update checker should be executed in debug mode. Debug mode forces raw exception messages to be displayed in error dialog boxes; with this mode turned off, "friendly" messages will be shown instead, and in some cases the error dialogs will not be displayed at all. Production releases should have this value turned off.

Once the `UpdateChecker` object has been constructed, call `CheckForNewVersion()` to launch the update check process. At this point, your application should not have to worry about much else. From the user's perspective, the workflow should continue on to whatever would normally be the next step. Any further interaction with the `UpdateChecker` object will occur through the `IUpdateCheckListener` interface methods.

Always include the `UpdateChecker` constructor and `CheckForNewVersion()` call inside a `try/catch` block. These methods may throw exceptions if called with invalid data. What you choose to do upon these errors is up to you, but whenever an exception is thrown here the update check will cease and the worker threads it launches will be halted.

# Implementing the IUpdateCheckListener Interface #

`IUpdateCheckListener` is an interface that allows the `GPFUpdateChecker.UpdateChecker` class to communicate events back to the main application. Each method needs to be implemented in order for the update check to process correctly. However, many of the implementations can be very simple or may even consist of an empty body if no interaction is necessary.

## OnFoundNewerVersion() ##

This method is called if the `UpdateChecker` successfully finds a new version of the application in the XML feed. At this point, the user has **not** been notified of this discovery. At a bare minimum, the listener should call `UpdateChecker.GetNewerVersion()` to move the process to the next step. Whether or not the listener wishes to perform any additional tasks is up to the developer.

## OnNoUpdateFound() ##

This method is called if the `UpdateChecker` successfully accessed the XML feed, but no updated version was found. The simplest and easiest implementation for this method is to do nothing; in this case, the update check will close silently and the user will never be notified that it even occurred. If, however, you wish to perform additional tasks, here is the place to do it.

One example use case is if the update check is user-initiated. Cryptnos performs its update checks in two places. The automatic check occurs on application start-up. During this check if no updates were found the update check closes silently and the user is not notified. In this case, the body for `OnNoUpdateFound()` is empty. In the second instance, a "Check for Updates" button appears in the settings dialog box. When the user clicks this button, a more interactive update check occurs. In this case, it makes sense to notify the user if no new update was found. This time, `OnNoUpdateFound()` displays a dialog notifying the user of this fact, then re-enables a button that was disabled when the update check started.

## OnUpdateCheckError() ##

This method is called if the `UpdateChecker` fails for any reason. The `UpdateChecker` handles many of its own errors with dialog boxes of its own, so it is perfectly safe to give this method an empty implementation, just like described under `OnNoUpdateFound()`. If, however, you wish to implement some additional functionality when the update check fails, this is the place to put it. You do not need to worry about the `UpdateChecker` itself when this is called; all of its worker threads will be closed automatically.

## OnRecordLastUpdateCheck() ##

This method is called by the `UpdateChecker` to ask the listener to save the new last update check `DateTime` value. Where the listener stores this data is up to the implementer; it could be a database, the Windows Registry, a flat text file, etc. However, this is the value that should be restored and passed back to the `UpdateChecker` constructor the next time the application launches.

## OnRequestGracefulClose() ##

This method is called sometime after `UpdateChecker.GetNewerVersion()`. At this point the user has been notified that an update is available, they have agreed to download and install the update, the file has been downloaded, and the installer is ready to run. When the user clicks the OK button in the notifying dialog, this method will be called to request that the listener close the main application gracefully so the installer can update its files.

The application should do whatever is necessary to save the user's settings and data then close the application itself. It is generally a bad idea to have the application running when the installer for the new version executes.

## OnDownloadCanceled() ##

This method is called sometime after `UpdateChecker.GetNewerVersion()`. At this point the user has been notified that an update is available, but they have declined the offer to download the new version. The simplest implementation for this method will be to do nothing, much like `OnNoUpdateFound()` above. If the listener needs to perform some task when this even occurs, however, this is the place to put that code.