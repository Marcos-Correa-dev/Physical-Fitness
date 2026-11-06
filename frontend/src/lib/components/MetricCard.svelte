<script lang="ts">
	export let title: string;
	export let value: string | number;
	export let subtitle: string = '';
	export let change: string = '';
	export let trend: 'neutral' | 'positive' | 'negative' = 'neutral';
	export let accent: 'cyan' | 'emerald' | 'amber' | 'rose' | 'indigo' | 'nubank' = 'nubank';

	const accentStyles = {
		nubank: {
			border: 'hover:border-purple-400/50 border-purple-500/20',
			glow: 'from-nubank-600/20 via-purple-500/5 to-transparent',
			badge: 'text-purple-300 bg-purple-500/15 border-purple-500/30 shadow-[0_0_8px_rgba(130,10,209,0.3)]',
			valueGlow: 'text-glow-nubank'
		},
		cyan: {
			border: 'hover:border-cyan-400/50 border-cyan-500/20',
			glow: 'from-cyan-500/15 via-transparent to-transparent',
			badge: 'text-cyan-300 bg-cyan-500/15 border-cyan-500/30 shadow-[0_0_8px_rgba(0,240,255,0.3)]',
			valueGlow: 'text-glow-cyan'
		},
		emerald: {
			border: 'hover:border-emerald-400/50 border-emerald-500/20',
			glow: 'from-emerald-500/15 via-transparent to-transparent',
			badge: 'text-emerald-300 bg-emerald-500/15 border-emerald-500/30 shadow-[0_0_8px_rgba(0,255,163,0.3)]',
			valueGlow: 'text-glow-emerald'
		},
		amber: {
			border: 'hover:border-amber-400/50 border-amber-500/20',
			glow: 'from-amber-500/15 via-transparent to-transparent',
			badge: 'text-amber-300 bg-amber-500/15 border-amber-500/30 shadow-[0_0_8px_rgba(255,184,0,0.3)]',
			valueGlow: ''
		},
		rose: {
			border: 'hover:border-pink-400/50 border-pink-500/20',
			glow: 'from-pink-500/15 via-transparent to-transparent',
			badge: 'text-pink-300 bg-pink-500/15 border-pink-500/30 shadow-[0_0_8px_rgba(255,0,122,0.3)]',
			valueGlow: 'text-glow-pink'
		},
		indigo: {
			border: 'hover:border-indigo-400/50 border-indigo-500/20',
			glow: 'from-indigo-500/15 via-transparent to-transparent',
			badge: 'text-indigo-300 bg-indigo-500/15 border-indigo-500/30',
			valueGlow: ''
		}
	};
</script>

<div
	class="relative overflow-hidden rounded-2xl cyber-card p-5 transition-all duration-300 group {accentStyles[accent].border}"
>
	<!-- Gradient subtle background glow -->
	<div
		class="absolute inset-0 bg-gradient-to-br {accentStyles[accent].glow} pointer-events-none opacity-50 group-hover:opacity-80 transition-opacity"
	></div>

	<!-- Top ambient light accent line -->
	<div class="absolute top-0 left-0 right-0 h-[2px] bg-gradient-to-r from-transparent via-purple-500/40 to-transparent"></div>

	<div class="relative z-10 flex flex-col justify-between h-full">
		<div class="flex items-center justify-between mb-3">
			<span class="text-xs font-semibold uppercase tracking-wider text-purple-300/80 font-mono">
				{title}
			</span>
			<div class="p-2 rounded-xl bg-purple-950/40 border border-purple-500/20">
				<slot name="icon" />
			</div>
		</div>

		<div>
			<div class="text-3xl font-black tracking-tight text-white font-mono {accentStyles[accent].valueGlow}">
				{value}
			</div>

			<div class="mt-2.5 flex items-center justify-between gap-2 text-xs">
				{#if subtitle}
					<span class="text-slate-400 truncate text-[11px] font-mono">{subtitle}</span>
				{/if}

				{#if change}
					<span
						class="font-mono text-[10px] font-bold px-2 py-0.5 rounded-md border shrink-0 {accentStyles[accent].badge}"
					>
						{change}
					</span>
				{/if}
			</div>
		</div>
	</div>
</div>
