export interface TelemetryPoint {
	latitude: number;
	longitude: number;
	elevationMeters?: number | null;
	timestampUtc: string;
	instantSpeedKmh: number;
	heartRateBpm: number;
}

export interface ValidationIssue {
	ruleName: string;
	severity: 'Info' | 'Warning' | 'Critical';
	message: string;
	details?: string | null;
}

export interface ActivitySummary {
	id: string;
	title: string;
	activityType: string;
	status: 'Pending' | 'Valid' | 'Suspect' | 'Rejected';
	reportedDistanceKm: number;
	validatedDistanceKm: number;
	calculatedGpsDistanceKm: number;
	discrepancyPercentage: number;
	formattedDuration: string;
	durationSeconds: number;
	startTime: string;
	endTime: string;
	formattedDate: string;
	formattedTime: string;
	dayOfWeek: string;
	formattedPace: string;
	averageSpeedKmh: number;
	caloriesBurned: number;
	averageHeartRateBpm: number;
	maxHeartRateBpm: number;
	deviceName: string;
	validationSummary: string;
	issues: ValidationIssue[];
	routePoints: TelemetryPoint[];
}

export interface DashboardMetrics {
	generatedAt: string;
	currentFormattedDate: string;
	currentFormattedTime: string;
	currentDayOfWeek: string;
	totalReportedKm: number;
	totalValidatedKm: number;
	totalCaloriesBurned: number;
	totalActivitiesCount: number;
	validActivitiesCount: number;
	suspectActivitiesCount: number;
	rejectedActivitiesCount: number;
	overallAvgHeartRate: number;
	recentActivities: ActivitySummary[];
}

export interface SimulationRequest {
	scenario: string;
	deviceModel: string;
}
