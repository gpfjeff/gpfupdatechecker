# Introduction #

GPFUpdateChecker is an Open Source .NET Framework 2.0 library for to enable other .NET applications to check for updates via the Internet. When included into another .NET project, the application can launch a separate process thread to silently download an XML file from a specific URL, parse it, and compare the version information within the file to the current version of the application. If the version in the file is greater, the user is prompted to download the installer for the new version and upgrade.

The GPFUpdateChecker library handles most aspects of this process with few interactions with the main application. All the application needs to do is implement a few interface methods and initiate the update check process when convenient. The update checker downloads and parses the XML file, prompts the user to download the update if one is available, downloads the file in the background, verifies the file integrity based on a cryptographic hash in the XML feed, and initiates the installation process. All of this occurs outside of the main application thread, allowing the user to continue to use the application in the foreground. If no update is found, the update checker silently exits without bothering the user.

The GPFUpdateChecker is released under the GNU Public License, version 2, and is free to use by anyone who wishes to do so.

# History #

This library was primarily created in support of the [Cryptnos for Windows](https://code.google.com/p/cryptnos-for-windows/) application. Given that Cryptnos is a security application, I wanted a way to automatically check for updates so users could be notified quickly when a new version came out. I couldn't find anything to do what I wanted online, so in the grand Open Source tradition I rolled my own solution.

In the same spirit of openness that led me to Open Source Cryptnos, I wanted to release the code to the update checking library as well. Unfortunately, it did not seem natural to release it as part of the Cryptnos project. The update checker was in many ways a standalone product implemented as a separate library and itself consisted of a couple of sub-projects (the library itself and a reference/test application that implements the library interface). Eventually I released Cryptnos with the intent of releasing the update checking library separately, but time got away from me and I forgot all about releasing the library at the same time. Several versions of Cryptnos came and went, leaving the update checker unintentionally closed source.

While the first version (1.0) of the library served Cryptnos well for several versions, I eventually had to make a few changes to improve some of its error handling. During that process, I remembered my personal pledge to release the code as Open Source and finally made the commitment to setting up the Google Code site and checking the code into a repository.

I eventually plan to include this library in all of my Open Source .NET-based projects, such as [WinHasher](https://code.google.com/p/winhasher/).