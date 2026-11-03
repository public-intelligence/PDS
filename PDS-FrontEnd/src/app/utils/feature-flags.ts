import { environment } from '../../environments/environment';

export type FeatureFlag = keyof typeof environment.features;

export function isFeatureEnabled(flag: FeatureFlag): boolean {
  return environment.features[flag];
}
