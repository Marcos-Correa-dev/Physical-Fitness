<script lang="ts">
	import { onMount } from 'svelte';
	import {
		Bluetooth,
		X,
		Check,
		Zap,
		BatteryCharging,
		Heart,
		Activity,
		Dumbbell,
		Radio,
		RefreshCw,
		AlertCircle,
		CheckCircle2,
		Flame,
		Timer,
		Route,
		Watch,
		Smartphone,
		QrCode,
		Copy,
		UploadCloud,
		Info,
		Sparkles,
		ShieldCheck
	} from 'lucide-svelte';
	import {
		syncLiveDeviceTelemetry,
		syncGoogleFit,
		fetchDashboardMetrics
	} from '../services/api';
	import type { ActivitySummary } from '../types/activity';
	import { locale, t, translateActivityType, formatDate, formatTime, interpolate } from '$lib/i18n';

	export let isOpen: boolean = false;
	export let onClose: () => void;
	export let onDeviceConnected: (deviceName: string) => void;
	export let onActivitySaved: () => Promise<void>;
	export let onOpenImportModal: () => void = () => {};

	// Tabs: google_fit | quick_sync | mobile_wifi | gpx_guide | web_bluetooth
	let activeTab: 'google_fit' | 'quick_sync' | 'mobile_wifi' | 'gpx_guide' | 'web_bluetooth' = 'google_fit';

	// ==========================================
	// ABA 0: Health Connect & Google Fit API
	// ==========================================
	let googleFitToken = '';
	let googleFitDays = 7;
	let isSyncingGoogleFit = false;
	let googleFitSuccessMessage = '';
	let googleFitErrorMessage = '';

	async function handleGoogleFitSync() {
		isSyncingGoogleFit = true;
		googleFitErrorMessage = '';
		googleFitSuccessMessage = '';

		try {
			const res = await syncGoogleFit({
				accessToken: googleFitToken.trim() || undefined,
				daysToSync: googleFitDays,
				forceDemoIfNoToken: true,
				deviceName: 'Xiaomi Mi Band 10'
			});

			googleFitSuccessMessage = interpolate($t.bluetoothModal.gfitSuccessMsg, {
				count: res.syncedCount || res.activities.length
			});

			onDeviceConnected('Xiaomi Mi Band 10 (Google Fit / Health Connect)');
			await loadRealActivities();
			await onActivitySaved();

			setTimeout(() => {
				googleFitSuccessMessage = '';
				onClose();
			}, 1600);
		} catch (err: any) {
			googleFitErrorMessage = err.message || $t.bluetoothModal.gfitErrorMsg;
		} finally {
			isSyncingGoogleFit = false;
		}
	}

	// ==========================================
	// ABA 1: Sincronização Rápida Mi Band 10
	// ==========================================
	let quickActivityType: string = 'Musculação';
	let quickDurationMinutes: number = 50;
	let quickAvgHeartRate: number = 138;
	let quickMaxHeartRate: number = 168;
	let quickCalories: number = 385;
	let quickDistanceKm: number = 0.0;
	let isSubmittingQuickSync = false;
	let quickSyncSuccessMessage = '';
	let quickSyncErrorMessage = '';

	function applyPreset(type: string, duration: number, bpm: number, maxBpm: number, kcal: number, km: number) {
		quickActivityType = type;
		quickDurationMinutes = duration;
		quickAvgHeartRate = bpm;
		quickMaxHeartRate = maxBpm;
		quickCalories = kcal;
		quickDistanceKm = km;
	}

	async function handleQuickSyncSubmit() {
		isSubmittingQuickSync = true;
		quickSyncErrorMessage = '';
		quickSyncSuccessMessage = '';

		try {
			let actTypeEnum = 5; // StrengthTraining (Musculação)
			if (quickActivityType === 'Corrida') actTypeEnum = 1;
			else if (quickActivityType === 'Caminhada') actTypeEnum = 2;
			else if (quickActivityType === 'Ciclismo') actTypeEnum = 3;
			else if (quickActivityType === 'Esteira') actTypeEnum = 1;

			const now = new Date();
			const startTime = new Date(now.getTime() - quickDurationMinutes * 60 * 1000);

			const telemetryPayload = {
				deviceSerialNumber: `MIBAND10-${Date.now().toString(36).toUpperCase()}`,
				deviceModel: 'Xiaomi Mi Band 10',
				manufacturer: 'Xiaomi',
				deviceType: 4, // SmartBand
				activityType: actTypeEnum,
				title: `${quickActivityType} (Xiaomi Mi Band 10)`,
				startTimeUtc: startTime.toISOString(),
				endTimeUtc: now.toISOString(),
				reportedDistanceKm: Number(quickDistanceKm) || 0.0,
				caloriesBurned: Number(quickCalories) || 350,
				avgHeartRateBpm: Number(quickAvgHeartRate) || 135,
				maxHeartRateBpm: Number(quickMaxHeartRate) || 165,
				gpsPoints: [] // Sem simulação de falsas coordenadas - biometria autêntica da pulseira
			};

			await syncLiveDeviceTelemetry(telemetryPayload);
			onDeviceConnected('Xiaomi Mi Band 10');
			quickSyncSuccessMessage = interpolate($t.bluetoothModal.syncSuccess, { type: translateActivityType(quickActivityType, $locale) });

			await loadRealActivities();
			await onActivitySaved();

			setTimeout(() => {
				quickSyncSuccessMessage = '';
				onClose();
			}, 1300);
		} catch (err: any) {
			quickSyncErrorMessage = err.message || $t.bluetoothModal.syncError;
		} finally {
			isSubmittingQuickSync = false;
		}
	}

	// ==========================================
	// ABA 2: Acesso Local pelo Celular
	// ==========================================
	const localMobileUrl = 'http://192.168.2.49:5173';
	let copiedLink = false;

	function copyToClipboard() {
		if (navigator.clipboard) {
			navigator.clipboard.writeText(localMobileUrl);
			copiedLink = true;
			setTimeout(() => (copiedLink = false), 2000);
		}
	}

	// ==========================================
	// ABA 4: Web Bluetooth (GATT nativo para outros dispositivos)
	// ==========================================
	let isScanning = false;
	let connectedDevice: any = null;
	let gattServer: any = null;
	let hrCharacteristic: any = null;
	let realHeartRate: number | null = null;
	let realBattery: number | null = null;
	let statusMessage = '';
	let bluetoothErrorMessage = '';
	let scanMode: 'all' | 'heart_rate' = 'all';

	async function scanRealBluetoothDevice() {
		bluetoothErrorMessage = '';
		isScanning = true;
		statusMessage = 'Abrindo seletor nativo de Bluetooth do sistema...';

		if (typeof navigator === 'undefined' || !('bluetooth' in navigator)) {
			isScanning = false;
			bluetoothErrorMessage = $t.bluetoothModal.bleErrorSupport;
			return;
		}

		try {
			let requestOptions: any;
			if (scanMode === 'heart_rate') {
				requestOptions = {
					filters: [{ services: ['heart_rate'] }],
					optionalServices: ['battery_service', 'device_information']
				};
			} else {
				requestOptions = {
					acceptAllDevices: true,
					optionalServices: [
						'heart_rate',
						'battery_service',
						'device_information',
						'fitness_machine',
						'running_speed_and_cadence'
					]
				};
			}

			const device = await (navigator as any).bluetooth.requestDevice(requestOptions);
			statusMessage = `Conectando via Bluetooth com ${device.name || 'Dispositivo'}...`;
			connectedDevice = device;

			device.addEventListener('gattserverdisconnected', onDisconnected);
			gattServer = await device.gatt.connect();

			try {
				const hrService = await gattServer.getPrimaryService('heart_rate');
				hrCharacteristic = await hrService.getCharacteristic('heart_rate_measurement');
				await hrCharacteristic.startNotifications();
				hrCharacteristic.addEventListener('characteristicvaluechanged', handleHeartRateMeasurement);
			} catch (hrErr) {
				console.log('GATT Heart Rate:', hrErr);
			}

			try {
				const batteryService = await gattServer.getPrimaryService('battery_service');
				const batteryChar = await batteryService.getCharacteristic('battery_level');
				const value = await batteryChar.readValue();
				realBattery = value.getUint8(0);
			} catch (batErr) {
				console.log('GATT Battery:', batErr);
			}

			const devName = device.name || 'Dispositivo BLE';
			onDeviceConnected(devName);
			statusMessage = `${devName} conectado com sucesso via Bluetooth!`;
			await loadRealActivities();
		} catch (err: any) {
			if (err.name === 'NotFoundError') {
				bluetoothErrorMessage = 'Nenhum dispositivo foi selecionado na janela de emparelhamento.';
			} else if (err.name === 'SecurityError') {
				bluetoothErrorMessage = 'Acesso ao Bluetooth bloqueado pelas políticas do navegador.';
			} else {
				bluetoothErrorMessage = `Erro ao conectar Bluetooth: ${err.message}`;
			}
		} finally {
			isScanning = false;
		}
	}

	function handleHeartRateMeasurement(event: any) {
		const value: DataView = event.target.value;
		const flags = value.getUint8(0);
		const is16Bit = (flags & 0x01) !== 0;
		const bpm = is16Bit ? value.getUint16(1, true) : value.getUint8(1);

		if (bpm > 30 && bpm < 240) {
			realHeartRate = bpm;
		}
	}

	function onDisconnected() {
		connectedDevice = null;
		gattServer = null;
		realHeartRate = null;
		realBattery = null;
		statusMessage = 'Dispositivo Bluetooth desconectado.';
		onDeviceConnected('');
	}

	function disconnectDevice() {
		if (gattServer && gattServer.connected) {
			gattServer.disconnect();
		}
		onDisconnected();
	}

	// Histórico real gravado
	let realActivities: ActivitySummary[] = [];

	async function loadRealActivities() {
		try {
			const data = await fetchDashboardMetrics();
			realActivities = data.recentActivities || [];
		} catch (e) {
			console.log('Aguardando servidor:', e);
		}
	}

	$: if (isOpen) {
		loadRealActivities();
	}

	onMount(() => {
		loadRealActivities();
	});
