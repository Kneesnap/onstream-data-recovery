# OnStream Tape Drives
OnStream has many tape drives. This file attempts to detail them all.  

## Which drive do I need?
OnStream released many different tape drives. Unfortunately, compatibility between different drives and software is not documented officially.  
So, it is up to us to determine compatibility. The best drive to use is the drive which was originally used to write the data to the tape.  
Most likely though, you either have no idea what drive was used or can't find an exact match, and this guide is going to assume you do not have the original drive and the original software.  

Let's start by identifying what tape you have, and use that to narrow down your options.  

## Different Tapes  
Each tape has 192 tracks, read by an 8-channel thin film head.  
Tape: 8MM, some linear recording format (NOT helical scan)  
There are different kinds of tapes available. It seems that tapes were split by capacity.  
Meaning, any tape with the specified capacity, regardless of brand is the same as any other tape of that capacity.  

**ADR-30:**  
 - **Generation:** 1  
 - **Capacity:** 30GB (As Advertised/Compressed), 15GB (Physical Capacity)  
 - **Write Compatibility:** SC-30, SC-50, DI-30, DI-30 FAST, SC30e MAC, FW-30, DP-30, ADR-30, ADR-50, USB-30  
 - **Read Compatibility:** SC-30, SC-50, DI-30, DI-30 FAST, SC30e MAC, FW-30, DP-30, ADR-30[^1], ADR-50[^1], USB-30  
 - **Photos:** [Example #1](/pictures/tapes/adr30-1.jpg), [Example #2](/pictures/tapes/adr30-2.jpg)  

**ADR-50:**  
 - **Capacity:** 50GB (As Advertised/Compressed), 25GB (Physical Capacity)  
 - **Generation:** 1  
 - **Write Compatibility:** SC-30[^2], SC-50, ADR-50  
 - **Read Compatibility:** SC-30[^2], SC-50, ADR-50[^1]  
 - **Photos:** [Example](/pictures/tapes/adr50-1.jpg)  
 
**ADR2.60:**  
 - **Generation:** 2  
 - **Capacity:** 60GB (As Advertised/Compressed), 30GB (Physical Capacity)  
 - **Write Compatibility:** ADR2.60s, ADR2.60IDE, ADR2.60usb, ADR2.120s, ADR2.120IDE, ADR2.240Sr  
 - **Read Compatibility:** ADR2.60s, ADR2.60IDE, ADR2.60usb, ADR2.120s, ADR2.120IDE, ADR2.240Sr   
 - **Photos:** [Example](/pictures/tapes/adr2.60-1.jpg)  

**ADR2.120:**  
 - **Capacity:** 120GB (As Advertised/Compressed), 60GB (Physical Capacity)  
 - **Generation:** 2  
 - **Write Compatibility:** ADR2.120s, ADR2.120IDE, ADR2.240Sr  
 - **Read Compatibility:** ADR2.120s, ADR2.120IDE, ADR2.240Sr  
 - **Photos:** [Example](/pictures/tapes/adr2.120-1.jpg)  
 
[^1]: The "ADR-" prefixed drives do not seem to read data written by "SC-" prefixed drives. This may be possible to change with firmware modification.
  The inverse is untrue, for example, if data is written with an "ADR-" prefixed drive, it CAN be read by a "SC-" prefixed drive.
  However, the data which is read is compressed. This compression format has not been reverse engineered, but is likely an Lz77 variant, and may be ADLC.
  This is still good, if you can get your data, I can help out with reverse engineering the compression. Once done I will update this section.
[^2]: I have determined a method of turning a SC-30 into a SC-50. This is good because SC-30 devices seem to be plentiful compared to SC-50s.
  TODO: I haven't actually written the code to do this yet. Do that and update this section linking to a readme in a folder with a program to do it.

Now that you've identified your tape(s), it's time to determine what drives you can use to read data from it.  
Using the above information, you can see which tape drives can be used with the tape you have.  
In the case of 30 or 50GB tapes, if it was written with a "SC-" prefixed drive, it must be read with a "SC-" prefixed drive, not an "ADR-" one.  
However, the "SC-" prefixed drives are able to read data written with "ADR-" drives.

## Drive List

| Drive | Release Date | Interface | Advertised Speed | Hardware Compression | Command Specification | Photos |
| ------------- | ------------- | ------------- | ------------- | ------------- | ------------- | ------------- |
| SC-30 | May 19, 1999 | SCSI-2 (Internal, 50 Pin) | 2 MB/s | No | QIC-172 | [Front](/pictures/drives/sc50-front.png), [Back](/pictures/drives/sc50-back.png) |
| SC-50 | May 19, 1999 | SCSI-2 (Internal, 50 Pin) | 2 MB/s | No | QIC-172 | [Front](/pictures/drives/sc50-front.png), [Back](/pictures/drives/sc50-back.png) |
| DI-30 | May 19, 1999 | Internal IDE | 0.7 MB/s | No | ? | [Front](/pictures/drives/adr-di30-front.png), [Back](/pictures/drives/adr-di30-back.jpg), [Top](/pictures/drives/adr-di30-top.jpg) |
| DI-30 FAST | May 19, 1999 | Internal IDE | 2 MB/s | No | ? | [Front](/pictures/drives/sc50-front.png), [DI-30 Fast](/pictures/drives/adr-di30-fast.jpg) |
| DP-30 | May 19, 1999 | External Parallel Port | 0.7 MB/s | No | ? | [Front](/pictures/drives/sc50-front.png), [Back](/pictures/drives/adr-dp30.jpg) |
| SC30e MAC[^3] | February 2000 | External SCSI | 2 MB/s | No | ? | [Rotated](/pictures/drives/sc30e.jpg) |
| FW-30 | ? | External Firewire (Apple) | 2 MB/s | No | ? | [FW-30](/pictures/drives/adr-fw30.jpg) |
| ADR-30 | February 2000 | Ultra2 Wide SCSI 68 Pin | 2 MB/s | Yes | QIC-157 | [ADR30](/pictures/drives/adr30.png) |
| ADR-30e | February 2000 | Ultra2 Wide SCSI 68 Pin | 2 MB/s | Yes | QIC-157 | None |
| ADR-50 | February 2000 | Ultra2 Wide SCSI 68 Pin | 2 MB/s | Yes | QIC-157 | [ADR50](/pictures/drives/adr50-render.jpg) |
| ADR-50e | February 2000 | Ultra2 Wide SCSI 68 Pin | 2 MB/s | Yes | QIC-157 | [ADR50e](/pictures/drives/adr50e.png) |
| USB-30 | March 2000 | USB 1.0 | 0.85 MB/s | No | ? | [USB-30](/pictures/drives/adr-usb30.jpg) |
| ADR-70 | Announced: June 16th, 1999 (Unreleased?) | ? | 2 MB/s | ? | ? | None |
| ADR2.60ide | August 31, 2001 | IDE | 2.5 MB/s | No | QIC-157 | [Picture](/pictures/drives/adr2.60ide.jpg) |
| ADR2.60usb | ? | USB 2.0 | 2.5 MB/s | No | QIC-157 | [Picture](/pictures/drives/adr2.60usb.jpg) |
| ADR2.60si | July 1, 2002 | Ultra2 Wide SCSI 68 Pin | 4 MB/s | No | QIC-157 | [Picture](/pictures/drives/adr2.60si-1.jpg) |
| ADR2.60se | July 1, 2002 | Ultra2 Wide SCSI 68 Pin | 4 MB/s | No | QIC-157 | [Picture](/pictures/drives/adr2.60se-1.jpg) |
| ADR2.120si | July 1, 2002 | Ultra2 Wide SCSI 68 Pin | 4 MB/s | No | QIC-157 | [Picture](/pictures/drives/adr2.120si-1.jpg) |
| ADR2.120se | July 1, 2002 | Ultra2 Wide SCSI 68 Pin | 4 MB/s | No | QIC-157 | [Picture](/pictures/drives/adr2.120se-1.jpg) |
| ADR2.240sr[^4] | December 4, 2002 | Ultra2 Wide SCSI 68 Pin | 2x4 MB/s[^4] | No | QIC-157 | [Front](/pictures/drives/adr2.60sr-front.jpg), [Back](/pictures/drives/adr2.60sr-back.jpg) |
| ADR2.120ide | March 2003 | IDE | 2.5 MB/s | No | QIC-157 | [White](/pictures/drives/adr2.120ide-1.jpg) [Black](/pictures/drives/adr2.120ide-2.jpg) |

[^3]: It is not clear why there was a separate SCSI drive released for Apple machines. It is unclear if it is possible to make other drives work with these old Apple machines or not.
  It seems the recommended software was Dantz Retrospect Backup, it is unknown what software may have been used beyond it.
  The drive is not supported in Mac OS X by Retrospect, but may still be supportable by porting the `os_dump` program.
[^4]: This appears two ADR2.120Si drives put together into a 19-inch rack mount tape.