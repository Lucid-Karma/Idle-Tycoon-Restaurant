// Google Analytics 4 for the CrazyGames build (GoogleAnalytics.cs decides when to start it). gtag.js is loaded
// here, from the game, so the page template stays the same for every portal.
// - Nothing is sent from localhost (the editor's WebGL runs and local tests), unless the page is opened with
//   ?ga_test=1: then events go out in debug mode and show in GA's Admin > DebugView.
// - Analytics only: no ads, no Google signals, no ad personalisation (ad_* consent denied everywhere).
// - Players in the EEA, the UK and Switzerland get analytics_storage "denied" (Consent Mode): no cookies,
//   only cookieless pings - the game has no consent banner of its own and CrazyGames' consent is not passed to it.
// - The game runs in CrazyGames' iframe, so the GA cookie needs SameSite=None to be kept between visits.
mergeInto(LibraryManager.library, {
  ChibiGaInit: function (idPtr) {
    var id = UTF8ToString(idPtr);
    var host = window.location.hostname;
    var test = /[?&]ga_test=1/.test(window.location.search);
    var local = host === 'localhost' || host === '127.0.0.1' || host === '';
    if (window.chibiGa || (local && !test)) return 0;
    window.dataLayer = window.dataLayer || [];
    window.gtag = function () { window.dataLayer.push(arguments); };
    var regions = ['AT', 'BE', 'BG', 'HR', 'CY', 'CZ', 'DK', 'EE', 'FI', 'FR', 'DE', 'GR', 'HU', 'IE', 'IT', 'LV',
                   'LT', 'LU', 'MT', 'NL', 'PL', 'PT', 'RO', 'SK', 'SI', 'ES', 'SE', 'IS', 'LI', 'NO', 'GB', 'CH'];
    window.gtag('consent', 'default', {
      ad_storage: 'denied', ad_user_data: 'denied', ad_personalization: 'denied',
      analytics_storage: 'granted',
    });
    window.gtag('consent', 'default', { analytics_storage: 'denied', region: regions });
    window.gtag('js', new Date());
    var config = {
      cookie_flags: 'SameSite=None;Secure',
      allow_google_signals: false,
      allow_ad_personalization_signals: false,
    };
    if (test) config.debug_mode = true;   // any value turns debug mode on, so only set it when testing
    window.gtag('config', id, config);
    var s = document.createElement('script');
    s.async = true;
    s.src = 'https://www.googletagmanager.com/gtag/js?id=' + encodeURIComponent(id);
    document.head.appendChild(s);
    window.chibiGa = true;
    return 1;
  },

  ChibiGaEvent: function (namePtr, paramsPtr) {
    if (!window.chibiGa) return;
    var params = {};
    try { params = JSON.parse(UTF8ToString(paramsPtr) || '{}'); } catch (e) {}
    window.gtag('event', UTF8ToString(namePtr), params);
  },
});
