;***********************************************************************

; IMPORTANT: THIS VERSION IS FOR ATMEGA ONLY AS IT REQUIRES
; A UART FOR OPERATION.
; FOR ATTINYs USE THE REGULAR VERSION

;***********************************************************************

;***********************************************************************
;***********************************************************************
;***********************************************************************
; TinySafeBoot - The Universal Bootloader for AVR ATtinys and ATmegas
; ** Variation using UART and fixed baud rate by Seed Robotics.
;***********************************************************************
;***********************************************************************
;***********************************************************************
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
; ~  240 ... TSB Installer for ATtinys
; ~  470 ... TSB for ATtinys
; ~ 1180 ... TSB for ATmegas
;
;***********************************************************************
; ADJUSTMENTS FOR INDIVIDUAL ASSEMBLY
;***********************************************************************
;
; This Sourcecode is directly compatible to: AVRASM2
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
.set    YY      =       17
.set    MM      =       6
.set    DD      =       26
;
.set BUILDSTATE = $70   ; version management option
; BUILDSTATE >= F0: regular TSB with autobauding
; BUILDSTATE = 7xx: TSB tests using UART and fixed baud
;

;-----------------------------------------------------------------------
; BAUD RATE AND UART PINS CONFIGURATION
; 
; Here you need to enter te values for UBRRH and UBRRL
; and optionally the USE_2X flag.
;
; We suggest using the WormFood's AVR baud rate online calculator:
; http://wormfood.net/avrbaudcalc.php
; It is ESSENTIAL that you factor in your clock rate.
; Make sure your fuses are set to use the correct clock source
; IF you are using the inetrnal RC Oscilator (the default on new chips
; until you manully change the fuses), you should callibrate it first.

; UBRRH=0; UBBRL=51 sets 9600 bps on an 8Mhz clock 
; or 19200 bps if you are using a 16Mhz clock

.equ	UBRRH_VALUE		= 0
.equ	UBRRL_VALUE		= 51 

; .equ	USE_2X			= 0  ; the use of 2X is currently NOT supported 
							 ; due to code space saving

;-----------------------------------------------------------------------
; REGISTER DEFINITIONS
; These registers default to UART0 on an ATmegaX8 (ATmega88, ATmega168)
; If you use a Mega with more UARTs and wish to use another UART adjust
; the register names
; If you are using a processor with different register names, adjust
; accordingly
;-----------------------------------------------------------------------
;
;
.equ	UBRRL	= UBRR0L
.equ	UBRRH	= UBRR0H
.equ	UCSRA	= UCSR0A
.equ	UCSRB	= UCSR0B
.equ	UCSRC	= UCSR0C

.equ	UDR		= UDR0

.equ	RXEN	= RXEN0
.equ	TXEN	= TXEN0
.equ	TXC		= TXC0
.equ	RXC		= RXC0

.equ	UCSZ0	= UCSZ00
.equ	UCSZ1	= UCSZ01


;-----------------------------------------------------------------------
; *** Changes below this line are on your own risk! ***
;-----------------------------------------------------------------------
;
;
;
;***********************************************************************
; AUTO-ADJUST FOR DIFFERENT ASSEMBLY OPTIONS
;***********************************************************************
;
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

;-----------------------------------------------------------------------
; Universal Constants and Registers
;-----------------------------------------------------------------------

.equ    REQUEST         = '?'           ; request / answer / go on
.equ    CONFIRM         = '!'           ; confirm / attention

.equ	ACTIVATION_CHAR = '@'

; Current bootloader date coded into 16-bit number
.equ    BUILDDATE   = YY * 512 + MM * 32 + DD

; Other
.equ    INFOLEN         = 8              ; *Words* of Device Info
.equ    BUFFER          = SRAM_START

; Registers (in use by TSB-Firmware and TSB-Installer for ATtinys)
.def    avecl   = r4                    ; application vector temp low
.def    avech   = r5                    ; application vector temp high
;.def    bclkl   = r6                    ; baudclock low byte
;.def    bclkh   = r7                    ; baudclock high byte
.def    tmp1    = r16                   ; these are
.def    tmp2    = r17                   ; universal
.def    tmp3    = r18                   ; temporary
.def    tmp4    = r19                   ; registers
.def    bcnt    = r20                   ; page bytecounter

;
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
.endif

;-----------------------------------------------------------------------
; ACTIVATION CHECK
;-----------------------------------------------------------------------

;WaitRX:
;        sbi TXDDR, TXBIT                ; if RX=TX (One-Wire), port is
;        cbi RXDDR, RXBIT                ; driven open collector style,
;        sbi RXPORT, RXBIT               ; else RX is input with pullup
;        sbi TXPORT, TXBIT               ; and TX preset logical High

ConfigureUART:
		ldi tmp1, UBRRH_VALUE			; set UBRR baud rate registers
		sts UBRRH, tmp1
		ldi tmp1, UBRRL_VALUE
		sts UBRRL, tmp1
		ldi tmp1, (1 << UCSZ0 ) | (1 << UCSZ1 ) ; Set frame format: 8data, 1stop bit
		sts UCSRC, tmp1
		ldi tmp1, (1<<RXEN)				; enable RX only; TX normally disabled for one
		sts UCSRB, tmp1					; wire compatibility			


LoadTimeoutByte:                                 
        rcall ZtoLASTPAGE               ; set Z to start'o'LASTPAGE
        adiw zl, 2                      ; skip first 2 bytes
        lpm xh, z+                      ; load TIMEOUT byte
        ldi xl, 128						; TIMEOUT is loaded HIGH

