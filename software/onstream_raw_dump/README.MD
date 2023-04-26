# OnStream Raw Dump
Here contains two Linux-only tape dumping programs for OnStream SC-30/SC-50 tape drives. They may be compatible with other OnStream drives too.  
They rely upon the [sg Linux SCSI driver](https://sg.danny.cz/sg/) to talk to the drive, which has been in the Linux kernel since 2.2.6 (April 1999).  
There are two programs, `os_dump.c` and `onstreamsg.cpp`. Both are modified versions of programs found in the `osst` repository.  

## onstreamsg.cpp
This is the primary program for performing "raw dumps" of the tape data.  
**Compilation:** `g++ -g ./onstreamsg.cpp -o osg`  

### Usage
TODO: Document  

## os_dump.c
This program is the simplest program available which can just dump data from the drive.  
It is very simple and to the point, and is better suited for debugging. Usage of the other program is recommended.   