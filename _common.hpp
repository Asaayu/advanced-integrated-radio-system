// Base
#define QUOTE(TEXT) #TEXT

// CgPatches
#define AUTHORS authors[]= {"Asaayu"}; author = "Asaayu";
#define URL url = "https://github.com/Asaayu/advanced-integrated-radio-system";
#define NAME(SUBTITLE) name = QUOTE(Advanced Integrated Radio System - )##SUBTITLE;
#define VERSION requiredVersion = 0.1;

// UI defines
#define SW safezoneW
#define SH safezoneH
#define SX safezoneX
#define SY safezoneY

#define HI(VALUE) (VALUE * 1.75 * SH)

#define W(VALUE) (VALUE * SW)
#define H(VALUE) (VALUE * SH)
#define X(VALUE) (SX + VALUE * SW)
#define Y(VALUE) (SY + VALUE * SH)

#define LHX(VALUE) H(0.0625) + (LH * VALUE)
#define LH H(0.02)
