/* SCSI Generic OnStream interface.

  Copyright 1999 Terry Hardie

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

  The author may be contacted at onstream@orcas.net

  The author also makes no waranties as to the reliability of this software.
  Using it is done so entirely at your own risk, and the author will no be
  help liable for any damages ensuing from its use.

  Many thanks go to Onstream Inc, who helped greatly with the development
  of this software with their great specifications and help.

*/

/*

  WARNING: BETA CODE

  Please note! This software may trash your data on your tape(s), and other
  SCSI devices. It may also crash your system.

*/

/*
 * Some changes done by Kurt Garloff (KG) <garloff@suse.de>, 2-3/2000
 * Modified by Kneesnap in 2023 to be suitable for raw dumping.
 */

/*
  Changes
  =======

  0.9.13Beta 2000/3/03	Use polling mode only if needed, because of buggy firmware.
  0.9.12Beta 2000/3/01  Changed skip on read to 40.
	(KG)		Check for Firmware and save revision
			For new firmware (>=1.06): Use the SkipLocate() function for 
				error recovery on write.
			Signal handling changes
  0.9.11Beta 2000/2/29	Check buffer filling by READ_POSITION.
	(KG)		Disabled buffer draining, it does not work.
			Started to convert structs to directly match the ones on tape
				(except for the endinaness, obviously).
			Implemented skip 80 semantics on read, but locate back in
				case of high SeqNumbers.
			Accept both ADR-SEQ and ADR_SEQ as well as 1.1 and 1.2 and
				adjust according to the 1.1 typos if needed.
			If Initializing tapes, now use the fixed 1.2 spec: 0xBAE is the 
				location of the second cfg header frames.
			Flush(), if dirty buffers exist and a locate needs to be done
			VendorID now passed correctly to device.
			Added some debug messages.
			-> With all this, tapes written to by recent and earlier versions
				of osg as well as Gadis ide-tape driver can be handled.
  0.9.10Beta 2/8/2000 - Changed status to beta
  0.9.9Alpha 2/3/2000 - Changed the "f" switch to "i" and the "o" switch to "f"
                      The new "f" switch will now take the named file for reading
					  or writing, depending on the mode (either the presence or
					  absence of the -w switch) This is mostly useful when used
					  with a FIFO. When using this with a FIFO, remember to make
					  you application expect 8k blocks (with tar, use -b 16 or -B)
					  - Fixed a bug in the power on reset case that would start us
					  writing back at the beginning of the tape rather than from
					  the real last known written location
  0.9.8Alpha 2/1/2000 Changed the initial buffer checking to drain the buffer
                      rather than just flushing it - This makes the drive happy
					  for firmware >= 1.05. 
  0.9.7Alpha 12/31/99 Added the retension switch
  0.9.6Alpha 11/3/99  Added some code to attempt a recover after a power on
                      reset. No idea if it works or not. Also changed the
                      EOD block seek to 5 blocks instead of 20.
  0.9.5Alpha 10/16/99 Fixed a bug in the buffer management system - When the
                      tape was able to write all buffers to the disk, and all
					  corresponding internal buffers were freed, new buffers
					  were not tracked correctly, and would result in an
					  "Internal Frame Buffer/Tape buffer mismatch"

                      Added the "-l" flag to send logging to a file

                      Fixed a few other cosmetic bugs

  0.9.4Alpha 09/16/99 Fixed the rewind at the end to really just be a rewind
                      and not a rewind and unload. Also, if the sense is
                      "initializing command required", abort the program
                      Advance 20 frames on getting EOD, and try again.

  0.9.3Alpha 09/15/99 We now get the sense from the last SCSI command, rather
                      than re-requesting it. This fixes the problem with losing
					  the sense on a medium write error
					  Also, shut up the WaitForReady repeat itself when debug
					  is high enough

  0.9.2Alpha 09/12/99 Check to see what type of SCSI device we are talking to
                      before we start to send it commands

  */

/* In theory, anything below 57 should be possible here. In practice,
 * the firmware (mine: 108D) will not reconnect on writes, if this is
 * larger than 50 */
#define MAX_FILL_BUFF 50
/* The "failure to reconnect" firmware bug */
#define OS_NEED_POLL_MIN 10602 /*(107A)*/
#define OS_NEED_POLL_MAX 10708 /*(108D)*/
//#define OS_NEED_POLL(x) ((x) >= OS_NEED_POLL_MIN && (x) <= OS_NEED_POLL_MAX)
#define OS_NEED_POLL(x) (0)

#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <string.h>
#include <errno.h>
#include <fcntl.h>
#include <time.h>
#include <signal.h>
#include <stdarg.h>

#include <netinet/in.h>

#include <sys/ioctl.h>
#include <sys/stat.h>
#include <sys/time.h>
#include <sys/types.h>

#include <linux/version.h>

#include <linux/../scsi/sg.h>
#include <linux/../scsi/scsi.h>

#include <linux/cdrom.h>

//***********************************************
// Constants
//***********************************************

#define SG_GET_RESERVED_SIZE 0x2272
#define SG_GET_SG_TABLESIZE  0x227F
#define SG_SET_COMMAND_Q     0x2271 

#define VERSION "0.9.13Beta"
#define VENDORID "LINX"

const ssize_t cbSGHeader = sizeof(sg_header);

//***********************************************
// Global Variables
//***********************************************
int debug = 6;
FILE* fDebugFile = NULL;
volatile int signalled = 0;
unsigned int TotalBufferedFrames = 0;
const char* szOnStreamErrors[] = {
	"no error",
	"device never became ready for writing",
	"write error",
	"device never became ready for reading",
	"read error",
	"short read from device",
	"SG driver failed",
};


//***********************************************
// Data types
//***********************************************

typedef unsigned char      UINT8;
typedef unsigned short     UINT16;
typedef unsigned long      UINT24;
typedef unsigned long      UINT32;
typedef unsigned long long UINT64;


enum OnStreamError {
	oseNoError = 0,
	oseDeviceWriteTimeout,
	oseDeviceWriteError,
	oseDeviceReadTimeout,
	oseDeviceReadError,
	oseDeviceShortRead,
	oseDeviceFail,
};

enum Sense {
	SNoSense,
	SInvalidCDB,
	SNotReportable,
	SReadyInProgress,
	SInitRequired,
	SNoMedium,
	SLongWrite,
	SMediumWriteError,
	SUnrecoveredReadError,
	STimeoutWaitPos,
	SInvalidParameter,
	SEOD,
	SNotReadyToReady,
	SPowerOnReset,
	SEndOfMedium,
	SUnknown,
};


struct TAPE_PARAMETERS {
	UINT8 Density;
	UINT16 SegTrk;
	UINT16 Trks;
};

struct TAPEBUFFER {
	unsigned char *Frame;
	TAPEBUFFER *Next;
};

struct AUX_FRAME {
	UINT32 FormatID;
	UINT32 ApplicationSig;
	UINT32 HwField;
	UINT32 UpdateFrameCounter;
	UINT16 FrameType;
	UINT16 AUX_reserved1;
	struct PARTITION_DESCRIPTION {
		UINT8 PartitionNumber;
		UINT8 PartDescVersion;
		UINT16 WritePassCounter;
		UINT32 FirstFrameAddress;
		UINT32 LastFrameAddress;
		UINT32 PARTDES_reserved;
	} __attribute__ ((packed)) PartitionDescription;
	UINT8  AUX_reserved2[8];
	UINT32 FrameSequenceNumber;
	UINT64 LogicalBlockAddress;
	struct DATA_ACCESS_TABLE {
		UINT8 nEntries;
		struct DATA_ACCESS_TABLE_ENTRY {
			UINT32 size;
			UINT16 LogicalElements;
			UINT8 flags;
		} DataAccessTableEntry[16];
	} DataAccessTable;
	UINT32 FilemarkCount;
	UINT32 LastMarkFrameAddress;
	unsigned char DriverUnique[32];
} __attribute__ ((packed));

