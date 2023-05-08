# Arcserve OnStream Tape Dump Extractor
This is the program used to extract files from the Frogger 2 tape dump data.  
The program is only compatible with tapes written with Arcserve to OnStream tapes, with a drive which is not the ADR-30/ADR-50.  
This has tool has been tested with several undamaged tapes as well as the Frogger tape, and while it works for these tapes, it is unlikely to work .  
Strangely, ARCServe 2000 is incapable of recovering data even from the tapes which were not damaged. Upon performing a restore, ArcServe would create all the files, but they would contain almost none of their actual data.  
It seems this is because it fails to restore the tape catalogue from the tapes, meaning if someone restored using the same PC which wrote the tapes it would have worked. Great software guys.  
This program may be useful in such situations. This issue did not occur with tapes written by the ADR-X0 tape drives.  

## Dependencies
As such, the source code has been included. It relies upon a library called 'ModToolFramework' (which has not yet been open sourced), this functionality could be easily replaced with equivalent code, probably even just 'BinaryReader'.  
TinyInflate.cs is a port of tinflatex.c from [QuickBMS](https://aluigi.altervista.org/quickbms.htm), which is a modified version of [tinf](https://github.com/jibsen/tinf).  

## Credits
Credit to dkdave from the Xentax discord server for determining the compression to be what QuickBMS calls 'zlibx', and making a basic QuickBMS script which was crucial for debugging.  
Credit to aluigi for QuickBMS.  

## Rant
ARCserve shipped broken data recovery software which couldn't even recover the data it wrote. In fact, the software would report "Restoration Success" despite from a 10GB backup, the folder it restored to only having a couple megabytes worth of data in it.
It would find and create all of the files, but they would only contain a few kilobytes of their actual data.  
On top of this, ARCserve is using undocumented hardware features of the SC-50 tape drive in order to write data in a different order than the drive is specified by documentation to use.  
If that wasn't bad enough, it's also writing data to a portion of the tape called the "parking zone", which the [the tape drive's documentation](/archive/documents/ADR%20Driver%20Development%20Specifications%201.3.pdf) doesn't provide any way to read/write from.  
I had to reverse engineer the drive firmware and ARCserve to figure this out. This is wholly unacceptable for **backup software**, which was advertised as being able to recover damaged backups.  
