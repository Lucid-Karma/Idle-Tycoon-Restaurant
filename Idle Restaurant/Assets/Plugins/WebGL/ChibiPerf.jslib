// Frame-rate cap for the web build (PerformanceGovernor). The page's frame limiter in the WebGL template
// "ChibiCafe" reads window.chibiMaxFps and drops frames at vsync, so the cap holds on 90/120 Hz screens too.
mergeInto(LibraryManager.library, {
  ChibiSetMaxFps: function (fps) {
    window.chibiMaxFps = fps;
  },
});
