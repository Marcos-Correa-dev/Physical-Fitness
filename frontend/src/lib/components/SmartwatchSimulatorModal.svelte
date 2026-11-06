<script lang="ts">
	import { Watch, Zap, X, Check, Activity, ShieldCheck, AlertTriangle } from 'lucide-svelte';
	import type { SimulationRequest } from '../types/activity';

	export let isOpen: boolean = false;
	export let onClose: () => void;
	export let onSimulate: (req: SimulationRequest) => Promise<void>;

	let selectedModel = 'Xiaomi Mi Band 10';
	let selectedScenario = 'miband_strength_training';
	let isSubmitting = false;
	let successFeedback = false;

	const devices = [
		{ name: 'Xiaomi Mi Band 10', brand: 'Xiaomi', desc: 'Pulseira esportiva inteligente com sensor PPG de 12 eixos e Bluetooth 5.3 BLE' },
		{ name: 'Garmin Forerunner 965', brand: 'Garmin', desc: 'Relógio esportivo com GPS multi-band e sensor Elevate v5' },
		{ name: 'Apple Watch Ultra 2', brand: 'Apple', desc: 'GPS de dupla frequência L1/L5 com sensor óptico cardíaco' },
		{ name: 'Polar Vantage V3', brand: 'Polar', desc: 'Biossensores Elixir e ECG com satélite avançado' },
		{ name: 'Samsung Galaxy Watch 6', brand: 'Samsung', desc: 'Sensor BioActive 3-em-1 e acelerômetro' }
	];

	const scenarios = [
		{
			id: 'miband_strength_training',
			title: 'Atividade de Musculação (Treino de Hipertrofia)',
			badge: 'SUGERIDO PELO RELÓGIO',
			badgeColor: 'text-amber-400 bg-amber-500/10 border-amber-500/30',
			desc: 'Treino de força indoor na academia (48 min, 0 km, 425 kcal, picos de 169 bpm nas séries). Validação biométrica aprovada.'
		},
		{
			id: 'valid_5k_run',
			title: 'Corrida Matinal 5.0 km (Parque Ibirapuera)',
			badge: 'VÁLIDO ESPERADO',
			badgeColor: 'text-emerald-400 bg-emerald-500/10 border-emerald-500/30',
			desc: 'Trajeto de corrida contínuo com pace 5:35 min/km e frequência cardíaca fisiológica de 145-168 bpm.'
		},
		{
			id: 'valid_cycling',
			title: 'Treino de Ciclismo 21.8 km (Ciclovia)',
			badge: 'VÁLIDO ESPERADO',
			badgeColor: 'text-emerald-400 bg-emerald-500/10 border-emerald-500/30',
			desc: 'Velocidade média de 26 km/h compatível com bicicleta, satélite estável e esforço aeróbico.'
		},
		{
			id: 'fraud_vehicle_run',
			title: 'Tentativa de Fraude: Carro Disfarçado de Corrida',
			badge: 'REJEIÇÃO ESPERADA',
			badgeColor: 'text-rose-400 bg-rose-500/10 border-rose-500/30',
			desc: '14.5 km em 15 min (55 a 70 km/h) com batimentos de repouso (62 bpm). O motor deve barrar e rejeitar.'
		},
		{
			id: 'gps_glitch',
			title: 'Glitch de GPS: Teletransporte e Salto Abrupto',
			badge: 'SUSPEITO / REJEITADO',
			badgeColor: 'text-amber-400 bg-amber-500/10 border-amber-500/30',
			desc: 'Salto instantâneo de 4 km entre pontos de GPS (180 km/h instantâneo), gerando divergência Haversine.'
		},
		{
			id: 'abnormal_resting_hr',
			title: 'Anomalia Biométrica: Relógio Fora do Pulso',
			badge: 'REJEIÇÃO BIOMÉTRICA',
			badgeColor: 'text-rose-400 bg-rose-500/10 border-rose-500/30',
			desc: 'Corrida a 12 km/h com apenas 42 bpm registrados. O validador identifica ausência de contato cutâneo.'
		}
	];

	async function handleSubmit() {
		isSubmitting = true;
		successFeedback = false;
		try {
			await onSimulate({
				deviceModel: selectedModel,
				scenario: selectedScenario
			});
			successFeedback = true;
			setTimeout(() => {
				successFeedback = false;
				onClose();
			}, 900);
		} catch (err) {
			console.error(err);
		} finally {
			isSubmitting = false;
		}
	}
