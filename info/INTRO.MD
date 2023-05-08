# Introduction
Before getting into how to recover data, I want to introduce this guide with my story explaining what happened.  
This should serve as a warning, because learning from my mistakes will prevent you from making your recovery process harder.  
This took place over the span of months, and many details and side-tangents have been omitted for clarity.  

## My Story
I received a tape containing the "end of project" development for Frogger 2: Swampy's Revenge, part of my favorite childhood franchise.  
This tape is believed to be the only backup of the final game source code, game assets, and other development data.  
As one could imagine, this is priceless to recover. But how does one even read/write data from a tape? Why did they even use tapes?  
The average hard drive size in 1999/2000 seems to have been around 10GB, and hard drives are not known for their longevity.  
OnStream tapes were a very appealing proposition because they could offer 50GB cartridges (25GB when uncompressed) cheaper than most hard drives!  
Tapes are great for backups and have very high longevity when stored properly. And they could buy a tape drive that they could put into their computer just like a CD or floppy disk drive.  
Unforunately, while tapes have good longevity, the tape drives did not. OnStream, the company who made the tapes and tape drives ceased operations in 2003, after only just releasing their first drive in 1999.  
These drives are uncommon, especially the model which can use 50GB tapes. So, the first big hurdle was finding a compatible tape drive.  

### 1) Finding a compatible/working tape drive  
I was lucky to know that I needed an "OnStream SC-50" tape drive, because that was the model written on the label of the tape.  
And, I found one single business selling this drive online, so I pounced.
Unfortunately, no matter what I did, when I put the tape into the drive, it would appear to read, then eject automatically.  
I tried different operating systems, different software versions, different sofware, different drivers, etc. Nothing worked.  
Eventually, I concluded the drive was broken, and looking inside proved this to be true, as the rubber pinch roller had melted.  
My efforts to fix the drive were let's say unsuccessful, so I looked for another drive. Because there were no other options, I got an ADR-50e.
That drive was advertised as compatible with these tapes. When that drive worked with my "test tape" (a blank tape I had purchased exclusively for testing), but still refused to read the Frogger tape.  
At this point, I incorrectly concluded that there must have been a problem with the tape itself and it were damaged.  
What had actually happened was that the ADR-50e drive was advertised as compatible, but there was a cave-at. It was only compatible with tapes which were written with an ADR-50e drive, or tapes written with certain software.  
At this point, I didn't even know what software had been used to write the data, let alone this obscure quirk of the tape drive, it was advertised as compatible after all.  
So assuming the tape was defective, I sent it in to a professional data recovery company, believing it to be a mistake. This was a HUGE mistake...  

### 2) Professional Data Recovery Sucks  
They were the professionals, right? They made it seem like they had a special method of recovering data for this kind of tape!  
Their website said specifically that they could recover data from OnStream tapes, and they have good reviews.  
Perhaps they were fine for common data storage types like hard drives or SSDs. Those should be easier since there are advanced recovery tools available for them.  
However, I do not think I will ever consider sending tapes into a professional data recovery company again, even for more common digital tape formats.  

**What this company should have done:**  
This company should have not advertised the capability of recovering data from these tapes, a capability they clearly did not possess.  
The company should have made the risks clear they they didn't know if whatever machine they used was even capable of recovering data from the tape instead of pretending they knew it was.  
The company should not have told me they could recover data from this tape.  

**What this company actually actually did:**  
This company specifically advertised that they could recover data from OnStream tapes. Bullshit.  
They clearly didn't even have an OnStream SC-50 tape drive, because if they did they could have just used the official Linux kernel from 10-20 years ago which came with a driver that could have dumped this data.  
Over the span of about a month, I received very infrequent and vague communications from the company despite me providing extremely detailed technical information and questions.  
Eventually after a month and I had realized what the real problem was, and because things didn't sound good in data recovery, I asked for it to be sent back.  
I was told that "it will perform the way it did when we received it". What a fucking lie. The tape I got back had been ripped in several spots, and spliced back together.  
If this were like an audio tape or something archaic maybe it would have worked, but the tape I got back had problems which it did NOT have when I sent it in.  

