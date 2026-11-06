import { writable, derived, get } from 'svelte/store';
import { browser } from '$app/environment';
import type { SupportedLocale, Translations } from './types';
import { ptBR } from './translations/pt-BR';
import { en } from './translations/en';

const STORAGE_KEY = 'app_locale';

const dictionaries: Record<SupportedLocale, Translations> = {
	'pt-BR': ptBR,
	'en': en
};

function getInitialLocale(): SupportedLocale {
	if (browser) {
		const stored = localStorage.getItem(STORAGE_KEY) as SupportedLocale | null;
		if (stored === 'pt-BR' || stored === 'en') {
			return stored;
		}
		// Check browser language
		if (navigator.language && navigator.language.toLowerCase().startsWith('en')) {
			return 'en';
		}
	}
	return 'pt-BR';
}

export const locale = writable<SupportedLocale>(getInitialLocale());

if (browser) {
	locale.subscribe((val) => {
		try {
			localStorage.setItem(STORAGE_KEY, val);
			document.documentElement.lang = val;
		} catch (e) {
			console.warn('Could not persist locale to localStorage', e);
		}
	});
}

export function setLocale(newLocale: SupportedLocale) {
	if (newLocale === 'pt-BR' || newLocale === 'en') {
		locale.set(newLocale);
	}
}

export function toggleLocale() {
	const current = get(locale);
	setLocale(current === 'pt-BR' ? 'en' : 'pt-BR');
}

/**
 * Derived store giving the active translations object
 */
export const t = derived(locale, ($locale) => dictionaries[$locale] || dictionaries['pt-BR']);

/**
 * Helper to interpolate variables into template string like "Hello {name}"
 */
export function interpolate(template: string, params?: Record<string, string | number>): string {
	if (!params || !template) return template;
	return template.replace(/\{(\w+)\}/g, (_, key) => {
		return params[key] !== undefined ? String(params[key]) : `{${key}}`;
	});
}

/**
 * Locale-aware date/time formatting utilities
 */
export function formatDate(
	dateInput: string | Date,
	currentLocale: SupportedLocale,
	options: Intl.DateTimeFormatOptions = { day: '2-digit', month: 'long', year: 'numeric' }
): string {
	try {
		const date = typeof dateInput === 'string' ? new Date(dateInput) : dateInput;
		if (isNaN(date.getTime())) return String(dateInput);
		return date.toLocaleDateString(currentLocale === 'en' ? 'en-US' : 'pt-BR', options);
	} catch {
		return String(dateInput);
	}
}

export function formatDayOfWeek(
	dateInput: string | Date,
	currentLocale: SupportedLocale
): string {
	try {
		const date = typeof dateInput === 'string' ? new Date(dateInput) : dateInput;
		if (isNaN(date.getTime())) return '';
		return date.toLocaleDateString(currentLocale === 'en' ? 'en-US' : 'pt-BR', { weekday: 'long' });
	} catch {
		return '';
	}
}

export function formatTime(
	dateInput: string | Date,
	currentLocale: SupportedLocale,
	includeSeconds: boolean = true
): string {
	try {
		const date = typeof dateInput === 'string' ? new Date(dateInput) : dateInput;
		if (isNaN(date.getTime())) return String(dateInput);
		return date.toLocaleTimeString(currentLocale === 'en' ? 'en-US' : 'pt-BR', {
			hour: '2-digit',
			minute: '2-digit',
			second: includeSeconds ? '2-digit' : undefined,
			hour12: currentLocale === 'en'
		});
	} catch {
		return String(dateInput);
	}
}

/**
 * Translate activity types dynamically
 */
export function translateActivityType(type: string | undefined, currentLocale: SupportedLocale): string {
	if (!type) return currentLocale === 'en' ? 'Other' : 'Outro';
	const lower = type.toLowerCase();
	const dict = dictionaries[currentLocale].activityTypes;

	if (lower.includes('muscula') || lower.includes('strength') || lower.includes('hipertrofia') || lower.includes('força')) {
		return dict.strengthTraining;
	}
	if (lower.includes('corrida') || lower.includes('run')) {
		return dict.running;
	}
	if (lower.includes('ciclismo') || lower.includes('cycl') || lower.includes('bike')) {
		return dict.cycling;
	}
	if (lower.includes('caminhada') || lower.includes('walk')) {
		return dict.walking;
	}
	if (lower.includes('esteira') || lower.includes('treadmill')) {
		return dict.treadmill;
	}
	return type;
}

/**
 * Translate status dynamically
 */
export function translateStatus(status: string | undefined, currentLocale: SupportedLocale): string {
	if (!status) return dictionaries[currentLocale].status.pending;
	const s = status.toLowerCase();
	const dict = dictionaries[currentLocale].status;

	if (s === 'valid') return dict.valid;
	if (s === 'suspect') return dict.suspect;
	if (s === 'rejected') return dict.rejected;
	return dict.pending;
}
