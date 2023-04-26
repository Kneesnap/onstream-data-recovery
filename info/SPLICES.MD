# Splices
If a tape becomes ripped, a splice is performed generally with adhesive tape to fix the tape. 
Splices are very dangerous to data recovery, but are necessary in many situations.  
If your tape drive is infinitely retrying to skip or read a certain spot (Retries more than 5 times), you will likely need this part of the guide.  

## How to get data when there is a splice?
In order to explain this, we first need to explain how data is laid out on the tape.  
This is crucial to understand, because a splice should be handled differently depending on what happens.

Let's first look inside of these cartridges:  
![Picture of tapes](/pictures/TODO.png)  
  
  
The cartridges have two reels inside of them. We will refer to them as the "front" and "back" reels, based on close they are to the camera.  
But, take note of how much tape is on each reel. The smaller tape has most of its tape on the back reel, where-as the larger tape has it split about 50/50.  
The reason I took the photo like this is because this is the "beginning" of each of the tapes. With the smaller tape, the data is read/written start to finish.
It goes in order. However, the larger tape starts writing outwards from the center, and upon reaching the end of the tape, it will move back to the center, to start writing in the opposite direction.
Because splices can require manually spinning the tape yourself to move past a splice, you need to know where to move it.  

Data isn't just written once at a time. There are 192 separate tracks (eg: channels) which data can be written to. The drive can read/write 8 at at time, so there are 24 (192 / 8) streams of data on any one portion of the tape. This may have changed for ADR2 tapes (60GB/120GB capacity).
This means, a single splice interrupts 24 different parts of the data. Yikes! The reason you need to know this is because when you dump data from the tape, you'll be giving the tape drive a number.
This number is between 0 and 760,239 for 50GB tapes, and between 0 and 


Your only option for getting data is using the "raw dump" driver.  
### Hotswapping
Hotswapping is the technique of swapping tapes without letting the drive know that a tape has been swapped.  
Hotswapping has only been tested with an SC-50 drive. 
It is often necessary 

TODO: Describe different methods of doing a hotswap. (Soldering option may be different for a newer drive, but the Q-tip method may not.)  

### How is data actually laid out on the tape?
TODO: Describe how data is laid out on the tape once I figure it out.  

### How to use osg to dump your data?
[Osg](/software/onstream_raw_dump/) is a program which can be used to perform 

TODO: Describe how to read in a way to get all of the data on your tape, once I determine how splicing impacts reads.  


## After you have your data,
Because you can only use the raw dump driver, you need someone capable of turning the data you got back into something usable.  
Use a hex editor to look at what your data looks like. If you don't know, talk to 
I'd recommend reaching out to me, [aluigi](https://aluigi.altervista.org/), or posting on the [Xentax forums](https://forum.xentax.com/viewforum.php?f=10). Even your average software engineer may or may not be familiar with this.  

## Other Notes  
 - When performing a splice, do not leave any excess material above or below the normal height of the tape. There is no leeway in the spinning mechanism for additional material there.