;***********************************************************************
;***********************************************************************
;***********************************************************************
; TinySafeBoot - The Universal Bootloader for AVR ATmegas
;***********************************************************************
;***********************************************************************
;***********************************************************************
;
;-----------------------------------------------------------------------
; 2020 - Version using native UART, Fixed Baud by Seed Robotics in 2020 
;-----------------------------------------------------------------------
; meant for use on ATMEGA devices only (with native UART - UART0)
;
; Main differences to Regular TSB Bootloader:
; - Uses a native UART (UART0); therefore not compatible with ATTINY
; - Baud rate is fixed (set by a macro in the code). No auto bauding.
; - Disables TX while not transmitting to allow for one wire flashing 
;   (where RX and TX are shorted, for a multi drop bus)
; - Also works with separate RX and TX; however an external pull up 
;   on TX _may_ be required; alternatively you can modify the code
;   in the ReceiveByte routine so that it won't disable TX.
; - FIXES:
;     - situations where booting onto a bus with active communication could
;       lock the autobauding feature
;     - times out and boots to application code if the host stops interacting
;       with the bootloader
;
;-----------------------------------------------------------------------
; Extended by Seed Robotics from 2017
;-----------------------------------------------------------------------
; Seed Robotics contributions are available from the Github
; repository github.com/seedrobotics
; The License and conditions remain as stated below, in the
; original notice.
;
;
;-----------------------------------------------------------------------
; Written in 2011-2015 by Julien Thomas
;
; This program is free software; you can redistribute it and/or
; modify it under the terms of the GNU General Public License
; as published by the Free Software Foundation; either version 3
; of the License, or (at your option) any later version.
; This program is distributed in the hope that it will be useful,
; but WITHOUT ANY WARRANTY; without even the implied warranty
; of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
; See the GNU General Public License for more details.
; You should have received a copy of the GNU General Public License
; along with this program; if not, see:
; http://www.gnu.org/licenses/
;-----------------------------------------------------------------------
;
;
;
;***********************************************************************
; OVERVIEW
;***********************************************************************
;
; TSB assembly source is organized in 4 segments (approx. line numbers)
;
; ~   50 ... Global definitions
; ~      ... TSB for ATmegas
;
;***********************************************************************
; ADJUSTMENTS FOR INDIVIDUAL ASSEMBLY
;***********************************************************************
;
; This Sourcecode is directly compatible to: AVRASM2, GAVRASM
;
.nolist
;
;-----------------------------------------------------------------------
; SPECIFY TARGET AVR
;-----------------------------------------------------------------------
;
; Comment in and provide def.inc file for target device
;
; [Examples]
;
;.include "tn2313def.inc"
;.include "tn85def.inc"
;.include "m8515def.inc"
;.include "m168def.inc"
;.include "m161def.inc"
;.include "m324Adef.inc"
;.include "m328Pdef.inc"
;.include "tn441def.inc"
;.include "tn167def.inc"
;.include "tn861def.inc"
;.include "tn841def.inc"
;.include "tn84def.inc"
;.include "m8def.inc"
;.include "m644PAdef.inc"
;.include "m644def.inc"
;.include "tn167def.inc"
;.include "tn25def.inc"
;
; [...]
;
;
.list
;
;-----------------------------------------------------------------------
; BUILD INFO
;-----------------------------------------------------------------------
; YY = Year - MM = Month - DD = Day
.set    YY      =       20
.set    MM      =       7
.set    DD      =       27
;
.set BUILDSTATE = $F2   ; version management option
;
;-----------------------------------------------------------------------
; TSB / TSB-INSTALLER SWITCH
;-----------------------------------------------------------------------
; 0 = Regular assembly to target address
; Other value = NOT SUPPORTED
;
.set    TSBINSTALLER = 0
;
;-----------------------------------------------------------------------
; F_CPU and Baud rate setting
;-----------------------------------------------------------------------
.equ	F_CPU	= 16000000
.equ	BAUD	= 33333			; baudrate (for 56K, it is actually 55,555, so for BAUD_PRESC give an INT result of 8, we must set BAUD to 55500) 
.equ	BAUD_PRESC	= (F_CPU/16/BAUD) - 1	; baud prescale

