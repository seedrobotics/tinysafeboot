/*
===============================================
Copyright 2017, Seed Robotics Ltd, Pedro Ramilo
===============================================

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

*/

#ifndef __ONEWIRESERIAL_H__
#define __ONEWIRESERIAL_H__

#include "Arduino.h"

#define RX_BUFFER_SIZE 20
#define STOP_BIT_IX 8

enum ows_line_state { OWS_PORT_CLOSED, OWS_RX_IDLE, OWS_RX_RECEIVING, OWS_TX_TRANSMITTING, OWS_HONOURING_STOPBIT };

void ows_begin(byte pin_nr, unsigned int baud_rate_bps);
void ows_end();
boolean ows_available();
byte ows_getchar();
void ows_putchar(byte tx_char);

#endif