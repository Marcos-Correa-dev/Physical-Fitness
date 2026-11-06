<script lang="ts">
	import { ShieldCheck, Activity, PieChart as PieIcon, Layers, Info } from 'lucide-svelte';
	import { locale, t, translateActivityType } from '$lib/i18n';

	export let validCount: number = 0;
	export let suspectCount: number = 0;
	export let rejectedCount: number = 0;
	export let activities: Array<{ activityType?: string; status?: string }> = [];

	type ChartMode = 'status' | 'type';
	let mode: ChartMode = 'status';

	interface Slice {
		id: string;
		label: string;
		value: number;
		color: string;
		glowColor: string;
		accentClass: string;
	}

	let hoveredIndex: number | null = null;

	// Calculate slices dynamically based on mode and language
	$: slices = computeSlices(mode, validCount, suspectCount, rejectedCount, activities, $locale, $t);

	function computeSlices(
		currentMode: ChartMode,
		valid: number,
		suspect: number,
		rejected: number,
		acts: Array<{ activityType?: string; status?: string }>,
		curLocale: string,
		trans: any
	): Slice[] {
		if (currentMode === 'status') {
			const list: Slice[] = [
				{
					id: 'valid',
					label: trans?.status?.valid ?? 'KM Válido',
					value: valid,
					color: '#00ffa3', // Neon Emerald
					glowColor: 'rgba(0, 255, 163, 0.4)',
					accentClass: 'text-cyber-emerald border-emerald-500/30 bg-emerald-500/10'
				},
				{
					id: 'suspect',
					label: trans?.status?.suspect ?? 'Suspeito',
					value: suspect,
					color: '#ffb800', // Electric Amber
					glowColor: 'rgba(255, 184, 0, 0.4)',
					accentClass: 'text-amber-400 border-amber-500/30 bg-amber-500/10'
				},
				{
					id: 'rejected',
					label: trans?.status?.rejected ?? 'Rejeitado',
					value: rejected,
					color: '#ff007a', // Cyber Pink / Magenta
					glowColor: 'rgba(255, 0, 122, 0.4)',
					accentClass: 'text-cyber-pink border-pink-500/30 bg-pink-500/10'
				}
			];
			return list.filter((s) => s.value > 0);
		} else {
			// Count by activityType
			const counts: Record<string, number> = {};
			acts.forEach((a) => {
				const type = a.activityType || 'Outro';
				counts[type] = (counts[type] || 0) + 1;
			});

			const palette = [
				{ color: '#820ad1', glow: 'rgba(130, 10, 209, 0.45)', cls: 'text-purple-400 border-purple-500/30 bg-purple-500/10' }, // Nubank Purple
				{ color: '#00f0ff', glow: 'rgba(0, 240, 255, 0.45)', cls: 'text-cyan-400 border-cyan-500/30 bg-cyan-500/10' },       // Cyber Cyan
				{ color: '#c084fc', glow: 'rgba(192, 132, 252, 0.45)', cls: 'text-purple-300 border-purple-400/30 bg-purple-500/10' }, // Nubank Lilac Light
				{ color: '#00ffa3', glow: 'rgba(0, 255, 163, 0.45)', cls: 'text-emerald-400 border-emerald-500/30 bg-emerald-500/10' }, // Neon Emerald
				{ color: '#ffb800', glow: 'rgba(255, 184, 0, 0.45)', cls: 'text-amber-400 border-amber-500/30 bg-amber-500/10' },     // Solar Gold
				{ color: '#ff007a', glow: 'rgba(255, 0, 122, 0.45)', cls: 'text-pink-400 border-pink-500/30 bg-pink-500/10' }        // Neon Pink
			];

			const list: Slice[] = Object.entries(counts).map(([type, count], idx) => {
				const pal = palette[idx % palette.length];
				const translatedLabel = translateActivityType(type, curLocale as any);
				return {
					id: type.toLowerCase(),
					label: translatedLabel,
					value: count,
					color: pal.color,
					glowColor: pal.glow,
					accentClass: pal.cls
				};
			});

			return list;
		}
	}

	$: totalValue = slices.reduce((sum, s) => sum + s.value, 0);

	// Compute SVG arc paths
	interface ArcPath {
		pathData: string;
		slice: Slice;
		percentage: number;
		startAngle: number;
		endAngle: number;
	}

	function polarToCartesian(cx: number, cy: number, r: number, angleDeg: number) {
		const rad = ((angleDeg - 90) * Math.PI) / 180.0;
		return {
			x: cx + r * Math.cos(rad),
			y: cy + r * Math.sin(rad)
		};
	}

	function describeDonutSlice(
		cx: number,
		cy: number,
		innerR: number,
		outerR: number,
		startAngle: number,
		endAngle: number
	): string {
		// Cap angle slightly below 360 to avoid SVG arc loop degeneracy
		const angleDiff = Math.min(endAngle - startAngle, 359.99);
		const actualEnd = startAngle + angleDiff;

		const startOuter = polarToCartesian(cx, cy, outerR, startAngle);
		const endOuter = polarToCartesian(cx, cy, outerR, actualEnd);
		const startInner = polarToCartesian(cx, cy, innerR, actualEnd);
		const endInner = polarToCartesian(cx, cy, innerR, startAngle);

		const largeArcFlag = angleDiff <= 180 ? '0' : '1';

		return [
			'M', startOuter.x, startOuter.y,
			'A', outerR, outerR, 0, largeArcFlag, 1, endOuter.x, endOuter.y,
			'L', startInner.x, startInner.y,
			'A', innerR, innerR, 0, largeArcFlag, 0, endInner.x, endInner.y,
			'Z'
		].join(' ');
	}

	$: arcPaths = (() => {
		if (totalValue === 0) return [] as ArcPath[];
		let accumulatedAngle = 0;
		const cx = 100;
		const cy = 100;
		const innerR = 56;
		const outerR = 86;

		return slices.map((slice, index) => {
			const isHovered = hoveredIndex === index;
			const rOuter = isHovered ? outerR + 5 : outerR;
			const rInner = isHovered ? innerR - 2 : innerR;
			const sliceAngle = (slice.value / totalValue) * 360;
			const startAngle = accumulatedAngle;
			const endAngle = accumulatedAngle + sliceAngle;
			accumulatedAngle += sliceAngle;

			// Gap between slices if more than 1
			const padding = slices.length > 1 ? 1.5 : 0;
			const finalStart = startAngle + padding;
			const finalEnd = Math.max(finalStart + 0.1, endAngle - padding);

			return {
				pathData: describeDonutSlice(cx, cy, rInner, rOuter, finalStart, finalEnd),
				slice,
				percentage: Math.round((slice.value / totalValue) * 100),
				startAngle,
				endAngle
			};
		});
	})();

	$: activeSlice = hoveredIndex !== null && arcPaths[hoveredIndex] ? arcPaths[hoveredIndex] : null;
