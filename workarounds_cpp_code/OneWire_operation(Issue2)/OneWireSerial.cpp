/*
===============================================
Copyright 2017, Seed Robotics Ltd, Pedro Ramilo
===============================================

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

*/


// Seed Robotics - One Wire Serial Library (UART)
// June 2017, Pedro R
// Assumes 8n1, goes into RX IDLE prematurely to cope with TSB bootloader timing issues
// (relies on the external pull up to drive the line high for the necessary time fo the stop bit )

#include "OneWireSerial.h"
#include "DebugHelper.h"

#define BIT_TIME_UNITS 2

// Defining this will activate debug code thta will produce a pin state change
// whenever we're sampling a bit (RX) or when we send a char (TX)
// Make sure this is set to an unused pin (suggested 19 on the 4D/V3, 12 on the 7D/V4/V5)
//#define DEBUG_PIN 12

IntervalTimer ows_timer;
volatile unsigned long ows_timer_interval_micros;
volatile boolean ows_timer_enabled;

#define DEBUG_PIN 12

#ifdef DEBUG_PIN
volatile boolean toggle;
#endif

volatile static ows_line_state line_state = OWS_PORT_CLOSED;
volatile static byte comm_pin_nr;
volatile static byte tmr_wait_cycles;

volatile static byte chr_buffer;
volatile static byte bit_ix;


volatile static byte rx_buffer[RX_BUFFER_SIZE];
volatile static byte rx_buffer_head;
volatile static byte rx_buffer_tail;

/* isolate these functions and contain them to only this file using the
    static keyword (protects from users calling what they shouldn't) */
static void ows_pin_change_isr();
static void ows_timer_isr();
static void ows_set_rx_idle();


static inline void stop_timer() {
	if (ows_timer_enabled) ows_timer.end();
	ows_timer_enabled = false;
}

static inline void start_timer() {
	if (ows_timer_enabled) stop_timer(); // reset timer

	ows_timer.begin(ows_timer_isr, ows_timer_interval_micros);
	ows_timer_enabled = true;
}

static void ows_pin_change_isr() {

#ifdef DEBUG_PIN
	digitalWriteFast(DEBUG_PIN, toggle);
	digitalWriteFast(DEBUG_PIN, !toggle);
#endif

	cli();
	
	/* interrupt is only active to detect the first fall (start bit), subsequent falls belong to the character, so disable
	the interrupt while we receive */
	detachInterrupt(comm_pin_nr);
	line_state = OWS_RX_RECEIVING;

	tmr_wait_cycles = BIT_TIME_UNITS * 1.5; /* wait 1.5 bits to center us at the first data bit (the first bit is the start bit, half bit later is the center of the first data bit) */
	start_timer();
	sei();
}

