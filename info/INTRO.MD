# Introduction
I want to introduce the data recovery guide with my story explaining what lead me to create it.  
This should serve as a warning, because learning from my mistakes will prevent you from making your recovery process harder.  
This took place over the span of months, and many details and side-tangents have been omitted for clarity.  

## My Story
I received a tape containing the "end of project" development for Frogger 2: Swampy's Revenge, part of my favorite childhood franchise.  
This tape is believed to be the only backup of the final game source code, game assets, and other development data.  
To me, this data was priceless to recover. But how does one even read/write data from a tape? Why did they even use tapes?  
Back in 2000, when the game was made, the average hard drive size seems to have been approximately 10GB, and had much less longevity.
OnStream tapes were very appealing since they could offer 50GB cartridges at a price cheaper than most hard drives!  
Tapes are great for backups and have very high longevity when stored properly, and were just as easy to install/use as a CD drive.  
Unfortunately, while the tapes have good longevity, the tape drives required to read/write from the tapes did not. OnStream, the company who made the tapes and tape drives, ceased operations in 2003, after only just releasing their first drive in 1999.  
The drives are also somewhat uncommon, especially the model which supports 50GB tapes. So, the first big hurdle was finding a compatible tape drive.  

**Tape Picture:**  
![Picture of the tape](/pictures/visual-aids/frogger2-tape-scan.jpg)

### 1) Finding a compatible/working tape drive  
Determining the tape drive I needed as the "OnStream SC-50" wasn't too hard since it was written on the tape itself.
I found one single business selling this drive online, so I pounced.
Unfortunately, when I put the tape into the drive, it would make some noise, blink the status LED, and after a few seconds automatically eject the tape.  
I tried different operating systems, different software versions, different sofware, different drivers, etc. Nothing worked.  
Eventually, I concluded the drive was broken, and looking inside showed the rubber pinch roller had melted.  
My efforts to fix the drive were let's say unsuccessful, so I looked for another drive. There were no other SC-50 drives for sale, so I tried out an OnStream ADR-50e instead.
That drive was advertised as compatible with these tapes after all.
When it arrived, it appeared to work. I was able to read/write data using test tapes, there was no auto-ejection, and I even recovered the data from another tape for a different game by the same company.  
But I still had a problem: the Frogger tape could still not be read. When I would ask the drive to read the tape, it would return a generic error status without even attempting to read anything.  
I was using the open source Linux `st` driver to talk to the tape drive, and I went as far as to review its source code to ensure there was nothing else I could do to the drive.
After not finding anything, I incorrectly concluded that there must have been a problem with the tape itself, and it was damaged.  
What had actually happened was that the ADR-50e drive was advertised as compatible, but it wasn't so simple. It was only compatible with tapes which were either written with an ADR-50e drive or written with certain supported software.  
The Frogger tape was written with an OnStream SC-50 drive, and was not written using forward-compatible software it turns out.  
At this point I didn't even know what software had been used to write the data, let alone this obscure quirk of the tape drive which was advertised as compatible after all.  
So assuming the tape was damaged/defective, I sent it in to a professional data recovery company. What was the worst that could happen...?    

**Melted Pinch Roller:**  
![Melted Pinch Roller](/pictures/visual-aids/melted-pinch-roller.jpg)  

### 2) Professional Data Recovery Sucks  
I found a company whose website said specifically that they could recover data from OnStream tapes, and they had good non-botted reviews as far as I could tell.
I even got in contact with the general manager who made it sound like they had created their own proprietary machine which they could configure to read data from any tape.  
Perhaps they were fine for common data storage types like hard drives or SSDs, but my experience with tapes was awful.   
However, I do not think I will consider sending tapes into a professional data recovery company again, even for more common digital tape formats.  

**They were never capable of recovering the data:**  
They clearly didn't even have an OnStream SC-50 tape drive, because if they did, any version of the official Linux kernel from the past 10-20 years shipped with a driver capable of dumping any OnStream tape.  
Regardless of if they actually had a proprietary machine, with the benefit of hindsight and additional research, I believe they were absolutely not capable of recovering the data.  
OnStream used technology that no other tape drives did. One example of the custom hardware would be the dedicated data processing chip (ASIC), which was designed specifically for their machines, as opposed to the common chips available on other drives such as Travan.  
Another example would be how dense the data was packed on the tape, with no other tape manufacturers of the era coming close to having 192 data tracks on the physical tape.  
The entire data processing pipeline from the physical tape to chunks of digital uses custom hardware designed from scratch for OnStream tape drives, because it let OnStream get way more capacity than any other tape company could at the time.  
None of it was shared in any other tape drive. This effectively means that the only way to read data off of one of these tapes is to use one of the original tape drives or the hardware inside of one.  
In other words, whatever machine they put the the tape I was recovering on was NOT compatible with OnStream tapes.  
I don't know why they claimed to be able to recover data from OnStream tapes, but it's downright false advertising for their website to claim they can recover OnStream data.  
It's possible they saw the tape had the same physical size as other quarter-inch tape formats, and just put the tape on some ancient incompatible machine thinking that could get the data.  

