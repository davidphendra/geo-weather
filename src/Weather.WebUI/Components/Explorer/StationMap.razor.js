// Leaflet map for the selected station. Leaflet is loaded on demand from the pinned unpkg CDN
// (ADR-0018 D2); the CSP allows exactly those two artifacts (e15s03).
const LEAFLET = '1.9.4';
const LEAFLET_CSS = `https://unpkg.com/leaflet@${LEAFLET}/dist/leaflet.css`;
const LEAFLET_JS = `https://unpkg.com/leaflet@${LEAFLET}/dist/leaflet.js`;
const LEAFLET_CSS_INTEGRITY = 'sha256-p4NxAoJBhIIN+hmNHrzRCf9tD/miZyoHS5obTRR9BMY=';
const LEAFLET_JS_INTEGRITY = 'sha256-20nQCchB9co0qIjJZRGuk2/Z9VM+kNiyxNV1lvTlZBo=';
const TILES = 'https://tile.openstreetmap.org/{z}/{x}/{y}.png';
const ATTRIBUTION =
    '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors';

const maps = new WeakMap();
let leafletPromise;

function ensureStylesheet() {
    if (document.querySelector('link[data-leaflet]')) {
        return;
    }

    const link = document.createElement('link');
    link.rel = 'stylesheet';
    link.href = LEAFLET_CSS;
    link.integrity = LEAFLET_CSS_INTEGRITY;
    link.crossOrigin = 'anonymous';
    link.dataset.leaflet = 'true';
    document.head.appendChild(link);
}

function loadLeaflet() {
    if (globalThis.L) {
        return Promise.resolve(globalThis.L);
    }

    leafletPromise ??= new Promise((resolve, reject) => {
        ensureStylesheet();

        const script = document.createElement('script');
        script.src = LEAFLET_JS;
        script.integrity = LEAFLET_JS_INTEGRITY;
        script.crossOrigin = 'anonymous';
        script.onload = () =>
            globalThis.L ? resolve(globalThis.L) : reject(new Error('Leaflet did not initialise.'));
        script.onerror = () => reject(new Error('Leaflet failed to load.'));
        document.head.appendChild(script);
    });

    return leafletPromise;
}

export async function render(element, latitude, longitude, label) {
    if (!element) {
        return;
    }

    const L = await loadLeaflet();
    const point = [Number(latitude), Number(longitude)];
    let entry = maps.get(element);

    if (!entry) {
        const map = L.map(element, { zoomControl: true });
        L.tileLayer(TILES, { maxZoom: 19, attribution: ATTRIBUTION }).addTo(map);
        entry = { map, marker: L.marker(point).addTo(map) };
        maps.set(element, entry);
    } else {
        entry.marker.setLatLng(point);
    }

    // Leaflet renders popup content as HTML; build the label from text only.
    const popup = document.createElement('div');
    popup.textContent = label ?? '';
    entry.marker.bindPopup(popup);

    entry.map.setView(point, 11);
    entry.map.invalidateSize();
}

export function dispose(element) {
    const entry = element ? maps.get(element) : null;
    if (!entry) {
        return;
    }

    entry.map.remove();
    maps.delete(element);
}
