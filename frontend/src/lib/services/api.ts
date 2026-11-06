import type { DashboardMetrics, ActivitySummary, SimulationRequest } from '../types/activity';

const API_BASE_URL = import.meta.env.VITE_API_URL || 
	(typeof window !== 'undefined' && window.location.hostname !== 'localhost' ? '/api/v1' : 'http://localhost:5005/api/v1');

export async function fetchDashboardMetrics(): Promise<DashboardMetrics> {
	const res = await fetch(`${API_BASE_URL}/dashboard`);
	if (!res.ok) {
		throw new Error(`Erro ao buscar dados do dashboard: ${res.statusText}`);
	}
	return await res.json();
}

export async function fetchActivityDetails(id: string): Promise<ActivitySummary> {
	const res = await fetch(`${API_BASE_URL}/activities/${id}`);
	if (!res.ok) {
		throw new Error(`Erro ao buscar detalhes da atividade: ${res.statusText}`);
	}
	return await res.json();
}

export async function revalidateActivity(id: string): Promise<ActivitySummary> {
	const res = await fetch(`${API_BASE_URL}/activities/${id}/validate`, {
		method: 'POST'
	});
	if (!res.ok) {
		throw new Error(`Erro ao revalidar atividade: ${res.statusText}`);
	}
	return await res.json();
}

export async function uploadActivityFile(file: File, deviceName: string): Promise<ActivitySummary> {
	const formData = new FormData();
	formData.append('file', file);
	formData.append('deviceName', deviceName);

	const res = await fetch(`${API_BASE_URL}/activities/import-file`, {
		method: 'POST',
		body: formData
	});
	if (!res.ok) {
		const err = await res.json().catch(() => ({ message: res.statusText }));
		throw new Error(err.message || 'Falha ao processar arquivo do relógio.');
	}
	return await res.json();
}

export async function syncLiveDeviceTelemetry(telemetry: any): Promise<ActivitySummary> {
	const res = await fetch(`${API_BASE_URL}/devices/sync`, {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify(telemetry)
	});
	if (!res.ok) {
		const err = await res.json().catch(() => ({ message: res.statusText }));
		throw new Error(err.message || 'Falha ao sincronizar dados do relógio.');
	}
	return await res.json();
}

export interface SyncHistoryResponse {
	totalFound: number;
	syncedCount: number;
	newCount: number;
	alreadyExistingCount: number;
	activities: ActivitySummary[];
	message: string;
}

export async function syncDeviceHistory(
	deviceName: string = 'Xiaomi Mi Band 10',
	folderPath?: string
): Promise<SyncHistoryResponse> {
	const res = await fetch(`${API_BASE_URL}/devices/sync-history`, {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify({ deviceName, folderPath })
	});
	if (!res.ok) {
		const err = await res.json().catch(() => ({ message: res.statusText }));
		throw new Error(err.message || 'Falha ao sincronizar histórico do relógio.');
	}
	return await res.json();
}

export interface GoogleFitSyncRequest {
	accessToken?: string;
	daysToSync?: number;
	forceDemoIfNoToken?: boolean;
	deviceName?: string;
}

export interface GoogleFitSyncResponse {
	totalFound: number;
	syncedCount: number;
	newCount: number;
	alreadyExistingCount: number;
	activities: ActivitySummary[];
	message: string;
	source: string;
	syncedAt: string;
}

export interface GoogleFitStatus {
	isConfigured: boolean;
	providerName: string;
	authUrl: string;
	scopes: string[];
	message: string;
	howToConnectGuide: string;
}

export async function fetchGoogleFitStatus(): Promise<GoogleFitStatus> {
	const res = await fetch(`${API_BASE_URL}/integrations/google-fit/status`);
	if (!res.ok) {
		throw new Error(`Erro ao verificar status do Google Fit: ${res.statusText}`);
	}
	return await res.json();
}

export async function fetchGoogleFitAuthUrl(redirectUri?: string): Promise<{ authUrl: string; scopes: string[]; instructions: string }> {
	const url = redirectUri
		? `${API_BASE_URL}/integrations/google-fit/auth-url?redirectUri=${encodeURIComponent(redirectUri)}`
		: `${API_BASE_URL}/integrations/google-fit/auth-url`;
	const res = await fetch(url);
	if (!res.ok) {
		throw new Error(`Erro ao obter URL de autenticação do Google Fit: ${res.statusText}`);
	}
	return await res.json();
}

export async function syncGoogleFit(
	options: GoogleFitSyncRequest = {}
): Promise<GoogleFitSyncResponse> {
	const res = await fetch(`${API_BASE_URL}/integrations/google-fit/sync`, {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify({
			accessToken: options.accessToken || null,
			daysToSync: options.daysToSync ?? 7,
			forceDemoIfNoToken: options.forceDemoIfNoToken ?? true,
			deviceName: options.deviceName || 'Xiaomi Mi Band 10'
		})
	});
	if (!res.ok) {
		const err = await res.json().catch(() => ({ message: res.statusText }));
		throw new Error(err.message || 'Falha ao sincronizar treinos via Google Fit / Health Connect.');
	}
	return await res.json();
}
