# OnStream Data Recovery  
OnStream tapes saw use primarily as a backup storage medium during the early 2000s mostly in Europe due to their affordability.  
Unfortunately, OnStream ceased operations due to bankruptcy in 2003 and never had the longevity that other tapes had.  
Not only are the drives rare today, but they are also prone to failure and have numerous software and compatibility difficulties which make it annoying to dump the contents of the tape.  
So when I was put in a position of needing to recover data from OnStream tapes, I managed to recover data.  
Here's what I learned along the way. Hopefully this will help someone else.  
If you need to contact me for help, check the Troubleshooting section.  

### General Information  
 - SC50 is the best first-gen ADR drive. This is because using the custom driver it can read data from all of the first-gen tape drives.  
 - SC30 cannot read ADR50 tapes. [[[TODO: Will investigate if the driver can do anything to bypass this.]]]  
 - ADR tapes cannot be used in ADR2 tape drives, and ADR2 tapes cannot be used in ADR drives. This cannot be fixed with different firmware due to physical differences in the tape.  
 - The maximum IDE cable length supported by the IDE drives is 18 inches. It is likely there would be errors if a longer one was used.  
 - The cleaning cartridge is supposed to be used every 90 days. However, none of the drives I've seen needed cleaning, despite having not been used in at least a decade or two. It is speculated that isoprobyl alcohol on the tape head would do the job if it were necessary, but this is speculation.  
 - How does this jumper block thing work? Don't worry too much about it. Most of the SCSI PCI cards out there have an automatic termination setting enabled by default in the BIOS settings, and if you're not connecting any other SCSI devices, then it doesn't matter what ID is set.  
 - It is possible to use a 68pin to 50pin SCSI converter for the SCSI-2 based devices, but only if the converter properly terminates the 8-bit portion of the bus. Any competent adapter should do this automatically.  


## Dumping Guide  
Dumping data from OnStream tapes is difficult for several reasons.  
 1) Locating a *compatible* OnStream tape drive is difficult.  
 2) Determining and locating the original software used to write the backup is difficult.  

OnStream drives are rather rare, and it's somewhat difficult to find drives, let alone a drive which is compatible with your tape.  
Check out the drive list in order to determine which drives are compatible with which tapes.  
Assuming you have a working drive which can read the data on your tape, you have two options.  
  
To fully understand what to do, we need to understand that OnStream set it up so that any software could write data to the tape however it liked.  
Because so many different software configurations were compatible with the OnStream tape drives, there are many different formats the data could have been saved in.  
While this meant there was widespread support, it also means that we can't just create a one size fits all tool which will recover the data in the original form.
Because of this, we have two options.


### #1) Use the original software and an officially compatible drive. (Difficulty: Very Hard)  
Because of the sheer number of compatible software, this option is difficult. You'll need to not only determine what the original software was, but also obtain it.  
Assuming you can do that and get a system working, and the software can read the data, this might yield the best results, since you'll likely get actual files & folders as an output, in the original form which they were saved in.  
This process is involved, and basically what you'll want to do is follow the steps for #2, and then look at the output in a hex editor and hope it tells you which software was used to write it.  
Perhaps you might know which software was used from when the tape is written, or you could ask someone who did know.  
It's not possible to give individual guidance for each option here, as I've only seen a couple data formats.  

### #2) Use my custom Linux driver to dump the raw data. (Difficulty: Easier)  
This option is generally preferred because it is guaranteed to get you the data.  
It will generate a couple of very large file, which in turn contain the tape data / files on the tape.  
Unfortunately, you may need to use a hex editor to use to copy out the file data which you want.  
Those who have programming experience might want to make a script to automate this.  
If there's any compression, that might necessitate figuring out what the original software was.  

I've created a usermode Linux driver which can dump tapes.  
It's been tested with the ADR50, SC50, and SC30, but is expected to work with other drives. [[[TODO: Elaborate]]]  
This only supports the SCSI drives, but perhaps in the future it could be extended to support the others.  
  
#### Steps  
#1) Get a machine which the SCSI drive can connect to.  
#2) Install any distro of Linux which you'd like.  New, old, as long as it has SCSI support still. Ubuntu or Debian is recommended for those inexperienced. (I know Ubuntu 18 has this, don't know if anything newer does, but I'd imagine so)  
#3) Download the driver source code, and run 'sudo sh compile.sh' in the folder you saved it to.  
#4) If compilation fails, try another distro, or if you're a developer I trust you can figure out how to fix the build.  
#5) [[[TODO: How to do a raw dump.]]]  
#6) Look at the output file data in a hex editor and figure out what you need to do next. It will differ based on what software was used. If you need to ask me for help, check the Troubleshooting section for info on how to contact me.  

## Tape Information  
Each tape has 8 channels, read by an 8-channel thin film head. 
Tape: 8MM, some linear recording format (Not helical scan)

