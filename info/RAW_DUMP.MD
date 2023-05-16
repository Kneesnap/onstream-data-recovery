## Raw Dumps
If the original software isn't going to work out, a raw dump is necessary.
This will be a lot more complicated, because it will most likely some coding to get files out of this.  
I can help with that, and there are others who I know I can point towards for help.  

**Setup:**  
You'll need a machine running Linux. The distro doesn't matter, as long as it has `sg` installed.  
Personally, I used "Arch Aaru" (From the Aaru Data Preservation Suite) for this, and it's easy to setup a bootable USB.  
But, this is also true about Ubuntu. It seems some of the latest versions of Ubuntu do not come with sg, but 18.04 LTS should be OK.  
`sg` is ancient, and has been in the Kernel for a long time, so probably any mainstream Linux distro between 1999 - 2020 should have it. I don't know if `sg` was removed.  
The way you can check is if you run `ls /dev/` and you see devices matching the pattern 'sgX', for example: `/dev/sg0`.  

**Performing a raw dump:**  
Follow the instructions [here](/software/onstream_raw_dump/) to setup and run the raw dump program.  
Continue reading for detailed information for performing raw dumps (instead of just how to use the program).  

**After your data is dumped, how do you make it something usable?:**  
Now that you have your data dumped from the tape, there's one major problem remaining.  
If you want to do anything with your data, you need to turn it into something usable.  
For example, if your backup had a bunch of files in it, you'll want to turn your single large tape dump file into something like a zip file which contains your files.  
Unfortunately, this does depend on the software which wrote the backup, because everyone made their own proprietary data formats.  
From this point, a software engineer is likely necessary, unless a tool for the software your tape was written with has already been made.  
I've created a [.NET C# Library](/software/OnStreamTapeLibrary) to make extracting raw OnStream tape dumps easier, but someone will still need to make a program utilizing this library.  
I'd recommend reaching out to me over discord, [aluigi](https://aluigi.altervista.org/), or posting on the [Xentax forums](https://forum.xentax.com/viewforum.php?f=10). Make sure to link to this page so they can read the information here.  
Additionally, a software engineer may be able to help you, but most don't have experience reverse engineering data formats so it has to be someone with lots of low-level experience.  
I wish I could give a less involved option here, but there is no one-size fits all approach, and it will need to be handled on a case-by-case basis.  

# OnStream Tape Layout
When performing raw dumps, it is crucial to understand how data has is written onto these tapes.  
Knowing this will allow you to ensure you don't miss any data.  

