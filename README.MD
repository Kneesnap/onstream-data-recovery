# The OnStream Data Recovery Project  
OnStream tapes saw use primarily as a backup storage medium during the early 2000s mostly in Europe due to their affordability.  
Unfortunately, OnStream ceased operations due to bankruptcy in 2003 and never had the longevity that other tapes had.  
Not only are the drives rare today, but they are also prone to failure and have numerous software and compatibility difficulties which make it annoying to dump the contents of the tape.  
So when I was put in a position of needing to recover data from OnStream tapes, I ended up doing quite a lot to recover data.  
I've put together this guide so that hopefully someone else doesn't also need to spend months on this.  
I am available to help with OnStream data recovery for the current time, my contact info is on my GitHub profile page.  

## Dumping Guide  
The guide has been broken into several sections:  
 1) [What drive do you need?](/info/DRIVES.MD)
 2) [How do you get your data?](/info/SOFTWARE.MD)
 3) [Troubleshooting](/info/TROUBLESHOOTING.MD)

### General FAQ  
 - How does this jumper block thing work? Don't worry too much about it. Most of the SCSI PCI cards out there have an automatic termination setting enabled by default in the BIOS settings, and if you're not connecting any other SCSI devices, then it doesn't matter what ID is set.  
 - It is possible to use a 68pin to 50pin SCSI converter for the SCSI-2 based devices, but only if the converter properly terminates the 8-bit portion of the bus. Any competent adapter should do this automatically.  
 
 
 ## Helpful Resources  
 - [Archived Data](/archives/)
 - [OnStream Website Archive](https://web.archive.org/web/20030130111054/http://www.onstreamdata.com/)  
 - ['osst' Linux Driver Website](https://osst.sourceforge.net/)  
 - ['osst' Driver Source Code](https://github.com/Kneesnap/osst) ([Original Repository](https://sourceforge.net/projects/osst/) [^1])  
 - [Aaru Data Preservation Suite](https://github.com/aaru-dps/Aaru)  

[^1]: The files shown on the website are not actually the full repository, instead the CVS repo must be directly connected to.
  For this reason, the Github mirror is preferred.