## Troubleshooting  
Feel free to add me on discord or email me, both of those are available on my GitHub profile.  
Can find me in the [Highway Frogs](https://highwayfrogs.net/) discord too.  

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
The one recommendation is to clean the tape head sensor with rubbing alcohol and a Q-tip.  
If that doesn't help, most likely the issue is hardware, and it cannot easily be fixed. In theory the drive might be treating the tape as a cleaning tape, but even as such there might not be anything which can be done.

### Drive appears to accept the tape, but reading data throws an IO error.  
This has happened to me when using an incompatible tape. For example, putting an ADR50 tape into a SC-30 drive.  

### Cannot Eject Tape  
Ejecting is more of a suggestion than a command for these drives. These drives will always finish what they're currently working on before ejecting.  
Give it some time to finish the current operation. If it doesn't eject, the device is probably softlocked. Shut off the PC. Unplug the SCSI connector into the card, but keep the power on.  
Now, when the system is on / the device has power, but there's no SCSI connection, you should be able to eject without any kind of soft lock possibility.  
If this doesn't work, the drive is probably broken, but there are physical ways to cause an eject. On the SC-50, under the drive is a track which if you put a Q-tip in and push, it will eject the tape mechanism.  

### Drive continually resets and makes a clicking noise.  
This issue seems to go away when I try a different PC motherboard.  
Not a clue what's wrong with the first motherboard, but this motherboard sometimes also doesn't show the SCSI PCI card boot screen, and that has all sorts of issues.  
So perhaps motherboards can cause weird issues like this one.  

## Helpful Resources  
 - [OnStream Website Archive](https://web.archive.org/web/20030130111054/http://www.onstreamdata.com/)  
 - ['osst' Driver Website](https://osst.sourceforge.net/)  
 - ['osst' Driver Source Code](https://github.com/Kneesnap/osst)  
 - [Aaru Data Preservation Suite](https://github.com/aaru-dps/Aaru)  