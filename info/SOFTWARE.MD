# OnStream Compatible Software
After obtaining a compatible drive, the next challenge is software.  
In a best-case scenario, you can use the original backup software used to create the backup in order to restore the backup.  
Unfortunately, many people didn't realize they needed to document that, and don't know the original software used.  
If you can figure it out, great, use it! The guide ends here for you since different backup software worked differently, but you should get your files in a usable form.  
If that's not gonna work, then we're going to do something a bit more involved.  


Our other option is to do a raw dump of the tape contents, into a large file.   

**Setup:**  
You'll need a machine running Linux. The distro doesn't matter, as long as it has `sg` installed.
Personally, I use "Arch Aaru" (From the Aaru Data Preservation Suite) for this, and it's easy to setup a bootable USB.
But, this is also true about Ubuntu. It seems some of the latest versions of Ubuntu do not come with sg, but 18.04 LTS should be OK.  
`sg` is ancient, and has been in the Kernel for a long time, so probably any mainstream Linux distro between 1999 - 2020 should have it. I don't know if (or when) 'sg' started getting removed.  
The way you can check is if you run `ls /dev/` and you see devices matching the pattern 'sgX', for example: `/dev/sg0`.  

**Performing a raw dump:**  
Using the `onstream-raw-dump` tool, you can do a full dump of the tape. Instructions are included in the `software` folder.  


**Making the data usable:**  
Now that you have your data dumped from the tape, there's one major problem remaining.  
If you want to do anything with your data, you need to turn it into something usable.
For example, if your backup had a bunch of files in it, you'll want to turn your single large tape dump file into something like a zip file which contains your files.  
Unfortunately, this does depend on the software which wrote the backup. Different software will write data however it likes.  
There's no one-size fits all here for making your data usable.  
If you have some programming experience, you can crack open the file in a hex editor, and try to understand how the data is formatted, and write a program to extract the data.  
But I'd imagine most people don't, and in that case you have two options:  
 - #1) Find someone like me who can help. This is a pretty obscure thing, most programmers might not even know how to begin on this.  
 - #2) Use a hex editor and see if you can find obvious things like file names, and see if you can copy paste out the parts which look like file data.  


# Software List  
This list has been determined by grabbing data from the [Official List](https://web.archive.org/web/20030208023026/http://www.onstreamdata.com/software/index.asp).  
It is however, incomplete, it only includes data from software with confirmed information.  

## OnStream Echo / OnStream 1Safe  
 - The official software which comes with the drives.  
 - OS Compatibility: Windows 98, Windows NT, Windows 2000. Windows XP*  
    *The website claims Windows XP is supported, but I couldn't get it to work.  
 - Wasn't very popular.  
 - Compatible Drives: ALL EXCEPT MAC DRIVES (If your software version is new enough.)   

## ARCServe  
 - Known Compatible Versions: ARCServeIT >= version 6.61 build 834, ARCServe 2000, ARCServe v7.0 (Later versions may be compatible)  
 - OS Compatibility: Windows 98, Windows NT, Windows 2000, Windows XP  
 - Confirmed Compatible Drives: SC30/SC50, ADR30/ADR50
 - Data Pictures: [Picture #1](/pictures/data-formats/arcserve-header.png), [Picture #2](/pictures/data-formats/arcserve-datastart.png)

## ST (Linux Driver: Standard Tape)  
 - OS Compatibility: Works even in latest Debian/Ubuntu at time of writing.  
 - Compatible Drives: Everything including/after the ADR30/ADR50.  
 - Ships as 'st'  
 - Dump by using 'dd' on /dev/stX.  
 - Much more reliable than 'osst' for dumping data from arbitrary software. A viable alternative to my custom driver.  
 - Data Format: Arbitrary  

## OSST (Linux Driver: OnStream Standard Tape)  
 - OS Compatibility: Linux Kernel 2.4.X (Recommended: Debian 3.1, Kernel >= 2.6.X throws kernel exceptions)  
 - Compatible Drives: SC30/SC50/SC50e/USB30/DI30/FW30
 - Ships as part of Linux Kernel sometime between 2018 and 2021.  
 - Activate by running 'sudo modprobe osst'.  
 - While it is quite buggy, it is at least open source.  
 - Not recommended because of how buggy it is.  
 - Data Format: Arbitrary  
 
## Dantz Retrospect
 - The software recommended for use on Apple.  
 - The four character identifier which identifies this software in the tape aux header is "Rxvr".  
 - Backups created on Macintosh computers cannot be read on Windows by the official software.  
 - I wrote a program which can extract Apple format Retrospect Dantz 4.3 files from a raw tape dump, it has been included in the `software]` folder.  

## onstream-raw-dump (My Linux Driver)  
 - OS Compatibility: Pretty much any Linux version which supports SCSI via `sg`.  
 - Compatible Drives: SC-30, SC-50  
 - Based on the `os_dump.c` program found in the `osst` repository, but upgraded to contain error detection.  
 - Built to be extremely reliable at one thing, dumping data from the OnStream drives.  
 - Data Format: Whatever data is on the tape.  