WaitForActivation:
		rcall ReceiveByte				; pull byte; pull byte has an inner cycle (timeout) of 255
										; which we must do before every dec of the timeout byte
        cpi tmp1, 0				        ; if host HAS sent something other than 0
        brne ActivationCharReceived     ; register ActChar received
        sbiw xl, 1                      ; use X for Outer counter
        brcc WaitForActivation          ; to Timeout
        ;rjmp APPJUMP                   ; Timeout! Goto APPJUMP

;-----------------------------------------------------------------------
; ATMEGA APPJUMP = SIMPLE JUMP TO $0000 (ORIGINAL RESET VECTOR)
;-----------------------------------------------------------------------
; Boot Reset Vector (BOOTRST) must be activated for TSB on ATmegas.
; After timeout or executing commands, TSB for ATmegas will simply
; handover to the App by a (relative or absolute) jump to $0000.

APPJUMP:
        rcall SPMwait                   ; make sure everything's done
		clr tmp1
		sts UCSRA, tmp1					; disable RX and TX (ie disable UART function)

.if FLASHEND >= ($1fff)
        jmp  $0000                      ; absolute jump
.else
        rjmp $0000                      ; relative jump
.endif

;-----------------------------------------------------------------------
; REGISTER RECEIVING AN ACTIVATION CHAR
; AND CHECK IF WE'RE NOW ACTIVE
; IF NOT AN ACT CHAR, START THE APP
;-----------------------------------------------------------------------
ActivationCharReceived:
		cpi tmp1, ACTIVATION_CHAR        ; if it is act char
        breq ActivationCharReceived1     ; register ActChar received
        rjmp APPJUMP					 ; else start application

ActivationCharReceived1:
		inc tmp2		; increment count of Activation Chars received
		cpi tmp2, 3		; if 3 are now received
		breq CheckPassword ; goto CheckPassword
		rjmp WaitForActivation ; else go back and wait for activation

;-----------------------------------------------------------------------
; BAUDRATE CALIBRATION CYCLE
;-----------------------------------------------------------------------

;Activate:
;        clr xl                          ; clear temporary
;        clr xh                          ; baudrate counter
;        ldi tmp1, 6                     ; number of expected bit-changes
;actw1:
;        sbic RXPIN, RXBIT               ; idle 1-states (stopbits, ones)
;        rjmp actw1
;actw2:
;        adiw xl, 1                      ; precision measuring loop
;        sbis RXPIN, RXBIT               ; count clock cycles
;        rjmp actw2                      ; while RX is 0-state
;        dec tmp1
;        brne actw1
;actwx:
;        movw bclkl, xl                  ; save result in bclk

;-----------------------------------------------------------------------
; CHECK PASSWORD / EMERGENCY ERASE
;-----------------------------------------------------------------------

CheckPassword:
chpw0:  ser tmp4                        ; tmp4 = 255 enables comparison
chpw1:  lpm tmp3, z+                    ; load pw character from Z
        and tmp3, tmp4                  ; tmp3 = 0 disables comparison
        cpi tmp3, 255                   ; byte value 255 indicates
        breq chpwx                      ; end of password -> success
chpw2:  rcall Receivebyte               ; else receive next character
        cpi tmp1, 0                     ; rxbyte = 0 will branch
        breq chpwee                     ; to confirm emergency erase
        cp  tmp1, tmp3                  ; compare password with rxbyte
        breq chpw0                      ; if equal check next character
        clr  tmp4                       ; tmp4 = 0 to loop forever
        rjmp chpw1                      ; all to smoothen power profile
chpwee:
        ; Fix for ISSUE #1: only check for Emergency Erase if we haven't
		; gotten a wrong password; if we got a wrong password
		; then we should stay in loop and not escape to Emergency
		; Erase
		cpi tmp4, 0						; if tmp4=0 we are set to loop forever
		breq chpw1
		rcall RequestConfirm            ; request confirmation
        brts chpa                       ; not confirmed, leave
        rcall RequestConfirm            ; request 2nd confirmation
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
; received byte is returned in tmp1

ReceiveByte:
		clr tmp1						; clear the return register
		clr tmp3						; inner counter for timeout

ReceiveByte1:		
		lds xl, UCSRA					; load UART status register A
		sbrc xl, RXC					; if not RXComplete, skip
		rjmp LoadRXByte
		dec tmp3						; if counter not zero
		brne ReceiveByte1				; cycle again; else return (timeout)
		ret

LoadRXByte:
		ldi tmp1, UDR					; else load received character
		ret

;-----------------------------------------------------------------------
; RS232 SEND CONFIRM CHARACTER
;-----------------------------------------------------------------------

SendConfirm:
        ldi tmp1, CONFIRM
        ;rjmp Transmitbyte

;-----------------------------------------------------------------------
; RS232 TRANSMIT BYTE
;-----------------------------------------------------------------------
; byte to be transmitted is in tmp1
TransmitByte:
		ldi xl, (1 << TXEN)	; enable TX, disable RX
		sts UCSRB, xl
		sts UDR, tmp1
CheckTXComplete:
		lds xl, UCSRA			; load status register
		sbrs tmp3, TXC			; if TXC is complete (asserted) skip next instruction
		rjmp CheckTXComplete	; else jump to repeat cycle
		ldi xl, (1 << RXEN)		; disable TX, enable RX
		sts UCSRB, tmp3
		ret

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


