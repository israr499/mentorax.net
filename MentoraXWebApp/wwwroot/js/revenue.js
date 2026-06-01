/**
 * MentoraX Revenue JS
 * Handles data fetching, Chart.js rendering and CSV download
 * for both Tutor Dashboard and Admin Revenue page.
 */
(function () {
    'use strict';

    // ── Tutor Revenue Dashboard ──────────────────────────────────────
    function initTutorRevenue() {
        var section = document.getElementById('revenueSection');
        if (!section) return;

        fetch('/Tutor/GetRevenue')
            .then(function (res) { return res.json(); })
            .then(function (result) {
                if (!result.success) {
                    section.innerHTML = '<p class="text-danger">Could not load revenue data.</p>';
                    return;
                }
                var d = result.data;

                // Fill stat cards
                setEl('rev-earned',   '$' + fmt(d.totalEarned));
                setEl('rev-gross',    '$' + fmt(d.grossRevenue));
                setEl('rev-fee',      '$' + fmt(d.platformFee));
                setEl('rev-bookings', d.totalBookings);
                setEl('rev-hours',    d.totalHours + ' hrs');

                // Chart
                renderTutorChart(d.monthlyBreakdown);
            })
            .catch(function () {
                section.innerHTML = '<p class="text-danger text-center py-3"><i class="fas fa-exclamation-circle me-2"></i>Revenue data unavailable.</p>';
            });
    }

    function renderTutorChart(monthly) {
        var canvas = document.getElementById('revenueChart');
        if (!canvas || !monthly || !monthly.length) return;

        var labels  = monthly.map(function (m) { return m.month; });
        var amounts = monthly.map(function (m) { return m.amount; });

        new Chart(canvas, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Earnings ($)',
                    data: amounts,
                    backgroundColor: 'rgba(0,120,212,0.75)',
                    borderColor:     '#0078d4',
                    borderWidth: 1,
                    borderRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) { return ' $' + ctx.parsed.y.toFixed(2); }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function (v) { return '$' + v; }
                        }
                    }
                }
            }
        });
    }

    // ── Admin Revenue Page ───────────────────────────────────────────
    function initAdminRevenue() {
        var form = document.getElementById('revenueFilterForm');
        if (!form) return;

        // Load on page open with defaults
        loadAdminRevenue();

        form.addEventListener('submit', function (e) {
            e.preventDefault();
            loadAdminRevenue();
        });

        // Quick-filter buttons
        document.querySelectorAll('[data-quick-filter]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var filter = btn.getAttribute('data-quick-filter');
                var now    = new Date();
                var from, to = toIso(now);

                if (filter === 'today') {
                    from = to;
                } else if (filter === 'week') {
                    var d = new Date(now);
                    d.setDate(d.getDate() - 6);
                    from = toIso(d);
                } else if (filter === 'month') {
                    from = now.getFullYear() + '-' + pad(now.getMonth() + 1) + '-01';
                }

                document.getElementById('fromDate').value = from;
                document.getElementById('toDate').value   = to;
                loadAdminRevenue();
            });
        });
    }

    function loadAdminRevenue() {
        var from = document.getElementById('fromDate')?.value || '';
        var to   = document.getElementById('toDate')?.value   || '';

        showLoading(true);

        var url = '/api/Revenue/admin/summary';
        // Use Gateway directly to avoid adding a MVC proxy action
        var gatewayBase = document.getElementById('gatewayBase')?.value || '';
        if (gatewayBase) {
            url = gatewayBase + '/api/Revenue/admin/summary';
        }

        fetch(url + '?fromDate=' + encodeURIComponent(from) + '&toDate=' + encodeURIComponent(to))
            .then(function (res) { return res.json(); })
            .then(function (result) {
                showLoading(false);
                if (!result.success) { showError('Could not load data.'); return; }
                renderAdminDashboard(result.data);
            })
            .catch(function () {
                showLoading(false);
                showError('Network error. Please try again.');
            });
    }

    function renderAdminDashboard(d) {
        setEl('admin-gross',      '$' + fmt(d.totalGrossRevenue));
        setEl('admin-commission', '$' + fmt(d.totalPlatformCommission));
        setEl('admin-tutors',     '$' + fmt(d.totalTutorPayouts));
        setEl('admin-bookings',   d.totalBookings);
        setEl('admin-hours',      d.totalHours);

        // Booking table
        var tbody = document.getElementById('bookingsTableBody');
        if (!tbody) return;
        tbody.innerHTML = '';

        if (!d.bookings || !d.bookings.length) {
            tbody.innerHTML = '<tr><td colspan="7" class="text-center text-muted py-4">No completed bookings in this period.</td></tr>';
            return;
        }

        d.bookings.forEach(function (b) {
            var row = '<tr>' +
                '<td class="small">' + shortId(b.bookingID) + '</td>' +
                '<td class="small text-muted">' + fmtDate(b.bookingDate) + '</td>' +
                '<td>' + escHtml(b.tutorID) + '</td>' +
                '<td>' + b.durationHours + ' h</td>' +
                '<td class="text-success fw-semibold">$' + fmt(b.totalAmount) + '</td>' +
                '<td class="text-danger">$' + fmt(b.platformFee) + '</td>' +
                '<td class="text-primary">$' + fmt(b.tutorPayout) + '</td>' +
                '</tr>';
            tbody.insertAdjacentHTML('beforeend', row);
        });
    }

    // CSV download
    window.downloadRevenueCsv = function () {
        var from = document.getElementById('fromDate')?.value || '';
        var to   = document.getElementById('toDate')?.value   || '';
        window.location.href = '/Admin/DownloadRevenueCsv?fromDate=' + encodeURIComponent(from) + '&toDate=' + encodeURIComponent(to);
    };

    // ── Helpers ──────────────────────────────────────────────────────
    function setEl(id, val) {
        var el = document.getElementById(id);
        if (el) el.textContent = val;
    }

    function fmt(n) { return Number(n || 0).toFixed(2); }
    function pad(n) { return String(n).padStart(2, '0'); }
    function toIso(d) { return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate()); }
    function fmtDate(s) { return s ? new Date(s).toLocaleDateString() : ''; }
    function shortId(id) { return id ? id.substring(0, 8) + '…' : ''; }
    function escHtml(s) {
        var d = document.createElement('div');
        d.textContent = String(s || '');
        return d.innerHTML;
    }

    function showLoading(on) {
        var el = document.getElementById('revenueLoading');
        var tb = document.getElementById('revenueContent');
        if (el) el.style.display = on ? 'block' : 'none';
        if (tb) tb.style.display = on ? 'none' : 'block';
    }

    function showError(msg) {
        var el = document.getElementById('revenueError');
        if (el) { el.textContent = msg; el.style.display = 'block'; }
    }

    // ── Boot ─────────────────────────────────────────────────────────
    document.addEventListener('DOMContentLoaded', function () {
        initTutorRevenue();
        initAdminRevenue();
    });

})();