**What this company should have done:**  
This company should have not advertised the capability of recovering data from OnStream tapes.  
The company should have made the risks clear they they didn't know if whatever machine they used was even capable of recovering data from the tape instead of pretending they knew it was.  
The company should not have told me they could recover data from this tape.  

**What this company actually did:**  
This company specifically advertised that they could recover data from OnStream tapes, and I was given an estimate of a week.  
Instead, over the span of about a month I received very infrequent and vague communications from the company despite me providing extremely detailed technical information and questions.  
While this was going on, I received more tapes and did further testing & research on my own, eventually discovering the true problem was needing the SC-50 tape drive, not the tape.  
Since data recovery was taking so long and it didn't sound like progress was getting made in data recovery, I asked for the tape to be sent back to me.  
As part of that response, I was assured that "it will perform the way it did when we received it". What an bleeping lie. The tape I got back had been ripped in several spots, and stitched (spliced) back together.
The only silver lining is that since I made sure to pick a company that had a "no recovery, no charge" policy, I didn't have to pay the thousands of dollars they would have charged.  
![Screenshot of the data recovery website](/pictures/visual-aids/data-recovery-screenshot.png)  

### 3) The Realization  
It was too late. The damage had been done by the data recovery company, and upon finally locating another SC-50 tape drive (a working one this time), my worst fears were confirmed.  
When inserting a tape, the drive reads special reserved parts of the tape for information like what kind of tape is inserted, what parts of the tape have factory defects, etc.  
Only after this process is complete does the drive respond to any requests by the computer.  
The splices didn't just prevent reading from the spliced area, but the splices impacted the drive so severely that upon just putting the tape into the drive, it would enter a state of infinitely trying to re-read the spliced area.  
It wasn't even possible for the computer to interrupt this process, since the drive did not respond to any commands from the computer until it finished this "initialization" phase.  
This was devastating, and there was a degree of panic & stress. I had just the only copy of the development data for a game I cared deeply about. All because I misdiagnosed the problem.
Flooded with thoughts like "Was I too impatient when diagnosing the problem?" or "Why did I do trust this data recovery company, I should have pressed harder!??", this is probably where the story should have stopped.  
But those thoughts were short-lived. I care about this data, and I happen to know a thing or two about computers.  

**Splice Picture:**  
At least they made good quality splices.  
![Example of one of the splices](/pictures/visual-aids/frogger2-splice-example.jpg)  

### 4) It's hacking time, baby.
Time and time again, I've come to really learn that when you need something done right, sometimes you really do need to do it yourself.  
I don't think this is acceptable, professional data recovery is expensive. Recovering data from a single tape costs thousands of dollars, and if I hadn't chosen a company which explicitly had a "no data recovered, no charge" policy, I would have had to pay thousands of dollars for the priviledge of having said company wreck the tape.  
That's insane, and I'm still upset that they told me they could even recover the data on the tape. It feels like they didn't take their job seriously.  
But if you asked me at the time, that wasn't what I was thinking about. I burned these negative emotions as motivational fuel.  
Months of effort was spent understanding the drive, reverse engineering its firmware, studying its SCSI command interface, reading documentation, digging up patents, etc.  
While much was learned about the drive, the key breakthrough ended up being one of the simplest ideas.  
If the initialization process was the problem, then what if we were to skip the initialization process with?  
By running the initialization process by inserting an undamaged tape, we can do the initialization process as normal. Then, by soldering some wires, we could fake the electrical signals the drive used to test if a tape had been ejected.  
Then, we could insert the damaged tape. and thus have the drive in a state where it is willing to accept commands while the tape we want to recover is inserted.  
Using this trick, on April 5th, 2023, after months of effort, the first real chunks of data were successfully extracted from the tape.  
From this point, it was just a matter of dumping all of the data, and figuring out the format, right? Right?  

**Modified Drive:**  
![The modified tape drive](/pictures/visual-aids/onstream-modded-drive.jpg)

