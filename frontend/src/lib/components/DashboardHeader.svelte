<script lang="ts">
	import { onMount } from 'svelte';
	import { RefreshCw, Zap, Bluetooth, Activity, UploadCloud, Globe } from 'lucide-svelte';
	import { locale, t, setLocale, formatDate, formatDayOfWeek, formatTime, interpolate } from '$lib/i18n';

	export let onOpenImportModal: () => void;
	export let onOpenBluetooth: () => void;
	export let onRefresh: () => void;
	export let isRefreshing: boolean = false;
	export let bluetoothConnectedDevice: string | null = null;

	let currentTimeStr = '';
	let currentDateStr = '';
	let currentDayOfWeek = '';

	function updateClock() {
		const now = new Date();
		currentDateStr = formatDate(now, $locale);
		currentDayOfWeek = formatDayOfWeek(now, $locale);
		currentTimeStr = formatTime(now, $locale, true);
	}

	$: if ($locale) {
		updateClock();
	}

	onMount(() => {
		updateClock();
		const interval = setInterval(updateClock, 1000);
		return () => clearInterval(interval);
	});
</script>

<header class="border-b border-purple-500/20 bg-[#090317]/80 backdrop-blur-xl sticky top-0 z-30 px-4 sm:px-6 py-3.5 shadow-lg shadow-purple-950/20">
	<div class="max-w-7xl mx-auto flex flex-col md:flex-row md:items-center justify-between gap-4">
		<!-- Brand & Title with Nubank Lilac Glow -->
		<div class="flex items-center gap-3">
			<div class="relative group">
				<div class="w-11 h-11 rounded-xl bg-gradient-to-tr from-nubank-600 via-purple-500 to-cyber-cyan p-0.5 shadow-lg shadow-nubank-600/30 group-hover:shadow-purple-500/50 transition-shadow">
					<div class="w-full h-full bg-[#0a0316] rounded-[10px] flex items-center justify-center text-purple-400">
						<Activity class="w-6 h-6 text-purple-300 drop-shadow-[0_0_8px_rgba(192,132,252,0.8)]" />
					</div>
				</div>
				<!-- Futuristic corner neon pin -->
				<span class="absolute -top-1 -right-1 w-2.5 h-2.5 rounded-full bg-cyber-cyan shadow-[0_0_8px_#00f0ff]"></span>
			</div>

			<div>
				<div class="flex items-center gap-2">
					<h1 class="text-xl font-black tracking-tight text-white font-sans flex items-center gap-2">
						<span class="bg-gradient-to-r from-white via-purple-200 to-purple-400 bg-clip-text text-transparent">{$t.app.title}</span>
						<span class="text-[10px] font-mono px-2 py-0.5 rounded-full bg-nubank-600/20 border border-purple-500/40 text-purple-300 font-bold uppercase tracking-wider">
							{$t.app.badge}
						</span>
					</h1>
					{#if bluetoothConnectedDevice}
						<span class="inline-flex items-center gap-1 text-[11px] font-mono px-2.5 py-0.5 rounded-full bg-blue-500/15 border border-blue-500/30 text-blue-400 shadow-[0_0_10px_rgba(59,130,246,0.3)]">
							<Bluetooth class="w-3 h-3 text-blue-400 animate-pulse" />
							{bluetoothConnectedDevice}
						</span>
					{/if}
				</div>
				<p class="text-xs text-purple-300/70 font-sans">
					{$t.app.subtitle}
				</p>
			</div>
		</div>

		<!-- Dia, Data, Hora Real-time HUD -->
		<div class="flex items-center gap-3 bg-slate-950/80 border border-purple-500/20 px-3.5 py-2 rounded-xl text-xs font-mono shadow-inner">
			<div class="flex items-center gap-2">
				<span class="w-2 h-2 rounded-full bg-cyber-emerald shadow-[0_0_8px_#00ffa3] animate-pulse"></span>
				<span class="capitalize text-slate-300 font-semibold">{currentDayOfWeek}</span>
			</div>
			<div class="w-px h-4 bg-purple-500/20"></div>
			<div class="text-purple-300/80 text-[11px]">
				{currentDateStr}
			</div>
			<div class="w-px h-4 bg-purple-500/20"></div>
			<div class="text-cyber-cyan font-bold text-sm tracking-wider font-mono text-glow-cyan">
				{currentTimeStr}
			</div>
		</div>

		<!-- Action Controls with Futuristic Buttons -->
		<div class="flex items-center gap-2.5 flex-wrap">
			<!-- Language Switcher: PT-BR / EN -->
			<div class="flex items-center bg-slate-950/90 border border-purple-500/30 p-1 rounded-xl shadow-inner font-mono text-[11px]" role="group" aria-label={$t.header.language}>
				<button
					type="button"
					on:click={() => setLocale('pt-BR')}
					class="px-2.5 py-1 rounded-lg font-bold flex items-center gap-1.5 transition-all cursor-pointer {$locale === 'pt-BR' ? 'bg-gradient-to-r from-nubank-600 to-purple-600 text-white shadow-[0_0_10px_rgba(130,10,209,0.5)] border border-purple-400/40' : 'text-slate-400 hover:text-purple-200'}"
					title="Português (Brasil)"
				>
					<span>🇧🇷</span>
					<span>PT</span>
				</button>
				<button
					type="button"
					on:click={() => setLocale('en')}
					class="px-2.5 py-1 rounded-lg font-bold flex items-center gap-1.5 transition-all cursor-pointer {$locale === 'en' ? 'bg-gradient-to-r from-nubank-600 to-purple-600 text-white shadow-[0_0_10px_rgba(130,10,209,0.5)] border border-purple-400/40' : 'text-slate-400 hover:text-purple-200'}"
					title="English"
				>
					<span>🇺🇸</span>
					<span>EN</span>
				</button>
			</div>

			<!-- Conectar relógio e puxar histórico -->
			<button
				type="button"
				on:click={onOpenBluetooth}
				class="inline-flex items-center gap-2 px-3.5 py-2 text-xs font-semibold rounded-xl border transition-all cursor-pointer {bluetoothConnectedDevice ? 'bg-purple-950/60 border-purple-400/60 text-purple-200 shadow-[0_0_15px_rgba(130,10,209,0.4)]' : 'bg-[#140828] hover:bg-purple-950 text-purple-200 border-purple-500/30 hover:border-purple-400 shadow-md'}"
				title={$t.header.connectWatchTitle}
			>
				<Bluetooth class="w-4 h-4 {bluetoothConnectedDevice ? 'text-cyber-cyan animate-pulse' : 'text-purple-400'}" />
				<span>{bluetoothConnectedDevice ? interpolate($t.header.connectedWatch, { name: bluetoothConnectedDevice }) : $t.header.connectWatch}</span>
			</button>

			<!-- Atualizar -->
			<button
				type="button"
				on:click={onRefresh}
				disabled={isRefreshing}
				class="inline-flex items-center gap-1.5 px-3 py-2 text-xs font-medium text-slate-300 bg-slate-950/70 hover:bg-purple-950/50 border border-purple-500/20 hover:border-purple-500/40 rounded-xl transition-all disabled:opacity-50 cursor-pointer"
				title={$t.header.refreshTitle}
			>
				<RefreshCw class="w-3.5 h-3.5 {isRefreshing ? 'animate-spin text-cyber-cyan' : 'text-purple-400'}" />
				<span class="hidden sm:inline">{$t.header.refresh}</span>
			</button>

			<!-- Importar do Mi Fitness / Samsung Health -->
			<button
				type="button"
				on:click={onOpenImportModal}
				class="inline-flex items-center gap-2 px-4 py-2 text-xs font-bold text-white bg-gradient-to-r from-nubank-600 via-purple-600 to-fuchsia-600 hover:from-purple-600 hover:to-nubank-600 rounded-xl shadow-[0_0_20px_rgba(130,10,209,0.4)] hover:shadow-[0_0_25px_rgba(130,10,209,0.6)] transition-all cursor-pointer border border-purple-400/30"
			>
				<UploadCloud class="w-4 h-4 text-purple-200" />
				<span>{$t.header.importGpx}</span>
			</button>
		</div>
	</div>
</header>
