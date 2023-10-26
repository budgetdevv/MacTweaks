#include <stdio.h>
#include <IOKit/graphics/IOGraphicsLib.h>
#include <ApplicationServices/ApplicationServices.h>

extern double CoreDisplay_Display_GetUserBrightness(CGDirectDisplayID id);

extern void CoreDisplay_Display_SetUserBrightness(CGDirectDisplayID id, double brightness);

extern bool DisplayServicesCanChangeBrightness(CGDirectDisplayID id);

extern void DisplayServicesBrightnessChanged(CGDirectDisplayID id, double brightness);

extern int DisplayServicesGetBrightness(CGDirectDisplayID id, float *brightness);

extern int DisplayServicesSetBrightness(CGDirectDisplayID id, float brightness);

extern const int kMaxDisplays;
extern const CFStringRef kDisplayBrightness;

bool CFNumberEqualsUInt32(CFNumberRef number, uint32_t uint32);

/* CGDisplayIOServicePort is deprecated as of 10.9; try to match ourselves */
io_service_t CGDisplayGetIOServicePort(CGDirectDisplayID id);

bool SetBrightness(CGDirectDisplayID id, io_service_t service, float brightness);

bool GetBrightness(CGDirectDisplayID id, io_service_t service, float *brightness);
