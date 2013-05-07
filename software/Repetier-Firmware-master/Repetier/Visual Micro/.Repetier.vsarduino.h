//Board = [BootloaderCDC]Teensylu/Printrboard
#define ARDUINO 22
#define __AVR_AT90USB1286__
#define F_CPU 16000000L
#define __AVR__
#define __cplusplus
#define __attribute__(x)
#define __inline__
#define __asm__(x)
#define __extension__
#define __ATTR_PURE__
#define __ATTR_CONST__
#define __inline__
#define __asm__ 
#define __volatile__
#define __builtin_va_list
#define __builtin_va_start
#define __builtin_va_end
#define __DOXYGEN__
#define prog_void
#define PGM_VOID_P int
#define NOINLINE __attribute__((noinline))

typedef unsigned char byte;
extern "C" void __cxa_pure_virtual() {}

void check_mem();
void send_mem();
void initsd();
inline void write_command(GCode *code);
void update_ramps_parameter();
//already defined in arduno.h
//already defined in arduno.h
void log_long_array(PGM_P ptr,long *arr);
void log_float_array(PGM_P ptr,float *arr);
void log_printLine(PrintLine *p);
inline void disable_x();
inline void disable_y();
inline void disable_z();
inline void  enable_x();
inline void  enable_y();
inline void  enable_z();
inline long Div4U2U(unsigned long a,unsigned int b);
long CPUDivU2(unsigned int divisor);
byte get_coordinates(GCode *com);
inline float computeJerk(PrintLine *p1,PrintLine *p2);
inline float safeSpeed(PrintLine *p);
inline unsigned long U16SquaredToU32(unsigned int val);
void updateStepsParameter(PrintLine *p );
void finishNextSegment();
void updateTrapezoids(byte p);
void move_steps(long x,long y,long z,long e,float feedrate,bool waitEnd,bool check_endstop);
void queue_move(byte check_endstops,byte pathOptimize);
inline unsigned int ComputeV(long timer,long accel);
inline unsigned long mulu6xu16to32(unsigned int a,unsigned int b);
inline unsigned int mulu6xu16shift16(unsigned int a,unsigned int b);
inline long bresenham_step();
void kill(byte only_steppers);
inline void setTimer(unsigned long delay);

#include "C:\PiMaker\arduino-0022\hardware\at90usb1286\cores\at90usb1286\arduino.h"
#include "C:\PiMaker\PiMaker\PiMakerHost\Repetier-Firmware-master\Repetier\Repetier.pde"
#include "C:\PiMaker\PiMaker\PiMakerHost\Repetier-Firmware-master\Repetier\Commands.cpp"
#include "C:\PiMaker\PiMaker\PiMakerHost\Repetier-Firmware-master\Repetier\Configuration.h"
#include "C:\PiMaker\PiMaker\PiMakerHost\Repetier-Firmware-master\Repetier\Eeprom.cpp"
#include "C:\PiMaker\PiMaker\PiMakerHost\Repetier-Firmware-master\Repetier\Eeprom.h"
#include "C:\PiMaker\PiMaker\PiMakerHost\Repetier-Firmware-master\Repetier\Extruder.cpp"
#include "C:\PiMaker\PiMaker\PiMakerHost\Repetier-Firmware-master\Repetier\FatStructs.h"
#include "C:\PiMaker\PiMaker\PiMakerHost\Repetier-Firmware-master\Repetier\Reptier.h"
#include "C:\PiMaker\PiMaker\PiMakerHost\Repetier-Firmware-master\Repetier\Sd2Card.cpp"
#include "C:\PiMaker\PiMaker\PiMakerHost\Repetier-Firmware-master\Repetier\Sd2Card.h"
#include "C:\PiMaker\PiMaker\PiMakerHost\Repetier-Firmware-master\Repetier\Sd2PinMap.h"
#include "C:\PiMaker\PiMaker\PiMakerHost\Repetier-Firmware-master\Repetier\SdFat.h"
#include "C:\PiMaker\PiMaker\PiMakerHost\Repetier-Firmware-master\Repetier\SdFatUtil.h"
#include "C:\PiMaker\PiMaker\PiMakerHost\Repetier-Firmware-master\Repetier\SdFatmainpage.h"
#include "C:\PiMaker\PiMaker\PiMakerHost\Repetier-Firmware-master\Repetier\SdFile.cpp"
#include "C:\PiMaker\PiMaker\PiMakerHost\Repetier-Firmware-master\Repetier\SdInfo.h"
#include "C:\PiMaker\PiMaker\PiMakerHost\Repetier-Firmware-master\Repetier\SdVolume.cpp"
#include "C:\PiMaker\PiMaker\PiMakerHost\Repetier-Firmware-master\Repetier\fastio.h"
#include "C:\PiMaker\PiMaker\PiMakerHost\Repetier-Firmware-master\Repetier\gcode.cpp"
#include "C:\PiMaker\PiMaker\PiMakerHost\Repetier-Firmware-master\Repetier\gcode.h"
#include "C:\PiMaker\PiMaker\PiMakerHost\Repetier-Firmware-master\Repetier\pins.h"
#include "C:\PiMaker\PiMaker\PiMakerHost\Repetier-Firmware-master\Repetier\ui.cpp"
#include "C:\PiMaker\PiMaker\PiMakerHost\Repetier-Firmware-master\Repetier\ui.h"
#include "C:\PiMaker\PiMaker\PiMakerHost\Repetier-Firmware-master\Repetier\uiconfig.h"
#include "C:\PiMaker\PiMaker\PiMakerHost\Repetier-Firmware-master\Repetier\uilang.h"
