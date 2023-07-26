# OnStream Raw Dump
Here contains two Linux-only tape dumping programs for OnStream SC-30/SC-50 tape drives. They may be compatible with other OnStream drives too.  
They rely upon the [sg Linux SCSI driver](https://sg.danny.cz/sg/) to talk to the drive, which has been in the Linux kernel since 2.2.6 (April 1999).  
There are two programs, `os_dump.c` and `onstreamsg.cpp`. Both are modified versions of programs found in the `osst` repository.  

## onstreamsg.cpp
This is the primary program for performing "raw dumps" of the tape data.  
**Compilation:** `g++ -g ./onstreamsg.cpp -o osg`  

### Usage
```
onstreamsg: SCSI Generic OnStream Tape interface. Written by Terry Hardie, upgraded by Kneesnap
usage: onstreamsg -n <device_num> [-d [level]] [-f filename] [-p] [-s block] [-e block] [-i increment]
  -n device    The device number is described below. *
  -d [level]   set debug logging level (Numeric), optional
  -l filename  write debugging output to named file, optional
  -f filename  Use named file for data output, optional
  -p           Use physical addressing format. **
  -s block     start reading from the provided block
  -t           retention (apply equal tension throughout tape, reducing likelihood of read errors)
  -e block     stop reading once the provided block is reached. ***
  -i increment The amount of blocks to skip when End of Data (EOD) is seen, optional

*   The device number is not the SCSI ID number, but rather which numbered device in
    the bus this device is. For example, if you have a hard drive at ID 2,
    and your OnStream drive at ID 5, then this value should be 1 (0 is the
    hard drive. Look at the devices under /dev/sgX where X identifies your tape drive.

**  Reading the parking zone (only known to have data in tapes written by ARCserve) will change
    any blocks specified (such as for -s) to read hexadecimal numbers instead.
    
    Physical blocks are addressed via a 32 bit integer, but not one which starts counting from zero.
    The first byte represents the vertical tape position of the physical data. 0 <= y <= 23
    The final two form a 16-bit number representing the horizonal tape position. 0 <= x < 31959
    Example: 05000120, Y = 0x05 (5), X = 0x120 (288)
    
    Because these positions map directly onto a physical portion of tape, and cover the
    entire tape, they can be used to read from inaccessible parts, such as the \"parking zone\".
    The parking zone is a portion of the tape which the drive leaves the tape in when ejected,
    and it is NOT supposed to have any data written there. However, some software such as 
    ARCserve writes data there, so we need to support dumping it.
    NOTE: Only the 50GB tapes have this parking zone, do not use this setting for 30GB tapes.
    
*** If -e is provided with physical addressing enabled, the value is instead treated as a count of the number of blocks to read, instead of as a position.
```

**-n**

## os_dump.c
This program is the simplest program available which can just dump data from the drive.  
It is very simple and to the point, and is better suited for debugging. Usage of the other program is recommended.   