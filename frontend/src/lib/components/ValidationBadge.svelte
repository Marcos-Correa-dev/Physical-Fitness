<script lang="ts">
	import { t } from '$lib/i18n';

	export let status: 'Pending' | 'Valid' | 'Suspect' | 'Rejected' | string;
	export let size: 'sm' | 'md' = 'sm';

	$: config = getStatusConfig(status, $t);

	function getStatusConfig(s: string, trans: any) {
		switch (s?.toLowerCase()) {
			case 'valid':
				return {
					bg: 'bg-emerald-500/10',
					border: 'border-emerald-500/40',
					text: 'text-cyber-emerald',
					dot: 'bg-cyber-emerald shadow-[0_0_10px_#00ffa3]',
					label: trans.status.valid
				};
			case 'suspect':
				return {
					bg: 'bg-amber-500/10',
					border: 'border-amber-500/40',
					text: 'text-amber-300',
					dot: 'bg-amber-400 shadow-[0_0_10px_#ffb800]',
					label: trans.status.suspect
				};
			case 'rejected':
				return {
					bg: 'bg-pink-500/10',
					border: 'border-pink-500/40',
					text: 'text-cyber-pink',
					dot: 'bg-cyber-pink shadow-[0_0_10px_#ff007a]',
					label: trans.status.rejected
				};
			default:
				return {
					bg: 'bg-purple-500/10',
					border: 'border-purple-500/30',
					text: 'text-purple-300',
					dot: 'bg-purple-400 shadow-[0_0_10px_#c084fc]',
					label: trans.status.pending
				};
		}
	}
</script>

<div
	class="inline-flex items-center gap-1.5 font-mono font-semibold uppercase tracking-wider rounded-full border transition-all {config.bg} {config.border} {config.text} {size === 'sm' ? 'px-2.5 py-0.5 text-[11px]' : 'px-3.5 py-1 text-xs'}"
>
	<span class="w-1.5 h-1.5 rounded-full {config.dot}"></span>
	<span>{config.label}</span>
</div>
