# OnStream Drivers
These drivers are (mostly) programs which exist exclusively to add tape drive support to your OS.  
They do NOT let you restore data from backups DIRECTLY, but instead allow other software to.  


## Driver List
**adr2 1.12 signed.zip** contains drivers compatible with the ADR30, ADR50, ADR50e, and all ADR2 tape drives on Windows XP/2000.  
**SC2K.sys/SC2K.inf** support the DI30, DI30 FAST, DP30, USB30, SC30, SC30e and SC50 tape drives on Windows XP/2000.  
**ADRNT.sys/ADRNT.inf** support the ADR30, ADR50, ADR50e, and all ADR2 tape drives on Windows NT 4.0.  
**sc30sc50.sys/sc30sc50.inf** support the DI30, Di30 FAST, DP30, SC30, SC30e and SC50 tape drives on Windows NT 4.0.  
**osst.zip** contains a backup of the `osst` Linux SC- driver source code, migrated from CVS to Git.  
**Anything Freecom:** Appears to be a USB driver, for supporting the USB-30 drive. Not sure if this is even necessary or not.  
**epatap2k.sys/epatap2k.inf** is a "Parallel Port ATAPI Driver", probably used with the DP-30 tape drive.  



If OnStream Echo is installed (or other programs such as BackupExec or ARCServe), the standalone drivers are unnecessary / may not work.  