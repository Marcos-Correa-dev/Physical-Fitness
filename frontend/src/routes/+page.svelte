<script lang="ts">
	import { onMount } from 'svelte';
	import type { DashboardMetrics, ActivitySummary } from '$lib/types/activity';
	import {
		fetchDashboardMetrics,
		fetchActivityDetails,
		revalidateActivity
	} from '$lib/services/api';
	import DashboardHeader from '$lib/components/DashboardHeader.svelte';
	import InfographicOverview from '$lib/components/InfographicOverview.svelte';
	import RouteMap from '$lib/components/RouteMap.svelte';
	import ValidationBadge from '$lib/components/ValidationBadge.svelte';
	import ActivityAuditModal from '$lib/components/ActivityAuditModal.svelte';
	import BluetoothConnectModal from '$lib/components/BluetoothConnectModal.svelte';
	import ImportWorkoutModal from '$lib/components/ImportWorkoutModal.svelte';
	import {
		locale,
		t,
		translateActivityType,
		formatDate,
		formatTime,
		formatDayOfWeek,
		interpolate
	} from '$lib/i18n';
	import {
		Activity,
		Flame,
		Heart,
		Compass,
		Route,
		ShieldCheck,
		Watch,
		Clock,
		Calendar,
		Eye,
		RotateCw,
		CheckCircle,
		AlertCircle,
		XCircle,
		Layers,
		Dumbbell,
		Bluetooth,
		UploadCloud,
		Zap,
		ChevronRight,
		BarChart3
	} from 'lucide-svelte';

	let metrics: DashboardMetrics | null = null;
	let selectedActivity: ActivitySummary | null = null;
	let auditModalActivity: ActivitySummary | null = null;
	let isImportModalOpen = false;
	let isBluetoothOpen = false;
	let connectedBluetoothDevice: string | null = null;
	let isLoading = true;
	let isRefreshing = false;
	let errorMessage = '';

	async function loadData() {
		try {
			errorMessage = '';
			metrics = await fetchDashboardMetrics();
			if (metrics.recentActivities.length > 0) {
				if (!selectedActivity) {
					// Priorizar atividade com pontos GPS reais para abrir o mapa diretamente no trajeto
					const activityWithGps = metrics.recentActivities.find((a) => a.routePoints && a.routePoints.length > 0);
					selectedActivity = activityWithGps || metrics.recentActivities[0];
				} else {
					const updated = metrics.recentActivities.find((a) => a.id === selectedActivity?.id);
					if (updated) {
						selectedActivity = updated;
					} else {
						const activityWithGps = metrics.recentActivities.find((a) => a.routePoints && a.routePoints.length > 0);
						selectedActivity = activityWithGps || metrics.recentActivities[0];
					}
				}
			} else {
				selectedActivity = null;
			}
		} catch (err: any) {
			console.error(err);
			errorMessage = $t.app.backendError;
		} finally {
			isLoading = false;
			isRefreshing = false;
		}
	}

	async function handleRefresh() {
		isRefreshing = true;
		await loadData();
	}

	function handleSelectActivity(act: ActivitySummary) {
		selectedActivity = act;
	}

	function handleOpenAudit(act: ActivitySummary) {
		auditModalActivity = act;
	}

	async function handleRevalidate(id: string) {
		const updated = await revalidateActivity(id);
		auditModalActivity = updated;
		if (selectedActivity?.id === id) {
			selectedActivity = updated;
		}
		await loadData();
	}

	onMount(() => {
		loadData();
		// Polling suave a cada 20 segundos
		const interval = setInterval(loadData, 20000);
		return () => clearInterval(interval);
	});
</script>

<svelte:head>
	<title>{$t.app.pageTitle}</title>
</svelte:head>

<!-- Header -->
<DashboardHeader
	onOpenImportModal={() => (isImportModalOpen = true)}
	onOpenBluetooth={() => (isBluetoothOpen = true)}
	onRefresh={handleRefresh}
	{isRefreshing}
	bluetoothConnectedDevice={connectedBluetoothDevice}
/>