.if BAUD_PRESC > 255
.error "ERROR: BAUD RATE TOO LOW. WE ONLY WRITE THE UBRRL REGISTER, SO UBRR MUST BE <255 FOR THIS CLOCK FREQ AND BAUD"
.endif


;***********************************************************************
; AUTO-ADJUST FOR DIFFERENT ASSEMBLY OPTIONS
;***********************************************************************
;
; Always set TINYMEGA=1 bc this code only supports ATMEGA

.equ TINYMEGA=1

.if FLASHEND > ($7fff)
        .error "SORRY! DEVICES OVER 64 KB NOT SUPPORTED YET."
        .exit
.endif

;-----------------------------------------------------------------------
; Workarounds for devices with renamed or missing definitions
;-----------------------------------------------------------------------
;
.ifndef SPMCSR                  ; SPMEN / PGERS / ...
        .equ SPMCSR = SPMCR
.endif

.ifndef MCUSR                   ; PORF / EXTRF / BORF / WDRF
        .equ MCUSR = MCUCSR
.endif

; Detect Attiny441/841 to amend missing pagesize and apply 4-page mode

.set FOURPAGES = 0

.if ((SIGNATURE_000 == $1E) && (SIGNATURE_002 == $15) && (SIGNATURE_001 == $92))
                .equ PAGESIZE = 32
                .set FOURPAGES = 1
                .message "ATTINY441: 4-PAGE-ERASE MODE"
.endif

.if ((SIGNATURE_000 == $1E) && (SIGNATURE_002 == $15) && (SIGNATURE_001 == $93))
                .equ PAGESIZE = 32
                .set FOURPAGES = 1
                .message "ATTINY841: 4-PAGE-ERASE MODE"
.endif

;-----------------------------------------------------------------------
; Universal Constants and Registers
;-----------------------------------------------------------------------

.equ    REQUEST         = '?'           ; request / answer / go on
.equ    CONFIRM         = '!'           ; confirm / attention

; Current bootloader date coded into 16-bit number
.equ    BUILDDATE   = YY * 512 + MM * 32 + DD

; Other
.equ    INFOLEN         = 8              ; *Words* of Device Info
.equ    BUFFER          = SRAM_START

; Registers (in use by TSB-Firmware and TSB-Installer for ATtinys)
.def    avecl   = r4                    ; application vector temp low
.def    avech   = r5                    ; application vector temp high
.def    tmp1    = r16                   ; these are
.def    tmp2    = r17                   ; universal
.def    tmp3    = r18                   ; temporary
.def    tmp4    = r19                   ; registers
.def    bcnt    = r20                   ; page bytecounter
.def    cntr1   = r21                   ; timeout counter
.def    rxen    = r22					; check if RX enabled (meaning TX disabled)
.def    utimeoutH = r23                  ; user timeout High byte     
										; special purpose registers start at R26
;
;
;***********************************************************************
;***********************************************************************
;***********************************************************************
; START OF TSB FOR ATMEGAS
;***********************************************************************
;***********************************************************************
;***********************************************************************
;
; TSB for ATmegas is always coded directly to target address.

.if TINYMEGA == 1

.message "ASSEMBLY OF TSB FOR ATMEGA"

.equ    BOOTSTART       = (FLASHEND+1)-256      ; = 512 Bytes
.equ    LASTPAGE        = BOOTSTART - PAGESIZE  ; = 1 page below TSB!

.org    BOOTSTART

RESET:
        cli

        in tmp4, MCUSR                  ; check reset condition
        sbrc tmp4, WDRF                 ; in case of a Watchdog reset
        rjmp APPJUMP                    ; immediately leave TSB

        ldi tmp1, low (RAMEND)          ; write ramend low
        out SPL, tmp1                   ; into SPL (stackpointer low)
.ifdef SPH
        ldi tmp1, high(RAMEND)          ; write ramend high for ATtinys
        out SPH, tmp1                   ; with SRAM > 256 bytes
		.message "PROVIDING FOR STACK BIGGER THAN 256 BYTES"