struct TAPEBUFFER *TapeBuffer = NULL;

static char strbuf[128];
char* truncstring (char* buf, int len)
{
	memcpy (strbuf, buf, len);
	strbuf[len] = 0;
	return strbuf;
}

//***********************************************
// Classes
//***********************************************

class OnStream {
public:
	OnStream();
	OnStream(const char* szDevice);
	~OnStream();

	bool OpenDevice(const char* szDevice);
	bool CloseDevice(void);

	bool StartRead(void);
	bool Read(void* pBuffer);
	bool ModeSense(void *sense);
	bool RequestSense(void *sense);
	bool GetTapeParameters(unsigned char result[22]);
	void GetLastSense(void *sense);
	bool BufferStatus(unsigned int *max, unsigned int *current);
	bool VendorID(char ID[4]);
	bool DataTransferMode(bool Aux);
	bool ReadPosition(void);
	enum Sense WaitPosition(unsigned int, int to = 30, int ahead = 0);
	bool Locate(UINT32 nLogicalBlock, bool write = false);
	UINT32 SkipLocate(UINT32 skip);
	bool Flush(void);
	void Drain(void);
	bool Rewind(void);
	bool LURewind(void);
	bool LULoad(void);
	bool IsOnstream(void);

	bool TestUnitReady(void);

	bool ShowPosition(UINT32*, UINT32*);


	UINT8 SenseKey(void);
	UINT8 ASC(void);
	UINT8 ASCQ(void);
	
	OnStreamError GetLastError(void);
	UINT32 FWRev(void);

private:
	sg_header SG;

	UINT8*        pCommandBuffer;
	UINT8*        pResultBuffer;
	UINT8*        pTempBuffer;
	ssize_t       cbCommandBuffer;
	ssize_t       cbResultBuffer;
	ssize_t       cbTempBuffer;
	UINT8         pLastSense[16];

	UINT32        nPacketID;
	UINT32	      Firmware;

	int           nFD;
	OnStreamError LastError;

	void DumpSCSIResult(sg_header* pSG, UINT8* pBuffer);
	bool WaitForRead(const int nSec = 90, const int nUsec = 0);
	bool SCSICommand(const int nSec = 90, const int nUsec = 0);

	void NeedCommandBytes(ssize_t nBytes);
	void NeedResultBytes(ssize_t nBytes);
	void NeedTempBytes(ssize_t nBytes);
	UINT32 ParseFirmwareRev (const char *);

	template<class T> inline const T min(const T t1, const T t2) {
		if (t1 < t2) {
			return t1;
		} else {
			return t2;
		}
	}

	template<class T> inline const T max(const T t1, const T t2) {
		if (t1 < t2) {
			return t2;
		} else {
			return t1;
		}
	}
};

void WaitForReady(OnStream* , bool = false);

//***********************************************
// Function definitions
//***********************************************

void Debug(const int nDebugLevel, const char *format, ...);

void cpAndSwap(void *dest, void *source, unsigned int width) 
{
	unsigned int destPos = 0;
	unsigned int sourcePos = width - 1;

	unsigned char *tDest, *tSource;

	tDest = (unsigned char *) dest;
	tSource = (unsigned char *) source;

	while (destPos < width) {
		tDest[destPos] = tSource[sourcePos];
		sourcePos--;
		destPos++;
	}
}

void unFormatAuxFrame(unsigned char* FAuxFrame, struct AUX_FRAME *AuxFrame) {
	unsigned int counter;

	memset(AuxFrame, 0, sizeof(AuxFrame));
	if (FAuxFrame[0] != '\0' || FAuxFrame[1] != '\0' || FAuxFrame[2] != '\0' 
	 || FAuxFrame[3] != '\0')
		return;

	memcpy(&AuxFrame->ApplicationSig, &FAuxFrame[4], 4);
	cpAndSwap(&AuxFrame->UpdateFrameCounter, &FAuxFrame[12], 4);
	cpAndSwap(&AuxFrame->FrameType, &FAuxFrame[16], 2);
	AuxFrame->PartitionDescription.PartitionNumber = FAuxFrame[20];
	cpAndSwap(&AuxFrame->PartitionDescription.WritePassCounter, &FAuxFrame[22], 2);
	cpAndSwap(&AuxFrame->PartitionDescription.FirstFrameAddress, &FAuxFrame[24], 4);
	cpAndSwap(&AuxFrame->PartitionDescription.LastFrameAddress, &FAuxFrame[28], 4);
	cpAndSwap(&AuxFrame->FrameSequenceNumber, &FAuxFrame[44], 4);
	cpAndSwap(&AuxFrame->LogicalBlockAddress, &FAuxFrame[48], 8);
	AuxFrame->DataAccessTable.nEntries = (FAuxFrame[58] > 16 ? 16 : FAuxFrame[58]);

	for (counter = 0; counter < AuxFrame->DataAccessTable.nEntries; counter++) {
		cpAndSwap(&AuxFrame->DataAccessTable.DataAccessTableEntry[counter].size, &FAuxFrame[60 + (counter * 8)], 4);
		cpAndSwap(&AuxFrame->DataAccessTable.DataAccessTableEntry[counter].LogicalElements, &FAuxFrame[64 + (counter * 8)], 2);
		AuxFrame->DataAccessTable.DataAccessTableEntry[counter].flags = FAuxFrame[66 + (counter * 8)];
	}

	cpAndSwap(&AuxFrame->FilemarkCount, &FAuxFrame[192], 4);
	cpAndSwap(&AuxFrame->LastMarkFrameAddress, &FAuxFrame[200], 4);
	memcpy(AuxFrame->DriverUnique, &FAuxFrame[224], 32);
}

OnStream::OnStream() 
{
	cbCommandBuffer = 0;
	pCommandBuffer  = NULL;
	cbResultBuffer  = 0;
	pResultBuffer   = NULL;
	cbTempBuffer    = 0;
	pTempBuffer     = NULL;
	nPacketID       = 1;
	nFD             = -1;
	Firmware	= 0;
	LastError       = oseNoError;

	memset(&SG, 0, cbSGHeader);

}

OnStream::OnStream(const char* szDevice) 
{
	cbCommandBuffer = 0;
	pCommandBuffer  = NULL;
	cbResultBuffer  = 0;
	pResultBuffer   = NULL;
	cbTempBuffer    = 0;
	pTempBuffer     = NULL;
	nPacketID       = 1;
	nFD             = -1;
	LastError       = oseNoError;

	memset(&SG, 0, cbSGHeader);
	if (!OpenDevice(szDevice)) {
		Debug(0, "OnStream::OnStream: open: Failed - %s (%d)\n", strerror(errno), errno);
		exit(-1);
	}
}

OnStream::~OnStream() 
{
	if (NULL != pCommandBuffer)
		free(pCommandBuffer);

	if (NULL != pResultBuffer)
		free(pResultBuffer);

	if (NULL != pTempBuffer)
		free(pTempBuffer);

	if (-1 != nFD)
		CloseDevice();
}

inline UINT32 OnStream::FWRev(void) 
{
	return Firmware;
}

void OnStream::NeedCommandBytes(ssize_t nBytes) 
{
	void* pTemp;

	if (cbCommandBuffer != nBytes) {
		pTemp = realloc(pCommandBuffer, nBytes);
		if ((NULL == pTemp) && (nBytes > 0)) {
			Debug(0, "OnStream::NeedCommandBytes: fatal: realloc() returned NULL\n");
			abort();
		}
		if (nBytes > 0)
			pCommandBuffer = (UINT8*) pTemp;
		else
			pCommandBuffer = NULL;
		
		cbCommandBuffer = nBytes;
	}
}

