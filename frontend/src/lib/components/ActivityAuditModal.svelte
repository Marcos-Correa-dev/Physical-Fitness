<script lang="ts">
	import type { ActivitySummary } from '../types/activity';
	import ValidationBadge from './ValidationBadge.svelte';
	import { locale, t, translateActivityType, formatDate, formatTime, interpolate } from '$lib/i18n';
	import {
		X,
		ShieldAlert,
		ShieldCheck,
		CheckCircle2,
		AlertTriangle,
		XCircle,
		Watch,
		Heart,
		Flame,
		Gauge,
		RotateCw
	} from 'lucide-svelte';

	export let activity: ActivitySummary | null = null;
	export let onClose: () => void;
	export let onRevalidate: (id: string) => Promise<void>;

	let isRevalidating = false;

	async function handleRevalidate() {
		if (!activity) return;
		isRevalidating = true;
		try {
			await onRevalidate(activity.id);
		} finally {
			isRevalidating = false;
		}
	}
</script>

{#if activity}
	<div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-sm animate-fade-in">
		<div class="relative w-full max-w-2xl bg-slate-900 border border-slate-800 rounded-2xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
			<!-- Header -->
			<div class="flex items-center justify-between p-5 border-b border-slate-800 bg-slate-950/50">
				<div class="flex items-center gap-3">
					<div class="p-2 rounded-xl bg-slate-800 text-cyan-400">
						<ShieldCheck class="w-6 h-6" />
					</div>
					<div>
						<h3 class="text-base font-bold text-white">{$t.auditModal.title}</h3>
						<p class="text-xs text-slate-400 font-mono">
							{interpolate($t.auditModal.id, { id: activity.id.slice(0, 8) })} • 
							{formatDate(activity.startTime || activity.formattedDate, $locale)} {$t.auditModal.atTime} {formatTime(activity.startTime || activity.formattedTime, $locale, false)}
						</p>
					</div>
				</div>

				<div class="flex items-center gap-3">
					<ValidationBadge status={activity.status} size="md" />
					<button
						type="button"
						on:click={onClose}
						class="text-slate-400 hover:text-white p-1 rounded-lg hover:bg-slate-800 transition-colors"
					>
						<X class="w-5 h-5" />
					</button>
				</div>
			</div>

			<!-- Body (Scrollable) -->
			<div class="p-6 space-y-6 overflow-y-auto">
				<!-- Title and Device Banner -->
				<div class="p-4 rounded-xl bg-slate-950/60 border border-slate-800/80 flex items-center justify-between">
					<div>
						<span class="text-xs font-mono uppercase text-slate-400">{translateActivityType(activity.activityType, $locale)}</span>
						<h4 class="text-lg font-bold text-white">{activity.title}</h4>
					</div>
					<div class="flex items-center gap-2 text-xs font-mono text-cyan-400 bg-cyan-950/40 border border-cyan-800/50 px-3 py-1.5 rounded-lg">
						<Watch class="w-4 h-4 text-cyan-400" />
						<span>{activity.deviceName}</span>
					</div>
				</div>

				<!-- Comparison Metrics Grid -->
				<div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
					<div class="bg-slate-950/40 border border-slate-800/60 p-3 rounded-xl">
						<span class="text-[10px] font-mono uppercase text-slate-400 block">{$t.auditModal.watchKm}</span>
						<span class="text-xl font-bold font-mono text-white">{activity.reportedDistanceKm.toFixed(2)} km</span>
					</div>

					<div class="bg-slate-950/40 border border-slate-800/60 p-3 rounded-xl">
						<span class="text-[10px] font-mono uppercase text-slate-400 block">{$t.auditModal.gpsKm}</span>
						<span class="text-xl font-bold font-mono text-cyan-400">{activity.calculatedGpsDistanceKm.toFixed(2)} km</span>
					</div>

					<div class="bg-slate-950/40 border border-slate-800/60 p-3 rounded-xl">
						<span class="text-[10px] font-mono uppercase text-slate-400 block">{$t.auditModal.approvedKm}</span>
						<span class="text-xl font-bold font-mono text-emerald-400">{activity.validatedDistanceKm.toFixed(2)} km</span>
					</div>

					<div class="bg-slate-950/40 border border-slate-800/60 p-3 rounded-xl">
						<span class="text-[10px] font-mono uppercase text-slate-400 block">{$t.auditModal.discrepancy}</span>
						<span class="text-xl font-bold font-mono {activity.discrepancyPercentage > 10 ? 'text-amber-400' : 'text-slate-300'}">
							{activity.discrepancyPercentage.toFixed(1)}%
						</span>
					</div>
				</div>

				<!-- Telemetry Secondary Stats -->
				<div class="grid grid-cols-3 gap-3">
					<div class="flex items-center gap-3 p-3 bg-slate-950/40 border border-slate-800/60 rounded-xl">
						<div class="p-2 rounded-lg bg-rose-500/10 text-rose-400">
							<Heart class="w-4 h-4" />
						</div>
						<div>
							<span class="text-[10px] font-mono uppercase text-slate-400 block">{$t.auditModal.heartRate}</span>
							<span class="text-xs font-mono font-semibold text-slate-200">
								{$t.auditModal.avg} <b class="text-white">{activity.averageHeartRateBpm}</b> • {$t.auditModal.max} <b class="text-white">{activity.maxHeartRateBpm}</b> bpm
							</span>
						</div>
					</div>

					<div class="flex items-center gap-3 p-3 bg-slate-950/40 border border-slate-800/60 rounded-xl">
						<div class="p-2 rounded-lg bg-amber-500/10 text-amber-400">
							<Flame class="w-4 h-4" />
						</div>
						<div>
							<span class="text-[10px] font-mono uppercase text-slate-400 block">{$t.auditModal.calories}</span>
							<span class="text-xs font-mono font-semibold text-white">{activity.caloriesBurned} kcal</span>
						</div>
					</div>

					<div class="flex items-center gap-3 p-3 bg-slate-950/40 border border-slate-800/60 rounded-xl">
						<div class="p-2 rounded-lg bg-cyan-500/10 text-cyan-400">
							<Gauge class="w-4 h-4" />
						</div>
						<div>
							<span class="text-[10px] font-mono uppercase text-slate-400 block">{$t.auditModal.paceAndSpeed}</span>
							<span class="text-xs font-mono font-semibold text-white">{activity.formattedPace} ({activity.averageSpeedKmh} km/h)</span>
						</div>
					</div>
				</div>

				<!-- Diagnostic Summary -->
				<div class="p-4 rounded-xl border {activity.status === 'Valid' ? 'bg-emerald-950/20 border-emerald-800/40 text-emerald-300' : activity.status === 'Suspect' ? 'bg-amber-950/20 border-amber-800/40 text-amber-300' : 'bg-rose-950/20 border-rose-800/40 text-rose-300'}">
					<div class="flex items-center gap-2 mb-1">
						{#if activity.status === 'Valid'}
							<CheckCircle2 class="w-4 h-4 text-emerald-400" />
						{:else if activity.status === 'Suspect'}
							<AlertTriangle class="w-4 h-4 text-amber-400" />
						{:else}
							<XCircle class="w-4 h-4 text-rose-400" />
						{/if}
						<span class="text-xs font-mono font-bold uppercase tracking-wider">{$t.auditModal.diagnosticTitle}</span>
					</div>
					<p class="text-xs font-sans leading-relaxed">{activity.validationSummary}</p>
				</div>

				<!-- Validation Rules Issues -->
				<div>
					<h5 class="text-xs font-mono uppercase tracking-wider text-slate-400 mb-3 flex items-center gap-2">
						<ShieldAlert class="w-4 h-4 text-slate-400" />
						<span>{$t.auditModal.criteriaTitle}</span>
					</h5>

					{#if activity.issues && activity.issues.length > 0}
						<div class="space-y-2">
							{#each activity.issues as issue}
								<div class="p-3 rounded-lg border text-xs {issue.severity === 'Critical' ? 'bg-rose-950/30 border-rose-900/60 text-rose-300' : issue.severity === 'Warning' ? 'bg-amber-950/30 border-amber-900/60 text-amber-300' : 'bg-slate-800/40 border-slate-700/60 text-slate-300'}">
									<div class="flex items-center justify-between font-mono font-semibold mb-1">
										<span>{interpolate($t.auditModal.rulePrefix, { rule: issue.ruleName })}</span>
										<span class="uppercase text-[10px] px-2 py-0.5 rounded bg-slate-900 border border-slate-700">
											{issue.severity}
										</span>
									</div>
									<p class="font-sans text-slate-200">{issue.message}</p>
									{#if issue.details}
										<p class="font-mono text-[11px] text-slate-400 mt-1">{issue.details}</p>
									{/if}
								</div>
							{/each}
						</div>
					{:else}
						<div class="p-4 rounded-xl bg-slate-950/40 border border-slate-800/80 text-xs text-slate-400 flex items-center gap-2 font-mono">
							<CheckCircle2 class="w-4 h-4 text-emerald-400" />
							<span>{$t.auditModal.noViolations}</span>
						</div>
					{/if}
				</div>
			</div>

			<!-- Footer -->
			<div class="p-4 border-t border-slate-800 bg-slate-950/60 flex items-center justify-between">
				<button
					type="button"
					on:click={handleRevalidate}
					disabled={isRevalidating}
					class="inline-flex items-center gap-2 px-4 py-2 text-xs font-semibold text-slate-200 bg-slate-800 hover:bg-slate-700 border border-slate-700 rounded-lg transition-colors cursor-pointer disabled:opacity-50"
				>
					<RotateCw class="w-3.5 h-3.5 {isRevalidating ? 'animate-spin text-cyan-400' : ''}" />
					<span>{isRevalidating ? $t.auditModal.revalidating : $t.auditModal.revalidateBtn}</span>
				</button>

				<button
					type="button"
					on:click={onClose}
					class="px-5 py-2 text-xs font-semibold text-slate-300 bg-slate-800 hover:bg-slate-700 rounded-lg transition-colors cursor-pointer"
				>
					{$t.auditModal.closeBtn}
				</button>
			</div>
		</div>
	</div>
{/if}
