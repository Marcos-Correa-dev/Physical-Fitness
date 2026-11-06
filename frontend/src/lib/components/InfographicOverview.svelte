<script lang="ts">
	import type { DashboardMetrics } from '$lib/types/activity';
	import PieChart from './PieChart.svelte';
	import { t, interpolate } from '$lib/i18n';
	import {
		Route,
		Flame,
		Heart,
		ShieldCheck,
		Zap,
		Activity,
		Clock,
		Gauge,
		Layers,
		Compass,
		Cpu,
		TrendingUp,
		Radio
	} from 'lucide-svelte';

	export let metrics: DashboardMetrics | null = null;

	// Computations for infographics
	$: totalValidatedKm = metrics ? metrics.totalValidatedKm : 0;
	$: totalReportedKm = metrics ? metrics.totalReportedKm : 0;
	$: integrityPercentage =
		totalReportedKm > 0
			? Math.min(100, Math.round((totalValidatedKm / totalReportedKm) * 100))
			: 100;

	$: totalCalories = metrics ? metrics.totalCaloriesBurned : 0;
	$: totalActivities = metrics ? metrics.totalActivitiesCount : 0;
	$: avgCaloriesPerWorkout =
		totalActivities > 0 ? Math.round(totalCalories / totalActivities) : 0;

	$: avgBpm = metrics ? metrics.overallAvgHeartRate : 0;
	$: validCount = metrics ? metrics.validActivitiesCount : 0;
	$: suspectCount = metrics ? metrics.suspectActivitiesCount : 0;
	$: rejectedCount = metrics ? metrics.rejectedActivitiesCount : 0;
	$: reliabilityPercentage =
		totalActivities > 0
			? Math.round((validCount / totalActivities) * 100)
			: 100;

	// Total workout duration accumulated
	$: totalDurationMinutes = (metrics?.recentActivities ?? []).reduce((acc, a) => {
		return acc + (a.durationSeconds ? Math.round(a.durationSeconds / 60) : 0);
	}, 0);

	$: totalHoursFormatted = (() => {
		const h = Math.floor(totalDurationMinutes / 60);
		const m = totalDurationMinutes % 60;
		return `${h}h ${m}m`;
	})();

	// Maximum heart rate detected across all activities
	$: maxHeartRateOverall = (metrics?.recentActivities ?? []).reduce((max, a) => {
		return a.maxHeartRateBpm > max ? a.maxHeartRateBpm : max;
	}, 0);

	// Average speed overall
	$: avgSpeedOverall = (() => {
		const acts = (metrics?.recentActivities ?? []).filter((a) => a.averageSpeedKmh > 0);
		if (acts.length === 0) return '0.0';
		const sum = acts.reduce((acc, a) => acc + a.averageSpeedKmh, 0);
		return (sum / acts.length).toFixed(1);
	})();

	// Calculate circumference for circular gauge
	const radius = 34;
	const circumference = 2 * Math.PI * radius;
	$: strokeDashoffset =
		circumference - (integrityPercentage / 100) * circumference;
</script>

