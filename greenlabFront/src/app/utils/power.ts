/** Rated loads used for dashboard/lab power estimates. */
export const LIGHT_WATTS = 20;
export const FAN_WATTS = 70;
export const LAB_MAX_WATTS = LIGHT_WATTS + FAN_WATTS;

export function labWatts(lightOn: boolean, fanOn: boolean): number {
  return (lightOn ? LIGHT_WATTS : 0) + (fanOn ? FAN_WATTS : 0);
}

export function labSavedWatts(lightOn: boolean, fanOn: boolean): number {
  return (lightOn ? 0 : LIGHT_WATTS) + (fanOn ? 0 : FAN_WATTS);
}

/** kWh from ON runtime: light hours × 0.02 + fan hours × 0.07 */
export function energyFromRuntime(lightSeconds: number, fanSeconds: number): number {
  const lightH = Math.max(0, lightSeconds) / 3600;
  const fanH = Math.max(0, fanSeconds) / 3600;
  return Math.round((lightH * (LIGHT_WATTS / 1000) + fanH * (FAN_WATTS / 1000)) * 10000) / 10000;
}

export function formatRuntime(seconds: number): string {
  const s = Math.max(0, Math.floor(seconds));
  const h = Math.floor(s / 3600);
  const m = Math.floor((s % 3600) / 60);
  if (h > 0) return `${h}h ${m}m`;
  if (m > 0) return `${m}m`;
  return `${s}s`;
}

/** Convert instantaneous watts to kWh if held for `hours`. */
export function wattsToKwh(watts: number, hours: number): number {
  return Math.round(((watts * hours) / 1000) * 100) / 100;
}
