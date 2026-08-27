document.addEventListener('DOMContentLoaded', () => {
  'use strict';

  const form = document.getElementById('bookingForm');
  const success = document.getElementById('bookingSuccess');
  const summary = document.getElementById('bookingSummary');
  const newBooking = document.getElementById('newBooking');
  if (!form) return;

  const dateInput = document.getElementById('bookingDate');
  const timeInput = document.getElementById('bookingTime');
  const submitBtn = form.querySelector('button[type="submit"]');

  // Prevent selecting a date in the past.
  if (dateInput) {
    const today = new Date();
    const localDate = new Date(today.getTime() - today.getTimezoneOffset() * 60000)
      .toISOString().split('T')[0];
    dateInput.min = localDate;
  }

  const showToast = message => {
    if (window.FurShield?.showToast) window.FurShield.showToast(message);
  };

  form.addEventListener('submit', event => {
    event.preventDefault();

    if (!form.checkValidity()) {
      form.reportValidity();
      return;
    }

    const selectedDate = dateInput?.value || '';
    const selectedTime = timeInput?.value || '';
    if (selectedDate && selectedTime) {
      const selected = new Date(`${selectedDate}T${selectedTime}`);
      if (Number.isNaN(selected.getTime()) || selected.getTime() < Date.now()) {
        showToast('Please choose a future date and time.');
        dateInput?.focus();
        return;
      }
    }

    const data = new FormData(form);
    const booking = {
      name: data.get('') || form.querySelector('input[autocomplete="name"]')?.value.trim() || '',
      email: form.querySelector('input[type="email"]')?.value.trim() || '',
      phone: form.querySelector('input[type="tel"]')?.value.trim() || '',
      pet: form.querySelector('input[placeholder="Charlie"]')?.value.trim() || '',
      petType: form.querySelectorAll('select')[0]?.value || '',
      service: form.querySelectorAll('select')[1]?.value || '',
      date: selectedDate,
      time: selectedTime,
      notes: form.querySelector('textarea')?.value.trim() || '',
      createdAt: new Date().toISOString()
    };

    const bookings = JSON.parse(localStorage.getItem('furshield_bookings') || '[]');
    bookings.push(booking);
    localStorage.setItem('furshield_bookings', JSON.stringify(bookings));

    if (submitBtn) {
      submitBtn.disabled = true;
      submitBtn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Confirming...';
    }

    setTimeout(() => {
      form.hidden = true;
      if (success) {
        success.hidden = false;
        if (summary) {
          const date = new Date(`${booking.date}T${booking.time}`);
          const dateText = date.toLocaleDateString(undefined, {
            weekday: 'long', day: 'numeric', month: 'long', year: 'numeric'
          });
          const timeText = date.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' });
          summary.innerHTML =
            `<div><strong>Pet:</strong> ${escapeHtml(booking.pet)} (${escapeHtml(booking.petType)})</div>` +
            `<div><strong>Service:</strong> ${escapeHtml(booking.service)}</div>` +
            `<div><strong>Date:</strong> ${escapeHtml(dateText)}</div>` +
            `<div><strong>Time:</strong> ${escapeHtml(timeText)}</div>`;
        }
        success.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }
      showToast('Appointment request submitted successfully.');
    }, 500);
  });

  newBooking?.addEventListener('click', () => {
    form.reset();
    if (dateInput) {
      const today = new Date();
      dateInput.min = new Date(today.getTime() - today.getTimezoneOffset() * 60000)
        .toISOString().split('T')[0];
    }
    form.hidden = false;
    if (success) success.hidden = true;
    if (submitBtn) {
      submitBtn.disabled = false;
      submitBtn.innerHTML = '<i class="fa-solid fa-calendar-check"></i> Book Appointment';
    }
    window.scrollTo({ top: form.getBoundingClientRect().top + window.scrollY - 110, behavior: 'smooth' });
  });

  function escapeHtml(value) {
    return String(value).replace(/[&<>"']/g, char => ({
      '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;'
    }[char]));
  }
});