void OnStream::NeedResultBytes(ssize_t nBytes) 
{
	void* pTemp;

	if (cbResultBuffer != nBytes) {
		pTemp = realloc(pResultBuffer, nBytes);
		if ((NULL == pTemp) && (nBytes > 0)) {
			Debug(0, "OnStream::NeedResultBytes: fatal: realloc() returned NULL\n");
			abort();
		}
		if (nBytes > 0)
			pResultBuffer = (UINT8*) pTemp;
		else
			pResultBuffer = NULL;

		cbResultBuffer = nBytes;
	}
}

void OnStream::NeedTempBytes(ssize_t nBytes) 
{
	void* pTemp;

	if (cbTempBuffer != nBytes) {
		pTemp = realloc(pTempBuffer, nBytes);
		if ((NULL == pTemp) && (nBytes > 0)) {
			Debug(0, "OnStream::NeedTempBytes: fatal: realloc() returned NULL\n");
			abort();
		}
		if (nBytes > 0)
			pTempBuffer = (UINT8*) pTemp;
		else
			pTempBuffer = NULL;

		cbTempBuffer = nBytes;
	}
}

UINT8 OnStream::SenseKey(void) 
{
	return SG.sense_buffer[2] & 0x0F;
}

UINT8 OnStream::ASC(void) 
{
	return SG.sense_buffer[12];
}

UINT8 OnStream::ASCQ(void) 
{
	return SG.sense_buffer[13];
}

OnStreamError OnStream::GetLastError(void) 
{
	return LastError;
}

bool OnStream::OpenDevice(const char* szDeviceName) 
{
	nFD = open(szDeviceName, O_RDWR);
	return (-1 != nFD);
}

bool OnStream::CloseDevice(void) 
{
	if (-1 != nFD)
		close(nFD);

	return true;
}

bool OnStream::StartRead(void) 
{
	NeedCommandBytes(6);
	NeedResultBytes(0);

	pCommandBuffer[0] = 0x08; // READ
	pCommandBuffer[1] = 0x01; // 7-2: reserved; 1: SILI; 0: Fixed
	pCommandBuffer[2] = 0x00; // Transfer length, 23-16
	pCommandBuffer[3] = 0x00; // Transfer length, 15-8
	pCommandBuffer[4] = 0x00; // Transfer length, 7-0
	pCommandBuffer[5] = 0x00; // reserved

	return SCSICommand();
}

/* Acc. to OnStream, the vers. numbering is the following:
 * X.XX for released versions (X=digit), 
 * XXXY for unreleased versions (Y=letter)
 * Ordering 1.05 < 106A < 106a < 106B < ... < 1.06
 * This fn makes monoton numbers out of this scheme ...
 */
UINT32 OnStream::ParseFirmwareRev (const char * str)
{
	UINT32 rev;
	if (str[1] == '.') {
		rev = (str[0]-0x30)*10000
			+(str[2]-0x30)*1000
			+(str[3]-0x30)*100;
	} else {
		rev = (str[0]-0x30)*10000
			+(str[1]-0x30)*1000
			+(str[2]-0x30)*100 - 100;
		rev += 2*(str[3] & 0x1f)
			+(str[3] >= 0x60? 1: 0);
	}
	return rev;
}

bool OnStream::IsOnstream(void) 
{
	NeedCommandBytes(6);
	NeedResultBytes(36);

	pCommandBuffer[0] = 0x12;
	pCommandBuffer[1] = 0x00;
	pCommandBuffer[2] = 0x00;
	pCommandBuffer[3] = 0x00;
	pCommandBuffer[4] = 0x24;
	pCommandBuffer[5] = 0x00;

	if (false == SCSICommand())
		return false;

	if ((pResultBuffer[0] & 0x1F) != 0x01) {
		Debug(0, "Device is not a tape drive\n");
		return false;
	}
	
	char VendorID[9];
	char ProductID[17];
	char FirmRev[5];

	strncpy(VendorID, (const char *) &pResultBuffer[8], 8);
	VendorID[8] = 0;
	strncpy(ProductID, (const char *) &pResultBuffer[16], 16);
	ProductID[16] = 0;
	strncpy(FirmRev, (const char *) &pResultBuffer[32], 4);
	FirmRev[4] = 0;
	Firmware = ParseFirmwareRev (FirmRev);

	Debug(4, "Vendor-ID : %s\nProduct-ID: %s\nFirmware  : %s (%i)\n",
	      VendorID, ProductID, FirmRev, Firmware);
	if (strcmp(VendorID, "OnStream") != 0) {
		Debug(0, "Vendor-ID %s not supported\n", VendorID);
		return false;
	}
	if (strcmp(ProductID, "SC-30           ") != 0 && 
	    strcmp(ProductID, "SC-50           ") != 0 &&
	    strcmp(ProductID, "SC-70           ") != 0 )
	{
		Debug(0, "Product %s not supported by this version\n", ProductID);
		return false;
	}
	return true;
}

bool OnStream::Read(void* pBuffer) 
{
	NeedCommandBytes(6);
	NeedResultBytes(33280);

	pCommandBuffer[0] = 0x08; // READ
	pCommandBuffer[1] = 0x01; // 7-2: reserved; 1: SILI; 0: Fixed
	pCommandBuffer[2] = 0x00; // Transfer length, 23-16
	pCommandBuffer[3] = 0x00; // Transfer length, 15-8
	pCommandBuffer[4] = 0x01; // Transfer length, 7-0
	pCommandBuffer[5] = 0x00; // reserved

	if (false == SCSICommand())
		return false;

	memmove(pBuffer, pResultBuffer, 33280);
	return true;
}

void OnStream::GetLastSense(void *sense) 
{
	memcpy(sense, pLastSense, 16);
}

bool OnStream::RequestSense(void *sense) 
{
	NeedCommandBytes(6);
	NeedResultBytes(16);

	pCommandBuffer[0] = 0x03; // Request Sense
	pCommandBuffer[1] = 0x00; // Reserved
	pCommandBuffer[2] = 0x00; // Reserved
	pCommandBuffer[3] = 0x00; // Reserved
	pCommandBuffer[4] = 0x10; // Allocation length
	pCommandBuffer[5] = 0x00; // reserved

	if (false == SCSICommand())
		return false;

	memmove(sense, pResultBuffer, 16);
	return true;
}

bool OnStream::ModeSense(void *sense)
 {
	NeedCommandBytes(6);
	NeedResultBytes(32768);

	pCommandBuffer[0] = 0x1A; // MODE SENSE
	pCommandBuffer[1] = 0x08; // 7-4: reserved 3: DBD 2-0: reserved
	pCommandBuffer[2] = 0x00; // 7-6 = PC 5-0: Page Code
	*((UINT32*) &pCommandBuffer[3]) = htonl(32768); // Allocation length (bytes 3 and 4)
	pCommandBuffer[5] = 0x00; // Reserved

	if (false == SCSICommand(30, 0))
		return false;

	memmove(sense, pResultBuffer, 32768);
	return true;
}

bool OnStream::GetTapeParameters(unsigned char result[22]) 
{
	NeedCommandBytes(6);
	NeedResultBytes(22);

	pCommandBuffer[0] = 0x1A; // MODE SENSE
	pCommandBuffer[1] = 0x08; // 7-4: reserved 3: DBD 2-0: reserved
	pCommandBuffer[2] = 0x2B; // 7-6 = PC 5-0: Page Code
	pCommandBuffer[3] = 0x00; // Allocation length (bytes 3 and 4)
	pCommandBuffer[4] = 0x16; // Allocation length (bytes 3 and 4)
	pCommandBuffer[5] = 0x00; // Reserved

	if (false == SCSICommand(30, 0))
		return false;

	memmove(result, pResultBuffer, 16);
	return true;
}