**ADR30:** [Picture #1](/pictures/tapes/adr30-1.jpg), [Picture #2](/pictures/tapes/adr30-2.jpg), Physical Capacity: 15GB  
**ADR50:** [Picture](/pictures/tapes/adr50-1.jpg) Physical Capacity: 25GB  
**ADR2.60:** [Picture](/pictures/tapes/adr2.60-1.jpg) Physical Capacity: 30GB  
**ADR2.120:** [Picture](/pictures/tapes/adr2.120-1.jpg) Physical Capacity: 60GB  
ADR stands for Advanced Digital Recording.  

Yes, despite advertising double the capacity of the device, these tapes were advertised under the assumption that all data was compressed with a 2:1 compression ratio.
The original tape drives didn't even support hardware compression. In order to get compression, you had to use OnStream Echo and manually enable compression, since it is disabled by default.
This is especially awful because of how uncommon it was for people to use the bundled software.  

## OnStream Tape Drives  
Check out [DRIVES.MD](DRIVES.MD) for information about each tape drive model.  

## Software Compatibility  
Check out [SOFTWARE.MD](SOFTWARE.MD) for information on all of the different types of software which supported OnStream tape drives.  

## Troubleshooting  
Feel free to add me on discord or email me, both of those are available on my GitHub profile. Can find me in the [Highway Frogs](https://highwayfrogs.net/) discord too.  

### Front-Panel LED Status Codes  
The front-panel LED isn't just there for looks, it's constantly telling you what the drive is doing.  
Understanding the LED can be crucial for understanding the drive.  

```
The LED blinks when the drive door is open, and speeds up if it's getting ready to shut the door.

If the LED door is not open:
Light Off             - Idle (No Operation)
Light On (SOLID)      - Read/Write Operation
Light Blinking On/Off - Seek Operation (Fast Forward / Rewind / Move to new tape position)


Sometimes, morse code is repeatedly flashed to convey an error code. 4 flashes will display repeatedly, indicating either a problem with the drive or the tape.
The drive is expected to be quiet / not moving the tape when a LED error code is flashing.
In the list shown below, a long flash represents the character 'L' and a short flash represents 'S'.

LED Error Patterns:
S-S-S-S: Tape Header Failure (Tape Sensor)
S-S-S-L: Write Circuit IC Failed (Chip ?)
S-S-L-S: Timeout Error
S-S-L-L: Read Circuit IC Failed (Chip ?)
S-L-S-S: L_Sense Regulation Error
S-L-S-L: Servo Identity Timeout
S-L-L-S: Calibration Failure
S-L-L-L: Dummy Frame Error
L-S-S-S: Lock Dummy Frame Error
L-S-S-L: WPFail Dummy Frame Error
L-S-L-S: Cmd Control Comm Error
No other error codes exist.
```

### Drive always automatically ejects tape.  
It is believed this is some kind of hardware problem. Likely it can't read any of the data on the tape, and must eject it.  
In the situations I have encountered this, it appears to have been a motherboard problem, and the sensors / mechanical parts of the tape were functioning properly.  
It's also possible the drive may mistake the tape as being a cleaning tape.  

### Drive appears to accept the tape, but reading data throws an IO error.  
This has happened to me when using an incompatible tape. For example, putting an ADR50 tape into a SC30 drive.  

### Cannot Eject Tape  
Ejecting is more of a suggestion than a command for these drives. These drives will always finish what they're currently working on before ejecting.  
Give it some time to finish the current operation. If it doesn't eject, the device is probably softlocked. Shut off the PC. Unplug the SCSI connector into the card, but keep the power on.  
Now, when the system is on / the device has power, but there's no SCSI connection, you should be able to eject without any kind of soft lock possibility.  
If this doesn't work, the drive is probably broken.  

### Drive continually resets and makes a clicking noise.  
This issue seems to go away when I try a different PC motherboard.  
Not a clue what's wrong with the first motherboard, but this motherboard sometimes also doesn't show the SCSI PCI card boot screen, and that has all sorts of issues.  
So perhaps motherboards can cause weird issues like this one.  

## Helpful Resources  
 - [OnStream Website Archive](https://web.archive.org/web/20030130111054/http://www.onstreamdata.com/)  
 - ['osst' Driver Website](https://osst.sourceforge.net/)  
 - ['osst' Driver Source Code](https://github.com/Kneesnap/osst)  
 - [Aaru Data Preservation Suite](https://github.com/aaru-dps/Aaru)  

## TODO: Future Driver Ideas  
 - Does raw mode work with normal 'st' on the ADR50?  
 - Is it possible to disable compression on the ADR50 and read raw?  
 - osst 0.99.2 added support for #ls /sys/class/osst/osst0 Poke around that.  
 - Play with user mode driver. [os_dump.c]  
 - Try modifying osst to accept CSSW and MTST as valid headers in the place of LIN4. Perhaps it will just work?  
 - The specsheet has a command listed 3C as READ BUFFER. Is it possible we could raw dump the buffer?  
 - Do these commands work with the ADR50? What about the SC30?  
 - When dumping tapes written with an ADR50 drive, let's reverse engineer the compression algorithm. It's in the firmware, it could also be in OnStream echo. (Try writing the same data with Echo compressed vs with the ADR drive and see the output on the raw dump)  
 - After reverse engineering the algorithm, add it to the user driver.  
 - According to the website, QIC-172 is used for the SC50 in terms of the tape writing protocol, where ADR-30 is QIC-157.  
 - Reverse engineer firmware using information found from spec sheet.  
 - Hack firmware to allow SC30 to read SC50 tapes.  
 - Port dumping software to Aaru?  