**They were never capable of recovering the data:**  
OnStream used technology that no other tape drives did. They had a dedicated data processing chip (ASIC), which was designed specifically for their machines, as opposed to the common chips available on other drives such as Travan.  
The entire data processing pipeline from the tape to digital data uses custom hardware designed from scratch for OnStream tape drives, because it let OnStream get way more capacity than any other tape company could at the time.  
None of it was shared in any other tape drive. This effectively means that the only way to read data off of one of these tapes is to use one of the original tape drives, or the hardware inside of one.  
This means that whatever machine they put the the tape I was recovering on was NOT compatible with OnStream tapes.  
I don't know why they claimed to be able to recover data from OnStream tapes, but it's downright false advertising for their website to claim they can recover OnStream data.  
They clearly didn't even have an OnStream tape drive, because it would have just worked if they did.  

### 3) The Realization  
Unfortunately, this is only clear in hindsight.  
While the tape was in data recovery, I had received a few tapes for another game, and I was able to easily dump two with the 'OnStream ADR-50e' drive.
It was also determined the software which had been used to write the data was ARCServe 2000.
Because the labels on the tape had the tape drive model written, I realized the reason I couldn't read the Frogger 2 tape. It was not because the tape had problems, but because I did actually need an SC-50 drive instead of an ADR-50 drive.  
It was too late. The damage had been done by the data recovery company, and upon finally locating another SC-50 tape drive (this time a working one), my worst fears were confirmed.  
The splices didn't just prevent reading from the spliced area, but the splices impacted the drive so severely that upon just putting the tape into the drive, it would enter a state of infinitely trying to re-read the spliced area.  
By this point I had found documentation on the commands which could be sent by a computer to a drive, and what the drive would do.  
But because the tape would enter into an infinite retry loop when just inserted into the drive (a process henceforth referred to as "initialization"), the drive wasn't ever even reaching a state where the computer could tell the drive anything about what to do.  
This is where the story should probably have stopped. Given up and called it a day, right? Maybe, but I care about this data, and I happen to know a thing or two about computers.  

### 4) It's hacking time baby.
Time and time again, I've come to really learn that sometimes if you want something done right, you really do need to do it yourself.  
This really annoys me, because professional data recovery is expensive. If I hadn't chose a company which had a "no data recovered, no charge" policy, I would have paid thousands of dollars for them to screw up the tape.  
That's insane, and I'm still upset that they told me they could even recover the data on the tape. I think they legitimately thought they could but didn't know.  
But if you asked me at the time, you wouldn't have seen it. I used this as further motivation instead.  
Months of effort was spent understanding the drive, studying its SCSI command interface, reverse engineering firmware, reading documentation, digging up patents, etc.  
While much was learned about the drive, the major breakthrough came just after midnight on April 5th 2023 and ended up being significantly simpler than the other attempted methods.  
If the initialization process is the problem, then what if we were to run the initialization process with a working tape? Then, by tricking the drive sensors we could switch the tape without the drive knowing a swap occurred.  

### 5) Wait there's more?
Unfortunately, with such complicated restrictions in place, the original ARCServe software.
It actually turns out the original ARCserve software wouldn't have been capable of reading the data anyways. That is too deep of a rabbit hole to jump into here though.  
In short, the software which wrote the data had to be reverse engineered to understand the format in which it wrote data, its compression format, and of course the annoying discoveries such as how it uses an undocumented indexing mode for tape positions or how it writes data to a spot on the tape described as "not being possible to write data to".  
I will omit the details of this and save them for the technical writeup in the onstream data recovery repository.  
The main takeaway is that a custom driver had to be made and software had to be written to parse the recovered data and turn it into a zip file, and it got complicated.  
Source code and binaries are included (here)[/software/arcserve-file-extractor/].  

### 6) The Resolution  
In the end, the recovery was an unquestionable success. Of the approximately 8GB of data written on the tape, just under 7.5GB was recovered.
And while a couple thousand files were damaged, they make up less than 5% of the total files on the tape.  
But, it gets better. All except ONE of the damaged files were either found in another CD backup or were duplicated on another part of the tape.  
There was only one file in the entire tape which was unrecoverable, a CD image of a PC game build from 1 month after the master PC version had already shipped.  
Having recovered 58163 out of the 58164 files on the tape, this adventure can only be considered a success.  
Thank you everyone who helped with this project, without your help who knows how long it would have taken or if the data would have even been recovered.  

### 7) Further Technical Details
The hope is to share all the information we learned so that it might help someone else, and let those who are less experienced recover data too.  
Further technical information is available as [part of the data dumping guide](/info/SPLICES.MD) in this repository.  