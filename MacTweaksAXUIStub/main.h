#import <AppKit/AppKit.h>
#import <Foundation/Foundation.h>
#import <Cocoa/Cocoa.h>

typedef struct {
    NSString *AXTitle;
    NSString *AXSubrole;
    NSRect Rect;
    NSNumber *AXIsApplicationRunning;
} AXUIElement;

bool AXGetElementAtPosition(AXUIElementRef sysWide, float x, float y, AXUIElement* output);

AXUIElementRef AXUIGetApplicationAccessibilityElement(int pid);

bool GetWindowListForApplication(AXUIElementRef app, CFArrayRef* windowsList);

bool MinimizeAllWindowsForApplicationDirect(AXUIElementRef app);

bool MinimizeAllWindowsForApplication(int pid);

bool ApplicationAllWindowsAreMinimizedDirect(AXUIElementRef app);

bool ApplicationAllWindowsAreMinimized(int pid);