.endif

;-----------------------------------------------------------------------
; ACTIVATION CHECK
;-----------------------------------------------------------------------
		; Configure UART; no autobauding in this version

		ldi	tmp1,BAUD_PRESC			; load baud prescale
		sts	UBRR0L,tmp1				; set baud prescale
		; ldi	tmp2,HIGH(bpsc)			 ; save code by not loading UBBRH
		;sts	UBRRH,tmp2				; to UBRR0
		;ldi	tmp2,( (1<<RXEN0) )   	; enable transmiter and receiver
		;sts	UCSR0B,tmp2	

		; Enable Pull up on Port D2
		cbi DDRD, DDRD2
		sbi PORTD, PORTD2

		; we will enable RNEN/TXEN in the ReceiveByte and TransmitByte routines

        rcall ZtoLASTPAGE               ; set Z to start'o'LASTPAGE
        adiw zl, 2                      ; skip first 2 bytes (APPJUMP)
        lpm utimeoutH, z+               ; load TIMEOUT byte and store for use in RX byte timeout
		ori utimeoutH, (F_CPU / 1000000); prevent bootloader lockout due if it gets an invalid (to small) timeout setting
										; this ensures value is at least the clock rate, which shoudl give about 40ms
		clr tmp2						; apparently at times this is not set to 0 on boot? (seen while in debugWire)
		clr rxen						; same as above

WRX1To:
		; we'll check the X register which is where ReceibeByte controls the timeout
		; the overall timeout of receive byte is the timeout set by the user
		; therefore, if we get characters while X> 0 we're attempting to activate bootloader;
		; if not, if X=0 we timedout and go to app start
		rcall ReceiveByte
		brcs WRX2To						; if X got to 0 (i.e. carry set), assume we timed out
		cpi tmp1, '@'                   ; did we get an activation char = "@"
		breq ActCharRcvd
WRX2To:
		rjmp APPJUMP                    ; not an activation char goto APPJUMP in LASTPAGE
        

ActCharRcvd:
		inc tmp2
		cpi tmp2, 3
		brne WRX1To						; branch if not yet at 3; 
										; otherwise fall through to password check

;-----------------------------------------------------------------------
; CHECK PASSWORD / EMERGENCY ERASE
;-----------------------------------------------------------------------
		; we use the user timeout (utimeoutH) register for COMM timeout 
		; when we don't get valid data
		; increase this value to a fixed one now, to cope
		; with cases where the user timeout is set so low that we don't have time to
		; do anything
		ldi utimeoutH, (F_CPU / 78500) ; this should result in 255 for 20Mhz and proportionally
									   ; less for lower Clocks, so that we get the same time approx. 2.4sec

CheckPassword:

chpw0:  ser tmp4                        ; tmp4 = 255 enables comparison
chpw1:  lpm tmp3, z+                    ; load pw character from Z
        and tmp3, tmp4                  ; if tmp4 = 0 disables comparison, for wrong password scenarios
        cpi tmp3, 255                   ; byte value 255 indicates
        breq chpwx                      ; end of password -> success
chpw2:  rcall Receivebyte               ; else receive next character
        cpi tmp1, 0                     ; rxbyte = 0 will branch
        breq chpwee                     ; to confirm emergency erase
        cp  tmp1, tmp3                  ; compare password with rxbyte
        breq chpw0                      ; if equal check next character
        clr  tmp4                       ; tmp4 = 0 to loop forever
        rjmp chpw1                      ; and smoothen power profile
chpwee:
        ; Fix for ISSUE #1: only check for Emergency Erase if we haven't
		; gotten a wrong password; if we got a wrong password
		; then we should stay in loop and not escape to Emergency
		; Erase
		cpi tmp4, 0						; if tmp4=0 we are set to loop forever
		breq chpw1
        rcall RequestConfirm            ; request confirm
        brts chpa                       ; not confirmed, leave
        rcall RequestConfirm            ; request 2nd confirm
        brts chpa                       ; can't be mistake now
        rcall EmergencyErase            ; go, emergency erase!
        rjmp  Mainloop