**Tape Dumping:**  
![A computer screen showing tape data getting dumped from the tape](/pictures/visual-aids/early-successful-dump.jpg)

### 5a) ARCServe Sucks Too
In the previous months, ARCserve 2000 was determined to be the software which had originally wrote this tape.
The data which comes off the tape is formatted in whatever way the software which wrote it chose.  
Unfortunately, that means every single compatible backup software product made their own proprietary format, including ARCserve.  
This is the challenge of doing a raw dump, if I were able to use the original software, it would automatically give back the data in a usable form.
Or at least, it would if ARCserve wasn't garbage! It turns out ARCserve is broken. It isn't even capable of reading the OnStream tapes it writes, even when they are not damaged.  
This issue doesn't affect tapes written with the ADR-50 drive, but all the tapes I have tested written with the OnStream SC-50 do NOT restore from tape unless the PC which wrote the tape is the PC which restores the tape.
This is because the PC which writes the tape stores a catalog of tape information such as tape file listing locally, which is used when restoring from backup.  
ARCserve is supposed to be able to restore without the catalog because it's something which only the PC which wrote the backup has. But, it seems the software was bugged when it reads the catalog from the tape.  
ARCserve shows a popup which says "Restoration Successful", it restores up to the first 32KB of every file on the tape, but NO MORE.  
For an undamaged several gigabyte tape which takes two hours for ARCserve to read, it will restore about 1MB of data total.  

### 5b) Making it usable
In order to convert the files into a usable format (.zip), we have to cut out ARCserve. Since I can use an open source driver to get the raw data on the tape, if I can figure out how the ARCserve data is formatted, I could write some software to successfully recover the files inside the tape dumps.  
Thankfully, the ARCserve format wasn't very complex and I figured it out pretty quickly.  
However, with my program many many large files were corrupted.  
After spending some time debugging & manually analyzing the tape data dumps, it became apparent that ARCserve was using an undocumented mode for reading the tape.  
This is explained in more depth in [the guide](/info/RAW_DUMP.MD), but ARCServe was using a feature not documented in the [official OnStream driver development document](/archive/documents/ADR%20Driver%20Development%20Specifications%201.3.pdf).  
This undocumented feature allowed ARCServe to read and write data in a completely different physical pattern from what the documentation described.  
After enough experimentation, eventually I completely reverse engineered the layout of how data is organized when it is physically written to the tape.  
And with that experience, I was able to fix my software to automatically reorder the tape data chunks so my program would read the file data in order, causing most files to not be corrupted.  
It still wasn't enough. Even after fixing the data ordering issue, I still saw errors which I didn't expect to see.  
This time it didn't take long to see that there was missing data which was located in a position of the tape which [the documentation](/archive/documents/ADR%20Driver%20Development%20Specifications%201.3.pdf) explicitly states "no user data can be recorded".  
So, I modified the open source driver and the dumping program again to read the area "where no user data can be recorded" to match ARCserve's behavior.  
Finally, after months of reverse engineering of ARCServe, the drive firmware, and other garbage, the program worked, and the data was saved.  
Source code for the extraction program is included [here](/software/arcserve-file-extractor/).  

**Recovered Files:**  
![Program Output](/pictures/visual-aids/frogger2-arcserve-output.png)  
![Recovered Files](/pictures/visual-aids/frogger2-recovered-files.png)  

### 6) The Resolution  
In the end, the recovery was an unquestionable success.
Thank you everyone who helped with this project, without your help who knows how long it would have taken or if the data would have even been recovered.  
All the important data such as the VSS repository backup (source code history), final game assets, tools, and more were saved.  
The tape was the only backup for those things, and it completes Frogger 2's development archives, which will be released publicly.  
It might sound bad that approximately 12GB of the 15GB written data was recovered, but this is misleading.  
A couple thousand files were damaged, but they make up less than 5% of the total files on the tape.  
Nearly all of the damaged files were (via extreme luck) either found in backup CDs or were duplicated on another parts of the tape.  
There were only 15 files which were not perfectly recovered, and only one was noteworthy, a CD image of a PC game build from 1 month after release.  
Having recovered 58149 out of the 58164 files on the tape, this adventure can only be considered a success.  
Here's what the damage looks like to the Frogger 2 tape, showing the significant damage and how lucky the recovery was.  
![Frogger 2 Tape Damage](/pictures/visual-aids/frogger2-tape-damage.png)

### 7) Further Technical Details
The hope is to share all the information we learned so that it might help someone else, and let those who are less experienced recover data too.  
Further technical details are scattered throughout the repository, and this guide, and I am willing to answer questions.  