## Important
This page details how the ADR1 tapes (30GB / 50GB cartridges) organize data, but there's a catch.  
Tapes written with 1st generation drives (SC-30, SC-50, DI-30, etc) are non-standard and can use my [custom driver](/software/onstream_raw_dump/) which can utilize undocumented functionality.  
Later drives (such as the ADR30, ADR50, ADR2.60, etc) follow the QIC-157 specification and work with most standard tape drivers, such as [st](https://www.kernel.org/doc/html/latest/scsi/st.html) which comes in most Linux distros.
These drives, including ADR1 tapes written with an ADR30/ADR50, have not been researched, and likely contain differences. Additionally, because they follow the QIC-157 specification, undocumented features would have to be found in firmware (or firmware modification) may be required to do certain things mentioned here.

## The Basics
Let's first look inside of these cartridges:  
![Picture of tapes](/pictures/visual-aids/open-tapes.jpg)  
  
The cartridges have two reels inside of them. We will refer to them as the "front" and "back" reels, based on close they are to the camera.  
But, take note of how much tape is on each reel. The smaller tape has most of its tape on the back reel, where-as the larger tape has it split about 50/50.  
Both of these tapes are at their "beginning". This will be important for understanding how data is read/written.  

There are 192 tracks of data which span the entire width of the tape. Data is stored linearly, as opposed to helical scan.  
This can be visualized by thinking of the data as 192 separate straight lines, spanning the entire width of the tape.  
Eight of these tracks are read/written simultaneously.  
![Layout Visualization](/pictures/visual-aids/linear-tape-example.gif)  

Because these tracks are read in groups of eight, there are 24 readable track groups aka "logical tracks".  
In other words, `192 "physical" tracks / 8 tracks read at a time = 24 logical tracks`.  
Because the tape drive itself handles the 192 physical tracks behind the scenes, those individual tracks are never something we can deal with directly.  
So, all future mentions of just "tracks" will refer to the 24 logical tracks, instead of the 192 physical tracks.  

### Reading
The tape drive reads the tape by physically spinning the tape so that it passes by the tape head sensor, which can read the actual data from the tape.
If the drive is reading data over time, how does the computer get the data?
Instead of getting the data immediately after the drive gets it, the data is broken up into 32.5KB (33,280 byte) chunks, called "blocks".
Upon reading the entirety of a block, the tape drive will put it into a buffer which can be read by the connected PC one block at a time.

#### Read Errors
What happens if reading fails? There are a few situations for reading failure.
The word damage here is used to mean to be any physical condition to the tape which decreases the tape drive's ability to recover the impacted block(s).  

**Common Examples of Damage:**  
 - Tears / missing portions of tape  
 - Physical degradation over time, including warping caused by humidity.  
 - Damages caused by nearby magnets  
 - Melted rubber sticking to the tape (Some old drives have melted pinch rollers)  

**Note on Magnets & Magnetic Tape:**  
Magnets are dangerous! Even weak magnets are capable of destroying data on magnetic tapes!
The damage is impacted by the size of the magnet and the distance of the magnet.
Even something like a screwdriver with magnetic screw bits should be handled with caution, and do NOT touch the surface of the tape with one.
Touching the surface of the tape should be avoided when possible, wash your hands before touching it, or use clean gloves.  

**#1) Severe Damage (Drive gets stuck in a loop and can't be communicated with)**  
This damage is usually only observed if a splice has occurred, and some of the tape has been physically removed and taped back together.  
But, it may theoretically also get stuck if damage occurs to the servo tracking signals, or the position information.  
If severe damage has occurred and the drive becomes stuck in an infinite loop (not caused by the reading software on the PC), refer to [this section](/info/SPLICES.MD) for details.  

**#2) Corruption/Read Failure**  
The tape drive advertises than per every logical track (8 physical tracks), one full physical track can be completely unreadable/removed without any data loss.  
For information on how it recovers missing data, visit page 11 of [this brochure](/archive/documents/OnStream%20Brochure%202002.pdf).  
If the damage becomes too severe for the error correction to recover, a configurable number of retries will occur.  
If the drive has still failed after the maximum number of retries, the PC is notified of a read error, and the block should be skipped.  
The data before/after the missing portion is never exposed to the connected PC, and recovery is unlikely.  
But, it may be worthwhile to look at the damaged portion of tape. This can be done by telling the drive to seek/locate to the position with the error.  
Then, turn the PC off and manually eject the tape. The tape cannot be ejected normally is because it will seek away from the current position.  
Then, look at the tape to see any reasons why that portion may not be readable.  

## How data is actually organized

### Physical Positions   
Each logical track (not physical track) is a sequential list of data blocks.  
Imagine the tape were to be unrolled and laid out in a very long straight line, and recall that we're pretending there are 24 logical tracks instead of the 192 physical tracks.  
We can think of each track as a sequential list of blocks.  
And, since we have 24 tracks, we can think of it in terms of an X/Y coordinate, where your Y coordinate is the track, and the X coordinate is the index of the block on the track.  
Below are the images showing the general pattern followed when reading/writing data using physical positions. (Not to scale, instead it just shows the pattern.)  
The pattern is that a read will occur until the end of the tape is reached, where it will increase the Y coordinate, reverse direction, and read until the end of the track.
Part of the parking zone in the center of the tape is skipped.  

**ADR30 (30GB Tape Cartridge):**  
![ADR30 Physical Position Pattern](/pictures/visual-aids/adr30-PhysicalBlock.png)  

**ADR50 (50GB Tape Cartridge):**  
![ADR50 Physical Position Pattern](/pictures/visual-aids/adr50-PhysicalBlock.png)  

### Logical Positions
Tracking both X and Y is effective, but OnStream is still trying to be similar to other tape drives.  
Instead, it allows the software which talks to the drive to give a single number, a number between 0 and the total number of tape blocks, which the drive will turn into an X/Y position.  
It's also setup so that increasing the number by one will always give you the next block which is read by the tape.  
However, the way these numbers get turned into physical X/Y positions will take a little getting used to.  
Below are the images showing the general pattern followed when reading/writing data using physical positions. (This pattern scales with the size of the tape)  

**ADR30 (30GB Tape Cartridge):**  
![ADR30 Logical Position Pattern](/pictures/visual-aids/adr30-LogicalBlock.png)  

**ADR50 (50GB Tape Cartridge):**  
![ADR50 Logical Position Pattern](/pictures/visual-aids/adr50-LogicalBlock.png)  

### Converting between Logical Positions & Physical Positions   
[This folder](/archive/blocks/) contains text files which allow you to search for a logical block or a physical block, then get its alternative position, for example getting a physical position from a logical block position.  
It was generated by [this library](/software/OnStreamTapeLibrary/), which can be used to work with these positions in code.  