chpa:
        rjmp APPJUMP                    ; start application
chpwx:
;       rjmp SendDeviceInfo             ; go on to SendDeviceInfo

;-----------------------------------------------------------------------
; SEND DEVICEINFO
;-----------------------------------------------------------------------

SendDeviceInfo:
        ldi zl, low (DEVICEINFO*2)      ; load address of deviceinfo
        ldi zh, high(DEVICEINFO*2)      ; low and highbyte
        ldi bcnt, INFOLEN*2
        rcall SendFromFlash

;-----------------------------------------------------------------------
; MAIN LOOP TO RECEIVE AND EXECUTE COMMANDS
;-----------------------------------------------------------------------

Mainloop:
        clr zl                          ; clear Z pointer
        clr zh                          ; which is frequently used
        rcall SendConfirm               ; send CONFIRM via RS232
        rcall Receivebyte               ; receive command via RS232
        rcall CheckCommands             ; check command letter
        rjmp Mainloop                   ; and loop on

;-----------------------------------------------------------------------
; CHANGE USER DATA IN LASTPAGE
;-----------------------------------------------------------------------

ChangeSettings:
        rcall GetNewPage                ; get new LASTPAGE contents
        brtc ChangeS0                   ; from Host (if confirmed)
        ret
ChangeS0:
        rcall ZtoLASTPAGE               ; re-write LASTPAGE
        rcall EraseFlashPage
        rcall WritePage                 ; erase and write LASTPAGE

;-----------------------------------------------------------------------
; SEND USER DATA FROM LASTPAGE
;-----------------------------------------------------------------------

ControlSettings:
        rcall ZtoLASTPAGE               ; point to LASTPAGE
;       rcall SendPageFromFlash

;-----------------------------------------------------------------------
; SEND DATA FROM FLASH MEMORY
;-----------------------------------------------------------------------

SendPageFromFlash:
        ldi bcnt, low (PAGESIZE*2)      ; whole Page to send
SendFromFlash:
        rcall SPMwait                   ; (re)enable RWW read access
        lpm tmp1, z+                    ; read directly from flash
        rcall Transmitbyte              ; and send out to RS232
        dec bcnt                        ; bcnt is number of bytes
        brne SendFromFlash
        ret

;-----------------------------------------------------------------------
; READ APPLICATION FLASH
;-----------------------------------------------------------------------
; read and transmit application flash area (pagewise)

ReadAppFlash:
RAF0:
        rcall RwaitConfirm
        brts RAFx
        rcall SendPageFromFlash
RAF1:
        cpi zl, low (LASTPAGE*2)        ; count up to last byte
        brne RAF0                       ; below LASTPAGE
        cpi zh, high(LASTPAGE*2)
        brne RAF0
RAFx:
        ret

;-----------------------------------------------------------------------
; WRITE APPLICATION FLASH
;-----------------------------------------------------------------------
; Write Appflash pagewise, don't modify anything for ATmegas

WriteAppFlash:
        rcall EraseAppFlash             ; Erase whole app flash
Flash2:
        rcall GetNewPage                ; get next page from host
        brts FlashX                     ; stop on user's behalf
Flash3:
        rcall WritePage                 ; write page data into flash
Flash4:
        cpi zh, high(LASTPAGE*2-1)      ; end of available Appflash?
        brne Flash2                     ; if Z reached last location
        cpi zl, low (LASTPAGE*2-1)      ; then we are finished
        brne Flash2                     ; else go on
FlashX:
        ret                             ; we're already finished!

;-----------------------------------------------------------------------
; WRITE FLASH PAGE FROM BUFFER, VERIFYING AND VERIFY-ERROR-HANDLING
;-----------------------------------------------------------------------

WritePage:
        rcall YtoBUFFER                 ; Y=BUFFER, bcnt=PAGESIZE*2
