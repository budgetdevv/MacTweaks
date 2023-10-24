#import <AppKit/AppKit.h>
#import <Foundation/Foundation.h>
#import <Cocoa/Cocoa.h>

typedef struct
{
    NSString* AXTitle;
    NSString* AXSubrole;
    NSRect Rect;
    NSNumber* AXIsApplicationRunning;
    pid_t PID;

} AXUIElement;

bool AXGetElementAtPosition(AXUIElementRef sysWide, float x, float y, AXUIElement* outputPtr);

AXUIElementRef AXUIGetApplicationAccessibilityElement(int pid);

bool GetWindowListForApplicationDirect(AXUIElementRef app, CFArrayRef* windowsList);

bool GetWindowListForApplication(pid_t pid, CFArrayRef* windowsList);

bool MinimizeAllWindowsForApplicationDirect(AXUIElementRef app);

bool MinimizeAllWindowsForApplication(int pid);

bool ApplicationAllWindowsAreMinimizedDirect(AXUIElementRef app, bool* areMinimized);

bool ApplicationAllWindowsAreMinimized(int pid, bool* areMinimized);

bool ApplicationFocusedWindowIsFullScreenDirect(AXUIElementRef app);

bool ApplicationFocusedWindowIsFullScreen(int pid);

bool CGEventGetIntegerValueFieldWrapper(CGEventRef event, CGEventField field);

void CGEventSetIntegerValueFieldWrapper(CGEventRef event, CGEventField field, int64_t value);

bool CloseWindowDirect(AXUIElementRef window);

bool ApplicationCloseFocusedWindowDirect(AXUIElementRef app);

bool ApplicationCloseFocusedWindow(int pid);

typedef struct
{
    AXUIElement Base;
    AXUIElementRef Handle;
} AXUIElementRaw;

bool AXGetElementAtPositionRaw(AXUIElementRef sysWide, float x, float y, AXUIElementRaw* outputPtr);

bool WindowToggleMinimize(AXUIElementRef window);