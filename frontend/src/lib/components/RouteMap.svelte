<script lang="ts">
	import { onMount, onDestroy } from 'svelte';
	import { browser } from '$app/environment';
	import type { TelemetryPoint } from '../types/activity';
	import { t, interpolate } from '$lib/i18n';
	import {
		Navigation,
		Layers,
		MapPin,
		Dumbbell,
		Mountain,
		Satellite,
		Map as MapIcon,
		AlertCircle,
		ShieldAlert,
		Crosshair,
		Sparkles
	} from 'lucide-svelte';

	export let routePoints: TelemetryPoint[] = [];
	export let activityTitle: string = 'Percurso';
	export let status: string = 'Valid';
	export let activityType: string = 'Corrida';

	let mapContainer: HTMLElement;
	let mapInstance: any = null;
	let polylineLayer: any = null;
	let markersLayer: any = null;
	let currentBaseLayer: any = null;
	let activeLayerType: 'satellite' | 'terrain' | 'streets' = 'satellite';

	let detectedLocationName = '';
	let currentPolylineBounds: any = null;

	const mapLayers = {
		satellite: 'https://mt1.google.com/vt/lyrs=y&x={x}&y={y}&z={z}', // Google Maps Hybrid (Satélite + Ruas)
		terrain: 'https://mt1.google.com/vt/lyrs=p&x={x}&y={y}&z={z}',   // Google Maps Relevo
		streets: 'https://mt1.google.com/vt/lyrs=m&x={x}&y={y}&z={z}'    // Google Maps Ruas
	};

	$: hasGpsData = routePoints && routePoints.length > 0;

	$: if (browser && mapInstance && (routePoints || activeLayerType || activityTitle || $t)) {
		renderRoute();
	}

	async function switchLayer(type: 'satellite' | 'terrain' | 'streets') {
		if (!browser || !mapInstance) return;
		activeLayerType = type;

		const L = await import('leaflet');

		if (currentBaseLayer) {
			mapInstance.removeLayer(currentBaseLayer);
		}

		currentBaseLayer = L.tileLayer(mapLayers[type], {
			maxZoom: 20,
			subdomains: ['mt0', 'mt1', 'mt2', 'mt3'],
			attribution: '© Google Maps'
		}).addTo(mapInstance);
	}

	async function initMap() {
		if (!browser || !mapContainer) return;

		const L = await import('leaflet');

		if (mapInstance) {
			mapInstance.remove();
		}

		// Se há pontos de GPS, centraliza imediatamente na coordenada real (ex: Manaus)
		const initialLat = routePoints.length > 0 ? routePoints[0].latitude : -3.119;
		const initialLon = routePoints.length > 0 ? routePoints[0].longitude : -60.021;
		const initialZoom = routePoints.length > 0 ? 16 : 13;

		mapInstance = L.map(mapContainer, {
			attributionControl: false,
			zoomControl: true
		}).setView([initialLat, initialLon], initialZoom);

		currentBaseLayer = L.tileLayer(mapLayers[activeLayerType], {
			maxZoom: 20,
			subdomains: ['mt0', 'mt1', 'mt2', 'mt3']
		}).addTo(mapInstance);

		markersLayer = L.layerGroup().addTo(mapInstance);
		polylineLayer = L.layerGroup().addTo(mapInstance);

		renderRoute();
	}

	function detectCity(lat: number, lon: number): string {
		// Detecção precisa da Região Metropolitana de Manaus, AM
		if (lat >= -3.35 && lat <= -2.90 && lon >= -60.35 && lon <= -59.80) {
			return 'Manaus - AM, Brasil';
		}
		if (lat >= -4.5 && lat <= 2.5 && lon >= -73.5 && lon <= -56.0) {
			return 'Amazonas, Brasil';
		}
		return `${lat.toFixed(4)}°, ${lon.toFixed(4)}°`;
	}

	async function fetchGeocodedName(lat: number, lon: number) {
		detectedLocationName = detectCity(lat, lon);
		try {
			const res = await fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lon}&zoom=14`, {
				headers: { 'Accept-Language': 'pt-BR,pt;q=0.9,en;q=0.8' }
			});
			if (res.ok) {
				const data = await res.json();
				const addr = data.address || {};
				const neighborhood = addr.suburb || addr.neighbourhood || addr.city_district || '';
				const city = addr.city || addr.town || addr.municipality || 'Manaus';
				const state = addr.state || 'AM';
				detectedLocationName = neighborhood ? `${neighborhood}, ${city} - ${state}` : `${city} - ${state}`;
			}
		} catch {
			// Mantém detecção local de Manaus
		}
	}

	async function renderRoute() {
		if (!browser || !mapInstance) return;

		const L = await import('leaflet');

		markersLayer.clearLayers();
		polylineLayer.clearLayers();

		// Se o dispositivo NÃO gravou pontos GPS nesta atividade
		if (!routePoints || routePoints.length === 0) {
			detectedLocationName = '';
			currentPolylineBounds = null;
			return;
		}

		fetchGeocodedName(routePoints[0].latitude, routePoints[0].longitude);

		// Caso 1: O dispositivo registrou 1 ponto GPS de localização exata
		if (routePoints.length === 1) {
			const pt = routePoints[0];
			const strokeColor =
				status?.toLowerCase() === 'valid'
					? '#00ffa3'
					: status?.toLowerCase() === 'suspect'
					? '#ffb800'
					: '#ff007a';

			const markerIcon = L.divIcon({
				className: 'device-location-marker',
				html: `
					<div style="position: relative; width: 44px; height: 44px; display: flex; align-items: center; justify-content: center;">
						<span style="position: absolute; width: 100%; height: 100%; border-radius: 50%; background: ${strokeColor}50; animation: ping 1.5s cubic-bezier(0, 0, 0.2, 1) infinite;"></span>
						<div style="width: 32px; height: 32px; border-radius: 50%; background: #090317; border: 3px solid ${strokeColor}; display: flex; align-items: center; justify-content: center; box-shadow: 0 0 16px ${strokeColor}; color: white; font-weight: bold; font-size: 14px;">
							📍
						</div>
					</div>
				`,
				iconSize: [44, 44],
				iconAnchor: [22, 22]
			});

			L.marker([pt.latitude, pt.longitude], { icon: markerIcon })
				.bindPopup(`
					<div style="font-family: monospace; font-size: 12px; line-height: 1.5;">
						<b style="color: #00f0ff;">${detectedLocationName || 'Localização Registrada'}</b><br>
						<b>${$t.routeMap.popupActivity}</b> ${activityTitle}<br>
						<b>Lat:</b> ${pt.latitude.toFixed(6)}<br>
						<b>Lon:</b> ${pt.longitude.toFixed(6)}<br>
						${pt.heartRateBpm ? `<b>${$t.routeMap.popupHeartRate}</b> ${pt.heartRateBpm} BPM<br>` : ''}
						${pt.instantSpeedKmh > 0 ? `<b>${$t.routeMap.popupSpeed}</b> ${pt.instantSpeedKmh} km/h` : ''}
					</div>
				`)
				.addTo(markersLayer);

			setTimeout(() => {
				mapInstance.invalidateSize();
				mapInstance.flyTo([pt.latitude, pt.longitude], 17, { duration: 0.8 });
			}, 50);
			return;
		}

		// Caso 2: Múltiplos pontos de percurso GPS real (Mi Fitness / Relógio)
		const latLngs = routePoints.map((p) => [p.latitude, p.longitude] as [number, number]);

		// Traçado de alta fidelidade visual estilo Mi Fitness / Strava
		// 1. Linha externa Neon Cyan / Emerald de contraste no mapa satélite
		const polylineOuter = L.polyline(latLngs, {
			color: '#00f0ff',
			weight: 6,
			opacity: 0.95,
			lineCap: 'round',
			lineJoin: 'round'
		}).addTo(polylineLayer);

		// 2. Linha interna brilhante para definição milimétrica das ruas
		L.polyline(latLngs, {
			color: '#ffffff',
			weight: 2,
			opacity: 0.95,
			lineCap: 'round',
			lineJoin: 'round'
		}).addTo(polylineLayer);

		// Marcador de Início (Verde Neon)
		const startPoint = routePoints[0];
		const startIcon = L.divIcon({
			className: 'custom-start-marker',
			html: `<div style="background-color: #00ffa3; width: 18px; height: 18px; border-radius: 50%; border: 3px solid #ffffff; box-shadow: 0 0 16px #00ffa3;"></div>`,
			iconSize: [18, 18],
			iconAnchor: [9, 9]
		});

		L.marker([startPoint.latitude, startPoint.longitude], { icon: startIcon })
			.bindPopup(`<b>🏁 ${$t.routeMap.popupStart}</b><br>${detectedLocationName}<br>BPM: ${startPoint.heartRateBpm || '--'}<br>${$t.routeMap.popupSpeed} ${startPoint.instantSpeedKmh} km/h`)
			.addTo(markersLayer);

		// Marcador de Fim (Magenta Neon)
		const endPoint = routePoints[routePoints.length - 1];
		const endIcon = L.divIcon({
			className: 'custom-end-marker',
			html: `<div style="background-color: #ff007a; width: 18px; height: 18px; border-radius: 50%; border: 3px solid #ffffff; box-shadow: 0 0 16px #ff007a;"></div>`,
			iconSize: [18, 18],
			iconAnchor: [9, 9]
		});

		L.marker([endPoint.latitude, endPoint.longitude], { icon: endIcon })
			.bindPopup(`<b>🏁 ${$t.routeMap.popupEnd}</b><br>${detectedLocationName}<br>BPM: ${endPoint.heartRateBpm || '--'}<br>${$t.routeMap.popupSpeed} ${endPoint.instantSpeedKmh} km/h`)
			.addTo(markersLayer);

		currentPolylineBounds = polylineOuter.getBounds();

		setTimeout(() => {
			if (mapInstance && currentPolylineBounds && currentPolylineBounds.isValid()) {
				mapInstance.invalidateSize();
				mapInstance.fitBounds(currentPolylineBounds, { padding: [45, 45], maxZoom: 18 });
			}
		}, 100);
	}

	function handleRecenter() {
		if (mapInstance && currentPolylineBounds && currentPolylineBounds.isValid()) {
			mapInstance.invalidateSize();
			mapInstance.fitBounds(currentPolylineBounds, { padding: [45, 45], maxZoom: 18 });
		}
	}

	onMount(() => {
		initMap();
	});

	onDestroy(() => {
		if (mapInstance) {
			mapInstance.remove();
		}
	});
</script>

<div class="cyber-card rounded-2xl p-4 shadow-xl flex flex-col h-[480px]">
	<!-- Map Header -->
	<div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-3 mb-2 border-b border-purple-500/15">
		<div class="flex items-center gap-2 min-w-0">
			{#if activityType?.toLowerCase() === 'musculação'}
				<Dumbbell class="w-4 h-4 text-amber-400 shrink-0" />
			{:else}
				<Navigation class="w-4 h-4 text-cyber-cyan shrink-0 animate-pulse" />
			{/if}
			<h3 class="text-sm font-bold text-white tracking-wide truncate max-w-[200px] sm:max-w-xs">
				{activityTitle}
			</h3>

			{#if detectedLocationName}
				<span class="hidden md:inline-flex items-center gap-1 text-[11px] font-mono px-2 py-0.5 rounded-full bg-cyan-500/15 border border-cyan-500/30 text-cyber-cyan font-bold truncate max-w-[180px]">
					<MapPin class="w-3 h-3 text-cyber-cyan shrink-0" />
					<span class="truncate">{detectedLocationName}</span>
				</span>
			{/if}
		</div>

		<!-- Layer Selector: Satélite Google / Terreno / Ruas + Botão Recenter -->
		<div class="flex items-center gap-1 bg-slate-950/80 p-1 rounded-xl border border-purple-500/20 text-xs font-mono shrink-0">
			{#if hasGpsData}
				<button
					type="button"
					on:click={handleRecenter}
					class="flex items-center gap-1 px-2.5 py-1 rounded-lg transition-all cursor-pointer bg-slate-900 hover:bg-purple-950 text-cyan-300 hover:text-white border border-cyan-500/30 hover:border-cyan-400 shadow-sm"
					title="Focar e centralizar exatamente no percurso de GPS"
				>
					<Crosshair class="w-3.5 h-3.5 text-cyber-cyan" />
					<span class="hidden sm:inline">Focar Percurso</span>
				</button>
			{/if}

			<button
				type="button"
				on:click={() => switchLayer('satellite')}
				class="flex items-center gap-1 px-2.5 py-1 rounded-lg transition-all cursor-pointer {activeLayerType === 'satellite' ? 'bg-gradient-to-r from-nubank-600 to-purple-600 text-white font-bold shadow-[0_0_10px_rgba(130,10,209,0.4)]' : 'text-slate-400 hover:text-white'}"
				title={$t.routeMap.satelliteTitle}
			>
				<Satellite class="w-3.5 h-3.5" />
				<span class="hidden sm:inline">{$t.routeMap.satellite}</span>
			</button>

			<button
				type="button"
				on:click={() => switchLayer('terrain')}
				class="flex items-center gap-1 px-2.5 py-1 rounded-lg transition-all cursor-pointer {activeLayerType === 'terrain' ? 'bg-gradient-to-r from-nubank-600 to-purple-600 text-white font-bold shadow-[0_0_10px_rgba(130,10,209,0.4)]' : 'text-slate-400 hover:text-white'}"
				title={$t.routeMap.terrainTitle}
			>
				<Mountain class="w-3.5 h-3.5" />
				<span class="hidden sm:inline">{$t.routeMap.terrain}</span>
			</button>

			<button
				type="button"
				on:click={() => switchLayer('streets')}
				class="flex items-center gap-1 px-2.5 py-1 rounded-lg transition-all cursor-pointer {activeLayerType === 'streets' ? 'bg-gradient-to-r from-nubank-600 to-purple-600 text-white font-bold shadow-[0_0_10px_rgba(130,10,209,0.4)]' : 'text-slate-400 hover:text-white'}"
				title={$t.routeMap.streetsTitle}
			>
				<MapIcon class="w-3.5 h-3.5" />
				<span class="hidden sm:inline">{$t.routeMap.streets}</span>
			</button>
		</div>
	</div>

	<!-- Map Canvas -->
	<div class="flex-1 relative rounded-xl overflow-hidden border border-purple-500/20">
		<div bind:this={mapContainer} class="w-full h-full z-10"></div>

		<!-- Overlay Real: Quando o dispositivo não possui dados de satélite/GPS -->
		{#if !hasGpsData}
			<div class="absolute inset-0 z-20 flex items-center justify-center p-6 bg-slate-950/75 backdrop-blur-[2px]">
				<div class="max-w-md w-full p-5 rounded-2xl bg-slate-900/95 border border-purple-500/30 shadow-2xl text-center space-y-3">
					<div class="w-12 h-12 rounded-full bg-purple-950/40 border border-purple-500/30 text-purple-300 flex items-center justify-center mx-auto shadow-[0_0_12px_rgba(130,10,209,0.3)]">
						{#if activityType?.toLowerCase() === 'musculação'}
							<Dumbbell class="w-6 h-6 text-amber-400" />
						{:else}
							<MapPin class="w-6 h-6 text-purple-400" />
						{/if}
					</div>

					<div>
						<h4 class="text-sm font-bold text-white">{$t.routeMap.noGpsTitle}</h4>
						<p class="text-xs text-purple-200/70 mt-1 leading-relaxed">
							{interpolate($t.routeMap.noGpsDesc, { title: activityTitle })}
						</p>
					</div>

					<div class="pt-1 flex items-center justify-center gap-2 text-[11px] font-mono text-cyber-emerald">
						<span class="w-2 h-2 rounded-full bg-cyber-emerald shadow-[0_0_8px_#00ffa3]"></span>
						<span>{$t.routeMap.bioTelemetryValid}</span>
					</div>
				</div>
			</div>
		{:else}
			<!-- Banner Inferior com Coordenadas Reais Obtidas do Dispositivo -->
			<div class="absolute bottom-3 left-3 right-3 z-20 p-3 rounded-xl bg-slate-950/90 border border-purple-500/30 backdrop-blur-md shadow-2xl flex items-center justify-between">
				<div class="flex items-center gap-3">
					<div class="w-8 h-8 rounded-lg bg-purple-950/60 border border-purple-500/30 text-purple-300 flex items-center justify-center shrink-0">
						<MapPin class="w-4 h-4 text-cyber-cyan" />
					</div>
					<div>
						<div class="text-xs font-bold text-white flex items-center gap-2">
							<span>{detectedLocationName ? detectedLocationName : $t.routeMap.deviceLocation}</span>
							<span class="text-[10px] font-mono px-2 py-0.5 rounded-full bg-emerald-500/20 text-cyber-emerald border border-emerald-500/30 font-bold flex items-center gap-1">
								<span class="w-1.5 h-1.5 rounded-full bg-cyber-emerald animate-pulse"></span>
								DADOS REAIS DE MANAUS
							</span>
						</div>
						<span class="text-[11px] text-purple-200/80 font-mono">
							Lat: {routePoints[0].latitude.toFixed(6)}, Lon: {routePoints[0].longitude.toFixed(6)}
							{#if routePoints.length > 1}
								• {routePoints.length} pontos de percurso do relógio
							{/if}
						</span>
					</div>
				</div>

				<button
					type="button"
					on:click={handleRecenter}
					class="hidden sm:flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-purple-950/70 hover:bg-purple-900 border border-purple-400/40 text-xs font-mono text-purple-200 hover:text-white transition-all cursor-pointer shadow-md"
				>
					<Crosshair class="w-3.5 h-3.5 text-cyber-cyan" />
					<span>Centralizar Rota</span>
				</button>
			</div>
		{/if}
	</div>
</div>

<style>
	:global(.leaflet-container) {
		background-color: #0b1120 !important;
		font-family: inherit;
	}
	:global(.leaflet-popup-content-wrapper) {
		background-color: #0f172a !important;
		color: #f8fafc !important;
		border: 1px solid #334155 !important;
		border-radius: 8px !important;
		font-size: 11px !important;
		font-family: monospace;
	}
	:global(.leaflet-popup-tip) {
		background-color: #0f172a !important;
	}
</style>