WrPa1:
        ld r0, y+                       ; fill R0/R1 with word
        ld r1, y+                       ; from buffer position Y / Y+1
        ldi tmp1, 0b00000001            ; set only SPMEN in SPMCSR
        out SPMCSR, tmp1                ; to activate page buffering
        spm                             ; store word in page buffer
        adiw zl, 2                      ; and forward to next word
        subi bcnt, 2
        brne WrPa1
        ; Z = start of next page now
        subi zl, low (PAGESIZE*2)       ; point back Z to
        sbci zh, high(PAGESIZE*2)       ; start of current page
        ; Z = back on current page's start
WrPa2:
        ldi tmp1, 0b00000101            ; enable PRWRT + SPMEN
        out SPMCSR, tmp1                ; in SPMCSR
        spm                             ; write whole page to flash
WrPa3:
        in tmp1, SPMCSR                 ; wait for flash write finished
        sbrc tmp1, 0                    ; skip if SPMEN (bit0) cleared
        rjmp WrPa3                      ; ITS BEEN WRITTEN
        subi zl, low (-PAGESIZE*2)      ; same effect as
        sbci zh, high(-PAGESIZE*2)      ; Z = Z + PAGESIZE*2
        ret

;-----------------------------------------------------------------------
; CHECK COMMANDS
;-----------------------------------------------------------------------

CheckCommands:
        cpi tmp1, 'c'                   ; read LASTPAGE
        breq ControlSettings
        cpi tmp1, 'C'                   ; write LASTPAGE
        breq ChangeSettings
        cpi tmp1, 'f'                   ; read Appflash
        breq ReadAppFlash
        cpi tmp1, 'F'                   ; write Appflash
        breq WriteAppFlash
        cpi tmp1, 'e'                   ; read EEPROM
        breq EepromRead
        cpi tmp1, 'E'                   ; write EEPROM
        breq EEpromWrite
        rjmp APPJUMP                    ; else start application

;-----------------------------------------------------------------------
; EEPROM READ/WRITE ACCESS
;-----------------------------------------------------------------------

EepromWrite:
EEWr0:
        rcall GetNewPage                ; get EEPROM datablock
        brts EERWFx                     ; or abort on host's demand
EEWr1:
        rcall YtoBUFFER                 ; Y = Buffer and Bcnt = blocksize
EEWr2:
        ld tmp1, y+                     ; read EEPROM byte from buffer
        rcall EEWriteByte
        dec bcnt                        ; count down block byte counter
        brne EEWr2                      ; loop on if block not finished
        rjmp EeWr0

;-----------------------------------------------------------------------

EEpromRead:
EeRe1:
        rcall RwaitConfirm              ; wait to confirm
        brts EERWFx                     ; else we are finished
        ldi bcnt, low(PAGESIZE*2)       ; again PAGESIZE*2 is blocksize
EERe2:
        out EEARL, zl                   ; current EEPROM address low
        .ifdef  EEARH
        out EEARH, zh                   ; current EEPROM address high
        .endif
        sbi EECR, 0                     ; set EERE - EEPROM read enable
        in tmp1, EEDR                   ; read byte from current address
        rcall Transmitbyte              ; send out to RS232
        adiw zl,1                       ; count up EEPROM address
        dec bcnt                        ; count down block byte counter
        brne EERe2                      ; loop on if block not finished
        rjmp EERe1
EERWFx:
        ret

;-----------------------------------------------------------------------

EEWriteByte:
        out EEDR, tmp1                  ; write to EEPROM data register
        out EEARL, zl                   ; current EEPROM address low
        .ifdef  EEARH
        out EEARH, zh                   ; high EEARH for some attinys
        .endif
        sbi EECR, 2                     ; EEPROM master prog enable
        sbi EECR, 1                     ; EEPE initiate prog cycle
EeWB:
        sbic EECR, 1                    ; wait write cycle to complete
        rjmp EeWB                       ; before we can go on
        adiw zl,1                       ; count up EEPROM address
        ret

;-----------------------------------------------------------------------
; GET NEW PAGE
;-----------------------------------------------------------------------

GetNewPage:
        rcall RequestConfirm            ; check for Confirm
        brts GNPx                       ; abort if not confirmed
GNP0:
        rcall YtoBUFFER                 ; Y = BUFFER, bcnt = PAGESIZE*2
