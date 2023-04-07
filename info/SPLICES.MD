# Splices
If a tape becomes ripped, a splice is performed generally with adhesive tape to fix the tape. 
Splices are very dangerous to data recovery, but are necessary in many situations.  
If your tape drive is infinitely retrying to skip or read a certain spot (Retries more than 5 times), you will likely need this part of the guide.  

## How to get data despite .
Your only option for getting data is using the "raw dump" driver.  

TODO: Describe different methods of doing a hotswap. (Soldering option may be different for a newer drive, but the Q-tip method may not.)  
TODO: Describe how data is laid out on the tape once I figure it out.  
TODO: Describe how to read in a way to get all of the data on your tape, once I determine how splicing impacts reads.  


## After you have your data,
Because you can only use the raw dump driver, you need someone capable of turning the data you got back into something usable.  
Use a hex editor to look at what your data looks like. If you don't know, talk to 
I'd recommend reaching out to me, [aluigi](https://aluigi.altervista.org/), or posting on the [Xentax forums](https://forum.xentax.com/viewforum.php?f=10). Even your average software engineer may or may not be familiar with this.  

## Other Notes  
 - When performing a splice, do not leave any excess material above or below the normal height of the tape. There is no leeway in the spinning mechanism for additional material there.