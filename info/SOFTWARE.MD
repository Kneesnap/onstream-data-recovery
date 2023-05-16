# OnStream Compatible Software
After obtaining a compatible drive, the next challenge is software.  
In a best-case scenario, you determine the original backup software used to create the backup in order to restore the backup.  
Unfortunately, many people didn't realize they needed to document that (or aren't the people who made the backups), and don't know the original software used.  
If you can figure it out, great, use it! The guide ends here for you. The original software should restore the data.  
If the original software can't be identified, located,  or it doesn't work (LOOKING AT YOU ARCserve), continue reading after the software list.  

## Software List  
This list has been determined by grabbing data from the [Official List](https://web.archive.org/web/20030208023026/http://www.onstreamdata.com/software/index.asp).  
It is however, incomplete, it only includes data from software with confirmed information.  

### OnStream Echo / OnStream 1Safe  
 - The official software which comes with the drives.  
 - OS Compatibility: Windows 98, Windows NT, Windows 2000. Windows XP*  
    *The website claims Windows XP is supported, but I couldn't get it to work.  
 - Wasn't very popular.  
 - Compatible Drives: ALL EXCEPT MAC DRIVES (If your software version is new enough.)   

### ARCServe  
 - Known Compatible Versions: ARCServeIT >= version 6.61 build 834, ARCServe 2000, ARCServe v7.0 (Later versions may be compatible)  
 - OS Compatibility: Windows 98, Windows NT, Windows 2000, Windows XP  
 - Confirmed Compatible Drives: SC30/SC50, ADR30/ADR50
 - Data Pictures: [Picture #1](/pictures/data-formats/arcserve-header.png), [Picture #2](/pictures/data-formats/arcserve-datastart.png)
 
### Dantz Retrospect
 - The software recommended for use on Apple.  
 - The four character identifier which identifies this software in the tape aux header is "Rxvr".  
 - Backups created on Macintosh computers cannot be read on Windows by the official software.  
 - I wrote a program which can extract Apple format Retrospect Dantz 4.3 files from a raw tape dump, available [here](/software/RetrospectAppleTapeExtractor/).  

### ST (Linux Driver: Standard Tape)  
 - OS Compatibility: Works even in latest Debian/Ubuntu at time of writing.  
 - Compatible Drives: Everything including/after the ADR30/ADR50.  
 - Ships as 'st'  
 - Dump by using 'dd' on /dev/stX.  
 - Much more reliable than 'osst' for dumping data from arbitrary software. A viable alternative to my custom driver.  
 - Data Format: Raw Dump  

### onstream-raw-dump (Custom Linux Driver)  
 - OS Compatibility: Pretty much any Linux version which supports SCSI via `sg`.  
 - Compatible Drives: SC-30, SC-50  
 - Based on the `onstreamsg.cpp` program found in the `osst` repository, but upgraded to contain error detection.  
 - Built to be extremely reliable at one thing, dumping data from the OnStream drives.  
 - Data Format: Raw Dump  
 - Recommended for raw dumps for tapes which `st` cannot be used.  
 
### OSST (Linux Driver: OnStream Standard Tape)  
 - OS Compatibility: Linux Kernel 2.4.X (Recommended: Debian 3.1, Kernel >= 2.6.X throws kernel exceptions)  
 - Compatible Drives: SC30/SC50/SC50e/USB30/DI30/FW30
 - Ships as part of Linux Kernel sometime between 2018 and 2021.  
 - Activate by running 'sudo modprobe osst'.  
 - While it is quite buggy, it is at least open source.  
 - Data Format: Official ADR Specification    
 - Not recommended because of how buggy it is.  
  
  
There are more more programs which could write data to OnStream tapes, this is not a comprehensive list.  