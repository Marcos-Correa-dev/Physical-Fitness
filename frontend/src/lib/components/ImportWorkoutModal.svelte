<script lang="ts">
	import {
		UploadCloud,
		FileText,
		CheckCircle2,
		AlertCircle,
		X,
		RefreshCw,
		HelpCircle,
		Watch,
		ArrowRight,
		Smartphone
	} from 'lucide-svelte';
	import { uploadActivityFile } from '../services/api';
	import { t, interpolate } from '$lib/i18n';

	export let isOpen: boolean = false;
	export let onClose: () => void;
	export let onImportSuccess: () => Promise<void>;

	let selectedFile: File | null = null;
	let selectedDevice = 'Xiaomi Mi Band 10';
	let isUploading = false;
	let errorMessage = '';
	let successMessage = '';
	let activeTab: 'upload' | 'mifitness' | 'samsung' = 'upload';

	function handleFileChange(e: Event) {
		const target = e.target as HTMLInputElement;
		if (target.files && target.files.length > 0) {
			selectedFile = target.files[0];
			errorMessage = '';
		}
	}

	async function handleSubmit() {
		if (!selectedFile) {
			errorMessage = $t.importModal.fileRequiredError;
			return;
		}

		isUploading = true;
		errorMessage = '';
		successMessage = '';

		try {
			await uploadActivityFile(selectedFile, selectedDevice);
			successMessage = interpolate($t.importModal.successMessage, { device: selectedDevice });
			await onImportSuccess();
			setTimeout(() => {
				onClose();
				selectedFile = null;
				successMessage = '';
			}, 1200);
		} catch (err: any) {
			errorMessage = err.message || 'Erro ao processar arquivo do relógio.';
		} finally {
			isUploading = false;
		}
	}
</script>

