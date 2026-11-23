// Applies the persisted UI theme before first paint, and keeps it in sync.
// "dark" is the default when nothing is stored.
//
// Browsers that share storage between pages (an http host, or Chromium's
// file://) sync live via the `storage` event. Firefox gives every file://
// page a unique origin, so storage cannot cross pages there: the theme and
// the colourblind mode also ride on the ?theme= / ?cb= URL parameters of
// in-prototype links, and CrossdeckTheme.set / CrossdeckCB.set keep those
// links up to date.
(function () {
  const key = 'crossdeck-theme';
  const themes = ['dark', 'light', 'sepia', 'dusk'];

  // Unknown values (hand-edited URL, stale storage) fall back to the default.
  function valid(t) {
    return themes.indexOf(t) >= 0 ? t : null;
  }

  const urlTheme = valid(new URLSearchParams(location.search).get('theme'));
  let theme;
  if (urlTheme) {
    theme = urlTheme;
    try {
      localStorage.setItem(key, theme);
    } catch (e) {
      // Storage unavailable (private browsing): the URL parameter still applied.
    }
  } else {
    try {
      theme = valid(localStorage.getItem(key));
    } catch (e) {
      // Storage unavailable (private browsing, unique-origin file://): use the default.
      theme = null;
    }
  }

  function apply() {
    if (theme && theme !== 'dark') {
      document.documentElement.setAttribute('data-theme', theme);
    } else {
      document.documentElement.removeAttribute('data-theme');
    }
  }

  // ---- Colourblind modes ----------------------------------------------------
  // Repalette modes for the known colour-vision deficiencies:
  // design-system.css remaps the semantic role tokens (accent/ok/warn/info)
  // to hues that stay distinguishable under each deficiency, layered on top
  // of the active theme. Independent of the theme: its own storage key, URL
  // parameter and API.
  const cbKey = 'crossdeck-cb-mode';
  const cbModes = ['deuteranopia', 'protanopia', 'tritanopia', 'achromatopsia'];

  function validCb(m) {
    return cbModes.indexOf(m) >= 0 ? m : null;
  }

  const urlCb = validCb(new URLSearchParams(location.search).get('cb'));
  let cbMode;
  if (urlCb) {
    cbMode = urlCb;
    try {
      localStorage.setItem(cbKey, cbMode);
    } catch (e) {
      // Storage unavailable (private browsing): the URL parameter still applied.
    }
  } else {
    try {
      cbMode = validCb(localStorage.getItem(cbKey));
    } catch (e) {
      // Storage unavailable (private browsing, unique-origin file://): use the default.
      cbMode = null;
    }
  }

  function applyCb() {
    if (cbMode) {
      document.documentElement.setAttribute('data-cb', cbMode);
    } else {
      document.documentElement.removeAttribute('data-cb');
    }
  }
  applyCb();

  // The theme rides on in-prototype links so pages that cannot share storage
  // (Firefox file://) still pick it up when navigated to.
  function syncLinks() {
    document.querySelectorAll('a[href]').forEach(a => {
      const href = a.getAttribute('href');
      if (!href || !/\.html($|\?)/.test(href) || /^https?:/i.test(href)) return;
      const [path, query] = href.split('?');
      const params = new URLSearchParams(query || '');
      if (theme && theme !== 'dark') {
        params.set('theme', theme);
      } else {
        params.delete('theme');
      }
      if (cbMode) {
        params.set('cb', cbMode);
      } else {
        params.delete('cb');
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
      if (!valid(value)) return;
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

  window.CrossdeckCB = {
    get: () => cbMode,
    set(value) {
      const m = value === '' ? null : validCb(value);
      if (value !== '' && !m) return;
      cbMode = m;
      applyCb();
      try {
        localStorage.setItem(cbKey, cbMode || '');
      } catch (e) {
        // Storage unavailable: the attribute and the links still apply.
      }
      syncLinks();
    },
  };

  window.addEventListener('storage', e => {
    if (e.key !== key && e.key !== cbKey && e.key !== null) return;
    if (e.key === key || e.key === null) {
      try {
        theme = valid(localStorage.getItem(key));
      } catch (err) {
        theme = null;
      }
      apply();
    }
    if (e.key === cbKey || e.key === null) {
      try {
        cbMode = validCb(localStorage.getItem(cbKey));
      } catch (err) {
        cbMode = null;
      }
      applyCb();
    }
    syncLinks();
  });
})();