GNP1:
        rcall ReceiveByte               ; receive serial byte
        st y+, tmp1                     ; and store in buffer
        dec bcnt                        ; until full page loaded
        brne GNP1                       ; loop on
GNPx:
        ret                             ; finished
;-----------------------------------------------------------------------
; REQUEST TO CONFIRM / AWAIT CONFIRM COMMAND
;-----------------------------------------------------------------------

RequestConfirm:
        ldi tmp1, REQUEST               ; send request character
        rcall Transmitbyte              ; prompt to confirm (or not)

RwaitConfirm:
        rcall ReceiveByte               ; get host's reply
        clt                             ; set T=0 for confirmation
        cpi tmp1, CONFIRM               ; if host HAS sent CONFIRM
        breq RCx                        ; return with the T=0
        set                             ; else set T=1 (NOT CONFIRMED)
RCx:
        ret                             ; whether confirmed or not

;-----------------------------------------------------------------------
; FLASH ERASE TOP-TO-BOTTOM ( (BOOTSTART-1) ... $0000)
;-----------------------------------------------------------------------

EraseAppFlash:
        rcall ZtoLASTPAGE               ; point Z to LASTPAGE, directly
EAF0:
        subi zl, low (PAGESIZE*2)
        sbci zh, high(PAGESIZE*2)
        rcall EraseFlashPage
        brne EAF0                       ; until first page reached
EAFx:   ret                             ; and leave with Z = $0000

;-----------------------------------------------------------------------
; EMERGENCY ERASE OF FLASH / EEPROM / USERDATA
;-----------------------------------------------------------------------

EmergencyErase:
        rcall EraseAppFlash             ; erase Application Flash
        ser tmp1                        ; byte value for EEPROM writes
EEE0:
        rcall EEWriteByte               ; write EEPROM byte, Z = Z + 1
        cpi zh, high(EEPROMEND+1)+2     ; EEPROMEND
        brne EEE0                       ; and loop on until finished

        rcall ZtoLASTPAGE               ; LASTPAGE is to be erased
;        rcall EraseFlashPage

;-----------------------------------------------------------------------
; ERASE ONE FLASH PAGE
;-----------------------------------------------------------------------

EraseFlashPage:
        ldi tmp1, 0b00000011            ; enable PGERS + SPMEN
        out SPMCSR, tmp1                ; in SPMCSR and erase current
        spm                             ; page by SPM (MCU halted)

; Waiting for SPM to be finished is *obligatory* on ATmegas!
SPMwait:
        in tmp1, SPMCSR
        sbrc tmp1, 0                    ; wait previous SPMEN
        rjmp SPMwait
        ldi tmp1, 0b00010001            ; set RWWSRE and SPMEN
        out SPMCSR, tmp1
        spm
        ret

;-----------------------------------------------------------------------
; OTHER SUBROUTINES
;-----------------------------------------------------------------------

YtoBUFFER:
        ldi yl, low (BUFFER)            ; reset pointer
        ldi yh, high(BUFFER)            ; to programming buffer
        ldi bcnt, low(PAGESIZE*2)       ; and often needed
        ret

;-----------------------------------------------------------------------

ZtoLASTPAGE:
        ldi zl, low (LASTPAGE*2)        ; reset Z to LASTPAGE start
        ldi zh, high(LASTPAGE*2)
        ret

;-----------------------------------------------------------------------
; RS232 RECEIVE BYTE
;-----------------------------------------------------------------------

; uses: tmp1 (received data byte), cntr1 (for timeout)
; also uses utimeoutH which holds the default timeout defined by the user
; and X which is actually used to count down
SetRX:
		ldi	tmp1,(1<<RXEN0) 		; enable receiver (Transmitter disabled)
		sts	UCSR0B,tmp1
		ser rxen

ReceiveByte:
		sbrs rxen, 0
		rjmp SetRX		

		; outer counter
		mov xh, utimeoutH
		;ldi xl, 128

ReceiveByteShortTimeout:
		ser cntr1						; inner counter reset