</script>

{#if isOpen}
	<div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/85 backdrop-blur-sm animate-fade-in">
		<div class="relative w-full max-w-xl bg-slate-900 border border-slate-800 rounded-2xl shadow-2xl overflow-hidden flex flex-col">
			<!-- Header -->
			<div class="flex items-center justify-between p-5 border-b border-slate-800 bg-slate-950/60">
				<div class="flex items-center gap-3">
					<div class="p-2.5 rounded-xl bg-gradient-to-tr from-cyan-600 to-emerald-500 text-white shadow-lg shadow-cyan-600/30">
						<Watch class="w-6 h-6" />
					</div>
					<div>
						<h3 class="text-base font-bold text-white">Sincronizador de Relógio & Pulseira</h3>
						<p class="text-xs text-slate-400 font-mono">Transmissão Direta de Telemetria (Mi Band 10, Garmin, Apple Watch)</p>
					</div>
				</div>

				<button
					type="button"
					on:click={onClose}
					class="text-slate-400 hover:text-white p-1 rounded-lg hover:bg-slate-800 transition-colors"
				>
					<X class="w-5 h-5" />
				</button>
			</div>

			<!-- Form Content -->
			<form on:submit|preventDefault={handleSubmit} class="p-6 space-y-5 overflow-y-auto max-h-[75vh]">
				<!-- Select Device -->
				<div>
					<label class="block text-xs font-mono uppercase tracking-wider text-slate-300 mb-2">
						1. Selecione o Dispositivo Eletrônico:
					</label>
					<div class="grid grid-cols-1 sm:grid-cols-2 gap-2">
						{#each devices as dev}
							<button
								type="button"
								on:click={() => (selectedModel = dev.name)}
								class="p-3 text-left rounded-xl border transition-all {selectedModel === dev.name ? 'bg-cyan-950/50 border-cyan-500 ring-1 ring-cyan-500' : 'bg-slate-950/40 border-slate-800/80 hover:bg-slate-800/50'}"
							>
								<div class="flex items-center justify-between font-semibold text-xs text-white">
									<span>{dev.name}</span>
									{#if selectedModel === dev.name}
										<Check class="w-4 h-4 text-cyan-400" />
									{/if}
								</div>
								<span class="text-[11px] text-slate-400 mt-0.5 block leading-tight">{dev.desc}</span>
							</button>
						{/each}
					</div>
				</div>

				<!-- Select Scenario -->
				<div>
					<label class="block text-xs font-mono uppercase tracking-wider text-slate-300 mb-2">
						2. Escolha o Cenário de Treino para Validação:
					</label>
					<div class="space-y-2">
						{#each scenarios as sc}
							<button
								type="button"
								on:click={() => (selectedScenario = sc.id)}
								class="w-full p-3 text-left rounded-xl border transition-all {selectedScenario === sc.id ? 'bg-cyan-950/40 border-cyan-500 ring-1 ring-cyan-500' : 'bg-slate-950/40 border-slate-800/80 hover:bg-slate-800/50'}"
							>
								<div class="flex items-center justify-between gap-2 mb-1">
									<span class="font-bold text-xs text-white">{sc.title}</span>
									<span class="text-[10px] font-mono px-2 py-0.5 rounded border {sc.badgeColor}">
										{sc.badge}
									</span>
								</div>
								<p class="text-xs text-slate-400 leading-relaxed">{sc.desc}</p>
							</button>
						{/each}
					</div>
				</div>

				<!-- Submit Button -->
				<div class="pt-2">
					<button
						type="submit"
						disabled={isSubmitting}
						class="w-full py-3 px-4 rounded-xl text-sm font-bold text-white bg-gradient-to-r from-cyan-600 via-emerald-600 to-cyan-600 hover:from-cyan-500 hover:to-emerald-500 shadow-xl shadow-cyan-600/25 flex items-center justify-center gap-2 transition-all disabled:opacity-60 cursor-pointer"
					>
						{#if isSubmitting}
							<Zap class="w-4 h-4 animate-spin text-cyan-200" />
							<span>Transmitindo Pacote de Telemetria...</span>
						{:else if successFeedback}
							<Check class="w-5 h-5 text-emerald-300" />
							<span>Treino Sincronizado e Validado!</span>
						{:else}
							<Zap class="w-4 h-4 text-cyan-300" />
							<span>Transmitir Atividade e Validar</span>
						{/if}
					</button>
				</div>
			</form>
		</div>
	</div>
{/if}