bool OnStream::BufferStatus(unsigned int *max, unsigned int *current) 
{
	NeedCommandBytes(6);
	NeedResultBytes(8);

	pCommandBuffer[0] = 0x1A; // MODE SENSE
	pCommandBuffer[1] = 0x08; // 7-4: reserved 3: DBD 2-0: reserved
	pCommandBuffer[2] = 0x33; // 7-6 = PC 5-0: Page Code
	pCommandBuffer[3] = 0x00; // Allocation length (bytes 3 and 4)
	pCommandBuffer[4] = 0x08; // Allocation length (bytes 3 and 4)
	pCommandBuffer[5] = 0x00; // Reserved

	Debug(8, "Sending Buffer Status\n");
	if (false == SCSICommand(30, 0))
		return false;

	if (debug > 5) {
		int counter;
		for (counter = 0; counter < 8; counter++) {
			 Debug(6, "%02x ", (unsigned char) pResultBuffer[counter]);
		}
		Debug(6, "\n");
	}
	*max = (unsigned int) (unsigned char) pResultBuffer[6];
	*current = (unsigned int) (unsigned char) pResultBuffer[7];
	Debug(5, "Buffer_Status: %i/%i\n", *current, *max);
	if (*current > *max) {
		Debug(1, "WARNING: Drive reported more blocks in buffer than buffers available. Total = %d, used = %d\n", *max, *current);
	}
	return true;
}

/* This never actually works ... */
void OnStream::Drain ()
{
	unsigned char *buf = (unsigned char *) malloc (33280);
	int first, last;
	unsigned int MaxBuffer, CurrentBuffer;
	do {
		do {
			ReadPosition ();
			first = ntohl (*((UINT32*) &pResultBuffer[4]));
			last  = ntohl (*((UINT32*) &pResultBuffer[8]));
			BufferStatus(&MaxBuffer, &CurrentBuffer);
			Debug (3, "Position: %i-%i\n", first, last);
			if (CurrentBuffer < 128) {
				// This is because the actual value is a signed char - If its >= 128, the drive is reading blocks for us. We have to wait
				break;
			}
			Debug(2, "Drive is reading config for us...\n");
			sleep(5);
		} while (1);
		if (CurrentBuffer > 0 && last != first) {
			Debug(2, "Draining buffer(s) from drive.\n");
			
			for (unsigned int counter = 0; counter < CurrentBuffer; counter++) {
				Debug(5, "Draining buffer %d\n", counter);
				if (!Read(buf)) {
					Debug(0, "Can't drain buffer from drive.\n");
					exit (-1);
				}
			}
			Debug(2, "Done.");
		}
	} while (CurrentBuffer != 0 && last > first);
	free (buf);
}
 
bool OnStream::VendorID(char ID[4]) 
{
	NeedCommandBytes(18);
	NeedResultBytes(0);

	pCommandBuffer[0] = 0x15; // MODE SELECT
	pCommandBuffer[1] = 0x10; // 7-5: reserved 4: PF 3-1: Reserved 0: SP
	pCommandBuffer[2] = 0x00; // reserved
	pCommandBuffer[3] = 0x00; // length (MSB)
	pCommandBuffer[4] = 0x0c; // Length 12 bytes of mode data
	pCommandBuffer[5] = 0x00; // reserved
	pCommandBuffer[6] = 0x08; // Mode data length
	pCommandBuffer[7] = 0x00; // Medium type ??
	pCommandBuffer[8] = 0x00; // reserved
	pCommandBuffer[9] = 0x00; // block descriptor length
	pCommandBuffer[10] = 0xB6; // 7: PS 6: reserved 5-0: Page code (36 Vendor ID)
	pCommandBuffer[11] = 0x06; // ??
	memcpy(&pCommandBuffer[12], ID, 4); // Vendor ID string
	pCommandBuffer[16] = 0; pCommandBuffer[17] = 0;

	return SCSICommand();
}

bool OnStream::DataTransferMode(bool Aux) 
{
	NeedCommandBytes(18);
	NeedResultBytes(0);

	pCommandBuffer[0] = 0x15; // MODE SELECT
	pCommandBuffer[1] = 0x10; // 7-5: reserved 4: PF 3-1: Reserved 0: SP
	pCommandBuffer[2] = 0x00; // reserved
	pCommandBuffer[3] = 0x00; // length (MSB)
	pCommandBuffer[4] = 0x08; // Length 8 bytes of mode data
	pCommandBuffer[5] = 0x00; // reserved
	pCommandBuffer[6] = 0x07; // Mode data length
	pCommandBuffer[7] = 0x00; // Medium type ??
	pCommandBuffer[8] = 0x00; // reserved
	pCommandBuffer[9] = 0x00; // block descriptor length
	pCommandBuffer[10] = 0xB0; // 7: PS 6: reserved 5-0: Page code (30 Data Transfer Mode)
	pCommandBuffer[11] = 0x02; // ??
	pCommandBuffer[12] = 0x00; // reserved
	if (Aux)
		pCommandBuffer[13] = 0xA2; // 7: Streaming mode 6: reserved 5: 32.5k record 4: 32k record 3-2: reserved 1: 32.5k playback 0: 32k playback
	else
		pCommandBuffer[13] = 0x91; // 7: Streaming mode 6: reserved 5: 32.5k record 4: 32k record 3-2: reserved 1: 32.5k playback 0: 32k playback

	return SCSICommand();
}

bool OnStream::ReadPosition(void) 
{
	NeedCommandBytes(10);
	NeedResultBytes(20);

	pCommandBuffer[0] = 0x34; // READ POSITION
	pCommandBuffer[1] = 0x00; // 7-1: reserved 0: BT
	pCommandBuffer[2] = 0x00; // reserved
	pCommandBuffer[3] = 0x00; // reserved
	pCommandBuffer[4] = 0x00; // reserved
	pCommandBuffer[5] = 0x00; // reserved
	pCommandBuffer[6] = 0x00; // reserved
	pCommandBuffer[7] = 0x00; // reserved
	pCommandBuffer[8] = 0x00; // reserved
	pCommandBuffer[9] = 0x00; // reserved

	return SCSICommand();
}

bool OnStream::Locate(UINT32 nLogicalBlock, bool write) 
{
	if (write) {
		if (!Flush()) 
			return false;
		WaitForReady(this);
	}
	NeedCommandBytes(10);
	NeedResultBytes(0);

	pCommandBuffer[0] = 0x2B; // LOCATE
	pCommandBuffer[1] = 0x01; // 7-3: reserved; 2: BT; 1: 0; 0: Immed
	pCommandBuffer[2] = 0x00; // reserved
	cpAndSwap(&pCommandBuffer[3], &nLogicalBlock, 4); // logical block position
	pCommandBuffer[7] = 0x00; // reserved;
	pCommandBuffer[8] = 0x00; // reserved;
	pCommandBuffer[9] = 0x00; // 7: SKIP; 6-0: reserved

	return SCSICommand();
}

/* This is the new (1.06) way of recovering write errors */
UINT32 OnStream::SkipLocate(UINT32 skip) 
{
	if (Firmware < 10600) 
		return 0;
	if (!ReadPosition()) 
		return 0;
	UINT32 first = ntohl (*((UINT32*) &pResultBuffer[4]));
	UINT32 last  = ntohl (*((UINT32*) &pResultBuffer[8]));
	UINT32 nLogicalBlock = last + skip;

	Debug (2, "SkipLocate to pos %i\n", nLogicalBlock);
	
	NeedCommandBytes(10);
	NeedResultBytes(0);

	pCommandBuffer[0] = 0x2B; // LOCATE
	pCommandBuffer[1] = 0x01; // 7-3: reserved; 2: BT; 1: 0; 0: Immed
	pCommandBuffer[2] = 0x00; // reserved
	cpAndSwap(&pCommandBuffer[3], &nLogicalBlock, 4); // logical block position
	pCommandBuffer[7] = 0x00; // reserved;
	pCommandBuffer[8] = 0x00; // reserved;
	pCommandBuffer[9] = 0x80; // SKIP: Don't throw away buffers

	if (!SCSICommand()) 
		return 0;
	/* With the skip, it seems, we don't need to restart writing */
	/* StartWrite(); */
	if (!ReadPosition()) 
		return 0;
	first = ntohl (*((UINT32*) &pResultBuffer[4]));
	last  = ntohl (*((UINT32*) &pResultBuffer[8]));
	return first;
}