</script>

{#if isOpen}
	<div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/85 backdrop-blur-md animate-fade-in">
		<div class="relative w-full max-w-2xl bg-[#0e051e] border border-purple-500/30 rounded-2xl shadow-2xl shadow-purple-950/40 overflow-hidden flex flex-col max-h-[92vh]">
			
			<!-- Modal Header -->
			<div class="flex items-center justify-between p-5 border-b border-purple-500/20 bg-slate-950/70">
				<div class="flex items-center gap-3">
					<div class="p-2.5 rounded-xl bg-nubank-600/20 border border-purple-500/40 text-purple-300 shadow-[0_0_15px_rgba(130,10,209,0.35)]">
						<Watch class="w-6 h-6 text-purple-300" />
					</div>
					<div>
						<h3 class="text-base font-bold text-white flex items-center gap-2">
							<span>{$t.bluetoothModal.title}</span>
							<span class="text-[10px] font-mono px-2 py-0.5 rounded-full bg-purple-500/20 text-purple-300 font-bold border border-purple-500/30">
								HYPEROS / BLE
							</span>
						</h3>
						<p class="text-xs text-purple-300/70 font-mono">
							{$t.bluetoothModal.subtitle}
						</p>
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

			<!-- Notice Banner: Xiaomi Desktop limitation explained -->
			<div class="px-5 py-3 bg-purple-950/30 border-b border-purple-500/15 flex items-start gap-2.5 text-xs font-mono text-purple-200">
				<Info class="w-4 h-4 text-cyber-cyan shrink-0 mt-0.5" />
				<p class="text-[11px] text-purple-200/90 leading-relaxed font-sans">
					{$t.bluetoothModal.bannerNotice}
				</p>
			</div>

			<!-- Tabs Navigation -->
			<div class="flex border-b border-purple-500/20 bg-slate-950/60 text-xs font-mono overflow-x-auto">
				<button
					type="button"
					on:click={() => (activeTab = 'google_fit')}
					class="flex-1 min-w-[130px] py-3 text-center font-bold border-b-2 transition-all cursor-pointer flex items-center justify-center gap-1.5 {activeTab === 'google_fit' ? 'border-cyber-emerald text-emerald-300 bg-purple-950/40 shadow-[inset_0_0_12px_rgba(16,185,129,0.25)]' : 'border-transparent text-slate-400 hover:text-slate-200'}"
				>
					<Sparkles class="w-3.5 h-3.5 text-cyber-emerald" />
					<span>{$t.bluetoothModal.tabGoogleFit}</span>
				</button>

				<button
					type="button"
					on:click={() => (activeTab = 'quick_sync')}
					class="flex-1 min-w-[120px] py-3 text-center font-bold border-b-2 transition-all cursor-pointer flex items-center justify-center gap-1.5 {activeTab === 'quick_sync' ? 'border-nubank-500 text-purple-200 bg-purple-950/30 shadow-[inset_0_0_12px_rgba(130,10,209,0.3)]' : 'border-transparent text-slate-400 hover:text-slate-200'}"
				>
					<Zap class="w-3.5 h-3.5 text-cyber-cyan" />
					<span>{$t.bluetoothModal.tabQuickSync}</span>
				</button>

				<button
					type="button"
					on:click={() => (activeTab = 'mobile_wifi')}
					class="flex-1 min-w-[120px] py-3 text-center font-bold border-b-2 transition-all cursor-pointer flex items-center justify-center gap-1.5 {activeTab === 'mobile_wifi' ? 'border-nubank-500 text-purple-200 bg-purple-950/30 shadow-[inset_0_0_12px_rgba(130,10,209,0.3)]' : 'border-transparent text-slate-400 hover:text-slate-200'}"
				>
					<Smartphone class="w-3.5 h-3.5 text-cyber-emerald" />
					<span>{$t.bluetoothModal.tabMobileWifi}</span>
				</button>

				<button
					type="button"
					on:click={() => (activeTab = 'gpx_guide')}
					class="flex-1 min-w-[120px] py-3 text-center font-bold border-b-2 transition-all cursor-pointer flex items-center justify-center gap-1.5 {activeTab === 'gpx_guide' ? 'border-nubank-500 text-purple-200 bg-purple-950/30 shadow-[inset_0_0_12px_rgba(130,10,209,0.3)]' : 'border-transparent text-slate-400 hover:text-slate-200'}"
				>
					<UploadCloud class="w-3.5 h-3.5 text-purple-400" />
					<span>{$t.bluetoothModal.tabGpxGuide}</span>
				</button>

				<button
					type="button"
					on:click={() => (activeTab = 'web_bluetooth')}
					class="flex-1 min-w-[110px] py-3 text-center font-bold border-b-2 transition-all cursor-pointer flex items-center justify-center gap-1.5 {activeTab === 'web_bluetooth' ? 'border-nubank-500 text-purple-200 bg-purple-950/30 shadow-[inset_0_0_12px_rgba(130,10,209,0.3)]' : 'border-transparent text-slate-400 hover:text-slate-200'}"
				>
					<Bluetooth class="w-3.5 h-3.5 text-blue-400" />
					<span>{$t.bluetoothModal.tabWebBle}</span>
				</button>
			</div>

			<!-- Modal Body -->
			<div class="p-6 space-y-5 overflow-y-auto">

				<!-- ============================================================= -->
				<!-- TAB 0: HEALTH CONNECT & GOOGLE FIT API (RECOMENDADO)          -->
				<!-- ============================================================= -->
				{#if activeTab === 'google_fit'}
					<div class="space-y-5">
						<!-- Feedback Messages -->
						{#if googleFitSuccessMessage}
							<div class="p-4 rounded-xl bg-emerald-950/40 border border-emerald-500/40 text-cyber-emerald text-xs flex items-center gap-2.5 shadow-lg">
								<CheckCircle2 class="w-5 h-5 text-cyber-emerald shrink-0" />
								<span class="font-bold">{googleFitSuccessMessage}</span>
							</div>
						{/if}

						{#if googleFitErrorMessage}
							<div class="p-4 rounded-xl bg-pink-950/40 border border-pink-500/40 text-cyber-pink text-xs flex items-center gap-2.5 shadow-lg">
								<AlertCircle class="w-5 h-5 text-cyber-pink shrink-0" />
								<span>{googleFitErrorMessage}</span>
							</div>
						{/if}

						<!-- Hero Card -->
						<div class="p-4 rounded-xl bg-gradient-to-r from-purple-950/60 to-slate-900 border border-purple-500/30 space-y-3">
							<div class="flex items-center justify-between">
								<div class="flex items-center gap-2">
									<div class="p-1.5 rounded-lg bg-cyber-emerald/10 border border-cyber-emerald/30 text-cyber-emerald">
										<ShieldCheck class="w-4 h-4" />
									</div>
									<h4 class="text-sm font-bold text-white">{$t.bluetoothModal.gfitTitle}</h4>
								</div>
								<span class="text-[10px] font-mono px-2 py-0.5 rounded-full bg-cyber-emerald/20 text-cyber-emerald font-bold border border-cyber-emerald/40 tracking-wider">
									{$t.bluetoothModal.gfitBadge}
								</span>
							</div>
							<p class="text-xs text-purple-200/80 leading-relaxed font-sans">
								{$t.bluetoothModal.gfitDesc}
							</p>
						</div>

						<!-- 3 Steps Instructions Grid -->
						<div class="grid grid-cols-1 md:grid-cols-3 gap-3 text-xs">
							<div class="p-3.5 rounded-xl bg-slate-900/80 border border-purple-500/20 space-y-1.5">
								<div class="flex items-center gap-2 text-cyber-cyan font-bold">
									<Smartphone class="w-4 h-4" />
									<span>{$t.bluetoothModal.gfitStep1Title}</span>
								</div>
								<p class="text-[11px] text-slate-300 leading-snug">
									{$t.bluetoothModal.gfitStep1Desc}
								</p>
							</div>

							<div class="p-3.5 rounded-xl bg-slate-900/80 border border-purple-500/20 space-y-1.5">
								<div class="flex items-center gap-2 text-cyber-emerald font-bold">
									<Check class="w-4 h-4" />
									<span>{$t.bluetoothModal.gfitStep2Title}</span>
								</div>
								<p class="text-[11px] text-slate-300 leading-snug">
									{$t.bluetoothModal.gfitStep2Desc}
								</p>
							</div>

							<div class="p-3.5 rounded-xl bg-slate-900/80 border border-purple-500/20 space-y-1.5">
								<div class="flex items-center gap-2 text-purple-300 font-bold">
									<Zap class="w-4 h-4" />
									<span>{$t.bluetoothModal.gfitStep3Title}</span>
								</div>
								<p class="text-[11px] text-slate-300 leading-snug">
									{$t.bluetoothModal.gfitStep3Desc}
								</p>
							</div>
						</div>

						<!-- Action Panel -->
						<div class="p-4 rounded-xl bg-purple-950/20 border border-purple-500/30 space-y-4">
							<div class="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
								<div>
									<label for="gfit-days" class="block font-mono text-purple-300 text-[11px] mb-1">
										Período de Sincronização:
									</label>
									<select
										id="gfit-days"
										bind:value={googleFitDays}
										class="w-full bg-slate-900/90 border border-purple-500/30 rounded-lg px-3 py-2 text-xs text-white focus:outline-none focus:border-nubank-500 cursor-pointer"
									>
										<option value={7}>Últimos 7 dias (Recomendado)</option>
										<option value={15}>Últimos 15 dias</option>
										<option value={30}>Últimos 30 dias</option>
									</select>
								</div>

								<div>
									<label for="gfit-token" class="block font-mono text-purple-300 text-[11px] mb-1">
										Token Google OAuth (Opcional):
									</label>
									<input
										id="gfit-token"
										type="password"
										bind:value={googleFitToken}
										placeholder={$t.bluetoothModal.gfitTokenPlaceholder}
										class="w-full bg-slate-900/90 border border-purple-500/30 rounded-lg px-3 py-2 text-xs text-white placeholder-slate-500 focus:outline-none focus:border-nubank-500 font-mono"
									/>
								</div>
							</div>

							<p class="text-[11px] text-purple-300/70 italic font-mono flex items-center gap-1.5">
								<Info class="w-3.5 h-3.5 text-cyber-cyan shrink-0" />
								<span>{$t.bluetoothModal.gfitTokenOptional}</span>
							</p>

							<button
								type="button"
								on:click={handleGoogleFitSync}
								disabled={isSyncingGoogleFit}
								class="w-full py-3.5 rounded-xl bg-gradient-to-r from-nubank-600 via-purple-600 to-indigo-600 hover:from-nubank-500 hover:to-indigo-500 text-white font-bold text-xs shadow-lg shadow-purple-950/50 flex items-center justify-center gap-2 cursor-pointer transition-all disabled:opacity-60"
							>
								{#if isSyncingGoogleFit}
									<RefreshCw class="w-4 h-4 animate-spin text-white" />
									<span>{$t.bluetoothModal.gfitBtnSyncing}</span>
								{:else}
									<Sparkles class="w-4 h-4 text-cyber-emerald" />
									<span>{$t.bluetoothModal.gfitBtnSyncNow}</span>
								{/if}
							</button>
						</div>

						<div class="p-3 rounded-lg bg-slate-950/50 border border-purple-500/10 flex items-center justify-between text-[11px] font-mono text-purple-300/60">
							<span class="flex items-center gap-1.5">
								<span class="w-2 h-2 rounded-full bg-cyber-emerald animate-pulse"></span>
								{$t.bluetoothModal.gfitFreeApiNotice}
							</span>
							<span class="text-slate-400">Endpoint: /api/v1/integrations/google-fit</span>
						</div>
					</div>
				{/if}

				<!-- ============================================================= -->
				<!-- TAB 1: SINCRONIZAÇÃO RÁPIDA (DIRETA COM DADOS DO RELÓGIO)     -->
				<!-- ============================================================= -->
				{#if activeTab === 'quick_sync'}
					<div class="space-y-4">
						{#if quickSyncSuccessMessage}
							<div class="p-4 rounded-xl bg-emerald-950/40 border border-emerald-500/40 text-cyber-emerald text-xs flex items-center gap-2.5 shadow-lg">
								<CheckCircle2 class="w-5 h-5 text-cyber-emerald shrink-0" />
								<span class="font-bold">{quickSyncSuccessMessage}</span>
							</div>
						{/if}

						{#if quickSyncErrorMessage}
							<div class="p-4 rounded-xl bg-pink-950/40 border border-pink-500/40 text-cyber-pink text-xs flex items-center gap-2.5 shadow-lg">
								<AlertCircle class="w-5 h-5 text-cyber-pink shrink-0" />
								<span>{quickSyncErrorMessage}</span>
							</div>
						{/if}

						<!-- Quick Preset Bar -->
						<div class="p-3.5 rounded-xl bg-purple-950/30 border border-purple-500/20 space-y-2">
							<span class="text-[10px] font-mono uppercase tracking-wider text-purple-300 font-bold block">
								{$t.bluetoothModal.presetsTitle}
							</span>
							<div class="grid grid-cols-2 sm:grid-cols-4 gap-2 text-xs font-mono">
								<button
									type="button"
									on:click={() => applyPreset('Musculação', 50, 138, 168, 385, 0.0)}
									class="p-2 rounded-lg bg-slate-950/70 border border-purple-500/20 hover:border-purple-400 text-left transition-all cursor-pointer"
								>
									<span class="text-[10px] text-amber-300 font-bold block">{$t.bluetoothModal.presetStrength}</span>
									<span class="text-[11px] text-white">{$t.bluetoothModal.presetStrengthSub}</span>
								</button>

								<button
									type="button"
									on:click={() => applyPreset('Corrida', 30, 155, 178, 340, 5.2)}
									class="p-2 rounded-lg bg-slate-950/70 border border-purple-500/20 hover:border-purple-400 text-left transition-all cursor-pointer"
								>
									<span class="text-[10px] text-cyber-cyan font-bold block">{$t.bluetoothModal.presetRun}</span>
									<span class="text-[11px] text-white">{$t.bluetoothModal.presetRunSub}</span>
								</button>

								<button
									type="button"
									on:click={() => applyPreset('Ciclismo', 45, 142, 165, 410, 15.5)}
									class="p-2 rounded-lg bg-slate-950/70 border border-purple-500/20 hover:border-purple-400 text-left transition-all cursor-pointer"
								>
									<span class="text-[10px] text-cyber-emerald font-bold block">{$t.bluetoothModal.presetCycling}</span>
									<span class="text-[11px] text-white">{$t.bluetoothModal.presetCyclingSub}</span>
								</button>

								<button
									type="button"
									on:click={() => applyPreset('Caminhada', 40, 115, 135, 190, 3.2)}
									class="p-2 rounded-lg bg-slate-950/70 border border-purple-500/20 hover:border-purple-400 text-left transition-all cursor-pointer"
								>
									<span class="text-[10px] text-purple-300 font-bold block">{$t.bluetoothModal.presetWalk}</span>
									<span class="text-[11px] text-white">{$t.bluetoothModal.presetWalkSub}</span>
								</button>
							</div>
						</div>

						<!-- Telemetry Form (matching what appears on the Mi Band 10 screen) -->
						<div class="p-4 rounded-xl bg-slate-950/80 border border-purple-500/20 space-y-4">
							<div class="flex items-center justify-between border-b border-purple-500/15 pb-2">
								<span class="text-xs font-mono font-bold text-white uppercase flex items-center gap-1.5">
									<Watch class="w-4 h-4 text-purple-400" />
									{$t.bluetoothModal.screenFormTitle}
								</span>
								<span class="text-[10px] font-mono text-purple-300">Xiaomi Mi Band 10</span>
							</div>

							<div class="grid grid-cols-1 sm:grid-cols-2 gap-3.5 text-xs font-mono">
								<!-- Atividade -->
								<div>
									<label for="quick-act-type" class="text-[11px] text-purple-300/80 block mb-1">
										{$t.bluetoothModal.lblActType}
									</label>
									<select
										id="quick-act-type"
										bind:value={quickActivityType}
										class="w-full bg-[#140828] border border-purple-500/30 rounded-xl px-3 py-2 text-white font-mono focus:border-purple-400 outline-none"
									>
										<option value="Musculação">{$t.bluetoothModal.optStrength}</option>
										<option value="Corrida">{$t.bluetoothModal.optRun}</option>
										<option value="Esteira">{$t.bluetoothModal.optTreadmill}</option>
										<option value="Ciclismo">{$t.bluetoothModal.optCycling}</option>
										<option value="Caminhada">{$t.bluetoothModal.optWalk}</option>
									</select>
								</div>

								<!-- Duração -->
								<div>
									<label for="quick-dur" class="text-[11px] text-purple-300/80 block mb-1">
										{$t.bluetoothModal.lblDuration}
									</label>
									<input
										id="quick-dur"
										type="number"
										min="1"
										max="600"
										bind:value={quickDurationMinutes}
										class="w-full bg-[#140828] border border-purple-500/30 rounded-xl px-3 py-2 text-white font-mono focus:border-purple-400 outline-none"
									/>
								</div>

								<!-- Batimentos Médios -->
								<div>
									<label for="quick-hr" class="text-[11px] text-purple-300/80 block mb-1">
										{$t.bluetoothModal.lblAvgHr}
									</label>
									<input
										id="quick-hr"
										type="number"
										min="40"
										max="220"
										bind:value={quickAvgHeartRate}
										class="w-full bg-[#140828] border border-purple-500/30 rounded-xl px-3 py-2 text-white font-mono focus:border-purple-400 outline-none"
									/>
								</div>

								<!-- Calorias Gastas -->
								<div>
									<label for="quick-kcal" class="text-[11px] text-purple-300/80 block mb-1">
										{$t.bluetoothModal.lblCalories}
									</label>
									<input
										id="quick-kcal"
										type="number"
										min="10"
										max="5000"
										bind:value={quickCalories}
										class="w-full bg-[#140828] border border-purple-500/30 rounded-xl px-3 py-2 text-white font-mono focus:border-purple-400 outline-none"
									/>
								</div>

								<!-- Distância (se houver) -->
								<div class="sm:col-span-2">
									<label for="quick-dist" class="text-[11px] text-purple-300/80 block mb-1">
										{$t.bluetoothModal.lblDistance}
									</label>
									<input
										id="quick-dist"
										type="number"
										step="0.01"
										min="0"
										max="200"
										bind:value={quickDistanceKm}
										class="w-full bg-[#140828] border border-purple-500/30 rounded-xl px-3 py-2 text-white font-mono focus:border-purple-400 outline-none"
										placeholder={$t.bluetoothModal.distPlaceholder}
									/>
								</div>
							</div>

							<!-- Submit Button -->
							<button
								type="button"
								on:click={handleQuickSyncSubmit}
								disabled={isSubmittingQuickSync}
								class="w-full py-3.5 px-4 rounded-xl text-xs font-bold text-white bg-gradient-to-r from-nubank-600 via-purple-600 to-indigo-600 hover:from-purple-600 hover:to-nubank-600 shadow-[0_0_20px_rgba(130,10,209,0.4)] flex items-center justify-center gap-2 transition-all cursor-pointer border border-purple-400/30 disabled:opacity-50"
							>
								{#if isSubmittingQuickSync}
									<RefreshCw class="w-4 h-4 animate-spin text-white" />
									<span>{$t.bluetoothModal.btnSyncing}</span>
								{:else}
									<Zap class="w-4 h-4 text-cyber-cyan" />
									<span>{$t.bluetoothModal.btnSyncNow}</span>
								{/if}
							</button>
						</div>
					</div>

				<!-- ============================================================= -->
				<!-- TAB 2: ABRIR NO CELULAR VIA REDE LOCAL (SEM PRECISAR DE DEPLOY) -->
				<!-- ============================================================= -->
				{:else if activeTab === 'mobile_wifi'}
					<div class="p-5 rounded-2xl bg-slate-950/70 border border-purple-500/20 text-center space-y-4">
						<div class="w-14 h-14 rounded-2xl bg-emerald-500/10 border border-emerald-500/30 text-cyber-emerald flex items-center justify-center mx-auto shadow-lg shadow-emerald-500/20">
							<Smartphone class="w-7 h-7" />
						</div>

						<div>
							<h4 class="text-sm font-bold text-white">{$t.bluetoothModal.mobileTitle}</h4>
							<p class="text-xs text-purple-300/80 max-w-md mx-auto mt-1 leading-relaxed font-sans">
								{$t.bluetoothModal.mobileDesc}
							</p>
						</div>

						<!-- QR Code to scan with phone -->
						<div class="inline-block p-3 bg-white rounded-2xl shadow-xl shadow-purple-950/50">
							<img
								src="https://api.qrserver.com/v1/create-qr-code/?size=150x150&data={encodeURIComponent(localMobileUrl)}&color=0a0316"
								alt={$t.bluetoothModal.qrAlt}
								class="w-36 h-36 mx-auto rounded-lg"
							/>
						</div>

						<div class="space-y-2 max-w-md mx-auto">
							<span class="text-[11px] font-mono text-slate-400 block">
								{$t.bluetoothModal.orTypeBrowser}
							</span>
							<div class="flex items-center gap-2 bg-[#120626] border border-purple-500/30 p-2 rounded-xl">
								<span class="flex-1 font-mono text-xs text-cyber-cyan font-bold truncate">
									{localMobileUrl}
								</span>
								<button
									type="button"
									on:click={copyToClipboard}
									class="px-2.5 py-1 bg-purple-600/30 hover:bg-purple-600/50 border border-purple-500/40 rounded-lg text-[11px] font-mono text-purple-200 transition-colors cursor-pointer flex items-center gap-1"
								>
									{#if copiedLink}
										<Check class="w-3 h-3 text-cyber-emerald" />
										<span>{$t.bluetoothModal.copied}</span>
									{:else}
										<Copy class="w-3 h-3" />
										<span>{$t.bluetoothModal.copy}</span>
									{/if}
								</button>
							</div>
						</div>
					</div>

				<!-- ============================================================= -->
				<!-- TAB 3: TUTORIAL DE EXPORTAÇÃO GPX DO MI FITNESS               -->
				<!-- ============================================================= -->
				{:else if activeTab === 'gpx_guide'}
					<div class="p-5 rounded-2xl bg-slate-950/70 border border-purple-500/20 space-y-4">
						<div class="flex items-center gap-3 pb-3 border-b border-purple-500/15">
							<div class="p-2 rounded-xl bg-purple-600/20 border border-purple-500/30 text-purple-300">
								<UploadCloud class="w-5 h-5 text-cyber-cyan" />
							</div>
							<div>
								<h4 class="text-sm font-bold text-white">{$t.bluetoothModal.gpxGuideTitle}</h4>
								<p class="text-[11px] text-purple-300/70 font-mono">{$t.bluetoothModal.gpxGuideSubtitle}</p>
							</div>
						</div>

						<div class="space-y-3 text-xs font-sans text-slate-300">
							<div class="flex items-start gap-3 p-3 rounded-xl bg-purple-950/20 border border-purple-500/15">
								<span class="w-5 h-5 rounded-full bg-nubank-600 text-white font-mono font-bold text-xs flex items-center justify-center shrink-0">1</span>
								<p>{$t.bluetoothModal.gpxStep1}</p>
							</div>

							<div class="flex items-start gap-3 p-3 rounded-xl bg-purple-950/20 border border-purple-500/15">
								<span class="w-5 h-5 rounded-full bg-nubank-600 text-white font-mono font-bold text-xs flex items-center justify-center shrink-0">2</span>
								<p>{$t.bluetoothModal.gpxStep2}</p>
							</div>

							<div class="flex items-start gap-3 p-3 rounded-xl bg-purple-950/20 border border-purple-500/15">
								<span class="w-5 h-5 rounded-full bg-nubank-600 text-white font-mono font-bold text-xs flex items-center justify-center shrink-0">3</span>
								<p>{$t.bluetoothModal.gpxStep3}</p>
							</div>

							<div class="flex items-start gap-3 p-3 rounded-xl bg-purple-950/20 border border-purple-500/15">
								<span class="w-5 h-5 rounded-full bg-nubank-600 text-white font-mono font-bold text-xs flex items-center justify-center shrink-0">4</span>
								<p>{$t.bluetoothModal.gpxStep4}</p>
							</div>

							<div class="flex items-start gap-3 p-3 rounded-xl bg-purple-950/20 border border-purple-500/15">
								<span class="w-5 h-5 rounded-full bg-nubank-600 text-white font-mono font-bold text-xs flex items-center justify-center shrink-0">5</span>
								<p>{$t.bluetoothModal.gpxStep5}</p>
							</div>
						</div>

						<div class="pt-2">
							<button
								type="button"
								on:click={() => {
									onClose();
									onOpenImportModal();
								}}
								class="w-full py-3 px-4 rounded-xl text-xs font-bold text-white bg-gradient-to-r from-cyan-600 to-emerald-600 hover:from-cyan-500 hover:to-emerald-500 shadow-lg shadow-cyan-600/30 flex items-center justify-center gap-2 transition-all cursor-pointer border border-cyan-400/30"
							>
								<UploadCloud class="w-4 h-4 text-white" />
								<span>{$t.bluetoothModal.openGpxImporterBtn}</span>
							</button>
						</div>
					</div>

				<!-- ============================================================= -->
				<!-- TAB 4: WEB BLUETOOTH BLE (PARA DISPOSITIVOS ABERTOS)          -->
				<!-- ============================================================= -->
				{:else if activeTab === 'web_bluetooth'}
					<div class="space-y-4">
						{#if bluetoothErrorMessage}
							<div class="p-4 rounded-xl bg-pink-950/40 border border-pink-700/60 text-pink-200 text-xs flex items-start gap-2.5">
								<AlertCircle class="w-5 h-5 text-cyber-pink shrink-0 mt-0.5" />
								<div>
									<p class="font-bold text-sm text-pink-200">{bluetoothErrorMessage}</p>
									<p class="text-[11px] text-purple-200/80 mt-1">
										{$t.bluetoothModal.bleXiaomiNotice}
									</p>
								</div>
							</div>
						{/if}

						{#if !connectedDevice}
							<div class="text-center py-4 space-y-4 bg-slate-950/70 p-5 rounded-2xl border border-purple-500/20">
								<div class="w-14 h-14 rounded-full bg-blue-950/60 border border-blue-500/40 text-blue-400 flex items-center justify-center mx-auto shadow-xl shadow-blue-500/20">
									<Radio class="w-7 h-7 {isScanning ? 'animate-ping' : ''}" />
								</div>

								<div>
									<h4 class="text-sm font-bold text-white">{$t.bluetoothModal.bleGattTitle}</h4>
									<p class="text-xs text-slate-400 max-w-md mx-auto mt-1">
										{$t.bluetoothModal.bleGattDesc}
									</p>
								</div>

								<div class="flex items-center justify-center gap-2 max-w-xs mx-auto text-xs font-mono">
									<button
										type="button"
										on:click={() => (scanMode = 'all')}
										class="flex-1 py-1.5 px-3 rounded-lg border transition-all cursor-pointer {scanMode === 'all' ? 'bg-purple-600/30 border-purple-400 text-purple-200 font-bold' : 'bg-slate-950 border-slate-800 text-slate-400'}"
									>
										{$t.bluetoothModal.bleAll}
									</button>
									<button
										type="button"
										on:click={() => (scanMode = 'heart_rate')}
										class="flex-1 py-1.5 px-3 rounded-lg border transition-all cursor-pointer {scanMode === 'heart_rate' ? 'bg-purple-600/30 border-purple-400 text-purple-200 font-bold' : 'bg-slate-950 border-slate-800 text-slate-400'}"
									>
										{$t.bluetoothModal.bleHr}
									</button>
								</div>

								<button
									type="button"
									on:click={scanRealBluetoothDevice}
									disabled={isScanning}
									class="w-full py-3.5 px-4 rounded-xl text-xs font-bold text-white bg-blue-600 hover:bg-blue-500 shadow-xl shadow-blue-600/30 flex items-center justify-center gap-2 transition-all cursor-pointer disabled:opacity-50"
								>
									<Bluetooth class="w-4 h-4 {isScanning ? 'animate-spin' : ''}" />
									<span>{isScanning ? $t.bluetoothModal.bleScanning : $t.bluetoothModal.bleStartScan}</span>
								</button>
							</div>
						{:else}
							<div class="p-4 rounded-xl bg-blue-950/30 border border-blue-500/40 space-y-3">
								<div class="flex items-center justify-between">
									<div class="flex items-center gap-3">
										<div class="w-10 h-10 rounded-xl bg-blue-600/30 border border-blue-500/50 flex items-center justify-center text-blue-400">
											<Bluetooth class="w-5 h-5 animate-pulse" />
										</div>
										<div>
											<h4 class="text-sm font-bold text-white">{connectedDevice.name || 'Dispositivo Conectado'}</h4>
											<span class="text-[11px] font-mono text-emerald-400">{$t.bluetoothModal.bleConnected}</span>
										</div>
									</div>
									<button
										type="button"
										on:click={disconnectDevice}
										class="text-xs font-mono text-rose-400 hover:text-rose-300"
									>
										{$t.bluetoothModal.bleDisconnect}
									</button>
								</div>
							</div>
						{/if}
					</div>
				{/if}

				<!-- ============================================================= -->
				<!-- LISTA DE TREINOS JÁ REGISTRADOS NO SISTEMA                    -->
				<!-- ============================================================= -->
				<div class="p-4 rounded-xl bg-slate-950/80 border border-purple-500/20 space-y-3">
					<div class="flex items-center justify-between border-b border-purple-500/15 pb-2.5">
						<div class="flex items-center gap-2">
							<Activity class="w-4 h-4 text-cyber-cyan" />
							<h4 class="text-xs font-bold text-white font-mono uppercase">
								{interpolate($t.bluetoothModal.registeredWorkoutsTitle, { count: realActivities.length })}
							</h4>
						</div>
						<span class="text-[10px] font-mono text-purple-300 font-bold">
							Xiaomi & Wearables
						</span>
					</div>

					<div class="space-y-2 max-h-40 overflow-y-auto">
						{#if realActivities.length > 0}
							{#each realActivities.slice(0, 5) as act}
								<div class="p-2.5 rounded-xl bg-[#120626] border border-purple-500/15 flex items-center justify-between text-xs">
									<div class="flex items-center gap-2.5">
										<div class="w-7 h-7 rounded-lg bg-purple-950/60 border border-purple-500/20 text-purple-300 flex items-center justify-center font-bold text-xs">
											{#if act.activityType === 'Musculação'}
												🏋️
											{:else}
												🏃
											{/if}
										</div>
										<div>
											<span class="font-bold text-white block">{act.title}</span>
											<span class="text-[10px] text-purple-300/70 font-mono">
												{formatDate(act.startTime || act.formattedDate, $locale, { day: '2-digit', month: '2-digit', year: 'numeric' })} • {act.formattedDuration} • {act.caloriesBurned} kcal
											</span>
										</div>
									</div>

									<div class="text-right font-mono">
										<span class="text-[10px] px-1.5 py-0.5 rounded bg-emerald-500/20 text-cyber-emerald border border-emerald-500/30 font-bold">
											{act.status}
										</span>
										<span class="text-[10px] text-slate-400 block mt-0.5">{act.averageHeartRateBpm} bpm</span>
									</div>
								</div>
							{/each}
						{:else}
							<div class="py-4 text-center text-slate-500 font-mono text-xs">
								{$t.bluetoothModal.noWorkoutsYet}
							</div>
						{/if}
					</div>
				</div>
			</div>

			<!-- Modal Footer -->
			<div class="p-4 border-t border-purple-500/20 bg-slate-950/70 flex items-center justify-between text-xs text-slate-400 font-mono">
				<span class="text-[11px] text-purple-300/80">
					{activeTab === 'quick_sync' ? $t.bluetoothModal.footerDirectSync : $t.bluetoothModal.footerHub}
				</span>
				<button
					type="button"
					on:click={onClose}
					class="px-4 py-2 rounded-xl bg-slate-900 hover:bg-slate-800 text-purple-200 border border-purple-500/30 transition-colors cursor-pointer"
				>
					{$t.bluetoothModal.closeBtn}
				</button>
			</div>
		</div>
	</div>
{/if}
