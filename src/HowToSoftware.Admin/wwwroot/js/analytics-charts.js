// Chart.js interop for Blazor analytics dashboard
window.analyticsCharts = (function () {
    const instances = {};

    function createOrUpdate(canvasId, type, labels, datasets, options) {
        // Defer to next animation frame so Blazor's render batch is applied to the DOM
        requestAnimationFrame(function () {
            try {
                if (typeof Chart === 'undefined') {
                    console.error('analyticsCharts: Chart.js not loaded');
                    return;
                }

                const ctx = document.getElementById(canvasId);
                if (!ctx) {
                    console.warn('analyticsCharts: canvas not found:', canvasId);
                    return;
                }

                if (instances[canvasId]) {
                    const chart = instances[canvasId];
                    // If Blazor replaced the canvas element, destroy the stale chart
                    if (!document.body.contains(chart.canvas)) {
                        chart.destroy();
                        delete instances[canvasId];
                    } else {
                        chart.data.labels = labels;
                        chart.data.datasets = datasets;
                        chart.update('none');
                        return;
                    }
                }

                instances[canvasId] = new Chart(ctx, {
                    type: type,
                    data: { labels: labels, datasets: datasets },
                    options: Object.assign({
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {
                            legend: {
                                labels: { color: '#adb5bd' }
                            }
                        },
                        scales: type === 'doughnut' || type === 'pie' ? {} : {
                            x: {
                                ticks: { color: '#adb5bd' },
                                grid: { color: 'rgba(255,255,255,0.05)' }
                            },
                            y: {
                                ticks: { color: '#adb5bd' },
                                grid: { color: 'rgba(255,255,255,0.05)' }
                            }
                        }
                    }, options || {})
                });
            } catch (e) {
                console.error('analyticsCharts:', canvasId, e);
            }
        });
    }

    function destroy(canvasId) {
        if (instances[canvasId]) {
            instances[canvasId].destroy();
            delete instances[canvasId];
        }
    }

    function destroyAll() {
        for (const id of Object.keys(instances)) {
            instances[id].destroy();
        }
        Object.keys(instances).forEach(function (k) { delete instances[k]; });
    }

    return { createOrUpdate: createOrUpdate, destroy: destroy, destroyAll: destroyAll };
})();