static void ows_timer_isr() {
	//
	if (line_state == OWS_RX_IDLE) {
		stop_timer();
		return;
	}

	if (--tmr_wait_cycles > 0) {
		return;
	}

#ifdef DEBUG_PIN
	digitalWriteFast(DEBUG_PIN, toggle);
	toggle = !toggle;
#endif

	if (line_state == OWS_RX_RECEIVING) {
		byte pin_state = digitalReadFast(comm_pin_nr);

		/* check if it's the stop bit */
		if (bit_ix >= STOP_BIT_IX) {
			/* if (pin_state == HIGH) {
				rx_buffer[rx_buffer_tail] = chr_buffer;
				rx_buffer_tail = (rx_buffer_tail + 1) % RX_BUFFER_SIZE;
			}
			else {
				 framing error.
				 * Because the bootloader is very heterogeneous in terms of timing,
				 * just assume the framing error is occuring by a very short delay and accept the character anyway 
				
			}*/

			/* don't check for framing error; always accept the character bc the bootloader is
			   frequently off in terms of timing for start and stop bits */

			rx_buffer[rx_buffer_tail] = chr_buffer;
			rx_buffer_tail = (rx_buffer_tail + 1) % RX_BUFFER_SIZE;

			ows_set_rx_idle();

			line_state = OWS_HONOURING_STOPBIT;
			tmr_wait_cycles = BIT_TIME_UNITS * 0.5; /* we read the bit at the middle, so wait another 0.5 bit for the other
								    host to finish transmitting the stop bit, before we start sending a new char (if that's the case) */
			return;
		}

		/* shift the bit in */
		chr_buffer |= (pin_state << (bit_ix++));
		tmr_wait_cycles = BIT_TIME_UNITS; /* wait 2 half bits to get us to the center of next bit */

	}
	else if (line_state == OWS_TX_TRANSMITTING) {
		if (bit_ix >= STOP_BIT_IX) {
			// rely on the pull up to signal the stop bit and switch
			// immediately to RX mode. This way we're ready (ahead of time) to capture any
			// start bit
			ows_set_rx_idle();

			line_state = OWS_HONOURING_STOPBIT;
			tmr_wait_cycles = BIT_TIME_UNITS;
			return;			
		}

		/* shift the bit out */
		digitalWriteFast(comm_pin_nr, chr_buffer & 0x1);
		chr_buffer = chr_buffer >> 1;
		bit_ix++;

		tmr_wait_cycles = BIT_TIME_UNITS; /* hold the signal for 2 half bits */
	}
	else if (line_state == OWS_HONOURING_STOPBIT) {
		/* We will Honour the stop bit on two occasions:
			- When receiving: we read the bit at the middle, so we have to wait another half bit
			- When transmitting: we won't actively send the Stop bit high; we'll let the pull up do it
			  so that we can detect start bits that may appear too early.
			  However, we will honour the timing for the stop we should be sending (1 entire bit)
			  before moving on to sending another char 	*/
		line_state = OWS_RX_IDLE;
		stop_timer();
	}
}

static void ows_set_rx_idle() {
	cli();
	line_state = OWS_RX_IDLE;
	pinMode(comm_pin_nr, INPUT_PULLUP);

	chr_buffer = 0;
	bit_ix = 0;

	attachInterrupt(comm_pin_nr, ows_pin_change_isr, FALLING);
	sei();
}

void ows_begin(byte pin_nr, unsigned int baud_rate) {
	if (line_state != OWS_PORT_CLOSED) { halt_and_blink_error(3); }

#ifdef DEBUG_PIN
	pinMode(DEBUG_PIN, OUTPUT);
	digitalWriteFast(DEBUG_PIN, LOW);
#endif

	cli()
	rx_buffer_head = 0;
	rx_buffer_tail = 0;

	comm_pin_nr = pin_nr;

	/* timer is set in microseconds; we want it to fire every half bit, so we divide by 2 */
	ows_timer_interval_micros = (1000000 / (baud_rate)) / BIT_TIME_UNITS;

	ows_set_rx_idle();

	sei();
}

void ows_end() {
	if (line_state == OWS_PORT_CLOSED) return;

	/* wait for any previous character to finish receving or transmitting;
	to ensure we're in state IDLE to make the necessary clean up operations */
	while (line_state != OWS_RX_IDLE) { ; }

	cli();
	stop_timer();
	detachInterrupt(comm_pin_nr);
	pinMode(comm_pin_nr, INPUT_PULLUP);
	line_state = OWS_PORT_CLOSED;
	sei();
}

boolean ows_available() {

	cli();
	boolean b = (rx_buffer_head != rx_buffer_tail);
	sei();

	return b;
}

byte ows_getchar() {
	if (! ows_available()) {
		return 0;
	}
	else {
		cli();
		byte c = rx_buffer[rx_buffer_head];
		rx_buffer_head = (rx_buffer_head + 1) % RX_BUFFER_SIZE;
		sei();
		return c;
	}
}


void ows_putchar(byte tx_char) {	

	/* wait for any previous character to finish receving or transmitting;
	   this avoids collisions on the bus */
	while (line_state != OWS_RX_IDLE) { ; }

	/* switch to transmitting immediately (interrupt RX if needed) */
	cli();
	detachInterrupt(comm_pin_nr);
	
	line_state = OWS_TX_TRANSMITTING;
	chr_buffer = tx_char;
	bit_ix = 0;

	/* Set TX - active drive */
	pinMode(comm_pin_nr, OUTPUT);
	
	/* write the start bit immediately */
	digitalWriteFast(comm_pin_nr, LOW);
	tmr_wait_cycles = BIT_TIME_UNITS; /* start bit */
	start_timer();
	sei();

}