<!-- ========================================== -->
<!-- 1. TOP INFOGRAPHIC KPI GRID (4 COLUMNS)    -->
<!-- ========================================== -->
<section class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
	<!-- CARD 1: KM VALIDADO & TELEMETRIA GPS (Nubank Lilac & Cyber Cyan) -->
	<div class="cyber-card rounded-2xl p-5 relative overflow-hidden group hover:border-purple-400/50">
		<div class="absolute -top-12 -right-12 w-32 h-32 bg-nubank-600/20 rounded-full blur-xl pointer-events-none"></div>
		
		<div class="relative z-10 flex items-start justify-between">
			<div>
				<span class="text-[11px] font-mono font-bold tracking-wider uppercase text-purple-300/80 flex items-center gap-1.5">
					<Route class="w-3.5 h-3.5 text-cyber-cyan" />
					<span>{$t.kpis.validatedKm}</span>
				</span>
				<div class="mt-2 text-3xl font-black font-mono text-white text-glow-nubank tracking-tight">
					{metrics ? metrics.totalValidatedKm.toFixed(1) : '0.0'} <span class="text-lg font-bold text-purple-300">km</span>
				</div>
			</div>

			<!-- Mini Radial Progress Gauge -->
			<div class="relative w-16 h-16 shrink-0 flex items-center justify-center">
				<svg class="w-full h-full transform -rotate-90" viewBox="0 0 80 80">
					<circle
						cx="40"
						cy="40"
						r={radius}
						stroke="rgba(168, 85, 247, 0.15)"
						stroke-width="6"
						fill="transparent"
					/>
					<circle
						cx="40"
						cy="40"
						r={radius}
						stroke="url(#purpleCyanGrad)"
						stroke-width="6"
						stroke-dasharray={circumference}
						stroke-dashoffset={strokeDashoffset}
						stroke-linecap="round"
						fill="transparent"
						class="transition-all duration-1000 ease-out"
					/>
					<defs>
						<linearGradient id="purpleCyanGrad" x1="0%" y1="0%" x2="100%" y2="100%">
							<stop offset="0%" stop-color="#820ad1" />
							<stop offset="50%" stop-color="#a855f7" />
							<stop offset="100%" stop-color="#00f0ff" />
						</linearGradient>
					</defs>
				</svg>
				<div class="absolute inset-0 flex flex-col items-center justify-center text-center">
					<span class="text-[11px] font-mono font-black text-white">{integrityPercentage}%</span>
					<span class="text-[7px] font-mono uppercase text-purple-300">{$t.kpis.intact}</span>
				</div>
			</div>
		</div>

		<!-- Footer Breakdown -->
		<div class="relative z-10 mt-3 pt-3 border-t border-purple-500/15 flex items-center justify-between text-xs font-mono">
			<span class="text-slate-400 text-[11px]">
				{interpolate($t.kpis.watchKm, { km: metrics ? metrics.totalReportedKm.toFixed(1) : '0.0' })}
			</span>
			<span class="text-[10px] px-2 py-0.5 rounded-full bg-purple-500/15 border border-purple-500/30 text-purple-300 font-bold">
				{$t.kpis.haversineActive}
			</span>
		</div>
	</div>

	<!-- CARD 2: DISPÊNDIO METABÓLICO & CALORIAS (Cyber Pink & Lilac) -->
	<div class="cyber-card rounded-2xl p-5 relative overflow-hidden group hover:border-pink-500/40">
		<div class="absolute -top-12 -right-12 w-32 h-32 bg-cyber-pink/15 rounded-full blur-xl pointer-events-none"></div>

		<div class="relative z-10 flex items-start justify-between">
			<div>
				<span class="text-[11px] font-mono font-bold tracking-wider uppercase text-purple-300/80 flex items-center gap-1.5">
					<Flame class="w-3.5 h-3.5 text-cyber-pink" />
					<span>{$t.kpis.metabolicExpenditure}</span>
				</span>
				<div class="mt-2 text-3xl font-black font-mono text-white text-glow-pink tracking-tight">
					{metrics ? metrics.totalCaloriesBurned.toLocaleString() : '0'} <span class="text-lg font-bold text-pink-300">kcal</span>
				</div>
			</div>

			<div class="p-2.5 rounded-xl bg-pink-500/10 border border-pink-500/30 text-cyber-pink shadow-[0_0_12px_rgba(255,0,122,0.3)]">
				<Zap class="w-5 h-5" />
			</div>
		</div>

		<!-- Visual 5-segment LED Energy Gauge -->
		<div class="relative z-10 mt-3 space-y-1.5">
			<div class="flex items-center gap-1">
				{#each [1, 2, 3, 4, 5] as bar}
					<div
						class="h-1.5 flex-1 rounded-full transition-all duration-500 {bar <= 4 ? 'bg-gradient-to-r from-nubank-600 to-cyber-pink shadow-[0_0_6px_#ff007a]' : 'bg-slate-800'}"
					></div>
				{/each}
			</div>

			<div class="pt-1.5 flex items-center justify-between text-xs font-mono">
				<span class="text-slate-400 text-[11px]">
					{interpolate($t.kpis.avgWorkoutKcal, { kcal: avgCaloriesPerWorkout })}
				</span>
				<span class="text-[10px] px-2 py-0.5 rounded-full bg-pink-500/15 border border-pink-500/30 text-pink-300 font-bold">
					{$t.kpis.activeRate}
				</span>
			</div>
		</div>
	</div>

	<!-- CARD 3: BIO-TELEMETRIA & FREQUÊNCIA CARDÍACA (Cyber Cyan & Nubank Lilac) -->
	<div class="cyber-card rounded-2xl p-5 relative overflow-hidden group hover:border-cyan-400/50">
		<div class="absolute -top-12 -right-12 w-32 h-32 bg-cyber-cyan/15 rounded-full blur-xl pointer-events-none"></div>

		<div class="relative z-10 flex items-start justify-between">
			<div>
				<span class="text-[11px] font-mono font-bold tracking-wider uppercase text-purple-300/80 flex items-center gap-1.5">
					<Heart class="w-3.5 h-3.5 text-rose-400" />
					<span>{$t.kpis.bioFrequency}</span>
				</span>
				<div class="mt-2 text-3xl font-black font-mono text-white text-glow-cyan tracking-tight">
					{metrics ? metrics.overallAvgHeartRate : 0} <span class="text-lg font-bold text-cyber-cyan">bpm</span>
				</div>
			</div>

			<div class="p-2.5 rounded-xl bg-cyan-500/10 border border-cyan-500/30 text-cyber-cyan shadow-[0_0_12px_rgba(0,240,255,0.3)]">
				<Activity class="w-5 h-5 animate-pulse" />
			</div>
		</div>

		<!-- Animated Futuristic ECG Waveform -->
		<div class="relative z-10 mt-2">
			<svg viewBox="0 0 160 28" class="w-full h-7 overflow-visible">
				<path
					d="M0,14 L30,14 L38,4 L44,24 L52,8 L58,18 L64,14 L100,14 L108,4 L114,24 L122,8 L128,18 L134,14 L160,14"
					fill="none"
					stroke="url(#ecgGradient)"
					stroke-width="2"
					stroke-linecap="round"
					stroke-linejoin="round"
					class="animate-ecg"
				/>
				<defs>
					<linearGradient id="ecgGradient" x1="0%" y1="0%" x2="100%" y2="0%">
						<stop offset="0%" stop-color="#820ad1" />
						<stop offset="50%" stop-color="#00f0ff" />
						<stop offset="100%" stop-color="#ff007a" />
					</linearGradient>
				</defs>
			</svg>

			<div class="pt-1 flex items-center justify-between text-xs font-mono">
				<span class="text-slate-400 text-[11px]">
					{interpolate($t.kpis.peakBpm, { bpm: maxHeartRateOverall > 0 ? maxHeartRateOverall : 178 })}
				</span>
				<span class="text-[10px] px-2 py-0.5 rounded-full bg-cyan-500/15 border border-cyan-500/30 text-cyber-cyan font-bold">
					{$t.kpis.zone3Aerobic}
				</span>
			</div>
		</div>
	</div>

	<!-- CARD 4: AUDITORIA NEURAL DE INTEGRIDADE (Neon Emerald & Lilac) -->
	<div class="cyber-card rounded-2xl p-5 relative overflow-hidden group hover:border-emerald-400/50">
		<div class="absolute -top-12 -right-12 w-32 h-32 bg-cyber-emerald/15 rounded-full blur-xl pointer-events-none"></div>

		<div class="relative z-10 flex items-start justify-between">
			<div>
				<span class="text-[11px] font-mono font-bold tracking-wider uppercase text-purple-300/80 flex items-center gap-1.5">
					<ShieldCheck class="w-3.5 h-3.5 text-cyber-emerald" />
					<span>{$t.kpis.neuralAudit}</span>
				</span>
				<div class="mt-2 text-3xl font-black font-mono text-white text-glow-emerald tracking-tight">
					{validCount} <span class="text-lg font-bold text-slate-400">/ {totalActivities}</span>
				</div>
			</div>

			<div class="p-2.5 rounded-xl bg-emerald-500/10 border border-emerald-500/30 text-cyber-emerald shadow-[0_0_12px_rgba(0,255,163,0.3)]">
				<Radio class="w-5 h-5 animate-pulse" />
			</div>
		</div>

		<!-- Status Pills Breakdown -->
		<div class="relative z-10 mt-3 pt-3 border-t border-purple-500/15 flex items-center justify-between text-xs font-mono">
			<div class="flex items-center gap-1.5">
				<span class="inline-block w-2 h-2 rounded-full bg-cyber-emerald shadow-[0_0_6px_#00ffa3]"></span>
				<span class="text-[11px] text-cyber-emerald font-bold">{interpolate($t.kpis.validCount, { count: validCount })}</span>
			</div>
			<div class="flex items-center gap-1.5">
				<span class="inline-block w-2 h-2 rounded-full bg-amber-400 shadow-[0_0_6px_#ffb800]"></span>
				<span class="text-[11px] text-amber-300 font-bold">{interpolate($t.kpis.suspectCount, { count: suspectCount })}</span>
			</div>
			<div class="flex items-center gap-1.5">
				<span class="inline-block w-2 h-2 rounded-full bg-cyber-pink shadow-[0_0_6px_#ff007a]"></span>
				<span class="text-[11px] text-cyber-pink font-bold">{interpolate($t.kpis.rejectedCount, { count: rejectedCount })}</span>
			</div>
		</div>
	</div>
</section>

<!-- ========================================================================= -->
<!-- 2. ANALYTICS HUB GRID: PIE CHART + INFOGRAPHIC PERFORMANCE TELEMETRY      -->
<!-- ========================================================================= -->
<section class="grid grid-cols-1 lg:grid-cols-12 gap-6 items-stretch">
	<!-- LEFT COLUMN: HOLOGRAPHIC DONUT / PIE CHART (5 cols on desktop) -->
	<div class="lg:col-span-5 flex flex-col">
		<PieChart
			{validCount}
			{suspectCount}
			{rejectedCount}
			activities={metrics?.recentActivities ?? []}
		/>
	</div>

	<!-- RIGHT COLUMN: INFOGRAPHIC PERFORMANCE & TELEMETRY HUB (7 cols on desktop) -->
	<div class="lg:col-span-7 cyber-card rounded-2xl p-5 flex flex-col justify-between relative overflow-hidden group">
		<!-- Background ambient lights -->
		<div class="absolute -top-16 -right-16 w-56 h-56 bg-gradient-to-br from-nubank-600/20 via-purple-500/10 to-transparent rounded-full blur-2xl pointer-events-none"></div>

		<!-- Header -->
		<div class="relative z-10 flex items-center justify-between pb-3 border-b border-purple-500/15">
			<div class="flex items-center gap-2.5">
				<div class="w-8 h-8 rounded-lg bg-nubank-600/20 border border-purple-500/30 flex items-center justify-center text-purple-300 shadow-[0_0_12px_rgba(130,10,209,0.3)]">
					<Cpu class="w-4 h-4 text-purple-300" />
				</div>
				<div>
					<h4 class="text-sm font-bold text-white tracking-wide font-sans flex items-center gap-2">
						<span>{$t.analytics.globalTelemetry}</span>
						<span class="text-[10px] font-mono px-2 py-0.5 rounded-full bg-cyan-500/10 border border-cyan-500/30 text-cyber-cyan font-bold">
							{$t.analytics.online}
						</span>
					</h4>
					<p class="text-[11px] text-purple-300/70 font-mono">
						{$t.analytics.sensorsAuditDesc}
					</p>
				</div>
			</div>

			<div class="hidden sm:flex items-center gap-1.5 text-[11px] font-mono text-purple-300 bg-slate-950/70 px-2.5 py-1 rounded-lg border border-purple-500/20">
				<TrendingUp class="w-3.5 h-3.5 text-cyber-emerald" />
				<span>{interpolate($t.analytics.overallScore, { score: reliabilityPercentage })}</span>
			</div>
		</div>

		<!-- Central Infographic Bars & Gauges -->
		<div class="relative z-10 my-4 space-y-4">
			<!-- Dual Bar 1: KM Validado vs Reportado -->
			<div class="p-3.5 rounded-xl bg-slate-950/60 border border-purple-500/15">
				<div class="flex items-center justify-between text-xs font-mono mb-2">
					<span class="text-slate-300 font-semibold flex items-center gap-1.5">
						<Route class="w-3.5 h-3.5 text-purple-400" />
						{$t.analytics.trajectoryFidelity}
					</span>
					<span class="text-cyber-cyan font-bold">
						{totalValidatedKm.toFixed(1)} km <span class="text-slate-400">/ {totalReportedKm.toFixed(1)} km</span>
					</span>
				</div>

				<!-- Dual progress bar track -->
				<div class="h-3 w-full bg-slate-900 rounded-full overflow-hidden p-0.5 border border-purple-500/20 flex gap-0.5">
					<div
						class="h-full rounded-full bg-gradient-to-r from-nubank-600 via-purple-500 to-cyber-cyan transition-all duration-1000 shadow-[0_0_10px_rgba(0,240,255,0.5)]"
						style="width: {integrityPercentage}%;"
					></div>
				</div>

				<div class="flex items-center justify-between text-[10px] font-mono text-slate-400 mt-1.5">
					<span>{$t.analytics.elevationCorrection}</span>
					<span class="text-purple-300 font-bold">{interpolate($t.analytics.percentIntact, { pct: integrityPercentage })}</span>
				</div>
			</div>

			<!-- 4 Mini Metrics HUD Grid -->
			<div class="grid grid-cols-2 sm:grid-cols-4 gap-2.5">
				<!-- Tempo Total -->
				<div class="p-3 rounded-xl bg-slate-950/50 border border-purple-500/15">
					<div class="flex items-center gap-1 text-[10px] font-mono uppercase text-purple-300/80 mb-1">
						<Clock class="w-3 h-3 text-purple-400" /> {$t.analytics.timeInAction}
					</div>
					<div class="text-lg font-black font-mono text-white">
						{totalHoursFormatted}
					</div>
					<div class="text-[10px] text-slate-400 font-mono">{$t.analytics.totalDuration}</div>
				</div>

				<!-- Ritmo Médio Geral -->
				<div class="p-3 rounded-xl bg-slate-950/50 border border-purple-500/15">
					<div class="flex items-center gap-1 text-[10px] font-mono uppercase text-purple-300/80 mb-1">
						<Gauge class="w-3 h-3 text-cyber-cyan" /> {$t.analytics.avgSpeed}
					</div>
					<div class="text-lg font-black font-mono text-cyber-cyan">
						{avgSpeedOverall} <span class="text-xs text-slate-400">km/h</span>
					</div>
					<div class="text-[10px] text-slate-400 font-mono">{$t.analytics.routeGeneral}</div>
				</div>

				<!-- Batimento Cardíaco de Pico -->
				<div class="p-3 rounded-xl bg-slate-950/50 border border-purple-500/15">
					<div class="flex items-center gap-1 text-[10px] font-mono uppercase text-purple-300/80 mb-1">
						<Heart class="w-3 h-3 text-rose-400" /> {$t.analytics.cardiacPeak}
					</div>
					<div class="text-lg font-black font-mono text-rose-400">
						{maxHeartRateOverall > 0 ? maxHeartRateOverall : 178} <span class="text-xs text-slate-400">bpm</span>
					</div>
					<div class="text-[10px] text-slate-400 font-mono">{$t.analytics.maxEffort}</div>
				</div>

				<!-- Dispositivos Conectados -->
				<div class="p-3 rounded-xl bg-slate-950/50 border border-purple-500/15">
					<div class="flex items-center gap-1 text-[10px] font-mono uppercase text-purple-300/80 mb-1">
						<Compass class="w-3 h-3 text-cyber-emerald" /> {$t.analytics.wearable}
					</div>
					<div class="text-sm font-bold font-mono text-white truncate" title="Xiaomi Band 10 / Mi Fitness">
						Band 10 & GPX
					</div>
					<div class="text-[10px] text-cyber-emerald font-mono">{$t.analytics.synchronized}</div>
				</div>
			</div>
		</div>

		<!-- Footer Info -->
		<div class="relative z-10 pt-3 border-t border-purple-500/15 flex flex-col sm:flex-row sm:items-center justify-between gap-2 text-[11px] font-mono text-purple-300/80">
			<div class="flex items-center gap-2">
				<span class="w-2 h-2 rounded-full bg-cyber-emerald animate-pulse"></span>
				<span>{$t.analytics.fraudProtocol}</span>
			</div>
			<span class="text-slate-400">{$t.analytics.cryptoSecurity}</span>
		</div>
	</div>
</section>