bool OnStream::Rewind(void) 
{
	NeedCommandBytes(6);
	NeedResultBytes(0);

	pCommandBuffer[0] = 0x01; // REWIND
	pCommandBuffer[1] = 0x01; // 7-1: reserved; 0: Immed
	pCommandBuffer[2] = 0x00; // reserved
	pCommandBuffer[3] = 0x00; // reserved
	pCommandBuffer[4] = 0x00; // reserved
	pCommandBuffer[5] = 0x00; // reserved

	return SCSICommand();
}


/* Flush is done by writefilemarks (!) */
bool OnStream::Flush(void) 
{
	NeedCommandBytes(6);
	NeedResultBytes(0);

	pCommandBuffer[0] = 0x10; // WRITE FILEMARKS
	pCommandBuffer[1] = 0x01; // 7-1: reserved; 0: Immed
	pCommandBuffer[2] = 0x00; // reserved
	pCommandBuffer[3] = 0x00; // reserved
	pCommandBuffer[4] = 0x00; // reserved
	pCommandBuffer[5] = 0x00; // reserved

	return SCSICommand();
}

bool OnStream::LURewind(void) 
{
	NeedCommandBytes(6);
	NeedResultBytes(0);

	pCommandBuffer[0] = 0x1B; // LOAD/UNLOAD
	pCommandBuffer[1] = 0x01; // 7-1: reserved; 0: Immed
	pCommandBuffer[2] = 0x00; // reserved
	pCommandBuffer[3] = 0x00; // reserved
	pCommandBuffer[4] = 0x00; // 7-3: reserved; 2: LoEj; 1: Re-Ten; 0: Load
	pCommandBuffer[5] = 0x00; // reserved

	return SCSICommand();
}

bool OnStream::LULoad(void) 
{
	NeedCommandBytes(6);
	NeedResultBytes(0);

	pCommandBuffer[0] = 0x1B; // LOAD/UNLOAD
	pCommandBuffer[1] = 0x01; // 7-1: reserved; 0: Immed
	pCommandBuffer[2] = 0x00; // reserved
	pCommandBuffer[3] = 0x00; // reserved
	pCommandBuffer[4] = 0x01; // 7-3: reserved; 2: LoEj; 1: Re-Ten; 0: Load
	pCommandBuffer[5] = 0x00; // reserved

	return SCSICommand();
}

bool OnStream::TestUnitReady(void) 
{
	NeedCommandBytes(6);
	NeedResultBytes(0);

	pCommandBuffer[0] = 0x00; // TEST UNIT READY
	pCommandBuffer[1] = 0x00; // reserved
	pCommandBuffer[2] = 0x00; // reserved
	pCommandBuffer[3] = 0x00; // reserved
	pCommandBuffer[4] = 0x00; // reserved
	pCommandBuffer[5] = 0x00; // reserved

	return SCSICommand();
}

//***********************************************
// WaitForRead: wait for the specified file descriptor to become ready for reading
// Inputs:  file descriptor to wait on and optional timeout
// Outputs: true if ready for reading
//          false if an error occured or not ready within timeout period
bool OnStream::WaitForRead(const int nSec, const int nUsec) 
{
	fd_set fds;
	timeval tv;
	int rc;

	FD_ZERO(&fds);
	FD_SET(nFD, &fds);
	tv.tv_sec = nSec;
	tv.tv_usec = nUsec;

	while (true) {
		rc = select(nFD+1, &fds, NULL, NULL, &tv);
		if (rc > 0)
			return true;

		if ((rc < 0) && (EINTR != errno))
			return false;

		if (0 == rc) 
			return false;
	}
}

void signalHandler(int sig) 
{
	signalled = sig;
	Debug(0, "Got signal %d. Completeing current action...", sig);
	/* Reset to standard behaviour: Next signal will be fatal ... */
	signal (sig, SIG_DFL);
}

//***********************************************
// SCSICommand: pass a SCSI command to the device and wait for the reply
// Inputs:  file descriptor for SCSI device
//          length of SCSI command
//          buffer containing SCSI command
//          expected response length
//          buffer to receive response
//          sg_header to store received SCSI status in
// Outputs: -1          device never became ready for writing
//          -2          write error
//          -3          device never became ready for reading
//          -4          read error
//          -5          short read looking for header
//          -6          SG driver failed silently
//          otherwise   number of bytes read (not counting sg_header)
//          false if an error occured
bool OnStream::SCSICommand(const int nSec, const int nUsec) 
{
	sg_header* pSG;
	ssize_t rc;

	NeedTempBytes(cbSGHeader + max(cbCommandBuffer, cbResultBuffer));
	pSG = (sg_header*) pTempBuffer;
	memset(pSG, 0, cbSGHeader);

	pSG->pack_id     = nPacketID++;
	pSG->twelve_byte = (12 == cbCommandBuffer);
	pSG->result      = 0;
	pSG->reply_len   = cbSGHeader + cbResultBuffer;

	memmove(&pTempBuffer[cbSGHeader], pCommandBuffer, cbCommandBuffer);

	Debug(7, "Sending command of %d bytes...", cbCommandBuffer);
	rc = write(nFD, pTempBuffer, cbCommandBuffer + cbSGHeader);
	if (rc < cbSGHeader + cbCommandBuffer) {
		LastError = oseDeviceWriteError;
		fprintf(stderr, "SCSICommand: write failed\n");
		DumpSCSIResult(pSG, &pTempBuffer[cbSGHeader]);
		return false;
	}

	//memset(pResultBuffer, 0xBB, cbResultBuffer);
	//memset(pTempBuffer, 0xAA, cbResultBuffer + cbSGHeader);
	//memset(pSG, 0xCC, cbSGHeader);

	Debug(7, "Waiting for read...");
	if (false == WaitForRead(nSec, nUsec)) {
		LastError = oseDeviceReadTimeout;
		Debug(0, "SCSICommand: WaitForRead failed\n");
		DumpSCSIResult(pSG, &pTempBuffer[cbSGHeader]);
		return false;
	}

	Debug(7, "Reading %d bytes...", cbResultBuffer);
	rc = read(nFD, pTempBuffer, cbSGHeader + cbResultBuffer);
	Debug(7, "Done.\n");
	memcpy(pLastSense, pSG->sense_buffer, 16);
	if (rc < 0) {
		LastError = oseDeviceReadError;
		Debug(0, "SCSICommand: read error\n");
		DumpSCSIResult(pSG, &pTempBuffer[cbSGHeader]);
		return false;
	}
	if (rc < cbSGHeader) {
		LastError = oseDeviceShortRead;
		Debug(0, "SCSICommand: short read failed\n");
		DumpSCSIResult(pSG, &pTempBuffer[cbSGHeader]);
		return false;
	}
/*
	if (*((long*) &pTempBuffer[cbSGHeader]) == (long) 0xAAAAAAAA) {
		LastError = oseDeviceFail;
		Debug(0, "SCSICommand: Return 0xAAAAAAAA failed\n");
		DumpSCSIResult(pSG, &pTempBuffer[cbSGHeader]);
		return false;
	}
*/
	if (debug >= 6)
		DumpSCSIResult(pSG, &pTempBuffer[cbSGHeader]);

	if (cbResultBuffer > 0)
		memmove(pResultBuffer, pTempBuffer + cbSGHeader, pSG->pack_len - cbSGHeader);

	memmove(&SG, pTempBuffer, cbSGHeader);
	NeedResultBytes(rc - cbSGHeader);
#if 0	
	if (signalled) {
		Debug(0, "Caught signal %d. Aborting\n", signalled);
		raise (signalled);
	}
#endif
	return true;
}

