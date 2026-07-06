// World choropleth map for analytics — uses chartjs-chart-geo + world-atlas (TopoJSON).
// Public surface: window.analyticsMap.render(canvasId, data)
//   data = [{ code: 'US', name: 'United States', visitors: 1234 }, ...]
// Country `code` is ISO 3166-1 alpha-2 (matching what GeoIpService returns).
window.analyticsMap = (function () {
    let chart = null;
    let countriesPromise = null;
    let countryNames = {};
    let registered = false;

    function ensureRegistered() {
        if (registered) return true;
        if (typeof Chart === 'undefined' || typeof ChartGeo === 'undefined') return false;
        try {
            Chart.register(
                ChartGeo.ChoroplethController,
                ChartGeo.GeoFeature,
                ChartGeo.ColorScale,
                ChartGeo.ProjectionScale
            );
            registered = true;
            return true;
        } catch (e) {
            console.error('analyticsMap: register failed', e);
            return false;
        }
    }

    // ISO 3166-1 alpha-2 -> numeric code (string, zero-padded to 3) so we can
    // match the world-atlas TopoJSON which uses numeric ids.
    const A2_TO_NUM = {
        AD: '020', AE: '784', AF: '004', AG: '028', AI: '660', AL: '008', AM: '051', AO: '024', AQ: '010', AR: '032',
        AS: '016', AT: '040', AU: '036', AW: '533', AX: '248', AZ: '031', BA: '070', BB: '052', BD: '050', BE: '056',
        BF: '854', BG: '100', BH: '048', BI: '108', BJ: '204', BL: '652', BM: '060', BN: '096', BO: '068', BQ: '535',
        BR: '076', BS: '044', BT: '064', BV: '074', BW: '072', BY: '112', BZ: '084', CA: '124', CC: '166', CD: '180',
        CF: '140', CG: '178', CH: '756', CI: '384', CK: '184', CL: '152', CM: '120', CN: '156', CO: '170', CR: '188',
        CU: '192', CV: '132', CW: '531', CX: '162', CY: '196', CZ: '203', DE: '276', DJ: '262', DK: '208', DM: '212',
        DO: '214', DZ: '012', EC: '218', EE: '233', EG: '818', EH: '732', ER: '232', ES: '724', ET: '231', FI: '246',
        FJ: '242', FK: '238', FM: '583', FO: '234', FR: '250', GA: '266', GB: '826', GD: '308', GE: '268', GF: '254',
        GG: '831', GH: '288', GI: '292', GL: '304', GM: '270', GN: '324', GP: '312', GQ: '226', GR: '300', GS: '239',
        GT: '320', GU: '316', GW: '624', GY: '328', HK: '344', HM: '334', HN: '340', HR: '191', HT: '332', HU: '348',
        ID: '360', IE: '372', IL: '376', IM: '833', IN: '356', IO: '086', IQ: '368', IR: '364', IS: '352', IT: '380',
        JE: '832', JM: '388', JO: '400', JP: '392', KE: '404', KG: '417', KH: '116', KI: '296', KM: '174', KN: '659',
        KP: '408', KR: '410', KW: '414', KY: '136', KZ: '398', LA: '418', LB: '422', LC: '662', LI: '438', LK: '144',
        LR: '430', LS: '426', LT: '440', LU: '442', LV: '428', LY: '434', MA: '504', MC: '492', MD: '498', ME: '499',
        MF: '663', MG: '450', MH: '584', MK: '807', ML: '466', MM: '104', MN: '496', MO: '446', MP: '580', MQ: '474',
        MR: '478', MS: '500', MT: '470', MU: '480', MV: '462', MW: '454', MX: '484', MY: '458', MZ: '508', NA: '516',
        NC: '540', NE: '562', NF: '574', NG: '566', NI: '558', NL: '528', NO: '578', NP: '524', NR: '520', NU: '570',
        NZ: '554', OM: '512', PA: '591', PE: '604', PF: '258', PG: '598', PH: '608', PK: '586', PL: '616', PM: '666',
        PN: '612', PR: '630', PS: '275', PT: '620', PW: '585', PY: '600', QA: '634', RE: '638', RO: '642', RS: '688',
        RU: '643', RW: '646', SA: '682', SB: '090', SC: '690', SD: '729', SE: '752', SG: '702', SH: '654', SI: '705',
        SJ: '744', SK: '703', SL: '694', SM: '674', SN: '686', SO: '706', SR: '740', SS: '728', ST: '678', SV: '222',
        SX: '534', SY: '760', SZ: '748', TC: '796', TD: '148', TF: '260', TG: '768', TH: '764', TJ: '762', TK: '772',
        TL: '626', TM: '795', TN: '788', TO: '776', TR: '792', TT: '780', TV: '798', TW: '158', TZ: '834', UA: '804',
        UG: '800', UM: '581', US: '840', UY: '858', UZ: '860', VA: '336', VC: '670', VE: '862', VG: '092', VI: '850',
        VN: '704', VU: '548', WF: '876', WS: '882', YE: '887', YT: '175', ZA: '710', ZM: '894', ZW: '716'
    };

    function loadCountries() {
        if (countriesPromise) return countriesPromise;
        // Served locally from wwwroot/js so the request stays within
        // connect-src 'self' under our edge CSP. The admin app is mounted
        // at /ghost so we use a base-relative URL.
        countriesPromise = fetch('js/world-atlas-countries-50m.json')
            .then(function (r) {
                if (!r.ok) throw new Error('Failed to load world atlas: ' + r.status);
                return r.json();
            })
            .then(function (topology) {
                if (typeof ChartGeo === 'undefined' || !ChartGeo.topojson) {
                    throw new Error('chartjs-chart-geo not loaded');
                }
                const features = ChartGeo.topojson.feature(
                    topology, topology.objects.countries
                ).features;
                features.forEach(function (f) {
                    if (f && f.id && f.properties && f.properties.name) {
                        countryNames[String(f.id).padStart(3, '0')] = f.properties.name;
                    }
                });
                return features;
            })
            .catch(function (err) {
                countriesPromise = null;
                throw err;
            });
        return countriesPromise;
    }

    function render(canvasId, data) {
        requestAnimationFrame(function () {
            try {
                if (typeof Chart === 'undefined') {
                    console.error('analyticsMap: Chart.js not loaded');
                    return;
                }
                if (typeof ChartGeo === 'undefined') {
                    console.error('analyticsMap: chartjs-chart-geo not loaded');
                    return;
                }
                if (!ensureRegistered()) return;
                const canvas = document.getElementById(canvasId);
                if (!canvas) return;

                loadCountries().then(function (features) {
                    // Build numeric -> visitors map from incoming alpha-2 data.
                    const valueByNum = {};
                    const safeData = Array.isArray(data) ? data : [];
                    for (let i = 0; i < safeData.length; i++) {
                        const row = safeData[i];
                        if (!row || typeof row.code !== 'string') continue;
                        const num = A2_TO_NUM[row.code.toUpperCase()];
                        if (!num) continue;
                        const v = Number(row.visitors) || 0;
                        valueByNum[num] = (valueByNum[num] || 0) + v;
                    }

                    const points = features.map(function (f) {
                        const num = String(f.id).padStart(3, '0');
                        return { feature: f, value: valueByNum[num] || 0 };
                    });

                    // Compute max so we can give the color scale an explicit
                    // domain. With all-zero data, leaving min/max to auto-detect
                    // produces NaN colors and the whole map fails to render.
                    let maxValue = 0;
                    for (let i = 0; i < points.length; i++) {
                        if (points[i].value > maxValue) maxValue = points[i].value;
                    }
                    const colorMin = 0;
                    const colorMax = maxValue > 0 ? maxValue : 1;

                    if (chart) {
                        if (!document.body.contains(chart.canvas)) {
                            chart.destroy();
                            chart = null;
                        } else {
                            chart.data.labels = points.map(function (p) {
                                return p.feature.properties.name;
                            });
                            chart.data.datasets[0].data = points;
                            if (chart.options && chart.options.scales && chart.options.scales.color) {
                                chart.options.scales.color.min = colorMin;
                                chart.options.scales.color.max = colorMax;
                            }
                            chart.update('none');
                            return;
                        }
                    }

                    chart = new Chart(canvas.getContext('2d'), {
                        type: 'choropleth',
                        data: {
                            labels: points.map(function (p) { return p.feature.properties.name; }),
                            datasets: [{
                                label: 'Visitors',
                                data: points,
                                outline: features,
                                borderColor: 'rgba(255,255,255,0.35)',
                                borderWidth: 0.6
                            }]
                        },
                        options: {
                            responsive: true,
                            maintainAspectRatio: false,
                            showOutline: true,
                            showGraticule: false,
                            plugins: {
                                legend: { display: false },
                                tooltip: {
                                    callbacks: {
                                        label: function (ctx) {
                                            const f = ctx.raw && ctx.raw.feature;
                                            const name = f && f.properties ? f.properties.name : 'Unknown';
                                            const v = ctx.raw && ctx.raw.value ? ctx.raw.value : 0;
                                            return name + ': ' + v.toLocaleString() + (v === 1 ? ' visitor' : ' visitors');
                                        }
                                    }
                                }
                            },
                            scales: {
                                projection: {
                                    axis: 'x',
                                    projection: 'equalEarth'
                                },
                                color: {
                                    axis: 'x',
                                    quantize: 5,
                                    min: colorMin,
                                    max: colorMax,
                                    legend: { position: 'bottom-right', align: 'right' },
                                    interpolate: function (t) {
                                        // Dark-theme friendly blue ramp: from near-card-bg to bright accent.
                                        const tn = (typeof t === 'number' && isFinite(t)) ? t : 0;
                                        const a = [33, 37, 41];        // #212529
                                        const b = [13, 110, 253];      // bootstrap primary
                                        const r = Math.round(a[0] + (b[0] - a[0]) * tn);
                                        const g = Math.round(a[1] + (b[1] - a[1]) * tn);
                                        const bl = Math.round(a[2] + (b[2] - a[2]) * tn);
                                        return 'rgb(' + r + ',' + g + ',' + bl + ')';
                                    }
                                }
                            }
                        }
                    });
                }).catch(function (err) {
                    console.error('analyticsMap: failed to render', err);
                });
            } catch (e) {
                console.error('analyticsMap:', e);
            }
        });
    }

    function destroy() {
        if (chart) {
            try { chart.destroy(); } catch (_) { /* ignore */ }
            chart = null;
        }
    }

    return { render: render, destroy: destroy };
})();
