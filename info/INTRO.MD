# Introduction
Before getting into how to recover data, I want to introduce this guide with my story explaining what happened.  
This should serve as a warning, because learning from my mistakes will prevent you from making your recovery process harder.  
This took place over the span of months, and many details and side-tangents have been omitted for clarity.  

## My Story
I received a tape containing the "end of project" development for Frogger 2: Swampy's Revenge, my favorite childhood franchise.  
This tape is believed to be the only backup of the final game source code, game assets, and other development data.  
As one could imagine, this is priceless to recover. Unfortunately, the first hurdle was finding a tape drive.  

**Part 1) Finding a compatible/working tape drive:**  
I was lucky to know that I needed an "OnStream SC-50" tape drive, because that was the model written on the label of the tape.  
And, I found one single person selling this drive online, so I pounced.
Unfortunately, no matter what I did, when I put the tape into the drive, it would appear to read, then eject automatically.  
I tried different operating systems, different software versions, different sofware, different drivers, etc. Nothing worked.  
Eventually, I concluded the drive was broken, and looking inside proved this to be true, as the rubber pinch roller had melted.  
My efforts to fix the drive were let's say unsuccessful, so I looked for another drive. Because there were no other options, I got an ADR-50e.
That drive was advertised as compatible with these tapes. When that drive worked with my "test tape" (a blank tape I had purchased exclusively for testing), but still refused to read the Frogger tape.  
At this point, I incorrectly concluded that there must have been a problem with the tape itself, so I thought it was 
What had actually happened was that the ADR-50e drive was advertised as compatible, but there was a cave-at. It was only compatible with tapes which were written with an ADR-50e drive, or tapes written with certain software.  
At this point, I didn't even know what software had been used to write the data, let alone this obscure quirk of the tape drive, it was advertised as compatible after all.  
So assuming the tape was defective, I sent it in to a professional data recovery company, believing it to be a mistake. This was a HUGE mistake...  

**Part 2) Professional Data Recovery Sucks**  
They were the professionals, right? They made it seem like they had a special method of recovering data for this kind of tape!  
Their website said specifically that they could recover OnStream, and they have good reviews.  
Well, as I've come to learn, this is deceiving. Most data recovery companies operate on very common types of storage.  
Hard drives are easy to recover from, there are lots of advanced recovery tools for hard drives and SSDs.  
They may even still be able to recover stuff from more common tape formats. However, OnStream was relatively rare.  
As such, when this company told me they could recover OnStream data tapes, in retrospect they were lying.  
OnStream used technology that no other tape drives did. They had a dedicated data processing chip, which was designed specifically for their machines, as opposed to the common chips available on other drives such as Travan.  
The entire data processing pipeline from the tape to digital data uses custom hardware designed from scratch for OnStream tape drives, because it let OnStream get way more capacity than any other tape company could at the time.  
None of it was shared in any other tape drive. This effectively means that the only way to read data off of one of these tapes is to use one of the original tape drives, the hardware inside of one.  
This means that whatever machine they put the the tape I was recovering on was NOT compatible with OnStream tapes, and they lied.  

**Part 3) The Realization**
While the tape was in data recovery, I had received a few tapes for another game, and I was able to easily dump two with the 'ADR50e' drive.
I also determined the software which had been used to write the data, ARCServe 2000.
Because the tapes had the type of drive used to write them written on their labels, I noticed the pattern, and realized the reason I couldn't read the Frogger 2 tape was not because the tape had problems, but because I did actually need an SC-50 drive.  
Unfortunately, it was too late by this point. Data recovery had ripped the tape several times, and while they did manage to splice it, I asked for them to send it back immediately.  
Upon managing to finally locate another SC-50 drive (this time a working one), I learned the worst.
The splices didn't just prevent reading from the spliced area, but the splices impacted the drive so severely that upon just putting the tape into the drive, it would enter a state of infinitely trying to re-read the spliced area.  
By this point I had found documentation on the commands which could be sent by a computer to a drive, and what the drive would do.  
But because the tape would enter into an infinite retry loop when just inserted into the drive (a process henceforth referred to as "initialization"), the drive wasn't ever even reaching a state where the computer could tell the drive anything about what to do.  
Most people I spoke with originally had just told me to send it to professional data recovery anyways, so what the heck do I do now that I have a confirmed defect CAUSED by professional data recovery?  

**Part 4) If you want something done right, do it yourself.**  
Time and time again, I've come to really learn that sometimes if you want something done right, you really do need to do it yourself.  
This really annoys me, because professional data recovery is expensive. If I hadn't chose a company which had a "no data recovered, no charge" policy, I would have paid thousands of dollars for them to screw up the tape.  
That's insane, and I'm still upset that they told me they could even recover the data on the tape. I think they legitimately thought they could but didn't know.  
The only option at this point is to hack the drive. It took months to determine that this was the only option, but it truly is.  

**Part 5) The Resolution**  
I did eventually recover everything which was still possible to recover, but it took months.  
Ultimately, it only was possible because I'm an software engineer and already experienced with reverse engineering data formats, assembly code, and got help from electrical engineers who helped with the hardware side of things.  
The details of how data was dumped despite the splices is part of the guide [here](/info/SPLICES.MD), but it's pretty complicated.  
  
  
My takeaway is just to be careful, and if you're in charge of backups, you need a redundant backup strategy.  