bool OnStream::ShowPosition(UINT32 *host, UINT32 *tape) 
{
	if (!ReadPosition())
		return false;

	if (pResultBuffer[0] & 0xc0) {
		if (pResultBuffer[0] & 0x80)
			Debug(3, "BOP\n");
		else
			Debug(3, "EOP\n");
	}
	Debug(3, "First Frame postion to/from host: %d\n", ntohl(*((UINT32*) &pResultBuffer[4])));
	if (host != NULL)
		*host = ntohl(*((UINT32*) &pResultBuffer[4]));

	Debug(3, "Last Frame postion to/from tape: %d\n", ntohl(*((UINT32*) &pResultBuffer[8])));
	if (tape != NULL)
		*tape = ntohl(*((UINT32*) &pResultBuffer[8]));

	Debug(3, "Blocks in tape buffer: %d\n", *((UINT8*) &pResultBuffer[15]));
	return true;
}
		
enum Sense CheckSense(OnStream *pOnStream);
enum Sense OnStream::WaitPosition (unsigned int CurrentFrame, int timeout, int ahead)
{
	unsigned int first, last; int cntr = 0; enum Sense sense;
	/* The tape should not need longer than half a minute */
	while (cntr <= 5*timeout) {
		//WaitForReady(this);
		sense = CheckSense (this);
		if (!ReadPosition())
			return (enum Sense)0x020400;
		first = ntohl (*((UINT32*) &pResultBuffer[4]));
		last  = ntohl (*((UINT32*) &pResultBuffer[8]));
		if (cntr) Debug (3, "Wait for buffer (pos=%i, buffer=%i-%i, wait>%i) %3i.%i \r",
		       CurrentFrame, first, last, CurrentFrame-ahead, cntr/5, (cntr%5)*2);
		if ((CurrentFrame == first && CurrentFrame < last + ahead) ||
		    sense != SNoSense)
		{
			if (cntr) Debug (3, "\n");
			return sense;
		}
		usleep (200*1000); cntr++;
	}
	Debug (3, "\n");
	return STimeoutWaitPos;
}

void OnStream::DumpSCSIResult(sg_header* pSG, UINT8* pBuffer) 
{
	bool shouldDisplay = false;
	for (int i = 0; !shouldDisplay && i < 16; i++)
		if (pSG->sense_buffer[i])
			shouldDisplay = true;
	
	if (false == shouldDisplay)
		return; // No response, don't bother displaying.
	
	Debug(0, "pack_len:      %d\n",   pSG->pack_len);
	Debug(0, "pack_id:       %d\n",   pSG->pack_id);
	Debug(0, "result:        %02x\n", pSG->result);
	//Debug(0, "target_status: %02x\n", pSG->target_status);
	//Debug(0, "host_status:   %02x\n", pSG->host_status);
	//Debug(0, "driver_status: %02x\n", pSG->driver_status);
	Debug(0, "other_flags:   %03x\n", pSG->other_flags);
	Debug(0, "sense[0..3]:   %02x %02x %02x %02x\n", pSG->sense_buffer[0],  pSG->sense_buffer[1],  pSG->sense_buffer[2],  pSG->sense_buffer[3]);
	Debug(0, "sense[4..7]:   %02x %02x %02x %02x\n", pSG->sense_buffer[4],  pSG->sense_buffer[5],  pSG->sense_buffer[6],  pSG->sense_buffer[7]);
	Debug(0, "sense[8..11]:  %02x %02x %02x %02x\n", pSG->sense_buffer[8],  pSG->sense_buffer[9],  pSG->sense_buffer[10], pSG->sense_buffer[11]);
	Debug(0, "sense[12..15]: %02x %02x %02x %02x\n", pSG->sense_buffer[12], pSG->sense_buffer[13], pSG->sense_buffer[14], pSG->sense_buffer[15]);
	//write(1, &pTempBuffer[cbSGHeader], rc - cbSGHeader);
}

struct TAPE_PARAMETERS GetTapeParameters(OnStream* pOnStream) 
{
	unsigned char buf[22];
	struct TAPE_PARAMETERS tp;

	if (false == pOnStream->GetTapeParameters(buf)) {
		Debug(0, "GetTapeParameters: GetTapeParameters failed: '%s'\n", szOnStreamErrors[pOnStream->GetLastError()]);
		delete pOnStream;
		exit(1);
	}

	tp.Density = buf[6];
	tp.SegTrk = (buf[10] << 8) + buf[11];
	tp.Trks = (buf[12] << 8) + buf[13];

	return tp;
}

void WaitForReady(OnStream* pOnStream, bool fReadyOnNoMedium) 
{
	bool fNotReady = true;
	UINT32 uThisSense = 0;
	UINT32 uLastSense = 0;

	while (fNotReady) {
		if (false == pOnStream->TestUnitReady()) {
			Debug(0, "WaitForReady: TestUnitReady failed: '%s'\n", szOnStreamErrors[pOnStream->GetLastError()]);
			delete pOnStream;
			exit(1);
		}

		switch (uThisSense = ((pOnStream->SenseKey() << 16) | (pOnStream->ASC() << 8) | pOnStream->ASCQ())) {
			case 0x000000: // NO ADDITIONAL SENSE INFORMATION
				fNotReady = false;
				continue;

			case 0x052400: // INVALID FIELD IN CDB
				Debug(0, "WaitForReady: Invalid field in CDB\n");
				exit(1);

			case 0x020400: // NOT READY - CAUSE NOT REPORTABLE
				if (uThisSense != uLastSense) {
					Debug(0, "WaitForReady: Not ready, cause not reportable\n");
				}
				break;

			case 0x020401: // NOT READY - IN PROGRESS OF BECOMING READY
				if (uThisSense != uLastSense) {
					Debug(2, "WaitForReady: Not ready, in progress of becoming ready\n");
				}
				break;

			case 0x020402: // NOT READY - INITIALIZING COMMAND REQUIRED
				Debug(0, "WaitForReady: Not ready, initializing command required\n");
				exit(-1);

			case 0x023A00: // MEDIUM NOT PRESENT
				Debug(0, "WaitForReady: Medium not present\n");
				if (!fReadyOnNoMedium) {
					exit(-1);
				}
				break;

			case 0x020408: // NOT READY - LONG WRITE IN PROGRESS
				if (uThisSense != uLastSense) {
					Debug(0, "WaitForReady: Not ready, long write in progress\n");
				}
				break;

			case 0x030C00: // MEDIUM ERROR: WRITE ERROR
				Debug(0, "WaitForReady: Medium error: write error\n");
				exit(1);

			case 0x062800: // NOT READY TO READY TRANSITION
				if (uThisSense != uLastSense) {
					Debug(2, "WaitForReady: Not ready to ready transition\n");
				}
				break;

			case 0x062900: // POWER ON RESET OR DEVICE RESET OCCURED
				Debug(0, "WaitForReady: Power-on reset or device reset occured\n");
				exit(1);

			default:
				Debug(0, "WaitForReady: Unknown sense key %02x, ASC %02x, ASCQ %02x\n", pOnStream->SenseKey(), pOnStream->ASC(), pOnStream->ASCQ());
				exit(1);
		}
		sleep(1);
		uLastSense = uThisSense;
	}
	Debug(2, "Ready.\n");
}