ReceiveByteShortTimeout1:		
		lds tmp1, UCSR0A				; load UART status register A
		sbrc tmp1, RXC0					; if not RXComplete, skip
		rjmp LoadRXByte
		dec cntr1						; if counter not zero
		brne ReceiveByteShortTimeout1	; cycle again; else fall through
		sbiw xl, 1						; dec outter counter
		brcc ReceiveByteShortTimeout	; continue of outter counetr still active
		;ret							; 

LoadRXByte:
		lds tmp1, UDR0					; load received character even if RXC is not set
		ret								; (it loads 0 and UDR FIFO should recover for next char)

;-----------------------------------------------------------------------
; RS232 SEND CONFIRM CHARACTER
;-----------------------------------------------------------------------

SendConfirm:
        ldi tmp1, CONFIRM
        rjmp Transmitbyte

;-----------------------------------------------------------------------
; RS232 TRANSMIT BYTE
;-----------------------------------------------------------------------
; uses: tmp1 (transmit byte will be shifted out), tmp2 (bitcounter)
;
; with different portlines defined for RX and TX ("Two-Wire")
; => TX-line is actively driving high/low levels (LSTTL/HCMOS)
;
; with the same portline defined for RX and TX ("One-Wire")
; => TX-line is acting like an open collector/drain with weak pullup

SetTX:
		ldi	tmp2,(1<<TXEN0)   	; enable transmitter (Receiver disabled)
		sts	UCSR0B,tmp2	
		clr rxen

		; wait some guard time to allow receiving devices ot transition
		; from TX t RX state
		ser cntr1						; inner counter reset
SetTXShortTimeout:
		nop
		dec cntr1						; if counter not zero
		brne SetTXShortTimeout			; cycle again; else fall through


TransmitByte:
		sbrc rxen, 0
		rjmp SetTX

		; no need to wait for UDRE bc we will wait for TXC on
		; every char transmitted. TXC occurs later that UDRE
		; so UDRE should be asserted when TXC asserts
		sts UDR0, tmp1

WaitForTXC:
		lds tmp2, UCSR0A		; wait for TXC (and not UDRE)
		sbrs tmp2, TXC0			; bc after this char we may transition
		rjmp WaitForTXC			; to receiving chars and we want to make sure we get a clean transition
		; we need to write a 1 to clear the TXC flag; otherwise the flag won't clear
		sts UCSR0A, tmp2		; tmp2 should contain an asserted TXC bit
		ret


;-----------------------------------------------------------------------
; ATMEGA APPJUMP = SIMPLE JUMP TO $0000 (ORIGINAL RESET VECTOR)
;-----------------------------------------------------------------------
; Boot Reset Vector (BOOTRST) must be activated for TSB on ATmegas.
; After timeout or executing commands, TSB for ATmegas will simply
; handover to the App by a (relative or absolute) jump to $0000.

APPJUMP:
        rcall SPMwait                   ; make sure everything's done

.if FLASHEND >= ($1fff)
        jmp  $0000                      ; absolute jump
.else
        rjmp $0000                      ; relative jump
.endif


;-----------------------------------------------------------------------
; DEVICE INFO BLOCK = PERMANENT DATA
;-----------------------------------------------------------------------

DEVICEINFO:
.message "DEVICE INFO BLOCK FOR ATMEGA"
.db "TSB", low (BUILDDATE), high (BUILDDATE), BUILDSTATE
.db SIGNATURE_000, SIGNATURE_001, SIGNATURE_002, low (PAGESIZE)
.dw BOOTSTART-PAGESIZE
.dw EEPROMEND
.db $AA, $AA

.message "ASSEMBLY OF TSB FOR ATMEGA SUCCESSFULLY FINISHED!"

.endif               ; closing TSB for ATmega sourcecode;

;***********************************************************************
; END OF TSB FOR ATMEGAS
;***********************************************************************

.exit

;***********************************************************************
;***********************************************************************
;***********************************************************************
; END OF CONDITIONAL ASSEMBLY SOURCE OF TSB FOR ATTINYS AND ATMEGAS
;***********************************************************************
;***********************************************************************
;***********************************************************************