</script>

<div class="cyber-card rounded-2xl p-5 flex flex-col justify-between relative overflow-hidden group">
	<!-- Holographic Lilac Ambient Lighting -->
	<div
		class="absolute -top-16 -right-16 w-48 h-48 bg-gradient-to-br from-nubank-600/20 via-purple-500/10 to-transparent rounded-full blur-2xl pointer-events-none"
	></div>
	<div
		class="absolute -bottom-16 -left-16 w-40 h-40 bg-gradient-to-tr from-cyber-cyan/15 via-transparent to-transparent rounded-full blur-2xl pointer-events-none"
	></div>

	<!-- Header with Mode Selector -->
	<div class="relative z-10 flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-3 border-b border-purple-500/15">
		<div class="flex items-center gap-2.5">
			<div class="w-8 h-8 rounded-lg bg-nubank-600/20 border border-purple-500/30 flex items-center justify-center text-nubank-400 shadow-[0_0_12px_rgba(130,10,209,0.3)]">
				<PieIcon class="w-4 h-4 text-purple-300" />
			</div>
			<div>
				<h4 class="text-sm font-bold text-white tracking-wide font-sans flex items-center gap-1.5">
					<span>{$t.pieChart.title}</span>
					<span class="inline-block w-1.5 h-1.5 rounded-full bg-nubank-400 animate-ping"></span>
				</h4>
				<p class="text-[11px] text-purple-300/70 font-mono">{$t.pieChart.subtitle}</p>
			</div>
		</div>

		<!-- Interactive View Toggle Button -->
		<div class="flex items-center bg-slate-950/70 p-1 rounded-xl border border-purple-500/20 text-[11px] font-mono">
			<button
				type="button"
				class="px-2.5 py-1 rounded-lg transition-all cursor-pointer font-medium {mode === 'status' ? 'bg-gradient-to-r from-nubank-600 to-purple-600 text-white shadow-[0_0_12px_rgba(130,10,209,0.5)] font-bold' : 'text-slate-400 hover:text-slate-200'}"
				on:click={() => { mode = 'status'; hoveredIndex = null; }}
			>
				{$t.pieChart.tabAudit}
			</button>
			<button
				type="button"
				class="px-2.5 py-1 rounded-lg transition-all cursor-pointer font-medium {mode === 'type' ? 'bg-gradient-to-r from-nubank-600 to-purple-600 text-white shadow-[0_0_12px_rgba(130,10,209,0.5)] font-bold' : 'text-slate-400 hover:text-slate-200'}"
				on:click={() => { mode = 'type'; hoveredIndex = null; }}
			>
				{$t.pieChart.tabModalities}
			</button>
		</div>
	</div>

	<!-- Chart Graphic & Central Telemetry Readout -->
	<div class="relative z-10 flex flex-col md:flex-row items-center justify-around gap-6 my-4">
		<!-- SVG Donut Chart with Cyber HUD -->
		<div class="relative w-52 h-52 shrink-0 flex items-center justify-center">
			<!-- Outer Futuristic Orbit Ring -->
			<div class="absolute inset-0 rounded-full border border-dashed border-purple-500/20 animate-spin-slow pointer-events-none" style="animation-duration: 40s;"></div>
			
			<svg viewBox="0 0 200 200" class="w-full h-full transform -rotate-90">
				<defs>
					<filter id="glow-effect" x="-20%" y="-20%" width="140%" height="140%">
						<feGaussianBlur stdDeviation="3" result="blur" />
						<feComposite in="SourceGraphic" in2="blur" operator="over" />
					</filter>
				</defs>

				<!-- Empty State Circle -->
				{#if totalValue === 0}
					<circle
						cx="100"
						cy="100"
						r="70"
						fill="transparent"
						stroke="#2e1065"
						stroke-width="26"
						opacity="0.4"
					/>
				{/if}

				<!-- Interactive Slices -->
				{#each arcPaths as arc, i}
					<!-- svelte-ignore a11y-mouse-events-have-key-events -->
					<!-- svelte-ignore a11y-no-static-element-interactions -->
					<path
						role="img"
						aria-label="{arc.slice.label}: {arc.slice.value}"
						d={arc.pathData}
						fill={arc.slice.color}
						stroke="#0a0316"
						stroke-width="1.5"
						class="transition-all duration-300 cursor-pointer"
						style="filter: {hoveredIndex === i ? `drop-shadow(0 0 10px ${arc.slice.glowColor})` : 'none'}; opacity: {hoveredIndex === null || hoveredIndex === i ? '1' : '0.45'};"
						on:mouseenter={() => (hoveredIndex = i)}
						on:mouseleave={() => (hoveredIndex = null)}
					/>
				{/each}

				<!-- Inner Subtle HUD Circle -->
				<circle
					cx="100"
					cy="100"
					r="52"
					fill="#0a0316"
					stroke="rgba(168, 85, 247, 0.2)"
					stroke-width="1"
				/>
			</svg>

			<!-- Donut Hole Interactive Information HUD -->
			<div class="absolute inset-0 flex flex-col items-center justify-center text-center pointer-events-none select-none p-4">
				{#if activeSlice}
					<span class="text-[10px] font-mono uppercase tracking-wider text-purple-300 font-semibold truncate max-w-[90px]">
						{activeSlice.slice.label}
					</span>
					<div class="text-2xl font-black font-mono text-white text-glow-nubank leading-tight">
						{activeSlice.slice.value}
					</div>
					<span class="text-[11px] font-mono font-bold text-cyber-cyan">
						{activeSlice.percentage}%
					</span>
				{:else}
					<span class="text-[9px] font-mono uppercase tracking-widest text-slate-400">
						{mode === 'status' ? $t.pieChart.audited : $t.pieChart.total}
					</span>
					<div class="text-2xl font-black font-mono text-white tracking-tight text-glow-nubank leading-tight">
						{totalValue}
					</div>
					<span class="text-[10px] font-mono text-purple-300 font-medium">
						{mode === 'status' ? $t.pieChart.activities : $t.pieChart.workouts}
					</span>
				{/if}
			</div>
		</div>

		<!-- Slices Legend List with Cyber Lilac & Neon Badges -->
		<div class="flex-1 w-full space-y-2">
			{#if slices.length === 0}
				<div class="p-3 text-center text-xs text-slate-400 font-mono">
					{$t.pieChart.empty}
				</div>
			{:else}
				<div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-1 gap-2">
					{#each slices as slice, i}
						{@const pct = totalValue > 0 ? Math.round((slice.value / totalValue) * 100) : 0}
						<!-- svelte-ignore a11y-mouse-events-have-key-events -->
						<!-- svelte-ignore a11y-no-static-element-interactions -->
						<div
							role="button"
							tabindex="0"
							class="flex items-center justify-between p-2 rounded-xl border transition-all cursor-pointer {hoveredIndex === i ? 'border-purple-400 bg-purple-950/40 shadow-[0_0_15px_rgba(130,10,209,0.3)]' : 'border-purple-500/15 bg-slate-950/40 hover:bg-slate-950/70'}"
							on:mouseenter={() => (hoveredIndex = i)}
							on:mouseleave={() => (hoveredIndex = null)}
						>
							<div class="flex items-center gap-2 min-w-0">
								<span
									class="w-3 h-3 rounded-full shrink-0 shadow-sm transition-transform duration-300 {hoveredIndex === i ? 'scale-125' : ''}"
									style="background-color: {slice.color}; box-shadow: 0 0 8px {slice.glowColor};"
								></span>
								<span class="text-xs font-medium text-slate-200 truncate">
									{slice.label}
								</span>
							</div>

							<div class="flex items-center gap-2 font-mono shrink-0">
								<span class="text-xs font-bold text-white">{slice.value}</span>
								<span class="text-[10px] px-1.5 py-0.5 rounded-md font-semibold {slice.accentClass}">
									{pct}%
								</span>
							</div>
						</div>
					{/each}
				</div>
			{/if}
		</div>
	</div>

	<!-- Bottom Infographic Status Footer -->
	<div class="relative z-10 pt-3 border-t border-purple-500/15 flex items-center justify-between text-[11px] font-mono text-purple-300/80">
		<div class="flex items-center gap-1.5">
			<ShieldCheck class="w-3.5 h-3.5 text-cyber-cyan" />
			<span>{$t.pieChart.footerInfo}</span>
		</div>
		<span class="text-slate-400">{$t.pieChart.realTime}</span>
	</div>
</div>

<style>
	@keyframes spinSlow {
		from {
			transform: rotate(0deg);
		}
		to {
			transform: rotate(360deg);
		}
	}
	.animate-spin-slow {
		animation: spinSlow 30s linear infinite;
	}
</style>
