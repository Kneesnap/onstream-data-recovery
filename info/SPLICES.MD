# Splices
If a tape becomes ripped, a splice is performed generally with adhesive tape to fix the OnStream tape. 
Splices are very dangerous to data recovery, but are necessary in many situations.  
If your tape drive is infinitely retrying to skip or read a certain spot (Retries more than 5 times), you will likely need this part of the guide.  

## The Process
In order to work around splices, [understanding how data is laid out on the tape is critical](/info/RAW_DUMP.MD).  
The following process describes how to track the data you've dumped, but not how to actually skip the splice, that comes later.  
The obstacle to dumping spliced tapes is that when the drive reaches a splice, it loses tracking, both of where it is on the tape, and where the data tracks are.  
While the drive handles most other errors well, in this situation it will unfortunately retry forever.
It doesn't matter if the drive is reading, or seeking to a position, it will fail.  
However, all other parts of the tape are fair game, and it becomes a game of reading as much of the tape as possible, without the splices.  
The best option for getting data is using the [raw dump driver](/software/onstream_raw_dump/), with the `-p` flag to enable physical addressing mode.  
Physical addressing mode makes it easier to think about / visualize the parts of the tape you have / have not dumped as well as where the splices are.  
By using [this program](/software/OnStreamTapeInfo/), you can generate an image as well as a list of which parts of the tape have / have not been dumped.  
Here's an example from the Frogger 2 tape, can you guess which parts were destroyed by the data recovery company?  
![Visualization of Frogger 2 Tape damage](/pictures/visual-aids/frogger2-tape-damage.png)  

## Hotswapping
Hotswapping is the technique of swapping tapes without letting the drive know that a tape has been swapped.  
Hotswapping has only been tested with an SC-50 drive. 
This allows for manually winding the tape to go to a certain position to skip a problem spot which the drive gets stuck on.  

**1) Getting Started:**  
The drive thankfully has given us a fairly easy way to hotswap. By removing the top cover with a screwdriver, you have direct access to solder the areas we need.  
Our goal is to take the following pads and use them to trick the drive into skipping the tape "initialization" process.  
![Guide showing where the connection pads are](/pictures/visual-aids/hotswap-wire-guide.jpg)  

Using a soldering iron, solder small wires to each of the pads. I'm not really qualified to teach soldering unfortunately, but there are many guides online, and I promise it's not as intimidating as it sounds.  
When you're done, it should look something like this:
![Completed soldering reference](/pictures/visual-aids/hotswap-soldering-complete.jpg)

**2) Breadboard Time:**  
We're going to connect these wires to a bread board, and use that for tricking the tape drive.  
A bread board is pretty much a plastic case surrounding a board which looks like this:  
![Inside of breadboard](/pictures/visual-aids/breadboard-example.jpg)

Its function is basically just to connect different things together. In our case, it will let us connect all of the wires we've soldered together.  
There are two situations we want to consider, where the drive is either tricked or not tricked.  

**Tricked Drive:**  
NOTE: I must have broken the black wire, because its end is missing. Pretend its there for the purpose of the picture.  
When we want to trick the drive into thinking a tape is present, we connect all the wires together, and it looks like this:  
![Picture showing the breadboard with the wires organized as described.](/pictures/visual-aids/breadboard-tricked.jpg)  

**Not Tricked Drive:**  
NOTE: I must have broken the black wire, because its end is missing. Pretend its there for the purpose of the picture.  
When we want to let the drive operate normally, we separate the wires from each other into separate rows, like this:  
![Picture showing the breadboard with the wires organized as described.](/pictures/visual-aids/breadboard-not-tricked.jpg)  

**3) Performing the Hotswap:**
The tape drive is capable of reading most of the damaged tape now, but you'll need to follow a special process to do so.  
 - 1) Insert an undamaged tape of the same type as the one you want to read.  
 - 2) Wait for the tape drive to become silent (accepting the tape), with the status LED off.  
 - 2b) If the desired read area is before the parking zone (50GB tapes only), use [onstreamsg](/software/onstream_raw_dump/) to move the tape somewhere before the parking zone.  
 - 3) Trick the drive with the breadboard into thinking a tape is always inserted.  
 - 4) Forcibly eject the tape by sliding a Q-tip, toothpick, or, any other rod in the manual eject track on the bottom of the tape drive.  
 - 5) Insert the damaged tape.  
 - 6) Stop tricking the tape drive with the breadboard. (This will prevent weird issues)  
 - 7) You are now able to read data with the [onstreamsg](/software/onstream_raw_dump/) program.  
 
**4) Reading Data:**  
Once you've successfully hotswapped the tape, you are now able to use [onstreamsg](/software/onstream_raw_dump/) to dump data from the tape.  
However, if you at any point read into an area with a splice, the drive will get stuck again, and a FULL SHUTDOWN OF THE ENTIRE COMPUTER is required.  
There is likely an electrical method we could use to reset the drive instead, but I did not look for one.  

When you've dumped all the data before a splice and are ready to dump the data beyond a splice, open the tape cartridge using a Philips screwdriver.  
Make sure you're familiar with the [way data is organized on the tape](/info/RAW_DUMP.MD), so you know which direction you want to wind the tape.  
I've found the best way to move the tape is by physically spinning the reels containing the tape with my thumb. Be careful not to touch any of the magnetic tape.  
I will usually do around 10 full rotations, depending on how evenly distributed the tape is across both reels, but the idea is to give the tape enough breathing room to not hit the splice.  
Then, you can continue reading from beyond the splice.  

## Troubleshooting
As mentioned in other places, feel free to contact me with the instructions in the [main readme](/README.MD), and I'll do my best to help you as best as possible.  

## After you have your data dumped
Review the section about what to do once data is dumped found in [this part of the guide](/info/RAW_DUMP.MD).

## Other Notes  
 - You can perform a splice yourself. We found Kapton tape to work very well for splices and it's available at most stores. It's not very hard to do a splice, but it can be hard to do it well.
 - When performing a splice, do not leave any excess material above or below the normal height of the tape. There is no leeway in the spinning mechanism for additional material there.