enum Sense CheckSense(OnStream *pOnStream) 
{
	switch ((pOnStream->SenseKey() << 16) | (pOnStream->ASC() << 8) | pOnStream->ASCQ()) {
		case 0x000000: // NO ADDITIONAL SENSE INFORMATION
			return SNoSense;

		case 0x052400: // INVALID FIELD IN CDB
			Debug(2, "CheckSense: Invalid field in CDB\n");
			return SInvalidCDB;

		case 0x020400: // NOT READY - CAUSE NOT REPORTABLE
			Debug(2, "CheckSense: Not ready, cause not reportable\n");
			return SNotReportable;

		case 0x020401: // NOT READY - IN PROGRESS OF BECOMING READY
			Debug(2, "CheckSense: Not ready, in progress of becoming ready\n");
			return SReadyInProgress;

		case 0x020402: // NOT READY - INITIALIZING COMMAND REQUIRED
			Debug(2, "CheckSense: Not ready, initializing command required\n");
			return SInitRequired;

		case 0x023A00: // MEDIUM NOT PRESENT
			Debug(2, "CheckSense: Medium not present\n");
			return SNoMedium;

		case 0x020408: // NOT READY - LONG WRITE IN PROGRESS
			Debug(2, "CheckSense: Not ready, long write in progress\n");
			return SLongWrite;

		case 0x031100: // UNRECOVERED READ ERROR
			Debug(2, "CheckSense: Unrecovered Read error\n");
			return SUnrecoveredReadError;

		case 0x030C00: // MEDIUM ERROR: WRITE ERROR
			Debug(2, "CheckSense: Medium error: write error\n");
			return SMediumWriteError;

		case 0x052602: // PARAMETER VALUE INVALID
			Debug(2, "CheckSense: Parameter value invalid\n");
			return SInvalidParameter;

		case 0x062800: // NOT READY TO READY TRANSITION
			Debug(2, "CheckSense: Not ready to ready transition\n");
			return SNotReadyToReady;

		case 0x062900: // POWER ON RESET OR DEVICE RESET OCCURED
			Debug(2, "CheckSense: Power-on reset or device reset occured\n");
			return SPowerOnReset;

		case 0x0D0002: // End of Medium
			Debug(2, "CheckSense: End of Medium detected\n");
			return SEndOfMedium;

		case 0x080005: // END OF DATA
			Debug(2, "CheckSense: End of Data\n");
			return SEOD;

		default:
			Debug(0, "CheckSense: Unknown sense key %02x, ASC %02x, ASCQ %02x\n", pOnStream->SenseKey(), pOnStream->ASC(), pOnStream->ASCQ());
			return SUnknown;
	}
}

void Debug(const int nDebugLevel, const char *format, ...) 
{
	char data[1024];
	va_list args;

	va_start(args, format);
	vsnprintf(data, 1024, format, args);
	va_end(args);

	if (debug >= nDebugLevel) {
		if (fDebugFile == NULL)
			fprintf(stderr, data);
		else
			fprintf(fDebugFile, data);
	}
}