<main class="flex-1 max-w-7xl w-full mx-auto p-4 sm:p-6 space-y-6">
	<!-- Error Banner if Backend is down -->
	{#if errorMessage}
		<div class="p-4 rounded-2xl bg-pink-950/40 border border-pink-700/60 text-pink-200 text-sm flex items-center justify-between shadow-lg">
			<div class="flex items-center gap-3">
				<AlertCircle class="w-5 h-5 text-cyber-pink shrink-0 animate-pulse" />
				<span>{errorMessage}</span>
			</div>
			<button
				on:click={loadData}
				class="px-3.5 py-1.5 bg-pink-900/60 hover:bg-pink-800 rounded-xl text-xs font-mono font-bold text-white transition-all cursor-pointer border border-pink-500/40"
			>
				{$t.app.retry}
			</button>
		</div>
	{/if}

	<!-- ======================================================== -->
	<!-- INFOGRAPHIC SUITE & PIE CHART GRID                       -->
	<!-- Totalized metrics as infographics + Pie Chart together   -->
	<!-- ======================================================== -->
	<InfographicOverview {metrics} />

	<!-- ======================================================== -->
	<!-- MAIN GRID: INTERACTIVE ROUTE MAP + TELEMETRY INSPECTOR   -->
	<!-- ======================================================== -->
	<section class="grid grid-cols-1 lg:grid-cols-3 gap-6">
		<!-- Map Viewer (2 columns on large screens) -->
		<div class="lg:col-span-2 rounded-2xl overflow-hidden border border-purple-500/25 shadow-xl shadow-purple-950/20 bg-slate-950/80">
			<RouteMap
				routePoints={selectedActivity?.routePoints ?? []}
				activityTitle={selectedActivity ? `${selectedActivity.title} (${selectedActivity.reportedDistanceKm} km)` : $t.routeMap.route}
				status={selectedActivity?.status ?? 'Valid'}
				activityType={selectedActivity?.activityType ?? 'Corrida'}
			/>
		</div>

		<!-- Selected Activity Telemetry Card -->
		<div class="cyber-card rounded-2xl p-5 shadow-xl flex flex-col justify-between relative overflow-hidden group">
			<!-- Subtle corner lilac glow -->
			<div class="absolute -top-12 -right-12 w-36 h-36 bg-nubank-600/15 rounded-full blur-xl pointer-events-none"></div>

			{#if selectedActivity}
				<div class="relative z-10">
					<div class="flex items-center justify-between pb-3 border-b border-purple-500/15">
						<span class="text-xs font-mono uppercase tracking-wider text-purple-300 font-bold flex items-center gap-1.5">
							<Zap class="w-3.5 h-3.5 text-cyber-cyan" />
							<span>{$t.details.workoutDetails}</span>
						</span>
						<ValidationBadge status={selectedActivity.status} size="sm" />
					</div>

					<div class="mt-4">
						<div class="flex items-center gap-2">
							{#if selectedActivity.activityType === 'Musculação'}
								<Dumbbell class="w-5 h-5 text-amber-400" />
							{:else}
								<Activity class="w-5 h-5 text-purple-400" />
							{/if}
							<h3 class="text-lg font-black text-white tracking-wide">{selectedActivity.title}</h3>
						</div>
						<div class="flex items-center gap-2 mt-1 text-xs text-purple-300/70 font-mono">
							<Watch class="w-3.5 h-3.5 text-cyber-cyan" />
							<span>{selectedActivity.deviceName}</span>
							<span class="text-purple-500/40">•</span>
							<span class="text-white font-bold">{translateActivityType(selectedActivity.activityType, $locale)}</span>
						</div>
					</div>

					<!-- Date, Time and Day of Week HUD -->
					<div class="mt-4 p-3 bg-slate-950/70 rounded-xl border border-purple-500/15 text-xs font-mono space-y-1.5">
						<div class="flex items-center justify-between">
							<span class="text-slate-400 flex items-center gap-1.5">
								<Calendar class="w-3.5 h-3.5 text-purple-400" /> {$t.details.dayAndDate}
							</span>
							<span class="text-white capitalize font-semibold">
								{formatDayOfWeek(selectedActivity.startTime || selectedActivity.dayOfWeek, $locale)}, {formatDate(selectedActivity.startTime || selectedActivity.formattedDate, $locale)}
							</span>
						</div>
						<div class="flex items-center justify-between">
							<span class="text-slate-400 flex items-center gap-1.5">
								<Clock class="w-3.5 h-3.5 text-purple-400" /> {$t.details.timeAndDuration}
							</span>
							<span class="text-cyber-cyan font-bold">
								{formatTime(selectedActivity.startTime || selectedActivity.formattedTime, $locale, false)} ({selectedActivity.formattedDuration})
							</span>
						</div>
					</div>

					<!-- Key Numbers Grid with Nubank & Futuristic Accents -->
					<div class="grid grid-cols-2 gap-2 mt-3.5">
						<div class="p-2.5 rounded-xl bg-slate-950/60 border border-purple-500/15 hover:border-emerald-500/30 transition-colors">
							<span class="text-[10px] font-mono uppercase text-purple-300/80 block">
								{selectedActivity.activityType === 'Musculação' ? $t.details.distanceIndoor : $t.details.validatedKm}
							</span>
							<span class="text-base font-black font-mono text-cyber-emerald">
								{selectedActivity.validatedDistanceKm} km
							</span>
							<span class="text-[10px] text-slate-400 block font-mono">
								{selectedActivity.activityType === 'Musculação' ? $t.details.staticGym : interpolate($t.details.ofKm, { km: selectedActivity.reportedDistanceKm })}
							</span>
						</div>

						<div class="p-2.5 rounded-xl bg-slate-950/60 border border-purple-500/15 hover:border-pink-500/30 transition-colors">
							<span class="text-[10px] font-mono uppercase text-purple-300/80 block">{$t.details.caloriesBurned}</span>
							<span class="text-base font-black font-mono text-cyber-pink">{selectedActivity.caloriesBurned} kcal</span>
							<span class="text-[10px] text-slate-400 block font-mono">{$t.details.energyExpenditure}</span>
						</div>

						<div class="p-2.5 rounded-xl bg-slate-950/60 border border-purple-500/15 hover:border-cyan-500/30 transition-colors">
							<span class="text-[10px] font-mono uppercase text-purple-300/80 block">
								{selectedActivity.activityType === 'Musculação' ? $t.details.metabolicFocus : $t.details.avgPace}
							</span>
							<span class="text-base font-black font-mono text-white">
								{selectedActivity.activityType === 'Musculação' ? $t.details.hypertrophy : selectedActivity.formattedPace}
							</span>
							<span class="text-[10px] text-slate-400 block font-mono">
								{selectedActivity.activityType === 'Musculação' ? $t.details.anaerobicDemand : `${selectedActivity.averageSpeedKmh} km/h`}
							</span>
						</div>

						<div class="p-2.5 rounded-xl bg-slate-950/60 border border-purple-500/15 hover:border-purple-400/40 transition-colors">
							<span class="text-[10px] font-mono uppercase text-purple-300/80 block">{$t.details.heartRate}</span>
							<span class="text-base font-black font-mono text-rose-400">{selectedActivity.averageHeartRateBpm} bpm</span>
							<span class="text-[10px] text-slate-400 block font-mono">{interpolate($t.details.peakHeartRate, { bpm: selectedActivity.maxHeartRateBpm })}</span>
						</div>
					</div>

					<!-- Validation Engine Summary -->
					<div class="mt-3.5 p-3 rounded-xl bg-slate-950/70 border border-purple-500/20 text-xs">
						<span class="text-[10px] font-mono uppercase tracking-wider text-purple-300 font-bold block mb-1">
							{$t.details.analysisReportTitle}
						</span>
						<p class="text-slate-300 leading-relaxed font-sans text-[11px]">{selectedActivity.validationSummary}</p>
					</div>
				</div>

				<div class="relative z-10 mt-4 pt-4 border-t border-purple-500/15 flex items-center gap-2">
					<button
						type="button"
						on:click={() => handleOpenAudit(selectedActivity)}
						class="w-full py-2.5 px-3 rounded-xl text-xs font-bold text-white bg-gradient-to-r from-nubank-600 via-purple-600 to-indigo-600 hover:from-purple-600 hover:to-nubank-600 border border-purple-400/30 shadow-[0_0_15px_rgba(130,10,209,0.3)] hover:shadow-[0_0_20px_rgba(130,10,209,0.5)] flex items-center justify-center gap-2 transition-all cursor-pointer"
					>
						<ShieldCheck class="w-4 h-4 text-cyber-cyan" />
						<span>{$t.details.openAuditReport}</span>
					</button>
				</div>
			{:else}
				<div class="h-full flex flex-col items-center justify-center text-center p-6 text-purple-300/60">
					<Layers class="w-8 h-8 mb-2 animate-bounce" />
					<p class="text-xs font-mono">{$t.details.selectToInspect}</p>
				</div>
			{/if}
		</div>
	</section>

	<!-- ======================================================== -->
	<!-- ACTIVITIES TABLE SECTION (HISTÓRICO DE ATIVIDADES)       -->
	<!-- ======================================================== -->
	<section class="cyber-card rounded-2xl p-5 shadow-xl relative overflow-hidden">
		<!-- Subtle ambient purple sheen -->
		<div class="absolute -top-16 -left-16 w-48 h-48 bg-nubank-600/10 rounded-full blur-2xl pointer-events-none"></div>

		<div class="relative z-10 flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-purple-500/15">
			<div>
				<h3 class="text-base font-bold text-white flex items-center gap-2">
					<span>{$t.table.title}</span>
					<span class="inline-block w-2 h-2 rounded-full bg-nubank-400 shadow-[0_0_8px_#c084fc]"></span>
				</h3>
				<p class="text-xs text-purple-300/70 font-sans">
					{$t.table.subtitle}
				</p>
			</div>

			<div class="flex items-center gap-2 text-xs font-mono text-purple-300 bg-slate-950/70 px-3 py-1.5 rounded-xl border border-purple-500/20">
				<span>{interpolate($t.table.totalCount, { count: metrics?.totalActivitiesCount ?? 0 })}</span>
			</div>
		</div>

		<!-- Table -->
		<div class="relative z-10 overflow-x-auto mt-4">
			<table class="w-full text-left text-xs font-sans">
				<thead>
					<tr class="border-b border-purple-500/20 text-purple-300 font-mono text-[11px] uppercase tracking-wider bg-slate-950/40">
						<th class="py-3 px-3">{$t.table.colDateTime}</th>
						<th class="py-3 px-3">{$t.table.colActivityDevice}</th>
						<th class="py-3 px-3 text-right">{$t.table.colWatchKm}</th>
						<th class="py-3 px-3 text-right">{$t.table.colValidatedKm}</th>
						<th class="py-3 px-3 text-right">{$t.table.colPaceFocus}</th>
						<th class="py-3 px-3 text-right">{$t.table.colCalories}</th>
						<th class="py-3 px-3 text-center">{$t.table.colHeartRate}</th>
						<th class="py-3 px-3 text-center">{$t.table.colStatus}</th>
						<th class="py-3 px-3 text-center">{$t.table.colActions}</th>
					</tr>
				</thead>
				<tbody class="divide-y divide-purple-500/10">
					{#if metrics && metrics.recentActivities.length > 0}
						{#each metrics.recentActivities as act}
							<tr
								class="hover:bg-purple-950/25 transition-all cursor-pointer {selectedActivity?.id === act.id ? 'bg-purple-950/40 border-l-2 border-l-nubank-500 shadow-[inset_0_0_15px_rgba(130,10,209,0.15)]' : ''}"
								on:click={() => handleSelectActivity(act)}
							>
								<!-- Dia / Data / Hora -->
								<td class="py-3 px-3 font-mono">
									<div class="font-bold text-slate-200 capitalize">{formatDayOfWeek(act.startTime || act.dayOfWeek, $locale)}</div>
									<div class="text-[11px] text-purple-300/70">
										{formatDate(act.startTime || act.formattedDate, $locale)} {$t.table.atTime} {formatTime(act.startTime || act.formattedTime, $locale, false)}
									</div>
								</td>

								<!-- Atividade & Dispositivo -->
								<td class="py-3 px-3">
									<div class="font-bold text-white flex items-center gap-1.5">
										{#if act.activityType === 'Musculação'}
											<Dumbbell class="w-3.5 h-3.5 text-amber-400" />
										{:else}
											<Activity class="w-3.5 h-3.5 text-purple-400" />
										{/if}
										<span>{act.title}</span>
									</div>
									<div class="text-[11px] text-slate-400 font-mono flex items-center gap-1 mt-0.5">
										<Watch class="w-3 h-3 text-cyber-cyan" />
										<span>{act.deviceName}</span>
										<span class="text-purple-500/40">•</span>
										<span class="text-purple-300/80">{act.formattedDuration}</span>
									</div>
								</td>

								<!-- KM Relógio -->
								<td class="py-3 px-3 text-right font-mono font-bold text-slate-200">
									{act.activityType === 'Musculação' ? '0.00 km' : `${act.reportedDistanceKm.toFixed(2)} km`}
								</td>

								<!-- KM Validado -->
								<td class="py-3 px-3 text-right font-mono font-bold {act.status === 'Valid' ? 'text-cyber-emerald' : act.status === 'Suspect' ? 'text-amber-300' : 'text-cyber-pink'}">
									{act.activityType === 'Musculação' ? '0.00 km' : `${act.validatedDistanceKm.toFixed(2)} km`}
								</td>

								<!-- Ritmo / Foco -->
								<td class="py-3 px-3 text-right font-mono">
									{#if act.activityType === 'Musculação'}
										<div class="text-amber-300 font-medium">{$t.table.hypertrophy}</div>
										<div class="text-[10px] text-slate-500">{$t.table.indoor}</div>
									{:else}
										<div class="text-slate-200 font-medium">{act.formattedPace}</div>
										<div class="text-[10px] text-purple-300/70">{act.averageSpeedKmh} km/h</div>
									{/if}
								</td>

								<!-- Calorias -->
								<td class="py-3 px-3 text-right font-mono font-bold text-cyber-pink">
									{act.caloriesBurned} kcal
								</td>

								<!-- Batimentos -->
								<td class="py-3 px-3 text-center font-mono">
									<div class="inline-flex items-center gap-1 text-slate-200 font-bold">
										<Heart class="w-3 h-3 text-rose-500 shrink-0" />
										<span>{act.averageHeartRateBpm}</span>
									</div>
									<div class="text-[10px] text-slate-500">{$t.table.max} {act.maxHeartRateBpm}</div>
								</td>

								<!-- Status -->
								<td class="py-3 px-3 text-center">
									<ValidationBadge status={act.status} size="sm" />
								</td>

								<!-- Ações -->
								<td class="py-3 px-3 text-center" on:click|stopPropagation>
									<div class="flex items-center justify-center gap-1.5">
										<button
											type="button"
											on:click={() => handleSelectActivity(act)}
											class="p-1.5 rounded-lg bg-slate-950/80 hover:bg-purple-950 text-purple-300 hover:text-white border border-purple-500/20 transition-all cursor-pointer shadow-sm"
											title={$t.table.showMapTooltip}
										>
											<Compass class="w-3.5 h-3.5 text-cyber-cyan" />
										</button>

										<button
											type="button"
											on:click={() => handleOpenAudit(act)}
											class="p-1.5 rounded-lg bg-slate-950/80 hover:bg-purple-950 text-purple-300 hover:text-white border border-purple-500/20 transition-all cursor-pointer shadow-sm"
											title={$t.table.showAuditTooltip}
										>
											<ShieldCheck class="w-3.5 h-3.5 text-cyber-emerald" />
										</button>
									</div>
								</td>
							</tr>
						{/each}
					{:else}
						<tr>
							<td colspan="9" class="py-12 text-center">
								<div class="max-w-md mx-auto space-y-3">
									<div class="w-12 h-12 rounded-full bg-purple-950/40 border border-purple-500/20 text-purple-400 flex items-center justify-center mx-auto shadow-lg shadow-purple-900/20">
										<Watch class="w-6 h-6" />
									</div>
									<h4 class="text-sm font-bold text-white">{$t.table.emptyTitle}</h4>
									<p class="text-xs text-purple-300/70 leading-relaxed font-sans">
										{$t.table.emptyDesc}
									</p>
									<div class="flex items-center justify-center gap-3 pt-2">
										<button
											type="button"
											on:click={() => (isBluetoothOpen = true)}
											class="px-4 py-2 bg-gradient-to-r from-nubank-600 to-purple-600 hover:from-purple-600 hover:to-nubank-600 rounded-xl text-xs font-bold text-white transition-all cursor-pointer flex items-center gap-1.5 border border-purple-400/30 shadow-[0_0_15px_rgba(130,10,209,0.3)]"
										>
											<Bluetooth class="w-3.5 h-3.5" />
											<span>{$t.table.btnConnectBluetooth}</span>
										</button>

										<button
											type="button"
											on:click={() => (isImportModalOpen = true)}
											class="px-4 py-2 bg-gradient-to-r from-cyan-600 to-emerald-600 hover:from-cyan-500 hover:to-emerald-500 rounded-xl text-xs font-bold text-white transition-all cursor-pointer flex items-center gap-1.5 border border-cyan-400/30 shadow-[0_0_15px_rgba(0,240,255,0.3)]"
										>
											<UploadCloud class="w-3.5 h-3.5" />
											<span>{$t.table.btnImportGpx}</span>
										</button>
									</div>
								</div>
							</td>
						</tr>
					{/if}
				</tbody>
			</table>
		</div>
	</section>
</main>

<!-- Modals -->
{#if auditModalActivity}
	<ActivityAuditModal
		activity={auditModalActivity}
		onClose={() => (auditModalActivity = null)}
		onRevalidate={handleRevalidate}
	/>
{/if}

<ImportWorkoutModal
	isOpen={isImportModalOpen}
	onClose={() => (isImportModalOpen = false)}
	onImportSuccess={loadData}
/>

<BluetoothConnectModal
	isOpen={isBluetoothOpen}
	onClose={() => (isBluetoothOpen = false)}
	onDeviceConnected={(name) => (connectedBluetoothDevice = name || null)}
	onActivitySaved={loadData}
	onOpenImportModal={() => (isImportModalOpen = true)}
/>
