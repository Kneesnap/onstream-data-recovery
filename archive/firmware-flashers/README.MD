# OnStream Firmware Flashers
These are the programs used to flash firmware onto the drives.  
Many of these contain current or outdated versions of the firmware inside of the executable.  

| File Name | Operating System | Included Firmware Date | Included Firmware Version | Supported Drives |
| ------------- | ------------- | ------------- | ------------- | ------------- |
| adr_4_12.exe | Windows | November 15, 2002 | 4.12 | ADR2 Drives |
| adr413.exe | Windows | January 31, 2003 | 4.13 | ADR2 Drives |
| adr_5_05.exe | Windows | October 22, 2002 | 5.05 | ADR2 Drives |
| ADR_6_06.exe | Windows | March 5, 2003 | 6.06 | ADR2 Drives |
| ADR_239_DOS.exe | DOS | February 27, 2001 | Drive: 2.39, Servo: 1.67 | ADR30/ADR50/ADR50e |
| ADR_239_Win.exe | Windows | February 27, 2001 | Drive: 2.39, Servo: 1.67 | ADR30/ADR50/ADR50e |
| DOSFL108.EXE | DOS | N/A | 1.08 (SCSI & IDE) | SC-30/SC-50/DI-30/DP-30 |
| DOSFL109.EXE | DOS | N/A | 1.09 (SCSI & IDE) | SC-30/SC-50/DI-30/DP-30 |
| OnStream_Flash_105.exe | DOS | N/A | 1.05 SCSI, IDE | SC-30/SC-30e/SC-50/DI-30 |
| OnStream_Flash_106.exe | DOS | N/A | 1.06 SCSI, IDE | SC-30/SC-30e/SC-50/DI-30 |
| OnStreamFlash108Ext.exe | Windows | N/A | 1.09 IDE | DP-30/USB-30 |
| OnStreamFlash108Ide.exe | Windows | N/A | 1.09 IDE | DI-30/DI-30 FAST |
| OnStreamFlash108Scsi.exe | Windows | N/A | 1.09 SCSI | SC-30/SC-30e/SC-50 |
| Flash_109_Ext.exe | Windows | None | None | DP-30? |
| Flash_109_Ide.exe | Windows | None | None | DI-30?/USB-30? |
| Flash_109_Scsi.exe | Windows | None | None | SC-30/SC-30e/SC-50 |
| OnStreampd1.06.sit.bin | Mac OS (StuffIt .SIT) | Unknown | Unknown | SC-30e/FW-30? |
| adrfix | Linux | None | None | ADR30/ADR50/ADR50e |
| adridefix | Linux | None | None | ADR30ide/ADR50ide |
| adr2idefix | Linux | None | None | All ADR2 Drives |
| di30-fix | Linux | None | None | DI-30/DI-30 FAST/USB-30?/FW-30? |
| di30-2fix | Linux | None | None | DI-30+/DI-30+ FAST |
| sc30-fix | Linux | None | None | SC30/SC30e/SC50 |

All of the Linux flash programs have two versions, a dynamically-linked version, and a statically linked version (suffixed with "static").  
If you're not a developer, this shouldn't mean anything to you, and you can treat both of the versions as identical.  
These files were mostly sourced from [here](https://web.archive.org/web/*/http://www.hastec.nl:80/files/onstream/*).

## Notes
 - According to [this](https://web.archive.org/web/20031113192335/http://www.hastec.nl:80/support/onstream/support/downgrade_108_win.html), Echo v3.6.24 is required to downgrade to 1.08.  
 - The DP-30 drive is not supported by any Linux software, so it's odd that there's a Linux program which can flash it. Does it really work?  
 - Updating Firmware comes with risk. Be extremely careful to only flash compatible firmware to your drive, and understand that this could render a drive inoperable.  
 - Open the door of your tape drive, and there should be a sticker showing what firmware version was flashed at the factory.  