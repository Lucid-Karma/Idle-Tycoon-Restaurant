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
  // Seconds since the page started loading (Boot.Mark): the same clock the portals measure load time with.
  ChibiPageSeconds: function () {
    return performance.now() / 1000;
  },
  // Straight to the browser console: the game's own Debug.Log lines are filtered out of release builds.
  ChibiConsole: function (text) {
    console.log(UTF8ToString(text));
  },
});
