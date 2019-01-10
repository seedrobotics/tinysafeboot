This folder contains some sample code to cope with issue #2
(https://github.com/seedrobotics/tinysafeboot/issues/2)

THIS ONLY APPLIES IF OPERATING IN ONE WIRE MODE (RX and TX shorted).
If you are using a FULL DUPLEX setup, this fix won't apply, nor is there any issue. Everything should work as expected.

Issue Explanation (taken from the Issue page):
==============================================
THIS ISSUE HAS NOT BEEN CONSISTENTLY REPRODUCED SO FAR AND IS UNCLEAR WHERE THE FAULT IS. IT DOES SEEM TO OCCUR SPECIFICALLY WITH THE TEENSY 3.1/3.2; 
HOWEVER THE WORKAROUND DESCRIBED HERE HAS SHOWN 100% RELIABLILTY REGARDLESS.

On some processors that natively support One Wire, the host device needs to shift between TX (active drive LOW/HIGH) and RX (input mode) to communicate with TSB in One Wire mode.

When a device sends the activation characters to TSB it often responds with the start bit (setting the line low) slightly too early, while the host is still holding the line HIGH for the stop bit.

This causes the host to miss the beginning of the Start bit and become out of sync with TSB, impairing communications.

This behaviour has been observed on TSB installed in an ATMEGA88PA@8Mhz, communicating with an MK20 COrtex M4 (Teensy 3.1/3.2) @96Mhz processor.
Communication was attempted at 9600bps and 19200bps yielding the same faulty result.
On occasion, the two chips may be in sync but this is a random situation that is likely related to power timing differences.

The only work around so far, was to use a bit banged uart algorithm on the host side, that moves into listening mode after sending the last data bit.
The STOP bit timing is respected, but the line is held high for the stop bit by the external pull up, effectively making the host release the line earlier. 
(as opposed to the Host actively holding the line high during a stop bit period).
After the last data bit is shifted out by the host, it releases the line, and begins actively looking for a line drop signaling the beginning of the START bit.
By not having the Host actively hold the line high during the STOP bit period and moving into a state of seeking the START bit earlier, seems to resolve the issue.

This behavior likely does not occur on Full duplex setups (where you have separate lines for RX and TX) and it also doesn't seem to happen with FTDI chips. FTDI chips that use a tristate buffer or other similar mechanisms don't seem to be affected by this, or rarely affected.

Further experiments and testing, using a Logic Analyser have shown the UART communication timing has large variations.
In addition to a start bit that is sent too early, on occasion the stop bit is initiated too late, thus causing a framing error.
At 9600bps,the last 9 bits of a byte (8 data bits + stop bit) vary throughout the transmission from a duration of 900microsecs to almost 1ms for transmitting the same character.

The only work around continues to be a software based UART, and accepting characters with framing errors as well. (stop bits sent too late)

It appears the only reliable alternative is probably to make the TSb Loader use the native UART on the ATMEGA (it currently bit bangs it). That has a couple of limitations and a few advantages: limitations are the need to use the specific RX and TX pins on ATMEGA, advantages are much higher achievable bauds.

For ATTINY, many of them don't have a UART so the soft UART will still need to be used.

As per my earlier remark, some devices such as the FT232 and FT231 are very tolerant to these timing issues and seem to communicate properly with the bootloader.


WORKAROUND AND POSSIBLE FIXES
=============================

There is no immediate fix other than changing the ASM code of TSB, in a possible extensive way.

However if you have control on the host side, you can implement a Software UART that goes into RX mode earlier:
it will respect normal timings for STOP bit if it is going to transmit something but it will accept the START bit even if the
timing for the stop bit hasn't ended. (in theory this would be called a framing error but not treating it as an error and accepting the start bit works reliably).

We have build some code at Seed Robotics that implements this software UART with special behaviour on a Teensy 3.1.
The code is C++ and self explanatory. It was built with Teensyduino for a Teensy 3.1. It should not be too difficult to port to other platforms as it only needs one Timer and
interrupt on Pin Change (to detect falling edge on the signal).

The code is based on the Algorithm described in ATMEL Notes AVR305 and AVR304 and works quite reliably at speeds up to 12800bps in a Teensy 3.1 runnnig at 96Mhz
(we presume 9600 baud would be achievable on lower spec processors or if necessary, lowering the baud rate to work on lower clock rates. 
Lowering the Baud rate is possible because TSB uses auto bauding)