int main(int argc, char* argv[]) 
{
	OnStream* pOnStream;
	Sense CurrentSense;
	unsigned char buf[33280];
	struct TAPEBUFFER *LastTapeBuffer = NULL;
	unsigned long long totalBytes = 0;
	int rc = 0;
	unsigned int CurrentTapeBuffer;
	unsigned int mode = 2;
	unsigned int eof = 0;
	unsigned int FormatUnderstood = 0;
	UINT32 CurrentFrame;
	UINT32 TotalFrames;
	unsigned int MaxBuffer, CurrentBuffer;
	unsigned int WritePass;
	unsigned long int CurrentSeqNo;
	unsigned long long capacity;
	unsigned int format = 0;
	unsigned int retry = 0;
	/* The ADR version: 1000*major + 2*minor */
 	unsigned int adr_version;
	/* StartFrame and second_cfg were different in old versions */
	unsigned int StartFrame = 0;
	bool StartFrameSet = false;
	char option;
	time_t startTime;
	struct TAPE_PARAMETERS tp;
	struct AUX_FRAME AuxFrame;
	char ApplicationSig[5];
	char *filename = NULL;
	char *logfilename = NULL;
	FILE *fil;
	FILE *fFile = stdin;
	short SCSIDeviceNo = -1;
	int help = 0;
	char deviceName[32];
	int rewind = 0;
	int multiple = 0;

	opterr = 0; // Supress errors from getops
	while ((option = getopt(argc, argv, "rd::f:l:s:n:")) != EOF) {
		switch (option) {
		case 'd':
			if ((debug = atoi(optarg)) == 0) {
				debug = 1;
			}
			break;
		case 'r':
			rewind = 1;
			break;
		case 'l':
			logfilename = strdup(optarg);
			break;
		case 'f':
			filename = strdup(optarg);
			break;
		case 'n':
			SCSIDeviceNo = atoi(optarg);
			break;
		case 's':
			StartFrameSet = true;
			StartFrame = atoi(optarg);
			break;
		}
	}

	if (help || SCSIDeviceNo == -1) {
		fprintf(stderr, "%s: SCSI Generic OnStream Tape interface. Written by Terry Hardie.\nVersion %s\n", argv[0], VERSION);
		fprintf(stderr, "usage: %s -n device_num [-d [level]] [-o filename] [-s block]\n", argv[0]);
		fprintf(stderr, "       -n device No SCSI device number of OnStream drive **\n");
		fprintf(stderr, "       -d [level]   set debug mode to level\n");
		fprintf(stderr, "       -l filename  write debugging output to named file\n");
		fprintf(stderr, "       -f filename  Use named file for data output\n");
		fprintf(stderr, "       -r           Rewind tape (and do nothing else)\n");
		fprintf(stderr, "       -s block     start reading from this block, instead of current position\n");
		fprintf(stderr, "\n");
		fprintf(stderr, "** This is not the SCSI ID number, but rather which numbered device in\n");
		fprintf(stderr, "   the bus this device is. For Eaxmple, if you have a hard drive at ID 2,\n");
		fprintf(stderr, "   and your OnStream drive at ID 5, then this value should be 1 (0 is the\n");
		fprintf(stderr, "   hard drive\n");
		fprintf(stderr, "***In this mode, when EOF is read from the input file, the tape is closed,\n");
		fprintf(stderr, "   rewound, and the file is then waited on for more data. When more data\n");
		fprintf(stderr, "   become avilable, the tape is then written to from the beginning again.\n");
		fprintf(stderr, "   After reading EOF from the file, the tape should be changed.\n");
		exit(-1);
	}

	if (NULL != logfilename) {
		fDebugFile = fopen(logfilename, "a+");
		if (NULL == fDebugFile) {
			fprintf(stderr, "Can't open file '%s' - Error: %s (%d)\n", logfilename, strerror(errno), errno);
		}
		setvbuf(fDebugFile, NULL, _IONBF, 0);
	} else {
		fDebugFile = NULL;
	}

	sprintf(deviceName, "/dev/sg%d", SCSIDeviceNo);

	signal(SIGHUP, signalHandler);
	signal(SIGINT, signalHandler);
	signal(SIGQUIT, signalHandler);
	//signal(SIGILL, signalHandler);
	signal(SIGPIPE, signalHandler);
	signal(SIGALRM, signalHandler);
	signal(SIGTERM, signalHandler);
	signal(SIGUSR1, signalHandler);
	signal(SIGUSR2, signalHandler);

	pOnStream = new OnStream(deviceName);

	if (!pOnStream->IsOnstream()) {
		delete pOnStream;
		return 1;
	}

	
	Debug(2, "Initializing.\n");

	pOnStream->VendorID((char *) VENDORID);
	if (CheckSense(pOnStream)) {
		return -1;
	}

	WaitForReady(pOnStream);	
		
	// Do not call LOAD, because we are assuming hotswap!.

	pOnStream->DataTransferMode(true);
	CheckSense(pOnStream);

	// A locate should clear all buffers, but we're designing this with hotswaps in mind so...
	pOnStream->Drain();
	WaitForReady(pOnStream);
	
	tp = GetTapeParameters(pOnStream);
	CheckSense(pOnStream);
		
	if (tp.SegTrk == 19239 && tp.Trks == 24) {
		TotalFrames = tp.SegTrk * tp.Trks;
		capacity = (long long) (TotalFrames) * 32768;
	} else {
		TotalFrames = (tp.SegTrk - 99) * tp.Trks;
		capacity = (long long) (TotalFrames) * 32768;
	}
	Debug(2, "Density: %d\nSegTrk: %d\nTrks: %d\n", tp.Density, tp.SegTrk, tp.Trks);
	Debug(2, "Capacity: ");
	Debug(2, "%Ld bytes\n", capacity);
	pOnStream->BufferStatus(&MaxBuffer, &CurrentBuffer);
	CurrentTapeBuffer = CurrentBuffer;

	WaitForReady(pOnStream);
	if (rewind) {
		Debug(2, "Rewinding...");
		if (false == pOnStream->Rewind()) {
			Debug(0, "main: Rewind failed: '%s'\n", szOnStreamErrors[pOnStream->GetLastError()]);
			delete pOnStream;
			return 1;
		}

		WaitForReady(pOnStream);
		Debug(2, "Done.\n");
		return 0;
	}

	if (StartFrameSet && false == pOnStream->Locate(StartFrame)) {
		Debug(0, "main: Locate failed: '%s'\n", szOnStreamErrors[pOnStream->GetLastError()]);
		delete pOnStream;
		return 1;
	}
	
	WaitForReady(pOnStream);
	pOnStream->ShowPosition(NULL, NULL);

	CurrentFrame = StartFrame;
	WaitForReady(pOnStream);

	Debug(2, "Starting read\n");

	if (false == pOnStream->StartRead()) {
		Debug(0, "main: StartRead failed: '%s'\n", szOnStreamErrors[pOnStream->GetLastError()]);
		delete pOnStream;
		return 1;
	}
	WaitForReady(pOnStream);
	
	if (false == StartFrameSet) {
		if (false == pOnStream->ShowPosition(&CurrentFrame, NULL)) {
			Debug(0, "Failed to ShowPosition() to get the current position. Error %s\n", szOnStreamErrors[pOnStream->GetLastError()]);
			delete pOnStream;
			return 1;
		}
		
		Debug(0, "The current tape position has been read as %d, but this may be wrong!\n", CurrentFrame);
	}

	if (NULL != filename) {
		if (NULL == (fil = fopen(filename, "w"))) {
			Debug(0, "Can't open file %s for writing - Error %s\n", filename, strerror(errno));
			return 1;
		}
	} else {
		fil = stdout;
	}
		
	startTime = time(NULL);
	CurrentSeqNo = 0;

	while (!eof && !signalled) {
		if (OS_NEED_POLL(pOnStream->FWRev()))
			CurrentSense = pOnStream->WaitPosition (CurrentFrame);
		else
			CurrentSense = SNoSense;
		if (CurrentSense == SNoSense) {
			if (false == pOnStream->Read(buf)) {
				Debug(0, "main: Read 0 failed: '%s'\n", szOnStreamErrors[pOnStream->GetLastError()]);
				delete pOnStream;
				return 1;
			}
			CurrentSense = CheckSense(pOnStream);
		}
		switch (CurrentSense) {
		case SNoSense:
			break;
		case SUnrecoveredReadError:
		case STimeoutWaitPos:
			// See if the next readable frame has our data in it.
			Debug(2, "Unrecoverable read error at frame %ld. Checking next block...\n", CurrentFrame);
			if (retry++ > 5) {
				eof = 1;
				continue;
			}
			if (CurrentSense == SUnrecoveredReadError) CurrentFrame++;
			else CurrentFrame += 40;
			if (false == pOnStream->Locate(CurrentFrame)) {
				Debug(0, "main: Locate failed: '%s'\n", szOnStreamErrors[pOnStream->GetLastError()]);
				delete pOnStream;
				return 1;
			}
			if (false == pOnStream->StartRead()) {
				Debug(0, "main: Read failed: '%s'\n", szOnStreamErrors[pOnStream->GetLastError()]);
				delete pOnStream;
				return 1;
			}
			WaitForReady(pOnStream);
			continue;
		case SEOD:
			Debug(2, "Sense: End-of-data at frame %ld. Advancing 5 frames...\n", CurrentFrame);
			if (false == pOnStream->Locate(CurrentFrame += 5)) {
				Debug(0, "main: Locate failed: '%s'\n", szOnStreamErrors[pOnStream->GetLastError()]);
				delete pOnStream;
				return 1;
			}
			WaitForReady(pOnStream);
			if (false == pOnStream->StartRead()) {
				Debug(0, "main: Read failed: '%s'\n", szOnStreamErrors[pOnStream->GetLastError()]);
				delete pOnStream;
				return 1;
			}
			WaitForReady(pOnStream);
			continue;
		default:
			Debug(0, "Unhandled sense %d\n", CurrentSense);
			return -1;
		}
		CurrentFrame++;
		unFormatAuxFrame(&buf[32768], &AuxFrame);
		switch (AuxFrame.FrameType) {
		case 0x8000:
			// Data frame

			memcpy(ApplicationSig, &AuxFrame.ApplicationSig, 4);
			if (debug > 5) {
				Debug(6, "Read Seq no: %ld\n", AuxFrame.FrameSequenceNumber);
				ApplicationSig[4] = '\0';
				Debug(6, "Application Sig: %s (0x%08x)\n", ApplicationSig, (unsigned int) *ApplicationSig);
			}
			if (AuxFrame.DataAccessTable.DataAccessTableEntry[0].LogicalElements != 1) {
				Debug(0, "More than 1 logical elements in the block. Only writing first one. (%d)\n", AuxFrame.DataAccessTable.DataAccessTableEntry[0].LogicalElements);
			}
			if (AuxFrame.PartitionDescription.WritePassCounter != WritePass) {
				Debug(2, "Old frame found in stream. Skipping...\n");
				continue;
			}
			if (CurrentSeqNo == 0) {
				CurrentSeqNo = AuxFrame.FrameSequenceNumber;
			}
			if (AuxFrame.FrameSequenceNumber < CurrentSeqNo) {
				Debug(2, "Frame with low sequence number %ld. Expecting %ld. Skipping...\n", AuxFrame.FrameSequenceNumber, CurrentSeqNo);
				continue;
			}
			/* The skip 80 forward could have been too far ... */
			if (AuxFrame.FrameSequenceNumber > CurrentSeqNo) {
				Debug(0, "Frame with high sequence number %ld. Expecting %ld. ", AuxFrame.FrameSequenceNumber, CurrentSeqNo);
				if (retry++ > 5) {
					eof = 1;
					Debug (0, "Aborting\n");
					break;
				}
				CurrentFrame -= AuxFrame.FrameSequenceNumber - CurrentSeqNo + 1;
				Debug (0, "Jump Back to %i.\n", CurrentFrame);
				if (false == pOnStream->Locate(CurrentFrame)) {
					Debug(0, "main: Locate failed: '%s'\n", szOnStreamErrors[pOnStream->GetLastError()]);
					delete pOnStream;
					return 1;
				}
				if (false == pOnStream->StartRead()) {
					Debug(0, "main: Read failed: '%s'\n", szOnStreamErrors[pOnStream->GetLastError()]);
					delete pOnStream;
					return 1;
				}
				WaitForReady(pOnStream);
				continue;
			}

			CurrentSeqNo++; retry = 0;
			fwrite(buf, 1, AuxFrame.DataAccessTable.DataAccessTableEntry[0].size, fil);
			totalBytes += AuxFrame.DataAccessTable.DataAccessTableEntry[0].size;
			break;
		case 0x0100:
			Debug(2, "EOD\n");
			/* Ignore EOD frames if in error recovery, 
			 * i.e. locating forward to find next valid frame */
			if (!retry) eof = 1;
			break;
		default:
			Debug(2, "Unknown frame 0x%04x at pos %d. Reading anyways...\n", 
			      AuxFrame.FrameType, CurrentFrame);
			fwrite(buf, 1, 33280, fil);
			totalBytes += 33280;
		}
	}
	if (NULL != filename) {
		fclose(fil);
	}
	Debug(2, "%Ld bytes in %ld seconds (%f bytes/sec %f kbytes/sec %f Mbytes/sec)\n", totalBytes, time(NULL) - startTime, totalBytes / (float) (time(NULL) - startTime), totalBytes / (float) (time(NULL) - startTime) / 1024.0, totalBytes / (float) (time(NULL) - startTime) / 1048576.0);

	delete pOnStream;
	return 0;
}
