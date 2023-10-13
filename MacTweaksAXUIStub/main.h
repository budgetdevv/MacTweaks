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