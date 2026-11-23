// Applies the persisted UI theme before first paint, and keeps it in sync.
// "dark" is the default when nothing is stored.
//
// Browsers that share storage between pages (an http host, or Chromium's
// file://) sync live via the `storage` event. Firefox gives every file://
// page a unique origin, so storage cannot cross pages there: the theme also
// rides on the ?theme= URL parameter of in-prototype links, and
// CrossdeckTheme.set keeps those links up to date.
(function () {
  const key = 'crossdeck-theme';
  const urlTheme = new URLSearchParams(location.search).get('theme');

  let theme;
  if (urlTheme === 'light' || urlTheme === 'dark') {
    theme = urlTheme;
    try {
      localStorage.setItem(key, theme);
    } catch (e) {
      // Storage unavailable (private browsing): the URL parameter still applied.
    }
  } else {
    try {
      theme = localStorage.getItem(key);
    } catch (e) {
      // Storage unavailable (private browsing, unique-origin file://): use the default.
    }
    if (theme !== 'light' && theme !== 'dark') theme = null;
  }

  function apply() {
    if (theme === 'light') {
      document.documentElement.setAttribute('data-theme', 'light');
    } else {
      document.documentElement.removeAttribute('data-theme');
    }
  }

  // The theme rides on in-prototype links so pages that cannot share storage
  // (Firefox file://) still pick it up when navigated to.
  function syncLinks() {
    document.querySelectorAll('a[href]').forEach(a => {
      const href = a.getAttribute('href');
      if (!href || !/\.html($|\?)/.test(href) || /^https?:/i.test(href)) return;
      const [path, query] = href.split('?');
      const params = new URLSearchParams(query || '');
      if (theme === 'light') {
        params.set('theme', 'light');
      } else {
        params.delete('theme');
      }
      const qs = params.toString();
      a.setAttribute('href', qs ? path + '?' + qs : path);
    });
  }

  apply();

  // theme.js loads in <head> (so the theme applies before first paint), when the
  // body's links do not exist yet. Rewrite them once the DOM is ready.
  function whenReady(fn) {
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', fn);
    } else {
      fn();
    }
  }
  whenReady(syncLinks);

  window.CrossdeckTheme = {
    get: () => theme,
    set(value) {
      theme = value;
      apply();
      try {
        localStorage.setItem(key, value);
      } catch (e) {
        // Storage unavailable: the attribute and the links still apply.
      }
      syncLinks();
    }
  };

  window.addEventListener('storage', e => {
    if (e.key !== key && e.key !== null) return;
    try {
      theme = localStorage.getItem(key);
    } catch (err) {
      theme = null;
    }
    if (theme !== 'light' && theme !== 'dark') theme = null;
    apply();
    syncLinks();
  });
})();
