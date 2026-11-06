/** @type {import('tailwindcss').Config} */
export default {
	content: ['./src/**/*.{html,js,svelte,ts}'],
	theme: {
		extend: {
			fontFamily: {
				sans: ['"Plus Jakarta Sans"', 'sans-serif'],
				mono: ['"JetBrains Mono"', 'monospace']
			},
			colors: {
				nubank: {
					50: '#faf5ff',
					100: '#f3e8ff',
					200: '#e9d5ff',
					300: '#d8b4fe',
					400: '#c084fc',
					500: '#a855f7',
					600: '#820ad1', // Nubank Official Signature Purple
					700: '#6d08b3',
					800: '#53058a',
					900: '#3c0363',
					950: '#1b012e',
					glow: '#9d4edd',
					dark: '#0e031c'
				},
				cyber: {
					cyan: '#00f0ff',
					pink: '#ff007a',
					emerald: '#00ffa3',
					amber: '#ffb800',
					violet: '#9d4edd',
					dark: '#080214',
					card: 'rgba(16, 7, 34, 0.75)',
					border: 'rgba(168, 85, 247, 0.25)'
				}
			},
			boxShadow: {
				'nubank-glow': '0 0 25px -5px rgba(130, 10, 209, 0.45)',
				'cyber-cyan': '0 0 25px -5px rgba(0, 240, 255, 0.4)',
				'cyber-pink': '0 0 25px -5px rgba(255, 0, 122, 0.4)',
				'cyber-emerald': '0 0 25px -5px rgba(0, 255, 163, 0.4)'
			}
		}
	},
	plugins: []
};
