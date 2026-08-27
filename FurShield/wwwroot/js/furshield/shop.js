document.addEventListener('DOMContentLoaded', () => {
  const grid = document.querySelector('.product-grid');
  if (!grid) return;

  const items = [...grid.querySelectorAll('.filter-item')];
  const searchInput = document.getElementById('shopSearch');
  const categoryChecks = [...document.querySelectorAll('.sidebar input[type="checkbox"]')];
  const priceRange = document.querySelector('.sidebar input[type="range"]');
  const sortSelect = document.querySelector('.sort-row select');

  const getPrice = (item) => {
    const priceEl = item.querySelector('.price');
    return priceEl ? parseFloat(priceEl.textContent.replace(/[^0-9.]/g, '')) || 0 : 0;
  };
  const getName = (item) => (item.querySelector('h3')?.textContent || '').toLowerCase();

  function applyFilters() {
    const query = (searchInput?.value || '').trim().toLowerCase();
    const activeCats = categoryChecks.filter(c => c.checked).map(c => c.parentElement.textContent.trim());
    const maxPrice = priceRange ? parseFloat(priceRange.value) : null;
    const rangeIsDefault = !priceRange || maxPrice === parseFloat(priceRange.max);

    items.forEach(item => {
      const name = getName(item);
      const category = item.dataset.category || '';
      const price = getPrice(item);

      const matchesSearch = !query || name.includes(query);
      const matchesCategory = activeCats.length === 0 || activeCats.includes(category);
      const matchesPrice = rangeIsDefault || price <= maxPrice;

      item.style.display = (matchesSearch && matchesCategory && matchesPrice) ? '' : 'none';
    });
  }

  function applySort() {
    if (!sortSelect) return;
    const mode = sortSelect.value;
    const sorted = [...items];
    if (mode.includes('Low to High')) {
      sorted.sort((a, b) => getPrice(a) - getPrice(b));
    } else if (mode.includes('High to Low')) {
      sorted.sort((a, b) => getPrice(b) - getPrice(a));
    } else {
      return; // "Featured" = original order, no reorder needed
    }
    sorted.forEach(item => grid.appendChild(item));
  }

  searchInput?.addEventListener('input', applyFilters);
  categoryChecks.forEach(c => c.addEventListener('change', applyFilters));
  priceRange?.addEventListener('input', applyFilters);
  sortSelect?.addEventListener('change', applySort);
});
