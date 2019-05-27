// Each notification type should have its own enum. Notes show what data is expected (if any) when sent and when the event is fired.
public enum Msg
{
    // key events
    AnyKeyDown,                     //                          Global. A key has just been pressed.

    // mouse events
    MouseDown,                      // int                      Global. Left mouse has been pressed down.
    MouseDownLong,                  // int                      Global. Left mouse has been down long enough to trigger a long hold.
    MouseUpQuick,                   // int                      Global. Left mouse button is released, not after a long hold.
    MouseUpLong,                    // int                      Global. Left mouse button is released, after a long hold.

    MouseDownRaw,                   // int                      These 'raw' mouse events are sent regardless of whether game controls are active or not.
    MouseDownLongRaw,               // int                      
    MouseUpQuickRaw,                // int                      
    MouseUpLongRaw,                 // int                                 
    MouseWheel,                     // int                      Global. Mouse wheel is scrolled either up (1) or down (-1). 

    // object targeting and placement
    ObjectTargeted,                 // GameObject, RaycastHit   Global. Object is targeted. Checked every frame.
    ObjectUntargeted,               // GameObject               Global. Object is untargeted. Checked every frame.
    ObjectTargetedLeftMouseDown,    // GameObject, RaycastHit   Global. Left mouse down on an object.
    ObjectTargetedLeftMouseUp,      // GameObject, RaycastHit   Global. Left mouse up on an object.
    ObjectTargetedRightMouseDown,   // GameObject, RaycastHit   Global. Right mouse down on an object.
    ObjectTargetedRightMouseUp,     // GameObject, RaycastHit   Global. Right mouse up on an object.

    // game
    ExitGameStart,                  //                          Global. The game is starting exit completely.
    ExitGame,                       //                          Global. The game is exiting completely. Called directly after ExitGameStart.

    // ai
    AiLoadedFromDisk,               //                          Global. The Utility AI has been loaded or reloaded.
    AiRevertedUnsavedChanges,       //                          Global. The Utility AI has reverted unsaved changes.
    AiNativeArrayssGenerated,       //                          Global. The Utility AI has generated its native arrays. They can now be accessed.

    TotalTypes // used as a count
};