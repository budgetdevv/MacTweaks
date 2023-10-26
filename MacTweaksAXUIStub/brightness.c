#include <stdio.h>
#include <stdbool.h>
#include <unistd.h>
#include <IOKit/graphics/IOGraphicsLib.h>
#include <ApplicationServices/ApplicationServices.h>

/* As of macOS 10.12.4, brightness set by public IOKit API is
   overridden by CoreDisplay's brightness (to support Night Shift). In
   addition, using CoreDisplay to get brightness supports additional
   display types, e.g. the 2015 iMac's internal display.

   The below functions in CoreDisplay seem to work to adjust the
   "user" brightness (like dragging the slider in System Preferences
   or using function keys).  The symbols below are listed in the .tbd
   file in CoreDisplay.framework so it is at least more "public" than
   a symbol in a private framework, though there are no public headers
   distributed for this framework. */
extern double CoreDisplay_Display_GetUserBrightness(CGDirectDisplayID id)
__attribute__((weak_import));
extern void CoreDisplay_Display_SetUserBrightness(CGDirectDisplayID id,double brightness)
__attribute__((weak_import));

/* Some issues with the above CoreDisplay functions include:

   - There's no way to tell if setting the brightness was successful

   - There's no way to tell if a brightness of 1 means that the
     brightness is actually 1, or if there's no adjustable brightness

   - Brightness changes aren't reflected in System Preferences
     immediately

   - They don't work on Apple Silicon Macs

   Fixing these means using the private DisplayServices.framework.  Be
   even more careful about these.
*/
extern bool DisplayServicesCanChangeBrightness(CGDirectDisplayID id)
__attribute__((weak_import));
extern void DisplayServicesBrightnessChanged(CGDirectDisplayID id,
                                             double brightness)
__attribute__((weak_import));

/* Below functions are necessary on Apple Silicon/macOS 11. */
extern int DisplayServicesGetBrightness(CGDirectDisplayID id,
                                        float *brightness)
__attribute__((weak_import));
extern int DisplayServicesSetBrightness(CGDirectDisplayID id,
                                        float brightness)
__attribute__((weak_import));

const int kMaxDisplays = 16;
const CFStringRef kDisplayBrightness = CFSTR(kIODisplayBrightnessKey);

bool CFNumberEqualsUInt32(CFNumberRef number, uint32_t uint32)
{
    if (number == NULL)
    {
        return (uint32 == 0);
    }

    /* there's no CFNumber type guaranteed to be a uint32, so pick
       something bigger that's guaranteed not to truncate */
    int64_t int64;

    if (!CFNumberGetValue(number, kCFNumberSInt64Type, &int64))
    {
        return false;
    }

    return int64 == uint32;
}

/* CGDisplayIOServicePort is deprecated as of 10.9; try to match ourselves */
io_service_t CGDisplayGetIOServicePort(CGDirectDisplayID dspy) {
    uint32_t vendor = CGDisplayVendorNumber(dspy);
    uint32_t model = CGDisplayModelNumber(dspy); // == product ID
    uint32_t serial = CGDisplaySerialNumber(dspy);

    CFMutableDictionaryRef matching = IOServiceMatching("IODisplayConnect");

    io_iterator_t iter;

    if (IOServiceGetMatchingServices(kIOMasterPortDefault, matching, &iter))
    {
        return 0;
    }

    io_service_t service, matching_service = 0;
    while ( (service = IOIteratorNext(iter)) != 0)
    {
        CFDictionaryRef info = IODisplayCreateInfoDictionary(service, kIODisplayNoProductName);

        CFNumberRef vendorID = CFDictionaryGetValue(info, CFSTR(kDisplayVendorID));
        CFNumberRef productID = CFDictionaryGetValue(info, CFSTR(kDisplayProductID));
        CFNumberRef serialNumber = CFDictionaryGetValue(info, CFSTR(kDisplaySerialNumber));

        if (CFNumberEqualsUInt32(vendorID, vendor) &&
            CFNumberEqualsUInt32(productID, model) &&
            CFNumberEqualsUInt32(serialNumber, serial))
        {
            matching_service = service;

            CFRelease(info);
            break;
        }

        CFRelease(info);
    }

    IOObjectRelease(iter);
    return matching_service;
}

bool SetBrightness(CGDirectDisplayID dspy, io_service_t service,float brightness) {
    /* 1. Try DisplayServices set SPI - more likely to work on
       recent macOS */
    if ((DisplayServicesSetBrightness != NULL) && !DisplayServicesSetBrightness(dspy, brightness))
    {
        return true;
    }

    /* 2. Try CoreDisplay SPI wrapped by DisplayServices (if available)
       to work around caveats as described above */
    if (CoreDisplay_Display_SetUserBrightness != NULL) {
        if ((DisplayServicesCanChangeBrightness != NULL) && !DisplayServicesCanChangeBrightness(dspy))
        {
            return false;
        }

        CoreDisplay_Display_SetUserBrightness(dspy, brightness);

        if (DisplayServicesBrightnessChanged != NULL)
            DisplayServicesBrightnessChanged(dspy, brightness);
        return true;
    }

    /* 3. Try IODisplay API */
    IOReturn err = IODisplaySetFloatParameter(service, kNilOptions,kDisplayBrightness, brightness);
    if (err != kIOReturnSuccess)
    {
        return false;
    }

    return true;
}

bool GetBrightness(CGDirectDisplayID dspy, io_service_t service,
                          float *brightness) {
    /* 1. Try DisplayServices get SPI - more likely to work on recent
       macOS */
    if ((DisplayServicesGetBrightness != NULL) &&
        !DisplayServicesGetBrightness(dspy, brightness)) {
        return true;
    }

    /* 2. Try CoreDisplay SPI wrapped by DisplayServices (if available)
       to work around caveats as described above */
    if (CoreDisplay_Display_GetUserBrightness != NULL) {
        if ((DisplayServicesCanChangeBrightness != NULL) && !DisplayServicesCanChangeBrightness(dspy))
        {
            return false;
        }

        *brightness = (float) CoreDisplay_Display_GetUserBrightness(dspy);
        return true;
    }

    /* 3. Try IODisplay API */
    IOReturn err = IODisplayGetFloatParameter(service, kNilOptions,kDisplayBrightness, brightness);

    return err == kIOReturnSuccess;
}