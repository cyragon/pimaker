/* USB API for Teensy USB Development Board
 * http://www.pjrc.com/teensy/teensyduino.html
 * Copyright (c) 2008 PJRC.COM, LLC
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

#include <avr/io.h>
#include <avr/pgmspace.h>
#include <stdint.h>
#include "usb_common.h"
#include "usb_private.h"
#include "usb_api.h"
#include "keylayouts.h"
#include "wiring.h"


// Step #1, decode UTF8 to Unicode code points
//
#if ARDUINO >= 100
size_t usb_keyboard_class::write(uint8_t c)
#else
void usb_keyboard_class::write(uint8_t c)
#endif
{
        if (c < 0x80) {
                // single byte encoded, 0x00 to 0x7F
                utf8_state = 0;
                write_unicode(c);
        } else if (c < 0xC0) {
                // 2nd, 3rd or 4th byte, 0x80 to 0xBF
                c &= 0x3F;
                if (utf8_state == 1) {
                        utf8_state = 0;
                        write_unicode(unicode_wchar | c);
                } else if (utf8_state == 2) {
                        unicode_wchar |= ((uint16_t)c << 6);
                        utf8_state = 1;
                }
        } else if (c < 0xE0) {
                // begin 2 byte sequence, 0xC2 to 0xDF
                // or illegal 2 byte sequence, 0xC0 to 0xC1
                unicode_wchar = (uint16_t)(c & 0x1F) << 6;
                utf8_state = 1;
        } else if (c < 0xF0) {
                // begin 3 byte sequence, 0xE0 to 0xEF
                unicode_wchar = (uint16_t)(c & 0x0F) << 12;
                utf8_state = 2;
        } else {
                // begin 4 byte sequence (not supported), 0xF0 to 0xF4
                // or illegal, 0xF5 to 0xFF
                utf8_state = 255;
        }
#if ARDUINO >= 100
        return 1;
#endif
}


// Step #2: translate Unicode code point to keystroke sequence
//
void usb_keyboard_class::write_unicode(uint16_t cpoint)
{
        // Unicode code points beyond U+FFFF are not supported
        // technically this input should probably be called UCS-2
        if (cpoint < 32) {
                if (cpoint == 10) write_key(KEY_ENTER);
                return;
        }
        if (cpoint < 128) {
                if (sizeof(KEYCODE_TYPE) == 1) {
                        write_keycode(pgm_read_byte(keycodes_ascii + (cpoint - 0x20)));
                } else if (sizeof(KEYCODE_TYPE) == 2) {
                        write_keycode(pgm_read_word(keycodes_ascii + (cpoint - 0x20)));
                }
                return;
        }
        #ifdef ISO_8859_1_A0
        if (cpoint <= 0xA0) return;
        if (cpoint < 0x100) {
                if (sizeof(KEYCODE_TYPE) == 1) {
                        write_keycode(pgm_read_byte(keycodes_iso_8859_1 + (cpoint - 0xA0)));
                } else if (sizeof(KEYCODE_TYPE) == 2) {
                        write_keycode(pgm_read_word(keycodes_iso_8859_1 + (cpoint - 0xA0)));
                }
                return;
        }
        #endif
        #ifdef UNICODE_20AC
                if (cpoint == 0x20AC) write_keycode(UNICODE_20AC);
        #endif
}

// Step #3: execute keystroke sequence
//
void usb_keyboard_class::write_keycode(KEYCODE_TYPE key)
{
        if (!key) return;
        #ifdef DEADKEYS_MASK
        KEYCODE_TYPE deadkey = key & DEADKEYS_MASK;
        #ifdef ACUTE_ACCENT_BITS
        if (deadkey == ACUTE_ACCENT_BITS) write_key(DEADKEY_ACUTE_ACCENT);
        #endif
        #ifdef CEDILLA_BITS
        if (deadkey == CEDILLA_BITS) write_key(DEADKEY_CEDILLA);
        #endif
        #ifdef CIRCUMFLEX_BITS
        if (deadkey == CIRCUMFLEX_BITS) write_key(DEADKEY_CIRCUMFLEX);
        #endif
        #ifdef DIAERESIS_BITS
        if (deadkey == DIAERESIS_BITS) write_key(DEADKEY_DIAERESIS);
        #endif
        #ifdef GRAVE_ACCENT_BITS
        if (deadkey == GRAVE_ACCENT_BITS) write_key(DEADKEY_GRAVE_ACCENT);
        #endif
        #ifdef TILDE_BITS
        if (deadkey == TILDE_BITS) write_key(DEADKEY_TILDE);
        #endif
        #ifdef RING_ABOVE_BITS
        if (deadkey == RING_ABOVE_BITS) write_key(DEADKEY_RING_ABOVE);
        #endif
        #endif // DEADKEYS_MASK
        write_key(key);
}

// Step #4: do each keystroke
//
void usb_keyboard_class::write_key(uint8_t key)
{
        uint8_t modifier=0;

        #ifdef SHIFT_MASK
        if (key & SHIFT_MASK) modifier |= MODIFIERKEY_SHIFT;
        #endif
        #ifdef ALTGR_MASK
        if (key & ALTGR_MASK) modifier |= MODIFIERKEY_RIGHT_ALT;
        #endif
        keyboard_modifier_keys = modifier;
        key &= 0x3F;
        #ifdef KEY_NON_US_100
        if (key == KEY_NON_US_100) key = 100;
        #endif
        keyboard_keys[0] = key;
        keyboard_keys[1] = 0;
        keyboard_keys[2] = 0;
        keyboard_keys[3] = 0;
        keyboard_keys[4] = 0;
        keyboard_keys[5] = 0;
        send_now();
        keyboard_modifier_keys = 0;
        keyboard_keys[0] = 0;
        send_now();
}

void usb_keyboard_class::set_modifier(uint8_t c)
{
        keyboard_modifier_keys = c;
}
void usb_keyboard_class::set_key1(uint8_t c)
{
        keyboard_keys[0] = c;
}
void usb_keyboard_class::set_key2(uint8_t c)
{
        keyboard_keys[1] = c;
}
void usb_keyboard_class::set_key3(uint8_t c)
{
        keyboard_keys[2] = c;
}
void usb_keyboard_class::set_key4(uint8_t c)
{
        keyboard_keys[3] = c;
}
void usb_keyboard_class::set_key5(uint8_t c)
{
        keyboard_keys[4] = c;
}
void usb_keyboard_class::set_key6(uint8_t c)
{
        keyboard_keys[5] = c;
}


void usb_keyboard_class::send_now(void)
{
        uint8_t intr_state, timeout;

        if (!usb_configuration) return;
        intr_state = SREG;
        cli();
        UENUM = KEYBOARD_ENDPOINT;
        timeout = UDFNUML + 50;
        while (1) {
                // are we ready to transmit?
                if (UEINTX & (1<<RWAL)) break;
                SREG = intr_state;
                // has the USB gone offline?
                if (!usb_configuration) return;
                // have we waited too long?
                if (UDFNUML == timeout) return;
                // get ready to try checking again
                intr_state = SREG;
                cli();
                UENUM = KEYBOARD_ENDPOINT;
        }
        UEDATX = keyboard_modifier_keys;
        UEDATX = 0;
        UEDATX = keyboard_keys[0];
        UEDATX = keyboard_keys[1];
        UEDATX = keyboard_keys[2];
        UEDATX = keyboard_keys[3];
        UEDATX = keyboard_keys[4];
        UEDATX = keyboard_keys[5];
        UEINTX = 0x3A;
        keyboard_idle_count = 0;
        SREG = intr_state;
}










static volatile uint8_t prev_byte=0;

void usb_serial_class::begin(long speed)
{
	// make sure USB is initialized
	usb_init();
	uint16_t begin_wait = (uint16_t)millis();
	while (1) {
		// wait for the host to finish enumeration
		if (usb_configuration) {
			delay(250);  // a little time for host to load a driver
			return;
		}
		// or for suspend mode (powered without USB)
		if (usb_suspended) {
			uint16_t begin_suspend = (uint16_t)millis();
			while (usb_suspended) {
				// must remain suspended for a while, because
				// normal USB enumeration causes brief suspend
				// states, typically under 0.1 second
				if ((uint16_t)millis() - begin_suspend > 250) {
					return;
				}
			}
		}
		// ... or a timout (powered by a USB power adaptor that
		// wiggles the data lines to keep a USB device charging)
		if ((uint16_t)millis() - begin_wait > 2500) return;
	}
	prev_byte = 0;
}

void usb_serial_class::end()
{
	usb_shutdown();
	delay(25);
}


// number of bytes available in the receive buffer
int usb_serial_class::available()
{
	uint8_t c;

	c = prev_byte;	// assume 1 byte static volatile access is atomic
	if (c) return 1;
	c = readnext();
	if (c) {
		prev_byte = c;
		return 1;
	}
	return 0;
}

// get the next character, or -1 if nothing received
int usb_serial_class::read()
{
	uint8_t c;

	c = prev_byte;
	if (c) {
		prev_byte = 0;
		return c;
	}
	c = readnext();
	if (c) return c;
	return -1;
}

int usb_serial_class::peek()
{
	uint8_t c;

	c = prev_byte;
	if (c) return c;
	c = readnext();
	if (c) {
		prev_byte = c;
		return c;
	}
	return -1;
}

// get the next character, or 0 if nothing
uint8_t usb_serial_class::readnext(void)
{
	uint8_t c, intr_state;

	// interrupts are disabled so these functions can be
	// used from the main program or interrupt context,
	// even both in the same program!
	intr_state = SREG;
	cli();
	if (!usb_configuration) {
		SREG = intr_state;
		return 0;
	}
	UENUM = DEBUG_RX_ENDPOINT;
try_again:
	if (!(UEINTX & (1<<RWAL))) {
		// no packet in buffer
		SREG = intr_state;
		return 0;
	}
	// take one byte out of the buffer
	c = UEDATX;
	if (c == 0) {
		// if we see a zero, discard it and
		// discard the rest of this packet
		UEINTX = 0x6B;
		goto try_again;
	}
	// if this drained the buffer, release it
	if (!(UEINTX & (1<<RWAL))) UEINTX = 0x6B;
	SREG = intr_state;
	return c;
}

// discard any buffered input
void usb_serial_class::flush()
{
	uint8_t intr_state;

	if (usb_configuration) {
		intr_state = SREG;
		cli();
		UENUM = DEBUG_RX_ENDPOINT;
		while ((UEINTX & (1<<RWAL))) {
			UEINTX = 0x6B;
		}
		SREG = intr_state;
	}
	prev_byte = 0;
}

// transmit a character.
#if ARDUINO >= 100
size_t usb_serial_class::write(uint8_t c)
#else
void usb_serial_class::write(uint8_t c)
#endif
{
	//static uint8_t previous_timeout=0;
	uint8_t timeout, intr_state;

	// if we're not online (enumerated and configured), error
	if (!usb_configuration) goto error;
	// interrupts are disabled so these functions can be
	// used from the main program or interrupt context,
	// even both in the same program!
	intr_state = SREG;
	cli();
	UENUM = DEBUG_TX_ENDPOINT;
	// if we gave up due to timeout before, don't wait again
#if 0
	if (previous_timeout) {
		if (!(UEINTX & (1<<RWAL))) {
			SREG = intr_state;
			return;
		}
		previous_timeout = 0;
	}
#endif
	// wait for the FIFO to be ready to accept data
	timeout = UDFNUML + TRANSMIT_TIMEOUT;
	while (1) {
		// are we ready to transmit?
		if (UEINTX & (1<<RWAL)) break;
		SREG = intr_state;
		// have we waited too long?  This happens if the user
		// is not running an application that is listening
		if (UDFNUML == timeout) {
			//previous_timeout = 1;
			goto error;
		}
		// has the USB gone offline?
		if (!usb_configuration) goto error;
		// get ready to try checking again
		intr_state = SREG;
		cli();
		UENUM = DEBUG_TX_ENDPOINT;
	}
	// actually write the byte into the FIFO
	UEDATX = c;
	// if this completed a packet, transmit it now!
	if (!(UEINTX & (1<<RWAL))) {
		UEINTX = 0x3A;
		debug_flush_timer = 0;
	} else {
		debug_flush_timer = TRANSMIT_FLUSH_TIMEOUT;
	}
	SREG = intr_state;
#if ARDUINO >= 100
        return 1;
#endif
error:
#if ARDUINO >= 100
        setWriteError();
        return 0;
#else
	return;
#endif
}


// These are Teensy-specific extensions to the Serial object

// immediately transmit any buffered output.
// This doesn't actually transmit the data - that is impossible!
// USB devices only transmit when the host allows, so the best
// we can do is release the FIFO buffer for when the host wants it
void usb_serial_class::send_now(void)
{
	uint8_t intr_state;

	intr_state = SREG;
	cli();
	if (debug_flush_timer) {
		UENUM = DEBUG_TX_ENDPOINT;
		while ((UEINTX & (1<<RWAL))) {
			UEDATX = 0;
		}
		UEINTX = 0x3A;
		debug_flush_timer = 0;
	}
	SREG = intr_state;
}

uint32_t usb_serial_class::baud(void)
{
	return ((uint32_t)DEBUG_TX_SIZE * 1000 / DEBUG_TX_INTERVAL);
}

uint8_t usb_serial_class::stopbits(void)
{
	return 0;
}

uint8_t usb_serial_class::paritytype(void)
{
	return 0;
}

uint8_t usb_serial_class::numbits(void)
{
	return 8;
}

uint8_t usb_serial_class::dtr(void)
{
	return 0;
}

uint8_t usb_serial_class::rts(void)
{
	return 0;
}



// Preinstantiate Objects //////////////////////////////////////////////////////

usb_serial_class	Serial = usb_serial_class();
usb_keyboard_class	Keyboard = usb_keyboard_class();
usb_disk_class		Disk = usb_disk_class();


