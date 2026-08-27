/* =========================================================
   FurShield Motion System (js/animations.js)
   Central, reusable animation + interaction layer shared by
   every page. Nothing here duplicates existing functionality
   in main.js (cart, forms, filters, hero slider) — it only
   layers motion on top of it and reacts to the classes that
   main.js already toggles (e.g. .hero-slide.active, .open).
   ========================================================= */
(function () {
  'use strict';

  var $ = function (s, root) { return (root || document).querySelector(s); };
  var $$ = function (s, root) { return Array.prototype.slice.call((root || document).querySelectorAll(s)); };

  var reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  var finePointer = window.matchMedia('(hover: hover) and (pointer: fine)').matches;
  var isDesktop = window.innerWidth > 900;

  // Run a setup step in isolation: if one motion feature throws (a selector
  // edge case, a missing element on a given page, etc.) it must never take
  // the rest of the page's animations — or worse, the page's visibility —
  // down with it. Every setup function below is wrapped with this.
  function safe(fn) {
    try { fn(); } catch (err) {
      if (window.console && console.warn) console.warn('[FurShield motion]', fn.name || 'setup step', 'failed:', err);
    }
  }

  // Absolute failsafe: whatever else happens above, nothing that carries a
  // `.reveal`/`.reveal-clip`/`.reveal-card` class (which are all opacity:0 or
  // clip-path:inset(...) by default, waiting for JS to add `.revealed`)
  // may stay invisible forever. If for any reason the scroll-reveal observer
  // never reaches an element (JS error, unusual page structure, extension
  // interference, etc.), force it visible after a short delay so content —
  // including the hero imagery — is never permanently hidden.
  function forceRevealFailsafe() {
    setTimeout(function () {
      $$('.reveal:not(.revealed), .reveal-clip:not(.revealed), .reveal-clip-left:not(.revealed), .reveal-card:not(.revealed), .reveal-scale:not(.revealed), .fs-step, .stat, .number-item').forEach(function (el) {
        el.classList.add('revealed');
      });
    }, 1800);
  }

  document.addEventListener('DOMContentLoaded', function () {
    document.documentElement.classList.add('fs-js');

    safe(annotateRevealTargets);
    safe(splitHeadingWords);
    safe(setupHeaderScroll);
    forceRevealFailsafe();

    if (reducedMotion) {
      // Show everything immediately; skip decorative-only motion.
      $$('.reveal, .fs-step, .stat, .number-item').forEach(function (el) {
        el.classList.add('revealed');
      });
      safe(function () { setupCounters(true); });
      safe(setupScrollToTop);
      return;
    }

    safe(setupScrollReveal);
    safe(function () { setupCounters(false); });
    safe(setupButtonMagnetism);
    safe(setupHeartPop);
    safe(setupParallax);
    safe(setupScrollToTop);
  });

  /* ---------------------------------------------------------
     0b. Sticky header polish — adds `.is-scrolled` once the
     page scrolls past a small threshold (shrinks nav height,
     switches to a more solid background + shadow), and removes
     it back near the top. Works on every page since all of them
     share the same `.header` element.
     --------------------------------------------------------- */
  function setupHeaderScroll() {
    var header = $('.header');
    if (!header) return;
    var toggle = function () {
      var scrolled = (window.scrollY || window.pageYOffset) > 30;
      header.classList.toggle('is-scrolled', scrolled);
    };
    window.addEventListener('scroll', toggle, { passive: true });
    toggle();

    // Verified fallback: some hosting/embedding contexts (older WebKit,
    // an ancestor CSS we don't control, a browser without `sticky`
    // support) can silently prevent `position:sticky` from sticking.
    // Instead of assuming the CSS fix works, actually check the header's
    // position after scrolling and switch to a robust `position:fixed`
    // (with body padding to compensate for the removed space) if it
    // isn't behaving like a sticky element should.
    var verifyStickyOnce = function () {
      if (document.documentElement.classList.contains('fs-header-fixed')) return;
      if ((window.scrollY || window.pageYOffset) < 80) return;
      var rect = header.getBoundingClientRect();
      if (rect.top > 1 || rect.top < -1) {
        document.documentElement.classList.add('fs-header-fixed');
        // Fixed header must sit below the topbar (if present) rather than
        // covering it, and the page needs the combined height compensated
        // so content doesn't jump/hide underneath.
        var topbar = $('.topbar');
        var topbarH = topbar ? topbar.getBoundingClientRect().height : 0;
        header.style.top = topbarH + 'px';
        document.body.style.paddingTop = (topbarH + header.offsetHeight) + 'px';
      }
      window.removeEventListener('scroll', verifyStickyOnce);
    };
    window.addEventListener('scroll', verifyStickyOnce, { passive: true });
  }
  // Note: full-card click-through for `.fs-card-link` cards is handled
  // entirely in CSS via `.stretched-link` (see animations.css) — the
  // anchor's own ::after is stretched over the card, so native click,
  // middle-click/new-tab and keyboard (Tab + Enter) all keep working
  // without any extra JS, and without double-firing navigation.

  /* ---------------------------------------------------------
     1. Auto-annotate elements that should participate in the
     scroll-reveal system, without requiring every page's HTML
     to be hand-edited. Elements that already carry a `.reveal`
     class (existing markup on several pages) are left as-is;
     this only adds the class to *additional* known components
     so secondary pages (cart, booking, forms, detail pages...)
     get the same treatment as the homepage.
     --------------------------------------------------------- */
  function annotateRevealTargets() {
    // Simple single-fade blocks
    var singleSelectors = [
      '.page-hero .inner > div', '.page-hero .inner > p', '.page-hero .inner > a',
      '.page-hero .container > .breadcrumb', '.page-hero .container > h1',
      '.section-head', '.fs-why-panel', '.dark-panel', '.testimonial', '.cta',
      '.elite-cta', '.newsletter', '.location-copy', '.form-card', '.info-card',
      '.cart-table', '.booking-success', '.auth', '.gallery-main', '.thumbs',
      '.faq-item', '.not-found > strong', '.not-found > h1', '.not-found > p', '.not-found > a',
      '.dash-side'
    ];
    singleSelectors.forEach(function (sel) {
      $$(sel).forEach(function (el) {
        if (!el.classList.contains('reveal')) el.classList.add('reveal');
      });
    });

    // Split-panel sections (about/product/service layouts): fade each half in from its side
    $$('.split').forEach(function (split) {
      var kids = $$(':scope > div', split);
      kids.forEach(function (el, i) {
        if (el.classList.contains('reveal')) return;
        el.classList.add('reveal', i === 0 ? 'reveal-left' : 'reveal-right');
      });
    });

    // Stagger groups: grid/list containers whose direct children should
    // enter one after another rather than all at once.
    var staggerContainers = [
      '.fs-services-grid', '.fs-pet-grid', '.fs-why-grid', '.service-grid',
      '.pet-grid', '.product-grid', '.blog-grid', '.quick-grid', '.feature-grid',
      '.premium-grid', '.numbers-grid', '.stats', '.locations', '.journal-grid',
      '.pet-carousel', '.shop-grid', '.benefits-grid', '.dash-content', '.check-list', '.faq'
    ];
    staggerContainers.forEach(function (sel) {
      $$(sel).forEach(function (container) {
        var kids = $$(':scope > *', container);
        kids.forEach(function (el, i) {
          el.style.setProperty('--i', i);
          el.classList.add('reveal-stagger');
          if (!el.classList.contains('reveal')) el.classList.add('reveal');
          // give card-like children a slightly richer entrance
          if (/(card|item|-grid > |stat\b)/.test(el.className) || container.matches('.stats,.numbers-grid,.locations')) {
            el.classList.add('reveal-card');
          }
        });
      });
    });

    // Unique-shape / hero-style imagery: clip-path reveal
    var clipSelectors = [
      '.fs-intro-media .fs-img-main', '.collage .main-img', '.story-main img',
      '#petImage', '.cta-media img'
    ];
    clipSelectors.forEach(function (sel, groupIndex) {
      $$(sel).forEach(function (el) {
        el.classList.add('reveal-clip');
      });
    });
    var clipLeftSelectors = ['.fs-intro-media .fs-img-small', '.collage .small-img', '.story-small img'];
    clipLeftSelectors.forEach(function (sel) {
      $$(sel).forEach(function (el) { el.classList.add('reveal-clip-left'); });
    });
    $$('.fs-badge-card, .experience, .fs-badge-card, .story-stamp').forEach(function (el) {
      el.classList.add('reveal', 'reveal-scale');
    });

    // Steps / process
    $$('.fs-steps').forEach(function (container) {
      $$(':scope > .fs-step', container).forEach(function (el, i) {
        el.style.setProperty('--i', i);
      });
    });
  }

  /* ---------------------------------------------------------
     2. Word-by-word heading reveal. Applies to the rotating
     hero headline and every inner page's h1, preserving any
     inline markup (e.g. <em>, <span>, <br>) as a single "word".
     --------------------------------------------------------- */
  function splitHeadingWords() {
    $$('.hero-content h1, .page-hero h1').forEach(function (h1) {
      if (h1.dataset.split) return;
      h1.dataset.split = 'true';
      var nodes = Array.prototype.slice.call(h1.childNodes);
      h1.innerHTML = '';
      var i = 0;
      nodes.forEach(function (node) {
        if (node.nodeType === 3) {
          var parts = node.textContent.split(/(\s+)/);
          parts.forEach(function (chunk) {
            if (chunk === '') return;
            if (/^\s+$/.test(chunk)) {
              h1.appendChild(document.createTextNode(chunk));
              return;
            }
            var span = document.createElement('span');
            span.className = 'word-reveal';
            span.style.setProperty('--i', i++);
            span.textContent = chunk;
            h1.appendChild(span);
          });
        } else {
          var wrap = document.createElement('span');
          wrap.className = 'word-reveal';
          wrap.style.setProperty('--i', i++);
          wrap.appendChild(node.cloneNode(true));
          h1.appendChild(wrap);
        }
      });
    });
  }

  /* ---------------------------------------------------------
     3. Scroll reveal observer (single pass, unobserve on entry)
     --------------------------------------------------------- */
  function setupScrollReveal() {
    if (!('IntersectionObserver' in window)) {
      $$('.reveal, .fs-step, .stat, .number-item').forEach(function (el) { el.classList.add('revealed'); });
      return;
    }
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (entry.isIntersecting) {
          entry.target.classList.add('revealed');
          io.unobserve(entry.target);
        }
      });
    }, { threshold: .12, rootMargin: '0px 0px -6% 0px' });

    $$('.reveal, .fs-step, .stat, .number-item').forEach(function (el) { io.observe(el); });
  }

  /* ---------------------------------------------------------
     4. Animated counters (data-count) — replaces the simpler
     version previously in main.js with eased, non-linear motion.
     --------------------------------------------------------- */
  function setupCounters(instant) {
    var counters = $$('[data-count]');
    if (!counters.length) return;

    var animate = function (el) {
      var target = Number(el.dataset.count) || 0;
      // Suffix/prefix and decimal precision are explicit per-element so a
      // count like "4" (cities) doesn't lose its "+", and values like
      // "4.9" (rating) keep their decimal instead of rounding to a whole
      // number. Falls back to the site's usual "+" so existing markup
      // without a data-suffix still behaves the same as before.
      var suffix = el.dataset.suffix !== undefined ? el.dataset.suffix : '+';
      var prefix = el.dataset.prefix || '';
      var decimals = Number(el.dataset.decimals) || 0;
      var format = function (n) {
        return prefix + n.toFixed(decimals) + suffix;
      };
      if (instant) { el.textContent = format(target); return; }
      var start = null;
      var duration = 1400;
      var ease = function (t) { return 1 - Math.pow(1 - t, 3); }; // easeOutCubic
      function step(ts) {
        if (start === null) start = ts;
        var progress = Math.min(1, (ts - start) / duration);
        var value = target * ease(progress);
        el.textContent = format(value);
        if (progress < 1) requestAnimationFrame(step);
      }
      requestAnimationFrame(step);
    };

    if (instant || !('IntersectionObserver' in window)) {
      counters.forEach(animate);
      return;
    }
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (entry.isIntersecting) {
          animate(entry.target);
          io.unobserve(entry.target);
        }
      });
    }, { threshold: .6 });
    counters.forEach(function (el) { io.observe(el); });
  }

  /* ---------------------------------------------------------
     5. Subtle magnetic pull for primary buttons (desktop, fine
     pointer only — never on touch devices).
     --------------------------------------------------------- */
  function setupButtonMagnetism() {
    if (!isDesktop || !finePointer) return;
    var targets = $$('.btn-primary, .btn-lg, .icon-btn');
    targets.forEach(function (btn) {
      btn.classList.add('is-magnetic');
      btn.addEventListener('mousemove', function (e) {
        var rect = btn.getBoundingClientRect();
        var x = e.clientX - rect.left - rect.width / 2;
        var y = e.clientY - rect.top - rect.height / 2;
        btn.style.transform = 'translate(' + (x * .18).toFixed(1) + 'px,' + (y * .28).toFixed(1) + 'px)';
      });
      btn.addEventListener('mouseleave', function () { btn.style.transform = ''; });
    });
  }

  /* ---------------------------------------------------------
     6. Heart / wishlist pop feedback (adds a class main.js's
     existing click handler already toggles fa-solid/fa-regular
     for — this just layers a short pop animation on top).
     --------------------------------------------------------- */
  function setupHeartPop() {
    $$('.heart:not([data-remove]), .pet-heart, .product-heart').forEach(function (btn) {
      btn.addEventListener('click', function () {
        btn.classList.remove('is-pop');
        // force reflow so the animation can restart on repeated clicks
        void btn.offsetWidth;
        btn.classList.add('is-pop');
      });
    });
  }

  /* ---------------------------------------------------------
     7. Subtle parallax for decorative shapes only (desktop).
     --------------------------------------------------------- */
  function setupParallax() {
    if (!isDesktop) return;
    var els = $$('.fs-corner-blob, .fs-deco');
    if (!els.length) return;
    var ticking = false;
    function update() {
      var scrollY = window.scrollY || window.pageYOffset;
      els.forEach(function (el, i) {
        var amount = (i % 3 === 0) ? 0.04 : (i % 3 === 1 ? -0.03 : 0.02);
        el.style.transform = 'translateY(' + (scrollY * amount).toFixed(1) + 'px)';
      });
      ticking = false;
    }
    window.addEventListener('scroll', function () {
      if (!ticking) { requestAnimationFrame(update); ticking = true; }
    }, { passive: true });
  }

  /* ---------------------------------------------------------
     8. Scroll-to-top control (paw-themed, matches the brand's
     existing icon language). Injected once per page. Stays
     fixed while scrolling, but lifts clear of the footer once
     the footer scrolls into view so it never sits on top of
     footer links/content — it stays visually anchored just
     above the footer instead.
     --------------------------------------------------------- */
  function setupScrollToTop() {
    if ($('.fs-scrolltop')) return;
    var btn = document.createElement('button');
    btn.className = 'fs-scrolltop';
    btn.type = 'button';
    btn.setAttribute('aria-label', 'Back to top');
    btn.innerHTML = '<i class="fa-solid fa-paw" aria-hidden="true"></i>';
    document.body.appendChild(btn);

    var toggle = function () {
      var show = (window.scrollY || window.pageYOffset) > 480;
      btn.classList.toggle('is-visible', show);
    };
    window.addEventListener('scroll', toggle, { passive: true });
    toggle();

    // Lift the button clear of the footer once it comes into view.
    var footer = $('.footer');
    if (footer && 'IntersectionObserver' in window) {
      var io = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
          var visiblePx = entry.isIntersecting ? entry.intersectionRect.height : 0;
          btn.style.setProperty('--fs-footer-lift', (visiblePx + 22) + 'px');
          btn.classList.toggle('is-above-footer', entry.isIntersecting);
        });
      }, { threshold: [0, 0.05, 0.15, 0.3, 0.6, 1] });
      io.observe(footer);
    }

    btn.addEventListener('click', function () {
      window.scrollTo({ top: 0, behavior: reducedMotion ? 'auto' : 'smooth' });
    });
  }
})();
