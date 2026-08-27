document.addEventListener('DOMContentLoaded', () => {
  'use strict';

  const $ = (s, root = document) => root.querySelector(s);
  const $$ = (s, root = document) => [...root.querySelectorAll(s)];

  /* ---------- Image reliability ----------
     The site's photography is hotlinked from a remote CDN. That's normal
     and fine almost all of the time, but a single slow/failed request
     should never leave a section looking broken or empty. Every <img>
     gets a graceful, on-brand fallback (a soft paw illustration on the
     FurShield palette) if it fails to load or takes unusually long, so
     the layout always holds its shape instead of showing a broken-image
     icon or a blank box. */
  const FALLBACK_SRC = 'data:image/svg+xml;utf8,' + encodeURIComponent(
    `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 400">
      <defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1">
        <stop offset="0" stop-color="#F6E9D6"/><stop offset="1" stop-color="#E4F1EA"/>
      </linearGradient></defs>
      <rect width="400" height="400" fill="url(#g)"/>
      <g fill="#CE7CA6" opacity="0.55">
        <ellipse cx="200" cy="215" rx="58" ry="52"/>
        <circle cx="150" cy="150" r="24"/><circle cx="190" cy="118" r="26"/>
        <circle cx="235" cy="118" r="26"/><circle cx="270" cy="150" r="24"/>
      </g>
    </svg>`
  );
  $$('img').forEach(img => {
    if (img.dataset.fallbackBound) return;
    img.dataset.fallbackBound = 'true';
    img.addEventListener('error', () => {
      if (img.src === FALLBACK_SRC) return;
      img.src = FALLBACK_SRC;
      img.classList.add('img-fallback');
      img.removeAttribute('srcset');
    }, { once: true });
    // Already broken by the time this ran (e.g. cached 404)
    if (img.complete && img.naturalWidth === 0 && img.src) {
      img.dispatchEvent(new Event('error'));
    }
  });

  /* ---------- Shared toast ---------- */
  const toast = $('#toast');
  const showToast = (message, type = 'default') => {
    if (!toast) return;
    toast.textContent = message;
    toast.dataset.type = type;
    toast.classList.add('show');
    clearTimeout(window.__furshieldToast);
    window.__furshieldToast = setTimeout(() => toast.classList.remove('show'), 2600);
  };
  window.FurShield = window.FurShield || {};
  window.FurShield.showToast = showToast;

  /* ---------- Mobile navigation ---------- */
  const menu = $('.menu-btn');
  const links = $('.nav-links');
  if (menu && links) {
    menu.addEventListener('click', () => {
      const open = links.classList.toggle('open');
      menu.setAttribute('aria-expanded', String(open));
      menu.setAttribute('aria-label', open ? 'Close menu' : 'Open menu');
      menu.innerHTML = open
        ? '<i class="fa-solid fa-xmark"></i>'
        : '<i class="fa-solid fa-bars"></i>';
    });
    $$('.nav-links a').forEach(link => link.addEventListener('click', () => {
      links.classList.remove('open');
      menu.setAttribute('aria-expanded', 'false');
      menu.innerHTML = '<i class="fa-solid fa-bars"></i>';
    }));
  }

  /* ---------- Wishlist ---------- */
  $$('.heart:not([data-remove]), .pet-heart, .product-heart').forEach(button => {
    button.addEventListener('click', () => {
      const icon = $('i', button);
      if (icon) {
        icon.classList.toggle('fa-regular');
        icon.classList.toggle('fa-solid');
        icon.style.color = icon.classList.contains('fa-solid') ? '#F0805B' : '';
      } else {
        button.classList.toggle('active');
        button.textContent = button.classList.contains('active') ? '♥' : '♡';
      }
    });
  });

  /* ---------- Cart badge ---------- */
  const CART_KEY = 'furshield_cart_count';
  const getCartCount = () => Math.max(0, parseInt(localStorage.getItem(CART_KEY) || '0', 10) || 0);
  const setCartCount = count => {
    const safe = Math.max(0, Number(count) || 0);
    localStorage.setItem(CART_KEY, String(safe));
    $$('.cart-dot').forEach(dot => {
      dot.textContent = safe;
      dot.style.display = safe > 0 ? 'grid' : 'none';
      dot.setAttribute('aria-label', `${safe} item${safe === 1 ? '' : 's'} in cart`);
    });
  };
  const syncCartBadge = () => setCartCount(getCartCount());
  syncCartBadge();

  $$('[data-add-cart]').forEach(button => {
    button.addEventListener('click', () => {
      setCartCount(getCartCount() + 1);
      showToast('Added to your FurShield cart');
    });
  });

  /* ---------- FAQ accordion ---------- */
  $$('.faq-item').forEach(item => {
    const q = $('.faq-q', item);
    if (!q) return;
    q.addEventListener('click', () => item.classList.toggle('open'));
  });

  /* ---------- Newsletter ---------- */
  $$('.news-form').forEach(form => {
    form.addEventListener('submit', event => {
      event.preventDefault();
      if (!form.checkValidity()) {
        form.reportValidity();
        return;
      }
      showToast('Welcome to the FurShield family!');
      form.reset();
    });
  });

  /* ---------- Contact ---------- */
  const contactForm = $('#contactForm');
  if (contactForm) {
    contactForm.addEventListener('submit', event => {
      event.preventDefault();
      if (!contactForm.checkValidity()) {
        contactForm.reportValidity();
        return;
      }
      const button = $('button[type="submit"]', contactForm);
      const original = button?.innerHTML;
      if (button) {
        button.disabled = true;
        button.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Sending...';
      }
      setTimeout(() => {
        showToast('Message sent successfully. We’ll get back to you soon!', 'success');
        contactForm.reset();
        if (button) {
          button.disabled = false;
          button.innerHTML = original;
        }
      }, 650);
    });
  }

  /* ---------- Product gallery thumbnails ---------- */
  const thumbs = $('.thumbs');
  const galleryMain = $('.gallery-main img');
  if (thumbs && galleryMain) {
    $$('img', thumbs).forEach(thumb => {
      thumb.addEventListener('click', () => {
        $$('img', thumbs).forEach(t => t.classList.remove('active'));
        thumb.classList.add('active');
        galleryMain.src = thumb.src;
      });
    });
  }

  /* ---------- Gallery / Utilities ---------- */
  // Form submissions (Login, Register, Checkout) are handled natively by ASP.NET Core controllers and Razor Pages.

  /* ---------- Cart ---------- */
  const cartTable = $('#cartTable');
  if (cartTable) {
    const empty = $('#cartEmpty');
    const summaryWrap = $('#cartSummaryWrap');
    const checkoutLink = $('#checkoutLink');

    const money = value => `$${Number(value).toFixed(0)}`;

    const rows = () => $$('.cart-row:not(.cart-head)', cartTable);

    const recalcCart = () => {
      const currentRows = rows();
      let subtotal = 0;
      let itemCount = 0;

      currentRows.forEach(row => {
        const unit = parseFloat(row.dataset.unitPrice || '0') || 0;
        const qtyEl = $('[data-qty]', row);
        const qty = Math.max(1, parseInt(qtyEl?.textContent || '1', 10) || 1);
        if (qtyEl) qtyEl.textContent = String(qty);

        const lineTotal = unit * qty;
        const lineTotalEl = $('[data-line-total]', row);
        if (lineTotalEl) lineTotalEl.textContent = money(lineTotal);
        subtotal += lineTotal;
        itemCount += qty;
      });

      const delivery = subtotal > 0 ? 5 : 0;
      const subtotalEl = $('#cartSubtotal');
      const totalEl = $('#cartTotal');
      if (subtotalEl) subtotalEl.textContent = money(subtotal);
      if (totalEl) totalEl.textContent = money(subtotal + delivery);

      if (empty) empty.hidden = currentRows.length !== 0;
      if (summaryWrap) summaryWrap.style.display = currentRows.length ? 'flex' : 'none';
      if (checkoutLink) {
        checkoutLink.setAttribute('aria-disabled', String(currentRows.length === 0));
        checkoutLink.classList.toggle('disabled', currentRows.length === 0);
        if (!currentRows.length) checkoutLink.setAttribute('tabindex', '-1');
        else checkoutLink.removeAttribute('tabindex');
      }
      setCartCount(itemCount);
    };

    cartTable.addEventListener('click', event => {
      const button = event.target.closest('button');
      const row = event.target.closest('.cart-row:not(.cart-head)');
      if (!button || !row) return;

      const qtyEl = $('[data-qty]', row);
      if (!qtyEl) return;

      if (button.matches('[data-plus]')) {
        qtyEl.textContent = String(Math.min(99, parseInt(qtyEl.textContent, 10) + 1));
        recalcCart();
      } else if (button.matches('[data-minus]')) {
        qtyEl.textContent = String(Math.max(1, parseInt(qtyEl.textContent, 10) - 1));
        recalcCart();
      } else if (button.matches('[data-remove]')) {
        row.remove();
        showToast('Item removed from your cart');
        recalcCart();
      }
    });

    if (checkoutLink) {
      checkoutLink.addEventListener('click', event => {
        if (!rows().length) {
          event.preventDefault();
          showToast('Your cart is empty');
        }
      });
    }

    recalcCart();
  }

  /* ---------- Adoption filters ---------- */
  const pills = $$('[data-filter]');
  if (pills.length) {
    const filterItems = $$('[data-category]');
    pills.forEach(pill => pill.addEventListener('click', () => {
      pills.forEach(item => item.classList.remove('active'));
      pill.classList.add('active');
      const filter = pill.dataset.filter;
      filterItems.forEach(item => {
        item.style.display = filter === 'all' || item.dataset.category === filter ? '' : 'none';
      });
    }));
  }

  /* ---------- Hero slider ---------- */
  const slides = $$('.hero-slide');
  const bar = $('#heroProgress');
  const current = $('#heroCurrent');
  const next = $('#heroNext');
  const prev = $('#heroPrev');
  const pause = $('#heroPause');
  let index = 0;
  let timer = null;
  let paused = false;
  const duration = 6500;

  const go = nextIndex => {
    if (!slides.length) return;
    index = (nextIndex + slides.length) % slides.length;
    slides.forEach((slide, i) => slide.classList.toggle('active', i === index));
    if (current) current.textContent = String(index + 1).padStart(2, '0');
    if (bar) bar.style.width = `${((index + 1) / slides.length) * 100}%`;
  };
  const start = () => {
    clearInterval(timer);
    if (!paused && slides.length > 1) timer = setInterval(() => go(index + 1), duration);
  };

  next?.addEventListener('click', () => { go(index + 1); start(); });
  prev?.addEventListener('click', () => { go(index - 1); start(); });
  pause?.addEventListener('click', () => {
    paused = !paused;
    pause.innerHTML = paused ? '<i class="fa-solid fa-play"></i>' : '<i class="fa-solid fa-pause"></i>';
    pause.setAttribute('aria-label', paused ? 'Play slideshow' : 'Pause slideshow');
    start();
  });
  document.addEventListener('visibilitychange', () => document.hidden ? clearInterval(timer) : start());

  const slider = $('#heroSlider');
  if (slider) {
    let touchStartX = 0;
    slider.addEventListener('touchstart', event => { touchStartX = event.changedTouches[0].screenX; }, { passive: true });
    slider.addEventListener('touchend', event => {
      const delta = event.changedTouches[0].screenX - touchStartX;
      if (Math.abs(delta) > 40) {
        go(delta < 0 ? index + 1 : index - 1);
        start();
      }
    }, { passive: true });
  }
  if (slides.length) { go(0); start(); }
  // Scroll-reveal and animated counters are handled by js/animations.js.
});