{#if isOpen}
	<div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/85 backdrop-blur-md animate-fade-in">
		<div class="relative w-full max-w-xl bg-[#0e051e] border border-purple-500/30 rounded-2xl shadow-2xl shadow-purple-950/40 overflow-hidden flex flex-col">
			<!-- Header -->
			<div class="flex items-center justify-between p-5 border-b border-purple-500/20 bg-slate-950/70">
				<div class="flex items-center gap-3">
					<div class="p-2.5 rounded-xl bg-nubank-600/20 border border-purple-500/40 text-purple-300 shadow-[0_0_15px_rgba(130,10,209,0.35)]">
						<UploadCloud class="w-6 h-6 text-purple-300" />
					</div>
					<div>
						<h3 class="text-base font-bold text-white">{$t.importModal.title}</h3>
						<p class="text-xs text-purple-300/70 font-mono">{$t.importModal.subtitle}</p>
					</div>
				</div>

				<button
					type="button"
					on:click={onClose}
					class="text-slate-400 hover:text-white p-1 rounded-lg hover:bg-purple-950/50 transition-colors cursor-pointer"
				>
					<X class="w-5 h-5" />
				</button>
			</div>

			<!-- Tab Navigation -->
			<div class="flex border-b border-purple-500/20 bg-slate-950/50 text-xs font-mono">
				<button
					type="button"
					on:click={() => (activeTab = 'upload')}
					class="flex-1 py-3 text-center font-bold border-b-2 transition-all cursor-pointer {activeTab === 'upload' ? 'border-nubank-500 text-purple-200 bg-purple-950/30' : 'border-transparent text-slate-400 hover:text-slate-200'}"
				>
					{$t.importModal.tabUpload}
				</button>
				<button
					type="button"
					on:click={() => (activeTab = 'mifitness')}
					class="flex-1 py-3 text-center font-bold border-b-2 transition-all cursor-pointer {activeTab === 'mifitness' ? 'border-nubank-500 text-purple-200 bg-purple-950/30' : 'border-transparent text-slate-400 hover:text-slate-200'}"
				>
					{$t.importModal.tabMiFitness}
				</button>
				<button
					type="button"
					on:click={() => (activeTab = 'samsung')}
					class="flex-1 py-3 text-center font-bold border-b-2 transition-all cursor-pointer {activeTab === 'samsung' ? 'border-nubank-500 text-purple-200 bg-purple-950/30' : 'border-transparent text-slate-400 hover:text-slate-200'}"
				>
					{$t.importModal.tabSamsung}
				</button>
			</div>

			<!-- Body -->
			<div class="p-6 space-y-5 overflow-y-auto max-h-[70vh]">
				{#if activeTab === 'upload'}
					{#if errorMessage}
						<div class="p-4 rounded-xl bg-pink-950/40 border border-pink-700/60 text-pink-200 text-xs flex items-center gap-3">
							<AlertCircle class="w-4 h-4 text-cyber-pink shrink-0" />
							<span>{errorMessage}</span>
						</div>
					{/if}

					{#if successMessage}
						<div class="p-4 rounded-xl bg-emerald-950/40 border border-emerald-500/40 text-cyber-emerald text-xs flex items-center gap-3 shadow-lg">
							<CheckCircle2 class="w-4 h-4 text-cyber-emerald shrink-0" />
							<span>{successMessage}</span>
						</div>
					{/if}

					<!-- Device Selector -->
					<div>
						<label for="import-device-select" class="block text-xs font-mono uppercase tracking-wider text-purple-300 mb-2 font-bold">
							{$t.importModal.sourceDeviceLabel}
						</label>
						<select
							id="import-device-select"
							bind:value={selectedDevice}
							class="w-full bg-[#140828] border border-purple-500/30 rounded-xl px-3 py-2.5 text-xs text-white font-mono focus:border-purple-400 focus:outline-none"
						>
							<option value="Xiaomi Mi Band 10">Xiaomi Mi Band 10 (via Mi Fitness)</option>
							<option value="Samsung Galaxy Watch">Samsung Galaxy Watch (via Samsung Health)</option>
							<option value="Garmin Forerunner">Garmin Forerunner (via Garmin Connect)</option>
							<option value="Apple Watch">Apple Watch (via HealthFit / Workouts)</option>
							<option value="Polar Vantage">Polar Vantage (via Polar Flow)</option>
						</select>
					</div>

					<!-- File Drop Zone -->
					<div>
						<label for="import-file-input" class="block text-xs font-mono uppercase tracking-wider text-purple-300 mb-2 font-bold">
							{$t.importModal.fileInputLabel}
						</label>

						<label
							for="import-file-input"
							class="border-2 border-dashed border-purple-500/30 hover:border-purple-400 rounded-2xl p-6 flex flex-col items-center justify-center text-center cursor-pointer bg-slate-950/50 hover:bg-purple-950/20 transition-all"
						>
							<input
								id="import-file-input"
								type="file"
								accept=".gpx,.tcx"
								on:change={handleFileChange}
								class="hidden"
							/>

							{#if selectedFile}
								<div class="flex items-center gap-3">
									<div class="w-10 h-10 rounded-xl bg-emerald-500/20 border border-emerald-500/40 text-cyber-emerald flex items-center justify-center">
										<FileText class="w-5 h-5" />
									</div>
									<div class="text-left font-mono">
										<p class="text-xs font-bold text-white truncate max-w-xs">{selectedFile.name}</p>
										<p class="text-[10px] text-purple-300/70">{(selectedFile.size / 1024).toFixed(1)} KB • {$t.importModal.readyValidation}</p>
									</div>
								</div>
							{:else}
								<div class="w-12 h-12 rounded-xl bg-purple-950/40 border border-purple-500/30 text-purple-300 flex items-center justify-center mb-3 shadow-[0_0_12px_rgba(130,10,209,0.3)]">
									<UploadCloud class="w-6 h-6 text-cyber-cyan" />
								</div>
								<span class="text-xs font-bold text-white font-mono">
									{$t.importModal.dropzonePrompt}
								</span>
								<span class="text-[11px] text-purple-300/70 mt-1 font-mono">
									{$t.importModal.dropzoneSub}
								</span>
							{/if}
						</label>
					</div>

					<div class="pt-2">
						<button
							type="button"
							on:click={handleSubmit}
							disabled={isUploading || !selectedFile}
							class="w-full py-3.5 px-4 rounded-xl text-xs font-bold text-white bg-gradient-to-r from-nubank-600 via-purple-600 to-indigo-600 hover:from-purple-600 hover:to-nubank-600 shadow-[0_0_20px_rgba(130,10,209,0.4)] flex items-center justify-center gap-2 transition-all cursor-pointer border border-purple-400/30 disabled:opacity-50"
						>
							{#if isUploading}
								<RefreshCw class="w-4 h-4 animate-spin text-white" />
								<span>{$t.importModal.btnProcessing}</span>
							{:else}
								<UploadCloud class="w-4 h-4 text-cyber-cyan" />
								<span>{$t.importModal.btnValidateSave}</span>
							{/if}
						</button>
					</div>

				{:else if activeTab === 'mifitness'}
					<div class="space-y-4 text-xs font-sans text-slate-300">
						<div class="p-3.5 rounded-xl bg-purple-950/30 border border-purple-500/20">
							<h4 class="font-bold text-white font-mono text-xs flex items-center gap-2 mb-2">
								<Smartphone class="w-4 h-4 text-cyber-cyan" />
								{$t.importModal.mifitnessGuideTitle}
							</h4>
							<ol class="space-y-2 list-decimal list-inside text-[11px] text-purple-200/90 leading-relaxed">
								<li>{$t.importModal.mfStep1}</li>
								<li>{$t.importModal.mfStep2}</li>
								<li>{$t.importModal.mfStep3}</li>
								<li>{$t.importModal.mfStep4}</li>
								<li>{$t.importModal.mfStep5}</li>
								<li>{$t.importModal.mfStep6}</li>
							</ol>
						</div>
					</div>

				{:else if activeTab === 'samsung'}
					<div class="space-y-4 text-xs font-sans text-slate-300">
						<div class="p-3.5 rounded-xl bg-purple-950/30 border border-purple-500/20">
							<h4 class="font-bold text-white font-mono text-xs flex items-center gap-2 mb-2">
								<Watch class="w-4 h-4 text-cyber-emerald" />
								{$t.importModal.samsungGuideTitle}
							</h4>
							<ol class="space-y-2 list-decimal list-inside text-[11px] text-purple-200/90 leading-relaxed">
								<li>{$t.importModal.samsungStep1}</li>
								<li>{$t.importModal.samsungStep2}</li>
								<li>{$t.importModal.samsungStep3}</li>
								<li>{$t.importModal.samsungStep4}</li>
								<li>{$t.importModal.samsungStep5}</li>
							</ol>
						</div>
					</div>
				{/if}
			</div>

			<!-- Footer -->
			<div class="p-4 border-t border-purple-500/20 bg-slate-950/70 flex items-center justify-between text-xs text-slate-400 font-mono">
				<span class="text-[11px] text-purple-300/80">
					{$t.importModal.footerParser}
				</span>
				<button
					type="button"
					on:click={onClose}
					class="px-4 py-2 rounded-xl bg-slate-900 hover:bg-slate-800 text-purple-200 border border-purple-500/30 transition-colors cursor-pointer"
				>
					{$t.importModal.closeBtn}
				</button>
			</div>
		</div>
	</div>
{/if}
