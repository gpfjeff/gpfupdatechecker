# Project Status #

| **Current Release** | 1.0.0.0 |
|:--------------------|:--------|
| **Date of Release** | May 2010 |
| **Next Release Milestone** | 1.1.0.0 |
| **Date of Next Release** | Who knows...? |
| **Status of Development** | Sporadic |

# About GPFUpdateChecker #

GPFUpdateChecker is an Open Source .NET Framework 2.0 library for to enable other .NET applications to check for updates via the Internet. When included into another .NET project, the application can launch a separate process thread to silently download an XML file from a specific URL, parse it, and compare the version information within the file to the current version of the application. If the version in the file is greater, the user is prompted to download the installer for the new version and upgrade.

The GPFUpdateChecker library handles most aspects of this process with few interactions with the main application. All the application needs to do is implement a few interface methods and initiate the update check process when convenient. The update checker downloads and parses the XML file, prompts the user to download the update if one is available, downloads the file in the background, verifies the file integrity based on a cryptographic hash in the XML feed, and initiates the installation process. All of this occurs outside of the main application thread, allowing the user to continue to use the application in the foreground. If no update is found, the update checker silently exits without bothering the user.

This library was primarily created in support of the [Cryptnos for Windows](https://code.google.com/p/cryptnos-for-windows/) application, but was never included in that application's Open Source repository. We have since opened the code of this library to the public, so anyone can use it if they wish.
