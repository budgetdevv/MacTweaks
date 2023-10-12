#import <AppKit/AppKit.h>
#import <Foundation/Foundation.h>
#import <Cocoa/Cocoa.h>

typedef struct
{
    NSString* AXTitle;
    NSString* AXSubrole;
    NSRect Rect;
    NSNumber* AXIsApplicationRunning;
} AXUIElement;

bool AXGetElementAtPosition(AXUIElementRef sysWide, float x, float y, AXUIElement output)
{
    // This element will contain whatever we are hovering over.
    AXUIElementRef element = NULL;

    // Check to see what it is
    int err = AXUIElementCopyElementAtPosition(sysWide, x, y, &element);

    AXValueRef value;
    NSRect rect;

    // Check to see if this found something, if not, return undefined.
    if (err == kAXErrorSuccess && AXUIElementCopyAttributeValue(element, kAXSubroleAttribute, (CFTypeRef*) &output.AXSubrole) == 0)
    {
        // Get the size of the element
        AXUIElementCopyAttributeValue(element, kAXSizeAttribute, (CFTypeRef*)&value);
        AXValueGetValue(value, kAXValueCGSizeType, (void*) &rect.size);

        // Get the position of the element
        AXUIElementCopyAttributeValue(element, kAXPositionAttribute, (CFTypeRef*) &value);
        AXValueGetValue(value, kAXValueCGPointType, (void*) &rect.origin);

        // Get the title of the element
        AXUIElementCopyAttributeValue(element, kAXTitleAttribute, (CFTypeRef*) &output.AXTitle);

        // Get the running status of the element
        AXUIElementCopyAttributeValue(element, kAXIsApplicationRunningAttribute, (CFTypeRef*) &output.AXIsApplicationRunning);

        output.Rect = rect;

        CFRelease(value);

        return true;
    }

    return false;
}
