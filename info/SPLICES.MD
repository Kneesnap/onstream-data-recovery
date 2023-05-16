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

TODO: This requires soldering, but it's easy soldering. Give both the manual pin swapping a description since it's simplest but also go for a switch because that's easier to use.  
TODO: Show process manual ejection.

## After you have your data dumped
Review the section about what to do once data is dumped found in [this part of the guide](/info/RAW_DUMP.MD).

## Other Notes  
 - When performing a splice, do not leave any excess material above or below the normal height of the tape. There is no leeway in the spinning mechanism for additional material there.