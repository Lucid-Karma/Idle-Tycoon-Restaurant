// Frame-rate cap for the web build (PerformanceGovernor). The page's frame limiter in the WebGL template
// "ChibiCafe" reads window.chibiMaxFps and drops frames at vsync, so the cap holds on 90/120 Hz screens too.
mergeInto(LibraryManager.library, {
  ChibiSetMaxFps: function (fps) {
    window.chibiMaxFps = fps;
  },
  // The game scene is up (Boot): the page's loading screen can go - one loading screen from the first byte to the
  // kitchen, instead of the page's and then the Boot scene's own "Loading..." (2026-10-20).
  ChibiGameReady: function () {
    if (window.chibiGameReady) window.chibiGameReady();
  },
});
