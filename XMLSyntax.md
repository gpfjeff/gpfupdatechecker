# Introduction #

The GPFUpdateChecker library searches for and parses an XML feed specified during the construction of the `UpdateChecker` object. This feed should be stored somewhere on a Web server you control and must be accessible by any Web client. The `UpdateChecker` class will download this feed, parse it, and compare its contents with the data provided by the calling application. Once it has determined whether or not a new version of the application is available, the update check will proceed to its next step.

There is an XSD schema file included with the GPFUpdateChecker project. This XSD file should also be distributed with your application and should be kept in the same folder or directory as the `GPFUpdateChecker.dll` assembly file. This page provides a more user-friendly description of that schema syntax.

# Headers #

Near the top of the XML file are a few standard, required headers.

```xml

<?xml version="1.0" encoding="utf-8"?>
<gpfupdate xmlns="http://www.gpf-comics.com/">
<version>1</version>
<generator>Some application generated this</generator>
<comment>This is a test comment.</comment>
<pubDate>20100513140600</pubDate>
// ...
</gpfupdate>

```

The main body begins with the standard XML header and the schema reference for the `<gpfupdate>` tag. This tag contains the rest of the feed's body. There are a few required tags following the opening tag:

  * `<version>` is the version of the feed syntax. This must be a positive integer and, for now at least, must always be the number one (1). Any other value is currently considered invalid.
  * `<generator>` is a string value identifying the application that generated the XML feed file. Currently, this tag is optional and the GPFUpdateChecker ignores its value. It is still suggested to make the file self-documenting.
  * `<comment>` is a string value holding a human-readable comment. This tag is optional and ignored if present.
  * `<pubDate>` contains the publication date of the feed. This must be a date value in the following format: YYYYMMDDHHMMSS. Currently, this tag is also optional and ignored, but that may change in future versions.

## The `<apps>` Tag ##

After `<pubDate>` above there is a comment signifying something else should follow. The next section consists of the `<apps>` tag. There should be one and only one `<apps>` tag in the feed, but this tag may contain multiple `<app>` tags within it. See the following example:

```xml

<apps>
    <app>
        <name>Application 1</name>
        <currentVer>1.0.0.0</currentVer>
        <url>http://www.example.org/SomeInstallerFile1.exe</url>
        <size>783850</size>
        <digest>Dy7ubCve1vAMdqfnkCMm8yHCENBtAoXDQlKWV+yt6X0=</digest>
    </app>
    <app>
        <name>Application 2</name>
        <currentVer>2.3.4.5</currentVer>
        <url>http://www.example.org/SomeInstallerFile2.exe</url>
        <size>783850</size>
        <digest>Dy7ubCve1vAMdqfnkCMm8yHCENBtAoXDQlKWV+yt6X0=</digest>
    </app>
</apps>

```

The GPFUpdateChecker XML feed syntax is intended to allow multiple applications (or multiple versions of the same application targeted at different platforms) to share the same feed file. Each separate application should have its own `<app>` section, and the `<name>` for each application must be unique.

For example, the intended use case for the multiple `<app>` tags was to allow [Cryptnos](http://www.cryptnos.com/) for Windows and Cryptnos for Java to both share the same XML feed. Each application would have its own `<app>` section. If the `<name>` in an `<app>` section does not match the name provided to the `UpdateChecker` constructor, that `<app>` section will be ignored.

## The `<app>` Tag ##

Each `<app>` section must consist of the following tags:

  * `<name>` uniquely identifies an application within the file. As stated above, this must match the name provided to the `UpdateChecker` constructor for the information to be found. Each application in the feed must have a unique `<name>` value.
  * `<currentVer>` is version number of the most up-to-date version of the application. This will be compared against the version stored internally within the parent application when the update check is performed. This should be in the semi-standard "major.minor.sub-minor.revision" format as you would see if you called `System.Version.ToString()`.
  * `<url>` is a string representing the URL where the installer for the latest version of the application can be downloaded from. This may be any protocol `System.Net.WebClient.OpenRead()` can read. Note that the file at this URL must be accessible in order for the update to succeed; if the updater cannot find or read this file, the update check will fail.
  * `<size>` represents the size in bytes of the installer referenced in the `<url>` tag. This must be a positive integer greater than zero (0) and should not have any spaces, commas, periods, or other formatting. Note that this should be the actual size of the file itself; some operating systems such as Windows may also provide a "size on disk" value that does not represent the file's true size. Abbreviations for common multipliers (5.2MB, 3.0GiB, etc.) are not permitted; use the raw, precise number of bytes. This value will be used to display the download progress, as well as the verify that the download was complete and successful. Thus, it must be accurate.
  * `<digest>` is a Base64-encoded SHA-256 hash of the installer referenced in the `<url>` tag. This will be used to verify that the download is complete and successful. The SHA-256 hash of the downloaded file will be compared to the value from the feed. If the hashes match, the installation process will proceed; if they do not match, an error will be displayed.
