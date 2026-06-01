/**
 * MentoraX Client-Side Validations
 * Real-time validation feedback matching server-side rules.
 * No external dependencies — plain ES5-compatible JS.
 */
(function () {
    'use strict';

    var EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    var PHONE_REGEX = /^\+?[\d\s\-(]{7,20}$/;

    // ── Generic helpers ──────────────────────────────────────────────
    function markValid(el) {
        el.classList.remove('is-invalid');
        el.classList.add('is-valid');
        var fb = el.nextElementSibling;
        if (fb && fb.classList.contains('invalid-feedback')) fb.textContent = '';
    }

    function markInvalid(el, msg) {
        el.classList.remove('is-valid');
        el.classList.add('is-invalid');
        var fb = el.nextElementSibling;
        if (!fb || !fb.classList.contains('invalid-feedback')) {
            fb = document.createElement('div');
            fb.className = 'invalid-feedback';
            el.parentNode.insertBefore(fb, el.nextSibling);
        }
        fb.textContent = msg;
    }

    function clearValidation(el) {
        el.classList.remove('is-valid', 'is-invalid');
    }

    function attachValidation(selector, validator) {
        document.querySelectorAll(selector).forEach(function (el) {
            el.addEventListener('blur',  function () { validator(el); });
            el.addEventListener('input', function () {
                if (el.classList.contains('is-invalid')) validator(el);
            });
        });
    }

    // ── Field validators ─────────────────────────────────────────────
    function validateEmail(el) {
        var v = el.value.trim();
        if (!v)                          return markInvalid(el, 'Email is required.');
        if (!EMAIL_REGEX.test(v))        return markInvalid(el, 'Please enter a valid email address.');
        markValid(el);
    }

    function validatePassword(el) {
        var v = el.value;
        if (!v)                          return markInvalid(el, 'Password is required.');
        if (v.length < 6)               return markInvalid(el, 'Password must be at least 6 characters.');
        if (v.length > 50)              return markInvalid(el, 'Password cannot exceed 50 characters.');
        if (!/[a-zA-Z]/.test(v))        return markInvalid(el, 'Password must contain at least one letter.');
        if (!/[0-9]/.test(v))           return markInvalid(el, 'Password must contain at least one number.');
        markValid(el);
    }

    function validateFullName(el) {
        var v = el.value.trim();
        if (!v)                          return markInvalid(el, 'Full name is required.');
        if (v.length > 100)             return markInvalid(el, 'Full name cannot exceed 100 characters.');
        markValid(el);
    }

    function validatePhone(el) {
        var v = el.value.trim();
        if (!v) { clearValidation(el); return; }  // optional
        if (!PHONE_REGEX.test(v))        return markInvalid(el, 'Please enter a valid phone number.');
        markValid(el);
    }

    function validateHourlyRate(el) {
        var v = parseFloat(el.value);
        if (isNaN(v))                    return markInvalid(el, 'Hourly rate is required.');
        if (v < 0)                       return markInvalid(el, 'Hourly rate cannot be negative.');
        if (v > 500)                     return markInvalid(el, 'Hourly rate cannot exceed $500.');
        markValid(el);
    }

    function validateExperienceYears(el) {
        var v = parseFloat(el.value);
        if (isNaN(v))                    return markInvalid(el, 'Experience years is required.');
        if (v < 0)                       return markInvalid(el, 'Experience years cannot be negative.');
        if (v > 50)                      return markInvalid(el, 'Experience years cannot exceed 50.');
        markValid(el);
    }

    function validateSpecialization(el) {
        var v = el.value.trim();
        if (!v)                          return markInvalid(el, 'Specialization is required.');
        markValid(el);
    }

    function validateQualification(el) {
        var v = el.value.trim();
        if (!v)                          return markInvalid(el, 'Qualification is required.');
        markValid(el);
    }

    function validateBio(el) {
        var v = el.value.trim();
        if (v.length > 2000)            return markInvalid(el, 'Bio cannot exceed 2000 characters.');
        if (v) markValid(el);
        else clearValidation(el);
    }

    function validateDurationHours(el) {
        var v = parseInt(el.value);
        if (isNaN(v) || v <= 0)         return markInvalid(el, 'Duration must be at least 1 hour.');
        if (v > 4)                      return markInvalid(el, 'Duration cannot exceed 4 hours.');
        markValid(el);
    }

    function validateBookingDate(el) {
        var v = new Date(el.value);
        var today = new Date(); today.setHours(0,0,0,0);
        var max   = new Date(today); max.setDate(max.getDate() + 30);
        if (!el.value)                   return markInvalid(el, 'Booking date is required.');
        if (v < today)                   return markInvalid(el, 'Booking date cannot be in the past.');
        if (v > max)                     return markInvalid(el, 'Booking date must be within the next 30 days.');
        markValid(el);
    }

    function validateRating(el) {
        var v = parseInt(el.value);
        if (isNaN(v) || v < 1 || v > 5) return markInvalid(el, 'Rating must be between 1 and 5.');
        markValid(el);
    }

    function validateComment(el) {
        var v = el.value.trim();
        if (v.length > 1000)            return markInvalid(el, 'Comment cannot exceed 1000 characters.');
        markValid(el);
    }

    function validateMessage(el) {
        var v = el.value;
        if (!v || !v.trim())            return markInvalid(el, 'Message cannot be empty.');
        if (v.length > 5000)            return markInvalid(el, 'Message cannot exceed 5000 characters.');
        markValid(el);
    }

    // ── Character counters ────────────────────────────────────────────
    function attachCharCounter(selector, max) {
        document.querySelectorAll(selector).forEach(function (el) {
            var counter = document.createElement('small');
            counter.className = 'text-muted d-block text-end';
            el.parentNode.appendChild(counter);

            function update() {
                var left = max - el.value.length;
                counter.textContent = left + ' chars remaining';
                counter.classList.toggle('text-danger', left < 0);
            }
            el.addEventListener('input', update);
            update();
        });
    }

    // ── Bootstrap form submit guard ───────────────────────────────────
    function guardForm(formId) {
        var form = document.getElementById(formId);
        if (!form) return;
        form.addEventListener('submit', function (e) {
            var invalids = form.querySelectorAll('.is-invalid');
            if (invalids.length > 0) {
                e.preventDefault();
                invalids[0].focus();
            }
        });
    }

    // ── Boot ─────────────────────────────────────────────────────────
    document.addEventListener('DOMContentLoaded', function () {
        // Registration forms
        attachValidation('[data-val-email]',         validateEmail);
        attachValidation('[data-val-password]',      validatePassword);
        attachValidation('[data-val-fullname]',      validateFullName);
        attachValidation('[data-val-phone]',         validatePhone);

        // Tutor profile form
        attachValidation('[data-val-hourlyrate]',    validateHourlyRate);
        attachValidation('[data-val-expyears]',      validateExperienceYears);
        attachValidation('[data-val-specialization]',validateSpecialization);
        attachValidation('[data-val-qualification]', validateQualification);
        attachValidation('[data-val-bio]',           validateBio);

        // Booking form
        attachValidation('[data-val-duration]',      validateDurationHours);
        attachValidation('[data-val-bookingdate]',   validateBookingDate);

        // Review form
        attachValidation('[data-val-rating]',        validateRating);
        attachValidation('[data-val-comment]',       validateComment);

        // Chat input
        attachValidation('[data-val-message]',       validateMessage);

        // Character counters
        attachCharCounter('[data-val-bio]',          2000);
        attachCharCounter('[data-val-comment]',      1000);

        // Form guards
        guardForm('registerForm');
        guardForm('tutorProfileForm');
        guardForm('bookingForm');
        guardForm('reviewForm');

        console.log('[MentoraX] Client validations active.');
    });

    // Expose for inline usage
    window.MxValidate = {
        email:        validateEmail,
        password:     validatePassword,
        hourlyRate:   validateHourlyRate,
        rating:       validateRating,
        bookingDate:  validateBookingDate
    };

})();
