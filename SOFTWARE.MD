# OnStream Compatible Software  
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

## onstream-raw-dump (My Linux Driver)  
 - OS Compatibility: Pretty much any Linux version which supports SCSI.  
 - Compatible Drives: [[[TODO: NOT SURE YET]]] (Other Drives may be compatible, these are just what was tested)  
 - Built to be extremely reliable at one thing, dumping data from the OnStream drives.  
 - Data Format: Whatever data is on the tape.  
 - TODO: Does it bypass anything like the SC30 crap?  